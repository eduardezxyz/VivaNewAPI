using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace NewVivaApi.Models
{
    public class DbEntityValidationExceptionFormatter : Exception
    {
        public DbEntityValidationExceptionFormatter(DbUpdateException innerException) : base(null, innerException)
        {
        }

        public override string Message
        {
            get
            {
                if (InnerException is DbUpdateException innerException)
                {
                    StringBuilder sb = new StringBuilder();
                    
                    // For DbUpdateException, we need to handle validation differently
                    // as EF Core doesn't have DbEntityValidationException
                    sb.AppendLine($"Database update failed: {innerException.Message}");
                    
                    // If there are entries that failed
                    foreach (var entry in innerException.Entries ?? Enumerable.Empty<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry>())
                    {
                        sb.AppendLine($"Entity \"{entry.Entity.GetType().FullName}\" failed to update:");
                        sb.AppendLine($"State: {entry.State}");
                        
                        // Get validation errors if any
                        var validationResults = new List<ValidationResult>();
                        var context = new ValidationContext(entry.Entity);
                        if (!Validator.TryValidateObject(entry.Entity, context, validationResults, true))
                        {
                            foreach (var validationResult in validationResults)
                            {
                                foreach (var memberName in validationResult.MemberNames)
                                {
                                    var currentValue = entry.Property(memberName).CurrentValue;
                                    sb.AppendLine($"Property: \"{memberName}\" = \"{currentValue}\", Error: \"{validationResult.ErrorMessage}\"");
                                }
                            }
                        }
                    }
                    
                    return sb.ToString();
                }

                return base.Message;
            }
        }
    }
}