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

namespace NewVivaApi.Controllers
{
    // [Authorize]
    public class UserProfilesController : ODataController
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        //private readonly UserManager<AspNetUser> _userManager;

        public UserProfilesController(AppDbContext context, IMapper mapper) //UserManager<AspNetUser> userManager
        {
            _context = context;
            _mapper = mapper;
            //_userManager = userManager;
        }

        private IQueryable<UserProfilesVw> GetSecureModel()
        {
            // Temporarily return all UserProfiles - we'll add auth logic later
            /*
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
                int generalContractorID = (int)User.Identity.GetGeneralContractorID();
                model = _context.UserProfilesVws.Where(prof => prof.GeneralContractorId == generalContractorID).OrderBy(usr => usr.FullName);
            }
            else if (User.Identity.IsSubContractor())
            {
                int subContractorID = (int)User.Identity.GetSubcontractorID();
                model = _context.UserProfilesVws.Where(prof => prof.SubcontractorId == subContractorID).OrderBy(usr => usr.FullName);
            }
            else
            {
                string userId = User.Identity.GetUserId();
                model = _context.UserProfilesVws.Where(prof => prof.UserId == userId);
            }

            return model;
            */

            // For now, return all UserProfiles
            return _context.UserProfilesVws.OrderBy(up => up.FullName);
        }

        [EnableQuery]
        public ActionResult<IQueryable<UserProfilesVw>> Get()
        {
            // Temporarily comment out auth check
            /*
            if (User.Identity.IsServiceUser())
            {
                return null;
            }
            */
            var model = _context.UserProfilesVws.OrderBy(u => u.UserId);
            if (model == null)
                return BadRequest();

            return Ok(model);
        }

        [EnableQuery]
        public ActionResult<UserProfilesVw> Get(string key)
        {
            // Temporarily comment out auth check
            /*
            if (User.Identity.IsServiceUser())
            {
                return null;
            }
            */

            var model = _context.UserProfilesVws.FirstOrDefault(u => u.UserId == key);
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

        // [HttpPatch("{key}")]
        // public async Task<IActionResult> Patch(string key, [FromBody] UserProfilesVw patch)
        // {
        //     // Temporarily comment out auth check
        //     /*
        //     if (User.Identity.IsServiceUser())
        //     {
        //         return BadRequest();
        //     }
        //     */

        //     if (patch == null)
        //         return BadRequest("No patch data provided");

        //     var dbModel = await _context.UserProfiles
        //         .FirstOrDefaultAsync(s => s.UserId == key && s.DeleteDt == null);
            
        //     if (dbModel == null)
        //         return NotFound();

        //     // Manual mapping for PATCH to avoid issues
        //     if (!string.IsNullOrEmpty(patch.FirstName))
        //         dbModel.FirstName = patch.FirstName;
                
        //     if (!string.IsNullOrEmpty(patch.LastName))
        //         dbModel.LastName = patch.LastName;
                
        //     if (!string.IsNullOrEmpty(patch.PhoneNumber))
        //         dbModel.PhoneNumber = patch.PhoneNumber;

        //     // Update audit fields
        //     dbModel.LastUpdateDt = DateTimeOffset.UtcNow;
        //     dbModel.LastUpdateUser = "system@api.com";

        //     try
        //     {
        //         await _context.SaveChangesAsync();
        //     }
        //     catch (DbUpdateException ex)
        //     {
        //         var innerMessage = ex.InnerException?.Message ?? "No inner exception";
        //         return BadRequest($"Database error: {ex.Message}. Inner: {innerMessage}");
        //     }

        //     var updatedViewModel = await _context.UserProfilesVws
        //         .FirstOrDefaultAsync(up => up.UserId == key);
                
        //     return Updated(updatedViewModel);
        // }

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
    }
}