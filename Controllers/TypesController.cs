using NewVivaApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewVivaApi.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using NewVivaApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;

namespace NewVivaApi.Controllers
{
    public class PayAppStatusTypes
    {
        public int StatusTypeID { get; set; }

        public string StatusOption { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class TypesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TypesController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            // var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            //             ?? User.FindFirst("sub")?.Value;

            // var adminUser = _context.AdminUsers.Any(perm => perm.UserId == userId);


            List<PayAppStatusTypes> statusTypes = new List<PayAppStatusTypes>()
            {
                new PayAppStatusTypes{ StatusTypeID = 1, StatusOption = "Unsubmitted" },
                new PayAppStatusTypes{ StatusTypeID = 2, StatusOption = "Submitted" },
                new PayAppStatusTypes{ StatusTypeID = 3, StatusOption = "Approved" },
                new PayAppStatusTypes{ StatusTypeID = 4, StatusOption = "Paid by Viva" },
                new PayAppStatusTypes{ StatusTypeID = 5, StatusOption = "Paid by GC" },
                new PayAppStatusTypes{ StatusTypeID = 6, StatusOption = "Cancelled" }
            };

            List<PayAppStatusTypes> statusTypesForSubcontractors = new List<PayAppStatusTypes>()
            {
                new PayAppStatusTypes{ StatusTypeID = 1, StatusOption = "Unsubmitted" },
                new PayAppStatusTypes{ StatusTypeID = 2, StatusOption = "Submitted" },
                new PayAppStatusTypes{ StatusTypeID = 3, StatusOption = "Approved" },
                new PayAppStatusTypes{ StatusTypeID = 4, StatusOption = "Paid by Viva" },
                new PayAppStatusTypes{ StatusTypeID = 6, StatusOption = "Cancelled" }
            };

            return Ok(statusTypes);

            // if (User.Identity.IsSubContractor())
            // {
            //     return Ok(statusTypesForSubcontractors);
            // }
            // else
            // {
            //     return Ok(statusTypes);
            // }
        }
    }
}