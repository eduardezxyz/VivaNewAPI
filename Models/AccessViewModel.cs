using System;
using System.Collections.Generic;

namespace NewVivaApi.Models
{
    // Models returned by AccountController actions.

    public class ExternalLoginViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? State { get; set; }
    }

    public class ManageInfoViewModel
    {
        public string LocalLoginProvider { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public IEnumerable<UserLoginInfoViewModel> Logins { get; set; } = new List<UserLoginInfoViewModel>();
        public IEnumerable<ExternalLoginViewModel> ExternalLoginProviders { get; set; } = new List<ExternalLoginViewModel>();
    }

    public class UserInfoViewModel
    {
        public string Email { get; set; } = string.Empty;
        public bool HasRegistered { get; set; }
        public string? LoginProvider { get; set; }
    }

    public class UserLoginInfoViewModel
    {
        public string LoginProvider { get; set; } = string.Empty;
        public string ProviderKey { get; set; } = string.Empty;
    }
}
