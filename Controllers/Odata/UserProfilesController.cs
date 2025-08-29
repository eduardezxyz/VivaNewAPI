using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.OData;
using NewVivaApi.Data;
using NewVivaApi.Models;
using System.Text.Json;
using AutoMapper;
using NewVivaApi.Authentication;
using Microsoft.AspNetCore.Identity;
using NewVivaApi.Extensions;
using Microsoft.AspNet.Identity;

namespace NewVivaApi.Controllers
{
    // [Authorize]
    public class UserProfilesController : ODataController
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;

        public UserProfilesController(AppDbContext context, IMapper mapper, Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _mapper = mapper;
            _userManager = userManager;
        }

        private IQueryable<UserProfilesVw> GetSecureModel()
        {
            if (User.Identity.IsServiceUser())
            {
                return null;
            }

            IQueryable<UserProfilesVw> model;

            if (User.Identity.IsVivaUser())
            {
                model = _context.UserProfilesVws.OrderBy(usr => usr.FullName);
            }
            else if (User.Identity.IsGeneralContractor())
            {
                int generalContractorID = (int)User.Identity.GetGeneralContractorId();

                //List<int> subProjList = context.SubcontractorProjects_vw
                //                            .Where(subProj => subProj.GeneralContractorID == generalContractorID)
                //                            .Select(subproj => subproj.SubcontractorID).ToList();

                //model = context.UserProfiles_vw.Where(prof => prof.GeneralContractorID == generalContractorID || 
                //                                        (prof.SubcontractorID.HasValue && subProjList.Contains((int)prof.SubcontractorID)));

                model = _context.UserProfilesVws.Where(prof => prof.GeneralContractorId == generalContractorID).OrderBy(usr => usr.FullName);
            }
            else if (User.Identity.IsSubContractor())
            {
                int subContractorID = (int)User.Identity.GetSubcontractorId();
                model = _context.UserProfilesVws.Where(prof => prof.SubcontractorId == subContractorID).OrderBy(usr => usr.FullName);
            }
            else
            {
                string userId = User.Identity.GetUserId();
                model = _context.UserProfilesVws.Where(prof => prof.UserId == userId);
            }

            return model;
        }

        [EnableQuery]
        public ActionResult<IQueryable<UserProfilesVw>> Get()
        {
            if (User.Identity.IsServiceUser())
            {
                return null;
            }
            var model = GetSecureModel().OrderBy(u => u.UserId);
            if (model == null)
                return BadRequest();

            return Ok(model);
        }

        [EnableQuery]
        public ActionResult<UserProfilesVw> Get(string key)
        {
            if (User.Identity.IsServiceUser())
            {
                return null;
            }

            var model = GetSecureModel().FirstOrDefault(u => u.UserId == key);
            if (model == null)
                return NotFound();

            return Ok(model);
        }

        // [HttpPost]
        // public async Task<IActionResult> Post([FromBody] UserProfilesVw model)
        // {
        //     // Temporarily comment out auth check
        //     /*
        //     if (User.Identity.IsServiceUser())
        //     {
        //         return BadRequest();
        //     }
        //     */

        //     if (!ModelState.IsValid)
        //     {
        //         return BadRequest(ModelState);
        //     }

        //     if (string.IsNullOrEmpty(model.UserName) || string.IsNullOrEmpty(model.Password))
        //     {
        //         return BadRequest("UserName and Password are required.");
        //     }

        //     // Create ASP.NET Identity User first
        //     var applicationUser = new AspNetUser
        //     {
        //         UserName = model.UserName,
        //         Email = model.UserName, // Assuming username is email
        //         NormalizedUserName = model.UserName.ToUpper(),
        //         NormalizedEmail = model.UserName.ToUpper(),
        //         EmailConfirmed = true,
        //         SecurityStamp = Guid.NewGuid().ToString()
        //     };

