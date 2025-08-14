using Microsoft.EntityFrameworkCore;
using NewVivaApi.Data;
using NewVivaApi.Authentication.Models;
using NewVivaApi.Authentication;
using NewVivaApi.Models;

namespace NewVivaApi.Extensions
{
    public static class ApplicationUserExtensions
    {
        public static async Task<AspNetUserExtension?> GetExtensionAsync(this ApplicationUser au, AppDbContext? currentContext = null)
        {
            AspNetUserExtension? extension = null;

            if (currentContext != null)
            {
                extension = await GetExtensionsFromEntityAsync(au.Id, currentContext);
            }
            else
            {
                var serviceProvider = ServiceLocator.Current 
                    ?? throw new InvalidOperationException("ServiceLocator not initialized");
                    
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                extension = await GetExtensionsFromEntityAsync(au.Id, context);
            }

            return extension;
        }

        // Synchronous version for compatibility
        public static AspNetUserExtension? GetExtension(this ApplicationUser au, AppDbContext? currentContext = null)
        {
            return GetExtensionAsync(au, currentContext).GetAwaiter().GetResult();
        }

        private static async Task<AspNetUserExtension?> GetExtensionsFromEntityAsync(string userId, AppDbContext context)
        {
            return await context.AspNetUserExtensions
                .FirstOrDefaultAsync(e => e.Id == userId);
        }

        public static async Task<AspNetUserExtension?> CreateExtensionAsync(this ApplicationUser au, AppDbContext? currentContext = null)
        {
            if (currentContext != null)
            {
                return await CreateExtensionInEntityAsync(au.Id, currentContext);
            }
            else
            {
                var serviceProvider = ServiceLocator.Current 
                    ?? throw new InvalidOperationException("ServiceLocator not initialized");
                    
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                return await CreateExtensionInEntityAsync(au.Id, context);
            }
        }

        // Synchronous version for compatibility
        public static AspNetUserExtension? CreateExtension(this ApplicationUser au, AppDbContext? currentContext = null)
        {
            return CreateExtensionAsync(au, currentContext).GetAwaiter().GetResult();
        }

        private static async Task<AspNetUserExtension?> CreateExtensionInEntityAsync(string userId, AppDbContext context)
        {
            var existingCount = await context.AspNetUserExtensions
                .CountAsync(e => e.Id == userId);
                
            if (existingCount < 1)
            {
                var extension = new AspNetUserExtension
                {
                    Id = userId
                };

                context.AspNetUserExtensions.Add(extension);
                await context.SaveChangesAsync();

                return extension;
            }

            return null;
        }

        public static async Task<UserProfile?> GetProfileAsync(this ApplicationUser au, AppDbContext? currentContext = null)
        {
            if (currentContext != null)
            {
                return await GetProfileFromEntityAsync(au.Id, currentContext);
            }
            else
            {
                var serviceProvider = ServiceLocator.Current 
                    ?? throw new InvalidOperationException("ServiceLocator not initialized");
                    
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                return await GetProfileFromEntityAsync(au.Id, context);
            }
        }

        // Synchronous version for compatibility
        public static UserProfile? GetProfile(this ApplicationUser au, AppDbContext? currentContext = null)
        {
            return GetProfileAsync(au, currentContext).GetAwaiter().GetResult();
        }

        private static async Task<UserProfile?> GetProfileFromEntityAsync(string userId, AppDbContext context)
        {
            return await context.UserProfiles
                .FirstOrDefaultAsync(up => up.UserId == userId);
        }

        // Password Reset Helper Methods for AspNetUserExtension

        /// <summary>
        /// Sets password reset token for the user
        /// </summary>
        public static async Task<bool> SetPasswordResetTokenAsync(this ApplicationUser au, string token, DateTime expiration, AppDbContext? currentContext = null)
        {
            if (currentContext != null)
            {
                return await SetPasswordResetTokenInEntityAsync(au.Id, token, expiration, currentContext);
            }
            else
            {
                var serviceProvider = ServiceLocator.Current 
                    ?? throw new InvalidOperationException("ServiceLocator not initialized");
                    
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                return await SetPasswordResetTokenInEntityAsync(au.Id, token, expiration, context);
            }
        }

        private static async Task<bool> SetPasswordResetTokenInEntityAsync(string userId, string token, DateTime expiration, AppDbContext context)
        {
            var extension = await context.AspNetUserExtensions.FirstOrDefaultAsync(e => e.Id == userId);
            
            if (extension == null)
            {
                // Create new extension
                extension = new AspNetUserExtension
                {
                    Id = userId,
                    PasswordResetToken = token,
                    PasswordResetTokenExpiration = expiration
                };
                context.AspNetUserExtensions.Add(extension);
            }
            else
            {
                // Update existing extension
                extension.PasswordResetToken = token;
                extension.PasswordResetTokenExpiration = expiration;
                context.AspNetUserExtensions.Update(extension);
            }

            var result = await context.SaveChangesAsync();
            return result > 0;
        }

        /// <summary>
        /// Validates password reset token
        /// </summary>
        public static async Task<bool> ValidatePasswordResetTokenAsync(this ApplicationUser au, string token, AppDbContext? currentContext = null)
        {
            var extension = await au.GetExtensionAsync(currentContext);
            
            if (extension == null) return false;
            
            return extension.PasswordResetToken == token && 
                   extension.PasswordResetTokenExpiration.HasValue &&
                   extension.PasswordResetTokenExpiration.Value > DateTime.UtcNow;
        }

        /// <summary>
        /// Clears password reset token after successful reset
        /// </summary>
        public static async Task<bool> ClearPasswordResetTokenAsync(this ApplicationUser au, AppDbContext? currentContext = null)
        {
            if (currentContext != null)
            {
                return await ClearPasswordResetTokenInEntityAsync(au.Id, currentContext);
            }
            else
            {
                var serviceProvider = ServiceLocator.Current 
                    ?? throw new InvalidOperationException("ServiceLocator not initialized");
                    
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                return await ClearPasswordResetTokenInEntityAsync(au.Id, context);
            }
        }

        private static async Task<bool> ClearPasswordResetTokenInEntityAsync(string userId, AppDbContext context)
        {
            var extension = await context.AspNetUserExtensions.FirstOrDefaultAsync(e => e.Id == userId);
            
            if (extension != null)
            {
                extension.PasswordResetToken = null;
                extension.PasswordResetTokenExpiration = null;
                extension.PasswordResetIdentity = null;
                
                context.AspNetUserExtensions.Update(extension);
                var result = await context.SaveChangesAsync();
                return result > 0;
            }
            
            return false;
        }
    }
}