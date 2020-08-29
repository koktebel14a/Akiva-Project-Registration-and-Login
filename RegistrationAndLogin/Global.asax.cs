using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using PlayFab;
using PlayFab.ClientModels;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;

namespace RegistrationAndLogin
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            InitPlayFab();
        }

        private void InitPlayFab()
        {
            string playFabTitleId = ConfigurationManager.AppSettings["PlayFabTitleId"];
            PlayFabSettings.staticSettings.TitleId = playFabTitleId;

            string devSecretKey = ConfigurationManager.AppSettings["DevSecretKey"];
            PlayFabSettings.staticSettings.DeveloperSecretKey = devSecretKey;

            var request = new LoginWithCustomIDRequest { CustomId = "DCC1CC93ABB57DE4", CreateAccount = true };
            var loginTask = PlayFabClientAPI.LoginWithCustomIDAsync(request);
            OnLoginComplete(loginTask);
        }

        private static void OnLoginComplete(Task<PlayFabResult<LoginResult>> taskResult)
        {
            var apiError = taskResult.Result.Error;
            var apiResult = taskResult.Result.Result;

            if (apiError != null)
            {
                Console.ForegroundColor = ConsoleColor.Red; 
                Console.WriteLine("PlayFab LoginWithCustomIDAsync failed.  :(");
                Console.WriteLine(PlayFabUtil.GenerateErrorReport(apiError));
                Console.ForegroundColor = ConsoleColor.Gray; // Reset to normal
            }
        }
    }
}
