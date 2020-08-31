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

        public int i;
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

        public void Init (string titleId, string developerSecretKey, string customId)
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

        public string RegisterUser(User user)
        {
            var registerRequest = new RegisterPlayFabUserRequest()
            {
                TitleId = TitleId,
                RequireBothUsernameAndEmail = true,
                Username = user.FirstName,
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
        private static bool OnUpdatePlayerDataComplete(Task<PlayFabResult<UpdateUserDataResult>> taskResult)
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

        private string OnRegisterComplete(Task<PlayFabResult<RegisterPlayFabUserResult>> taskResult)
        {
            var apiError = taskResult.Result.Error;
            var apiResult = taskResult.Result.Result;

            if (apiError != null)
            {
                // add logging
                Console.ForegroundColor = ConsoleColor.Red; // Make the error more visible
                Console.WriteLine("Something went wrong ...  :(");
                Console.WriteLine("Here's some debug information:");
                Console.WriteLine(PlayFabUtil.GenerateErrorReport(apiError));
                Console.ForegroundColor = ConsoleColor.Gray; // Reset to normal
            }
            else if (apiResult != null)
            {
                return taskResult.Result.Result.PlayFabId;
            }
            return null;
        }

        private static bool OnLoginComplete(Task<PlayFabResult<LoginResult>> taskResult)
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



    }

}