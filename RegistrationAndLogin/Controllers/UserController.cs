using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RegistrationAndLogin.Models;
using System.Net.Mail;
using System.Net;
using System.Web.Security;
using System.Configuration;
using PlayFab.ClientModels;
using PlayFab;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Linq.Expressions;
using System.Text;
using RegistrationAndLogin.Models.Extended;

namespace RegistrationAndLogin.Controllers
{
    public class UserController : Controller
    {

        public PlayFabManager playFabManager = new PlayFabManager();

        // GET: User

        //Registration Action
        [HttpGet]
        public ActionResult Registration()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registration([Bind(Exclude = "IsEmailVerified,ActivationCode")] User user)
        { 
            bool Status = false;
            string message = "";
            //
            // Model Validation 
            if (ModelState.IsValid)
            {

                #region //Email is already Exist 

                if (emailExist(user.EmailID))
                {
                    ModelState.AddModelError("Email Exists", "Sorry, this email already exists");
                    return View(user);
                }
                #endregion

                #region Generate Activation Code 
                user.ActivationCode = Guid.NewGuid();
                #endregion

                #region  Password Hashing 
                user.Password = Crypto.Hash(user.Password);
                user.ConfirmPassword = Crypto.Hash(user.ConfirmPassword); //
                #endregion
                user.IsEmailVerified = false;

                #region Save to PlayFab
                PlayFabSettings.staticSettings.TitleId = "AF64B";
                var registerRequest = new RegisterPlayFabUserRequest()
                {
                    TitleId = ConfigurationManager.AppSettings["PlayFabTitleId"],
                    RequireBothUsernameAndEmail = true,
                    Username = user.FirstName,
                    Password = user.Password,
                    Email = user.EmailID
                };
                Task<PlayFabResult<RegisterPlayFabUserResult>> taskResult = PlayFabClientAPI.RegisterPlayFabUserAsync(registerRequest);
                string playFabId = OnRegisterComplete(taskResult);
      
                if (playFabId != null)
                {
                    int dbIndex = InsertPlayerInDB(user);

                    var request = new UpdateUserDataRequest()
                    {
                        Data = new Dictionary<string, string>() {
                            {"password", user.ConfirmPassword},
                            {"activationCode", user.ActivationCode.ToString()},
                            {"emailVerified", "0"},
                            {"dbIndex", dbIndex.ToString()},
                        }
                    };

                    var updateResult = PlayFabClientAPI.UpdateUserDataAsync(request);
                    OnUpdatePlayerDataComplete(updateResult);
                }

                #endregion

//                SendVerificationLinkEmail(user.EmailID, user.ActivationCode.ToString());
                SendVerificationLinkEmail(user.EmailID, playFabId);
                

                message = "Registration successfully done. Account activation link " +
                    " has been sent to your email: " + user.EmailID;
                Status = true;
            }
            else
            {
                message = "Invalid Request";
            }

            ViewBag.Message = message;
            ViewBag.Status = Status;
            return View(user);
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

        private static string OnRegisterComplete(Task<PlayFabResult<RegisterPlayFabUserResult>> taskResult)
        {
            var apiError = taskResult.Result.Error;
            var apiResult = taskResult.Result.Result;

            if (apiError != null)
            {
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

        //Verify Account  

        [HttpGet]
        public ActionResult VerifyAccount(string playerID)
        {
            bool Status = false;

            var accountInfoRequest = new GetAccountInfoRequest()
            {
                PlayFabId = playerID
            };
            var taskResult = PlayFabClientAPI.GetAccountInfoAsync(accountInfoRequest);
            Status = OnGetAccountInfoRequestComplete(taskResult);

            //update emailVerified flag
            var request = new UpdateUserDataRequest()
            {
                Data = new Dictionary<string, string>() {
                            {"emailVerified", "1"},
                        }
            };

            var updateResult = PlayFabClientAPI.UpdateUserDataAsync(request);
            Status = OnUpdatePlayerDataComplete(updateResult);


            // Playfab here
            //using (MyDatabaseEntities dc = new MyDatabaseEntities())
            //{
            //    dc.Configuration.ValidateOnSaveEnabled = false; // This line I have added here to avoid 
            //                                                    // Confirm password does not match issue on save changes
            //    var v = dc.Users.Where(a => a.ActivationCode == new Guid(id)).FirstOrDefault();
            //    if (v != null)
            //    {
            //        v.IsEmailVerified = true;
            //        dc.SaveChanges();
            //        Status = true;
            //    }
            //    else
            //    {
            //        ViewBag.Message = "Invalid Request";
            //    }
            //}
            ViewBag.Status = Status;
            return View();
        }

        //Login 
        [HttpGet]
        public ActionResult Login()
        {
            playFabManager.Init();
            return View();
        }

        [HttpGet]
        public ActionResult DeleteTestUser()
        {
            string testUserEmail = ConfigurationManager.AppSettings["PlayFabTestUserEmail"];
            string playFabID = FindPlayerByEmail(testUserEmail);

            if (playFabID != null)
            {
                PlayFab.ServerModels.DeletePlayerRequest request = new PlayFab.ServerModels.DeletePlayerRequest()
                {
                    PlayFabId = playFabID
                };

                var taskResult = PlayFab.PlayFabServerAPI.DeletePlayerAsync(request);
                OnDeletePlayerRequestComplete(taskResult);
            }

            return RedirectToAction("Login", "User");
        }

        private static bool OnDeletePlayerRequestComplete(Task<PlayFabResult<PlayFab.ServerModels.DeletePlayerResult>> taskResult)
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

        public static string FindPlayerByEmail(string email)
        {
           var accountInfoRequest = new GetAccountInfoRequest()
           {
                    Email = email
           };
           var taskResult = PlayFabClientAPI.GetAccountInfoAsync(accountInfoRequest);
           return OnGetPlayFabIDRequestComplete(taskResult);
        }

        private static string OnGetPlayFabIDRequestComplete(Task<PlayFabResult<GetAccountInfoResult>> taskResult)
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

        //Login POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(UserLogin login, string ReturnUrl = "")
        {
            string message = "";
            if (emailExist(login.EmailID))
            {
                int timeout = login.RememberMe ? 525600 : 20; // 525600 min = 1 year
                var ticket = new FormsAuthenticationTicket(login.EmailID, login.RememberMe, timeout);
                string encrypted = FormsAuthentication.Encrypt(ticket);
                var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encrypted);
                cookie.Expires = DateTime.Now.AddMinutes(timeout);
                cookie.HttpOnly = true;
                Response.Cookies.Add(cookie);


                if (Url.IsLocalUrl(ReturnUrl))
                {
                    return Redirect(ReturnUrl);
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
           else
           {
               message = "Invalid credentials provided";
           }
            ViewBag.Message = message;
            return View();
        }

        //Logout
        [Authorize]
        [HttpPost]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "User");
        }

        [NonAction]
        public bool emailExist(string emailID)
        {
            var accountInfoRequest = new GetAccountInfoRequest()
            {
                Email = emailID
            };
            var taskResult = PlayFabClientAPI.GetAccountInfoAsync(accountInfoRequest);
            return OnGetAccountInfoRequestComplete(taskResult);
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

        [NonAction]
        public void SendVerificationLinkEmail(string emailID, string activationCode)
        {
            var verifyUrl = "/User/VerifyAccount/" + activationCode;
            var link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, verifyUrl);

//            string adminEmail = ConfigurationManager.AppSettings["AdminEmail"];
            string adminEmail = "ashapiro14a@gmail.com";
            var fromEmail = new MailAddress(adminEmail, "Akiva Project");
//            var toEmail = new MailAddress(emailID);
            var toEmail = new MailAddress("ashapiro14a@gmail.com");

            //string adminEmailPassword = ConfigurationManager.AppSettings["AdminEmailPassword"];
            string adminEmailPassword = "take5take5";
            var fromEmailPassword = adminEmailPassword; 
            string subject = "Your Akiva Project account was successfully created!";

            string body = "<br/><br/>We are excited to tell you that your Akiva Project account was" +
                " successfully created. Please click on the below link to verify your account" +
                " <br/><br/><a href='" + link + "'>" + link + "</a> ";

            string snmpHost = ConfigurationManager.AppSettings["SNMPHost"];
            var smtp = new SmtpClient
            {            
                Host = snmpHost,
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromEmail.Address, fromEmailPassword)

            };

            using (var message = new MailMessage(fromEmail, toEmail)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
            smtp.Send(message);



        }

        private int InsertPlayerInDB(User user)
        {
            int modified = 0;
            SqlConnection connection = null;
            var today = DateTime.Today;
            // Calculate user age
            var age = today.Year - user.DateOfBirth.Year;

            // Go back to the year the person was born in case of a leap year
            if (user.DateOfBirth.Date > today.AddYears(-age))
            {
                age--;
            }

            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["AkivaWorldDbConnection"].ConnectionString;
                using (connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    String query = "INSERT INTO Users (ChildName,ChildGenderID, ChildAge, CreatedAt) VALUES (@ChildName,@ChildGenderID,@ChildAge, @CreatedAt)";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ChildName", user.FirstName);
                    command.Parameters.AddWithValue("@ChildGenderID", (int)user.StudentGender);
                    command.Parameters.AddWithValue("@ChildAge", age.ToString());
                    command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                    //getIdentity
                    command.ExecuteNonQuery();
                    command.CommandText = "Select @@Identity";
                    modified = Convert.ToInt32(command.ExecuteScalar());
                }

            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }
            connection.Close();
            return modified;
        }

    }

}