        //     var result = await _userManager.CreateAsync(applicationUser, model.Password);

        //     if (!result.Succeeded)
        //     {
        //         var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        //         return BadRequest($"User creation failed: {errors}");
        //     }

        //     // Create UserProfile record
        //     var databaseModel = new UserProfile
        //     {
        //         UserId = applicationUser.Id, // Use the generated Identity user ID
        //         UserName = model.UserName,
        //         FirstName = model.FirstName,
        //         LastName = model.LastName,
        //         PhoneNumber = model.PhoneNumber,
        //         CreateDt = DateTimeOffset.UtcNow,
        //         LastUpdateDt = DateTimeOffset.UtcNow,
        //         LastUpdateUser = "system@api.com"
        //     };

        //     _context.UserProfiles.Add(databaseModel);

        //     try
        //     {
        //         await _context.SaveChangesAsync();
        //     }
        //     catch (DbUpdateException ex)
        //     {
        //         // If UserProfile creation fails, clean up the Identity user
        //         await _userManager.DeleteAsync(applicationUser);
        //         var innerMessage = ex.InnerException?.Message ?? "No inner exception";
        //         return BadRequest($"Database error: {ex.Message}. Inner: {innerMessage}");
        //     }

        //     // Return the created user profile
        //     var resultModel = await _context.UserProfilesVws
        //         .FirstOrDefaultAsync(up => up.UserId == databaseModel.UserId);

        //     return Created(resultModel ?? new UserProfilesVw
        //     {
        //         UserId = databaseModel.UserId,
        //         UserName = databaseModel.UserName,
        //         FirstName = databaseModel.FirstName,
        //         LastName = databaseModel.LastName,
        //         PhoneNumber = databaseModel.PhoneNumber
        //     });
        // }

        [HttpPatch("{key}")]
        public async Task<IActionResult> Patch(string key, [FromBody] JsonElement data)
        {
            // Temporarily comment out auth check
            /*
            if (User.Identity.IsServiceUser())
            {
                return BadRequest();
            }
            */

            Console.WriteLine("=== PATCH USER PROFILE ===");
            try
            {
                Console.WriteLine($"Patching user profile for key: {key}");
                var patchData = ExtractPatchData(data);

                var validationErrors = ValidatePatchData(patchData);
                if (validationErrors.Any())
                {
                    return BadRequest(new { errors = validationErrors });
                }

                // Find the user profile
                var dbModel = await _context.UserProfiles
                    .FirstOrDefaultAsync(s => s.UserId == key && s.DeleteDt == null);

                if (dbModel == null)
                {
                    Console.WriteLine($"User profile not found for key: {key}");
                    return NotFound(new { Type = "error", Message = "User profile not found" });
                }

                // Apply patches using helper method
                ApplyPatches(dbModel, patchData);

                // Update audit fields
                dbModel.LastUpdateDt = DateTimeOffset.UtcNow;
                dbModel.LastUpdateUser = User?.Identity?.Name ?? "system@api.com";
                Console.WriteLine($"Updated by: {dbModel.LastUpdateUser} at {dbModel.LastUpdateDt}");

                await _context.SaveChangesAsync();
                Console.WriteLine("User profile updated successfully.");

                // Return updated view model
                var updatedViewModel = await _context.UserProfilesVws
                    .FirstOrDefaultAsync(up => up.UserId == key);

                return Ok(new
                {
                    Type = "success",
                    Message = "User profile updated successfully",
                    Data = updatedViewModel
                });

            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Patch error: {ex.Message}");
                return StatusCode(500, new { Type = "error", Message = "Internal server error" });
            }
        }

        // [HttpDelete("{key}")]
        // public async Task<IActionResult> Delete(string key)
        // {
        //     // Temporarily comment out auth check
        //     /*
        //     if (User.Identity.IsServiceUser())
        //     {
        //         return BadRequest();
        //     }
        //     */

