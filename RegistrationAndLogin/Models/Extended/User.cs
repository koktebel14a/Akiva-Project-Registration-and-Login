using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
//namespace RegistrationAndLogin.Models.Extended
namespace RegistrationAndLogin.Models
{
    //[MetadataType(typeof(UserMetadata))]
    //public partial class User
    //{
    //    public string ConfirmPassword { get; set; }
    //}

    //    public class UserMetadata

    public enum Gender
    {
        Male=1,
        Female
    }
    public class User
    {

        [Display(Name = "Child Name")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Child name is required")]
        public string ChildName { get; set; }

        [Display(Name = "Age")]
        [Range(1, 18, ErrorMessage = "Child age should be a number between 1 and 18")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Age is required")]
        public string Age { get; set; }


        [Display(Name = "Spectrum diagnosis")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Spectrum diagnosis is required")]
        public string SpectrumDiagnosis { get; set; }

        [Display(Name = "Username")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Username required")]
        public string UserName { get; set; }
        [Display(Name = "Email")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Email is required")]
        [RegularExpression(@"^[a-z0-9_\\+-]+(\\.[a-z0-9_\\+-]+)*@[a-z0-9-]+(\\.[a-z0-9]+)*\\.([a-z]{2,4})$", ErrorMessage = "Invalid email format")]
        [DataType(DataType.EmailAddress)]
        public string EmailID { get; set; }

        [Display(Name = "Gender")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Gender is required")]
        public string Gender { get; set; }

        //[Display(Name = "Date of birth")]
        //[DataType(DataType.Date)]
        //[DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:MM/dd/yyyy}")]
        //public DateTime DateOfBirth { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Minimum 6 characters required")]
        public string Password { get; set; }

        [Display(Name = "Confirm Password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Confirm password and password do not match")]
        public string ConfirmPassword { get; set; }

        public Guid ActivationCode { get; set; }

        public bool IsEmailVerified { get; set; }

    }
}