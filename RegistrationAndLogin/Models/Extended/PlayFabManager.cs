﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PlayFab;
using PlayFab.ClientModels;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;

namespace RegistrationAndLogin.Models.Extended
{
    public sealed class PlayFabManager
    {
        static private string TitleId { get; set; }
        static private string DeveloperSecretKey { get; set; }
        static private string CustomId { get; set; }
        private static int counter = 0;

        private static PlayFabManager instance = null;
        public static PlayFabManager GetInstance
        {
            get
            {
                if (instance == null)
                    instance = new PlayFabManager();
                return instance;
            }
        }
        private PlayFabManager()
        {
            counter++;
            Console.WriteLine("Counter Value " + counter.ToString());
        }

        public void Init(string titleId, string developerSecretKey, string customId)
        {
            TitleId = titleId;
            DeveloperSecretKey = developerSecretKey;
            CustomId = customId;

            PlayFabSettings.staticSettings.TitleId = TitleId;
            PlayFabSettings.staticSettings.DeveloperSecretKey = DeveloperSecretKey;

            var request = new LoginWithCustomIDRequest { CustomId = customId, CreateAccount = true };
            var loginTask = PlayFabClientAPI.LoginWithCustomIDAsync(request);
            OnLoginComplete(loginTask);
        }

        public PlayFabStatus RegisterUser(User user)
        {
            var registerRequest = new RegisterPlayFabUserRequest()
            {
                TitleId = TitleId,
                RequireBothUsernameAndEmail = false,
                Password = user.Password,
                Email = user.EmailID
            };
            Task<PlayFabResult<RegisterPlayFabUserResult>> taskResult = PlayFabClientAPI.RegisterPlayFabUserAsync(registerRequest);
            return OnRegisterComplete(taskResult);
        }

        public bool UpdateUserData(Dictionary<string, string> data)
        {

            var request = new UpdateUserDataRequest()
            {
                Data = data
            };

            var updateResult = PlayFabClientAPI.UpdateUserDataAsync(request);
            return OnUpdatePlayerDataComplete(updateResult);
        }
        private bool OnUpdatePlayerDataComplete(Task<PlayFabResult<UpdateUserDataResult>> taskResult)
        {
            var apiError = taskResult.Result.Error;
            var apiResult = taskResult.Result.Result;

            if (apiError != null)
            {
                if (apiError.ErrorMessage == "User not found")
                {
                    return false;
                }
                // else - we have a serious problem -something went wrong
            }
            else if (apiResult != null)
            {
                return true;
            }

            return true;
        }

        private PlayFabStatus OnRegisterComplete(Task<PlayFabResult<RegisterPlayFabUserResult>> taskResult)
        {
            PlayFabStatus status = new PlayFabStatus();

            var apiError = taskResult.Result.Error;
            var apiResult = taskResult.Result.Result;

            if (apiError != null)
            {
                status.PlayFabId = null;
                status.ErrorDetails = apiError.ErrorMessage;

                // add logging
                Console.ForegroundColor = ConsoleColor.Red; // Make the error more visible
                Console.WriteLine("Something went wrong ...  :(");
                Console.WriteLine("Here's some debug information:");
                Console.WriteLine(PlayFabUtil.GenerateErrorReport(apiError));
                Console.ForegroundColor = ConsoleColor.Gray; // Reset to normal
            }
            else if (apiResult != null)
            {
                status.PlayFabId = taskResult.Result.Result.PlayFabId;
                status.ErrorDetails = null;

                //                return taskResult.Result.Result.PlayFabId;
            }
            return status;
        }

        private string OnLoginComplete(Task<PlayFabResult<LoginResult>> taskResult)
        {
            var apiError = taskResult.Result.Error;
            var apiResult = taskResult.Result.Result;

            if (apiError != null)
            {
                // Add logging
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("PlayFab LoginWithCustomIDAsync failed.  :(");
                Console.WriteLine(PlayFabUtil.GenerateErrorReport(apiError));
                Console.ForegroundColor = ConsoleColor.Gray; // Reset to normal  
                return null;
            }
            return apiResult.PlayFabId;
        }

        public string FindUserByEmail(string email)
        {
            var accountInfoRequest = new GetAccountInfoRequest()
            {
                Email = email
            };
            var taskResult = PlayFabClientAPI.GetAccountInfoAsync(accountInfoRequest);
            return OnGetPlayFabIDRequestComplete(taskResult);
        }

        private string OnGetPlayFabIDRequestComplete(Task<PlayFabResult<GetAccountInfoResult>> taskResult)
        {
            var apiError = taskResult.Result.Error;
            var apiResult = taskResult.Result.Result;

            if (apiError != null)
            {
                if (apiError.ErrorMessage == "User not found")
                {
                    return null;
                }
                // else - we have a serious problem -something went wrong
            }
            else if (apiResult != null)
            {
                return apiResult.AccountInfo.PlayFabId;
            }

            return null;
        }

        public Dictionary<string, UserDataRecord> GetUserDataByPlayFabId(string playFabId)
        {
            GetUserDataRequest request = new GetUserDataRequest()
            {
                PlayFabId = playFabId,
                Keys = null
            };


            var result =  PlayFabClientAPI.GetUserDataAsync(request);
            return OnGetUserDataComplete(result);
        }

        private static Dictionary<string, UserDataRecord> OnGetUserDataComplete(Task<PlayFabResult<GetUserDataResult>> taskResult)
        {
            var apiError = taskResult.Result.Error;
            var apiResult = taskResult.Result.Result;
            if (apiResult != null)
            {
                return apiResult.Data;
            }

            return null;
        }
        public bool GetAccountInfo(User user)
        {
            var accountInfoRequest = new GetAccountInfoRequest();

            accountInfoRequest.Email = user.EmailID;

            var taskResult = PlayFabClientAPI.GetAccountInfoAsync(accountInfoRequest);
            return OnGetAccountInfoRequestComplete(taskResult);
        }

        public string GetAccountEmailVerifiedStatus(string playFabId)
        {
            string retStatus = null;

            Dictionary<string, PlayFab.ClientModels.UserDataRecord> dictionary = GetUserDataByPlayFabId(playFabId);

            if (dictionary != null)
            {
                if (dictionary.ContainsKey("emailVerified"))
                {
                    retStatus = dictionary["emailVerified"].Value;
                }
            }

            return retStatus;
        }

        private static bool OnGetAccountInfoRequestComplete(Task<PlayFabResult<GetAccountInfoResult>> taskResult)
        {
            var apiError = taskResult.Result.Error;
            var apiResult = taskResult.Result.Result;

            bool retVal = true;

            if (apiError != null)
            {
                if (apiError.ErrorMessage == "User not found")
                {
                    return false;
                }
                // else - we have a serious problem -something went wrong
            }
            else if (apiResult != null)
            {
                return true;
            }

            return retVal;
        }

        public string LoginWithEmail(User user)
        {
            LoginWithEmailAddressRequest loginRequest = new LoginWithEmailAddressRequest()
            {
                TitleId = TitleId,
                Email = user.EmailID,
                Password = user.Password
            };
            var loginTask = PlayFabClientAPI.LoginWithEmailAddressAsync(loginRequest);
            return OnLoginComplete(loginTask);
        }

        public bool LogOut(User user)
        {
            PlayFabClientAPI.ForgetAllCredentials();
            return true;
        }

    }
}