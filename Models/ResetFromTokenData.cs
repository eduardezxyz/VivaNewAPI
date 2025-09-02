using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NewVivaApi.Models
{
    public class ResetFromTokenData
    {
        public string Token { get; set; }
        //public string OldPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}