using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using NewVivaApi.Models.Exceptions;
using NewVivaApi.Services;
using NewVivaApi.Data; // your DbContext namespace

namespace NewVivaApi.Models
{
    public class RegisterSystemUserModel
    {
        // Input properties
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public int CompanyID { get; set; }
        public bool IsAdminTF { get; set; }
        public bool IsGCTF { get; set; }
        public bool IsSCTF { get; set; }
        public bool GcApproveTF { get; set; }

        // Private state
        private string? _userId;

        // Dependencies
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;
        // private readonly IHttpContextAccessor _httpContextAccessor;
        // private readonly EmailService _emailService;

        public RegisterSystemUserModel(
            UserManager<ApplicationUser> userManager,
            AppDbContext context
            // IHttpContextAccessor httpContextAccessor,
            // EmailService emailService
            )
        {
            _userManager = userManager;
            _context = context;
            // _httpContextAccessor = httpContextAccessor;
            // _emailService = emailService;
        }

        public string? GetUserId() => _userId;

        public async Task RegisterAsync(string creatorUserName = "")
        {
            await CreateAspNetUserAsync();
            await CreateUserProfileAsync();
            await CreateVivaRoleAsync();
            // await SendEmailNotificationAsync(creatorUserName);
        }

        private async Task CreateAspNetUserAsync()
        {
            var user = new ApplicationUser
            {
                UserName = UserName,
                Email = UserName,
                PhoneNumber = PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, Password);

            if (!result.Succeeded)
                throw new UserCreationException(UserName, result);

            _userId = user.Id;
        }

        private async Task CreateUserProfileAsync()
        {
            var currentUser = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "system";

            var up = new UserProfile
            {
                FirstName = FirstName,
                LastName = LastName,
                PhoneNumber = PhoneNumber,
                UserName = UserName,
                UserID = _userId!,
                CreateDT = DateTime.UtcNow,
                LastUpdateDT = DateTime.UtcNow,
                LastUpdateUser = currentUser
            };

            _context.UserProfiles.Add(up);
            await _context.SaveChangesAsync();
        }

        private async Task CreateVivaRoleAsync()
        {
            var currentUser = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "system";

            if (IsAdminTF)
            {
                var au = new AdminUser
                {
                    UserID = _userId!,
                    CreateDT = DateTime.UtcNow,
                    LastUpdateDT = DateTime.UtcNow,
                    LastUpdateUser = currentUser
                };
                _context.AdminUsers.Add(au);
            }
            else if (IsGCTF)
            {
                var gcu = new GeneralContractorUser
                {
                    UserID = _userId!,
                    GeneralContractorID = CompanyID,
                    CanApproveTF = GcApproveTF,
                    CreateDT = DateTime.UtcNow,
                    LastUpdateDT = DateTime.UtcNow,
                    LastUpdateUser = currentUser
                };
                _context.GeneralContractorUsers.Add(gcu);
            }
            else if (IsSCTF)
            {
                var scu = new SubcontractorUser
                {
                    UserID = _userId!,
                    SubcontractorID = CompanyID,
                    CreateDT = DateTime.UtcNow,
                    LastUpdateDT = DateTime.UtcNow,
                    LastUpdateUser = currentUser
                };
                _context.SubcontractorUsers.Add(scu);
            }
            else
            {
                throw new Exception("User couldn't be assigned a role. No Match was found.");
            }

            await _context.SaveChangesAsync();
        }

        // private async Task SendEmailNotificationAsync(string creatorUserName)
        // {
        //     if (IsAdminTF)
        //     {
        //         await _emailService.SendNewAdminEmailAsync(_userId!, Password);
        //         await _emailService.SendAdminEmailNewAdminAsync(_userId!);
        //     }
        //     else if (IsGCTF)
        //     {
        //         await _emailService.SendNewGeneralContractorEmailAsync(_userId!, CompanyID, Password);
        //         await _emailService.SendAdminEmailNewGeneralContractorUserAsync(_userId!, CompanyID, creatorUserName);
        //     }
        //     else if (IsSCTF)
        //     {
        //         await _emailService.SendNewSubcontractorEmailAsync(_userId!, CompanyID, Password);
        //         await _emailService.SendAdminEmailNewSubcontractorUserAsync(_userId!, CompanyID, creatorUserName);
        //     }
        // }
    }
}
