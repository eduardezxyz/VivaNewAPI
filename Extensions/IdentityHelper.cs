using System.Security.Claims;
using System.Security.Principal;
using Microsoft.EntityFrameworkCore;
using NewVivaApi.Data;

namespace NewVivaApi.Extensions
{
    public static class IdentityHelper
    {
        public static int? GetSubcontractorID(this IIdentity identity)
        {
            var serviceProvider = GetServiceProvider();
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            
            string? currentUserId = GetUserId(identity);
            if (currentUserId == null) return null;

            var subcontractorRecord = context.SubcontractorUsers
                .FirstOrDefault(perm => perm.UserId == currentUserId);

            return subcontractorRecord?.SubcontractorId;
        }

        public static int? GetGeneralContractorID(this IIdentity identity)
        {
            var serviceProvider = GetServiceProvider();
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            
            string? currentUserId = GetUserId(identity);
            if (currentUserId == null) return null;

            var generalContractorRecord = context.GeneralContractorUsers
                .FirstOrDefault(perm => perm.UserId == currentUserId);

            return generalContractorRecord?.GeneralContractorId;
        }

        public static bool IsVivaUser(this IIdentity identity)
        {
            var serviceProvider = GetServiceProvider();
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            
            string? currentUserId = GetUserId(identity);
            if (currentUserId == null) return false;

            return context.AdminUsers.Any(perm => perm.UserId == currentUserId);
        }

        public static bool CanServiceAccountMakeProjectRecord(this IIdentity identity, int generalContractorId)
        {
            var serviceProvider = GetServiceProvider();
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            
            string? currentUserId = GetUserId(identity);
            if (currentUserId == null) return false;

            if (context.ServiceUsers.Any(x => x.UserId == currentUserId))
            {
                return context.GeneralContractors.Any(y => y.GeneralContractorId == generalContractorId && 
                                                          y.CreatedByUser == identity.Name);
            }

            return false;
        }

        public static bool CanServiceAccountMakeSubcontractorProjectsRecord(this IIdentity identity, int subcontractorId, int projectId)
        {
            var serviceProvider = GetServiceProvider();
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            
            string? currentUserId = GetUserId(identity);
            if (currentUserId == null) return false;

            if (context.ServiceUsers.Any(x => x.UserId == currentUserId))
            {
                bool subcontractorValid = context.Subcontractors.Any(y => y.SubcontractorId == subcontractorId && 
                                                                          y.CreatedByUser == identity.Name);
                bool projectValid = context.Projects.Any(y => y.ProjectId == projectId && 
                                                              y.CreatedByUser == identity.Name);
                return subcontractorValid && projectValid;
            }

            return true;
        }

        public static bool CanServiceAccountMakePayAppsRecord(this IIdentity identity, int subcontractorProjectId)
        {
            var serviceProvider = GetServiceProvider();
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            
            string? currentUserId = GetUserId(identity);
            if (currentUserId == null) return false;

            if (context.ServiceUsers.Any(x => x.UserId == currentUserId))
            {
                return context.SubcontractorProjects.Any(y => y.SubcontractorProjectId == subcontractorProjectId && 
                                                             y.CreatedByUser == identity.Name);
            }

            return true;
        }

        public static bool CanServiceAccountMakePayAppPaymentsRecord(this IIdentity identity, int payAppId)
        {
            var serviceProvider = GetServiceProvider();
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            
            string? currentUserId = GetUserId(identity);
            if (currentUserId == null) return false;

            if (context.ServiceUsers.Any(x => x.UserId == currentUserId))
            {
                return context.PayApps.Any(y => y.PayAppId == payAppId && 
                                               y.CreatedByUser == identity.Name);
            }

            return true;
        }

        public static bool IsGeneralContractor(this IIdentity identity)
        {
            var serviceProvider = GetServiceProvider();
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            
            string? currentUserId = GetUserId(identity);
            if (currentUserId == null) return false;

            return context.GeneralContractorUsers.Any(perm => perm.UserId == currentUserId);
        }

        public static bool IsSubContractor(this IIdentity identity)
        {
            var serviceProvider = GetServiceProvider();
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            
            string? currentUserId = GetUserId(identity);
            if (currentUserId == null) return false;

            return context.SubcontractorUsers.Any(perm => perm.UserId == currentUserId);
        }

        public static bool IsServiceUser(this IIdentity identity)
        {
            var serviceProvider = GetServiceProvider();
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            
            string? currentUserId = GetUserId(identity);
            if (currentUserId == null) return false;

            return context.ServiceUsers.Any(perm => perm.UserId == currentUserId);
        }

        public static bool CanApproveTf(this IIdentity identity)
        {
            if (identity.IsVivaUser())
            {
                return true;
            }

            if (identity.IsSubContractor())
            {
                return false;
            }

            var serviceProvider = GetServiceProvider();
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            
            string? currentUserId = GetUserId(identity);
            if (currentUserId == null) return false;

            var generalContractorUser = context.GeneralContractorUsers
                .FirstOrDefault(perm => perm.UserId == currentUserId);

            return generalContractorUser?.CanApproveTf ?? false;
        }

        // Helper methods
        private static string? GetUserId(IIdentity identity)
        {
            if (identity is ClaimsIdentity claimsIdentity)
            {
                return claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }
            return null;
        }

        private static IServiceProvider GetServiceProvider()
        {
            // This is a simplified approach - in a real application, you'd inject this properly
            var httpContextAccessor = ServiceLocator.Current?.GetService<IHttpContextAccessor>();
            return httpContextAccessor?.HttpContext?.RequestServices 
                ?? throw new InvalidOperationException("Unable to resolve service provider");
        }
    }

    // Simple service locator - this is a workaround for extension methods
    public static class ServiceLocator
    {
        public static IServiceProvider? Current { get; set; }
    }
}