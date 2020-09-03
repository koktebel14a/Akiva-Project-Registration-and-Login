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
        [Required(AllowEmptyStrings = false, ErrorMessage = "First name required")]
        public string ChildName { get; set; }

        [Display(Name = "Spectrum diagnosis")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Last name required")]
        public string SpectrumDiagnosys { get; set; }

        [Display(Name = "Email")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Email required")]
        [DataType(DataType.EmailAddress)]
        public string EmailID { get; set; }

        [Display(Name = "Gender")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Gender required")]
        public string Gender { get; set; }

        [Display(Name = "Date of birth")]
        [DataType(DataType.Date)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTime DateOfBirth { get; set; }

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