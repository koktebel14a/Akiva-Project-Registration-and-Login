using System;
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
                RequireBothUsernameAndEmail = true,
                Username = user.UserName,
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
                status.ErrorDetails = taskResult.Result.Error.ErrorDetails;

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

        private bool OnLoginComplete(Task<PlayFabResult<LoginResult>> taskResult)
        {
            var apiError = taskResult.Result.Error;
            var apiResult = taskResult.Result.Result;
            bool retVal = true;

            if (apiError != null)
            {
                // Add logging
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("PlayFab LoginWithCustomIDAsync failed.  :(");
                Console.WriteLine(PlayFabUtil.GenerateErrorReport(apiError));
                Console.ForegroundColor = ConsoleColor.Gray; // Reset to normal  
                retVal = false;
            }
            return retVal;
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

            if (user.EmailID.Contains('@'))
            {
                accountInfoRequest.Email = user.EmailID;
                accountInfoRequest.Username = user.UserName;
            }
            else
            {
                accountInfoRequest.Username = user.EmailID;
            }

            var taskResult = PlayFabClientAPI.GetAccountInfoAsync(accountInfoRequest);
            return OnGetAccountInfoRequestComplete(taskResult);
        }

        public bool GetAccountEmailVerifiedStatus(User user)
        {
            // we need to login user to set user data
            if (!PlayFabManager.GetInstance.LoginWithEmail(user))
            {
                return false;
            }


            var accountInfoRequest = new GetAccountInfoRequest();

            if (user.EmailID.Contains('@'))
            {
                accountInfoRequest.Email = user.EmailID;
                accountInfoRequest.Username = user.UserName;

            }
            else
            {
                accountInfoRequest.Username = user.EmailID;
            }


            return true;
            //string verified = "";
            //Dictionary<string, string> data = new Dictionary<string, string>() {
            //                                                    {"emailVerified", "0"},
            //                                                };
            //var taskResult = PlayFabClientAPI.GetAccountInfoAsync(accountInfoRequest, verified, data);
            //return OnGetAccountInfoRequestComplete(taskResult);
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

        public bool LoginWithEmail(User user)
        {
            LoginWithEmailAddressRequest loginRequest = new LoginWithEmailAddressRequest()
            {
                TitleId = TitleId,
                Email = user.EmailID,
                Password = user.Password
            };
            var loginTask = PlayFabClientAPI.LoginWithEmailAddressAsync(loginRequest);
            bool loginResult = OnLoginComplete(loginTask);
            return loginResult;
        }

        public bool LogOut(User user)
        {
            return true;
        }

    }
}