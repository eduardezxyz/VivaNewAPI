using System.ComponentModel.DataAnnotations;
using NewVivaApi.Authentication.Models;
using NewVivaApi.Models;
using NewVivaApi.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using NewVivaApi.Authentication;
using NewVivaApi.Services;

namespace NewVivaApi.Models;

public class RegisterSystemUserModel
{
    public string UserName { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }
    public string Password { get; set; }
    public string ConfirmPassword { get; set; }
    public int CompanyID { get; set; }
    public bool isAdminTF { get; set; }
    public bool isGCTF { get; set; }
    public bool isSCTF { get; set; }
    public bool gcApproveTF { get; set; }

    private string _userId { get; set; }
    private readonly AppDbContext _context;
    private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;
    private readonly EmailService _emailService;
    private readonly IHttpContextAccessor _httpContextAccessor;


    // Constructor with dependency injection
    public RegisterSystemUserModel(AppDbContext context,
        UserManager<ApplicationUser> userManager,
        EmailService emailService,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
        _emailService = emailService;
    }

    // public RegisterSystemUserModel(string userName, string firstName, string lastName, string phoneNumber, 
    //     string password, string confirmPassword, int companyId, bool isAdminTF = false, 
    //     bool isGCTF = false, bool isSCTF = false, bool gcApproveTF = false)
    // {
    //     UserName = userName;
    //     FirstName = firstName;
    //     LastName = lastName;
    //     PhoneNumber = phoneNumber;
    //     Password = password;
    //     ConfirmPassword = confirmPassword;
    //     CompanyID = companyId;
    //     this.isAdminTF = isAdminTF;
    //     this.isGCTF = isGCTF;
    //     this.isSCTF = isSCTF;
    //     this.gcApproveTF = gcApproveTF;
    // }

    public string GetUserId()
    {
        return _userId;
    }

    public async Task RegisterAsync(string creatorUserName = "")
    {
        await CreateAspNetUserAsync();
        await CreateUserProfileAsync();
        await CreateVivaRoleAsync();
        await SendEmailNotificationAsync(creatorUserName);
    }

    private async Task CreateUserProfileAsync() //TODO
    {
        // Create UserProfile Record
        var currentUser = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

        var userProfile = new UserProfile
        {
            FirstName = this.FirstName,
            LastName = this.LastName, 
            PhoneNumber = this.PhoneNumber,
            UserName = this.UserName,
            UserId = _userId,
            CreateDt = DateTime.UtcNow,
            LastUpdateDt = DateTime.UtcNow,
            LastUpdateUser = currentUser
        };

        _context.UserProfiles.Add(userProfile);
        await _context.SaveChangesAsync();
    }

    private async Task CreateAspNetUserAsync()
    {
        var user = new ApplicationUser
        {
            UserName = this.UserName,
            Email = this.UserName,
            NormalizedUserName = this.UserName.ToUpperInvariant(),
            NormalizedEmail = this.UserName.ToUpperInvariant(),
            EmailConfirmed = false,
            PhoneNumber = this.PhoneNumber,
            PhoneNumberConfirmed = false,
            TwoFactorEnabled = false,
            LockoutEnabled = true,
            AccessFailedCount = 0,
            FirstName = this.FirstName,
            LastName = this.LastName,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            ResetPasswordOnLoginTF = true // This will be set by your trigger anyway
        };

        var result = await _userManager.CreateAsync(user, this.Password);
        if (!result.Succeeded)
        {
            throw new UserCreationException(result);
        }

        _userId = user.Id;
    }

    private async Task CreateVivaRoleAsync()
    {
        var currentUser = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";
        var now = DateTime.UtcNow;

        if (this.isAdminTF)
        {
            var adminUser = new AdminUser
            {
                UserId = this._userId,
                CreateDt = now,
                LastUpdateDt = now,
                LastUpdateUser = currentUser
            };

            _context.AdminUsers.Add(adminUser);
            await _context.SaveChangesAsync();
        }
        else if (this.isGCTF)
        {
            var generalContractorUser = new GeneralContractorUser
            {
                UserId = this._userId,
                GeneralContractorId = this.CompanyID,
                CanApproveTf = this.gcApproveTF,
                CreateDt = now,
                LastUpdateDt = now,
                LastUpdateUser = currentUser
            };

            _context.GeneralContractorUsers.Add(generalContractorUser);
            await _context.SaveChangesAsync();
        }
        else if (this.isSCTF)
        {
            var subcontractorUser = new SubcontractorUser
            {
                UserId = this._userId,
                SubcontractorId = this.CompanyID,
                CreateDt = now,
                LastUpdateDt = now,
                LastUpdateUser = currentUser
            };

            _context.SubcontractorUsers.Add(subcontractorUser);
            await _context.SaveChangesAsync();
        }
        else
        {
            throw new InvalidOperationException("User couldn't be assigned a role. No match was found.");
        }
    }
    

    private async Task SendEmailNotificationAsync(string creatorUserName)
        {
            if (this.isAdminTF)
            {
                // Send Admin Email
                await _emailService.sendNewAdminEmail(this._userId, this.Password);
                await _emailService.sendAdminEmailNewAdmin(this._userId);
            }
            else if (this.isGCTF)
            {
                // Send GC Email
                await _emailService.sendNewGeneralContractorEmail(this._userId, this.CompanyID, this.Password);
                await _emailService.sendAdminEmailNewGeneralContractorUser(this._userId, this.CompanyID, creatorUserName);
            }
            else if (this.isSCTF)
            {
                // Send SC Email
                await _emailService.sendNewSubcontractorEmail(this._userId, this.CompanyID, this.Password);
                await _emailService.sendAdminEmailNewSubcontractorUser(this._userId, this.CompanyID, creatorUserName);
            }
        }


}