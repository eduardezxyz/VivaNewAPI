using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using NewVivaApi.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using NewVivaApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;

namespace NewVivaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DomainController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DomainController(AppDbContext context)
        {
            _context = context;
        }


        // // UNCOMMENT THIS WHEN TESTING FRONT END
        public async Task<IActionResult> Get(string name)
        {
            // TODO: Replace with your real identity check
            // if (User?.Identity?.IsAuthenticated == true && User.Identity.Name == "ServiceUser")
            // {
            //     return BadRequest();
            // }

            var generalContractor = await _context.GeneralContractors
                .FirstOrDefaultAsync(f => f.DommainName == name);

            if (generalContractor == null)
            {
                return Ok(); // Returns 200 with empty body
            }

            var jsonAttributes = JObject.Parse(generalContractor.JsonAttributes);

            var brandingModel = new brandingModel
            {
                GCName = generalContractor.GeneralContractorName,
                LogoImage = generalContractor.LogoImage,
                PrimaryColor = jsonAttributes["PrimaryColor"]?.ToString(),
                NavColor = jsonAttributes["NavColor"]?.ToString()
            };

            return Ok(brandingModel);
        }

        [HttpGet("GetDomainInfo/{name}")]
        public async Task<IActionResult> GetDomainInfo(string name)
        {
            // TODO: Replace with your real identity check
            // if (User?.Identity?.IsAuthenticated == true && User.Identity.Name == "ServiceUser")
            // {
            //     return BadRequest();
            // }

            var generalContractor = await _context.GeneralContractors
                .FirstOrDefaultAsync(f => f.DommainName == name);

            if (generalContractor == null)
            {
                return Ok(); // Returns 200 with empty body
            }

            var jsonAttributes = JObject.Parse(generalContractor.JsonAttributes);

            var brandingModel = new brandingModel
            {
                GCName = generalContractor.GeneralContractorName,
                LogoImage = generalContractor.LogoImage,
                PrimaryColor = jsonAttributes["PrimaryColor"]?.ToString(),
                NavColor = jsonAttributes["NavColor"]?.ToString()
            };

            return Ok(brandingModel);
        }

        [AllowAnonymous] // Equivalent to OverrideAuthentication
        [HttpGet("GetImage/{id}")]
        public async Task<IActionResult> GetImage(int id)
        {
            // TODO: Replace with your real identity check
            if (User?.Identity?.IsAuthenticated == true && User.Identity.Name == "ServiceUser")
            {
                return Unauthorized();
            }

            var generalContractor = await _context.GeneralContractorsVws
                .FirstOrDefaultAsync(gc => gc.GeneralContractorId == id);

            if (generalContractor == null || string.IsNullOrEmpty(generalContractor.LogoImage))
            {
                return NotFound();
            }

            var imageBytes = Convert.FromBase64String(generalContractor.LogoImage);
            return File(imageBytes, "image/png");
        }
    }
}
