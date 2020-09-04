using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using RegistrationAndLogin.Models;
using System.Net.Mail;
using System.Net;
using System.Web.Security;
using System.Configuration;
using System.Data.SqlClient;
using RegistrationAndLogin.Models.Extended;

namespace RegistrationAndLogin.Controllers
{
    public class UserController : Controller
    {
         PlayFabManager playFabManager = PlayFabManager.GetInstance;


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
                    ViewBag.Message = "Sorry, this email already exists";
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

                string playFabId = playFabManager.RegisterUser(user);
                if (playFabId == null)
                {
                    ViewBag.Message = "There was a problem creating your account, please try again later";
                    ViewBag.Status = false;
                    return View(user);
                }

                int dbIndex = InsertPlayerInDB(user);

                Dictionary<string, string> data = new Dictionary<string, string>() {
                                                                {"activationCode", user.ActivationCode.ToString()},
                                                                {"emailVerified", "0"},
                                                                {"dbIndex", dbIndex.ToString()},
                                                            };
                PlayFabManager.GetInstance.UpdateUserData(data);

                #endregion

//                SendVerificationLinkEmail(user.EmailID, user.ActivationCode.ToString());
                SendVerificationLinkEmail(user.EmailID, playFabId);
                

                message = "Registration successfull! Account activation link " +
                    " has been sent to your email: " + user.EmailID;
                Status = true;
            }
            //else
            //{
            //    message = "Invalid Request";
            //}

            ViewBag.Message = message;
            ViewBag.Status = Status;
            return View(user);
        }

        //Verify Account  

        [HttpGet]
        public ActionResult VerifyAccount(string playerID)
        {
            bool Status = false;

            //update emailVerified flag

            Status= PlayFabManager.GetInstance.UpdateUserData(new Dictionary<string, string>() {
                                                    {"emailVerified", "1"},
                                                });
            ViewBag.Status = Status;
            return View();
        }

        //Login 
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }



        public static string FindPlayerByEmail(string email)
        {
           return PlayFabManager.GetInstance.FindUserByEmail(email);
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
        public bool emailExist(string email)
        {
            return PlayFabManager.GetInstance.GetAccountInfo(email);
        }



        [NonAction]
        public void SendVerificationLinkEmail(string emailID, string activationCode)
        {
            var verifyUrl = "/User/VerifyAccount/" + activationCode;
            var link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, verifyUrl);

            var fromEmail = new MailAddress(ConfigurationManager.AppSettings["AdminEmail"], "Akiva Project");
            var toEmail = new MailAddress("ashapiro14a@gmail.com");
            string subject = "Your Akiva Project account was successfully created!<br>";

            string body =   "<br/><br/>Your Akiva Project account was" +
                            " successfully created. Please click on the below link to verify your account" +
                            " <br/><br/><a href='" + link + "'>" + link + "</a> ";

            string snmpHost = ConfigurationManager.AppSettings["SNMPHost"];
            var smtp = new SmtpClient
            {            
                Host = snmpHost,
                Port = 587,
                EnableSsl = true,
                //                DeliveryMethod = SmtpDeliveryMethod.Network,
                DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromEmail.Address, ConfigurationManager.AppSettings["AdminEmailPassword"])

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

            try
            {
                int genderId = 1;
                if (user.Gender == "Female")
                {
                    genderId = 2;
                }
                string connectionString = ConfigurationManager.ConnectionStrings["AkivaWorldDbConnection"].ConnectionString;
                using (connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    String query = "INSERT INTO Users (ChildName,ChildGenderID, ChildAge, CreatedAt) VALUES (@ChildName,@ChildGenderID,@ChildAge, @CreatedAt)";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ChildName", user.ChildName);
                    command.Parameters.AddWithValue("@ChildGenderID", genderId);
                    command.Parameters.AddWithValue("@ChildAge", user.Age.ToString());
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

        //[HttpGet]
        //public ActionResult DeleteTestUser()
        //{
        //    string testUserEmail = ConfigurationManager.AppSettings["PlayFabTestUserEmail"];
        //    string playFabID = FindPlayerByEmail(testUserEmail);

        //    if (playFabID != null)
        //    {
        //        PlayFab.ServerModels.DeletePlayerRequest request = new PlayFab.ServerModels.DeletePlayerRequest()
        //        {
        //            PlayFabId = playFabID
        //        };

        //        var taskResult = PlayFab.PlayFabServerAPI.DeletePlayerAsync(request);
        //        OnDeletePlayerRequestComplete(taskResult);
        //    }

        //    return RedirectToAction("Login", "User");
        //}

        //private static bool OnDeletePlayerRequestComplete(Task<PlayFabResult<PlayFab.ServerModels.DeletePlayerResult>> taskResult)
        //{
        //    var apiError = taskResult.Result.Error;
        //    var apiResult = taskResult.Result.Result;

        //    bool retVal = true;

        //    if (apiError != null)
        //    {
        //        if (apiError.ErrorMessage == "User not found")
        //        {
        //            return false;
        //        }
        //        // else - we have a serious problem -something went wrong
        //    }
        //    else if (apiResult != null)
        //    {
        //        return true;
        //    }

        //    return retVal;
        //}

    }

}