        //     var model = await _context.UserProfiles
        //         .FirstOrDefaultAsync(s => s.UserId == key && s.DeleteDt == null);

        //     if (model == null)
        //         return NotFound();

        //     // Soft delete the UserProfile
        //     model.DeleteDt = DateTimeOffset.UtcNow;

        //     try
        //     {
        //         await _context.SaveChangesAsync();
        //     }
        //     catch (DbUpdateException ex)
        //     {
        //         var innerMessage = ex.InnerException?.Message ?? "No inner exception";
        //         return BadRequest($"Database error: {ex.Message}. Inner: {innerMessage}");
        //     }

        //     // Note: We're NOT deleting the ASP.NET Identity user, just soft-deleting the profile
        //     // If you want to delete the Identity user too, uncomment below:
        //     /*
        //     var user = await _userManager.FindByIdAsync(key);
        //     if (user != null)
        //     {
        //         await _userManager.DeleteAsync(user);
        //     }
        //     */

        //     return NoContent();
        // }

        // Helper methods (following your RegisterSystemUser pattern)
        private PatchDataModel ExtractPatchData(JsonElement data)
        {
            Console.WriteLine("Extracting patch data from JSON...");

            return new PatchDataModel
            {
                FirstName = GetJsonProperty(data, "FirstName"),
                LastName = GetJsonProperty(data, "LastName"),
                PhoneNumber = GetJsonProperty(data, "PhoneNumber"),
                UserName = GetJsonProperty(data, "UserName")
            };
        }

        private List<string> ValidatePatchData(PatchDataModel data)
        {
            var errors = new List<string>();

            // Only validate fields that are being updated (not empty/null)
            if (!string.IsNullOrEmpty(data.UserName) && !IsValidEmail(data.UserName))
            {
                errors.Add("Invalid email format for UserName");
            }

            if (!string.IsNullOrEmpty(data.PhoneNumber) && !IsValidPhoneNumber(data.PhoneNumber))
            {
                errors.Add("Invalid phone number format");
            }

            return errors;
        }

        private void ApplyPatches(UserProfile dbModel, PatchDataModel patchData)
        {
            if (!string.IsNullOrEmpty(patchData.FirstName))
            {
                dbModel.FirstName = patchData.FirstName;
                Console.WriteLine($"Updated FirstName: {dbModel.FirstName}");
            }

            if (!string.IsNullOrEmpty(patchData.LastName))
            {
                dbModel.LastName = patchData.LastName;
                Console.WriteLine($"Updated LastName: {dbModel.LastName}");
            }

            if (!string.IsNullOrEmpty(patchData.PhoneNumber))
            {
                dbModel.PhoneNumber = patchData.PhoneNumber;
                Console.WriteLine($"Updated PhoneNumber: {dbModel.PhoneNumber}");
            }

            if (!string.IsNullOrEmpty(patchData.UserName))
            {
                dbModel.UserName = patchData.UserName;
                Console.WriteLine($"Updated UserName: {dbModel.UserName}");
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidPhoneNumber(string phoneNumber)
        {
            // Add your phone number validation logic here
            return !string.IsNullOrWhiteSpace(phoneNumber) && phoneNumber.Length >= 10;
        }

        private string GetJsonProperty(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var prop) ? prop.GetString() ?? "" : "";
        }

        // New helper method to handle boolean properties correctly
        private bool GetJsonBoolean(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.True)
                    return true;
                else if (prop.ValueKind == JsonValueKind.False)
                    return false;
                else if (prop.ValueKind == JsonValueKind.String)
                {
                    // Handle string representations of booleans
                    bool.TryParse(prop.GetString(), out bool result);
                    return result;
                }
            }
            return false; // Default to false if property doesn't exist or can't be parsed
        }

        // Data model for patch operations
        public class PatchDataModel
        {
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? PhoneNumber { get; set; }
            public string? UserName { get; set; }
        }
    }
}