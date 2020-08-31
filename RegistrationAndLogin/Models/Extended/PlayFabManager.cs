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