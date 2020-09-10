using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RegistrationAndLogin.Models.Extended
{
    public class PlayFabStatus
    {
        public string PlayFabId { get; set; }
        public Dictionary<string, string[]> ErrorDetails { get; set; }
    }
}