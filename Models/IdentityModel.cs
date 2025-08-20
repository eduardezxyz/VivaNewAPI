using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace NewVivaApi.Models
{
    /// <summary>
    /// ApplicationUser extends IdentityUser with additional properties for the Viva Pay App
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        // Additional properties beyond the base IdentityUser
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;
        public string? CompanyName { get; set; }
        public string? JobTitle { get; set; }

        /// <summary>
        /// Generates user identity with claims for JWT authentication
        /// This replaces the old GenerateUserIdentityAsync method
        /// </summary>
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Create the base identity
            var userIdentity = new ClaimsIdentity();

            // Add standard claims
            userIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, Id));
            userIdentity.AddClaim(new Claim(ClaimTypes.Name, UserName ?? ""));
            userIdentity.AddClaim(new Claim(ClaimTypes.Email, Email ?? ""));

            // Add custom claims
            if (!string.IsNullOrEmpty(FirstName))
                userIdentity.AddClaim(new Claim("firstName", FirstName));
            
            if (!string.IsNullOrEmpty(LastName))
                userIdentity.AddClaim(new Claim("lastName", LastName));
                
            if (!string.IsNullOrEmpty(CompanyName))
                userIdentity.AddClaim(new Claim("companyName", CompanyName));
                
            if (!string.IsNullOrEmpty(JobTitle))
                userIdentity.AddClaim(new Claim("jobTitle", JobTitle));

            // Add roles from Identity
            var roles = await manager.GetRolesAsync(this);
            foreach (var role in roles)
            {
                userIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
            }

            // Add any additional custom claims here
            userIdentity.AddClaim(new Claim("isActive", IsActive.ToString()));
            userIdentity.AddClaim(new Claim("createdAt", CreatedAt.ToString("O"))); // ISO 8601 format

            return userIdentity;
        }

        /// <summary>
        /// Gets the full display name of the user
        /// </summary>
        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName))
            {
                return $"{FirstName} {LastName}";
            }
            
            if (!string.IsNullOrEmpty(FirstName))
            {
                return FirstName;
            }
            
            if (!string.IsNullOrEmpty(LastName))
            {
                return LastName;
            }
            
            return UserName ?? Email ?? "Unknown User";
        }

        /// <summary>
        /// Gets initials from the user's name
        /// </summary>
        public string GetInitials()
        {
            var initials = "";
            
            if (!string.IsNullOrEmpty(FirstName))
                initials += FirstName[0];
                
            if (!string.IsNullOrEmpty(LastName))
                initials += LastName[0];
                
            return initials.ToUpper();
        }

        /// <summary>
        /// Updates the last login timestamp
        /// </summary>
        public void UpdateLastLogin()
        {
            LastLoginAt = DateTime.UtcNow;
        }

        // Navigation properties
        public virtual AspNetUserExtension? Extension { get; set; }
        public virtual UserProfile? Profile { get; set; }
    }
}