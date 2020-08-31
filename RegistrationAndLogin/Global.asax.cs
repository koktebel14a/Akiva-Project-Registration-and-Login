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
using RegistrationAndLogin.Models.Extended;

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
            string devSecretKey = ConfigurationManager.AppSettings["DevSecretKey"];

            PlayFabManager playFabManager = PlayFabManager.GetInstance;
            playFabManager.Init(playFabTitleId, devSecretKey, "DCC1CC93ABB57DE4");
        }
    }
}
