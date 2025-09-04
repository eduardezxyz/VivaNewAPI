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

namespace NewVivaApi.Controllers.Odata
{
    // [Authorize]
    public class PayAppHistoryController : ODataController
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public PayAppHistoryController(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        private IQueryable<PayAppHistoryVw> GetSecureModel()
        {
            /*
            IQueryable<PayAppHistoryVw> model;
            
            if (User.Identity.IsServiceUser())
            {
                return null;
            }

            if (User.Identity.IsVivaUser())
            {
                model = _context.PayAppHistoryVws;
            }
            else if (User.Identity.IsGeneralContractor())
            {
                int? currentGeneralContractorId = User.Identity.GetGeneralContractorID();
                model = _context.PayAppHistoryVws.Where(payApp =>
                    payApp.LowestPermToView == "General Contractor");
            }
            // SRS: Commented this out just in case they want the Sub Contractors to have access to the History again (11/8/19)
            // else if (User.Identity.IsSubContractor())
            // {
            //     int? currentSubcontractorId = User.Identity.GetSubcontractorID();
            //     model = _context.PayAppHistoryVws.Where(payApp =>
            //         payApp.LowestPermToView == "Subcontractor");
            // }
            else
            {
                model = null;
            }

            return model;
            */

            // For now, return all PayAppHistory records
            return _context.PayAppHistoryVws.OrderBy(h => h.CreateDt);
        }

        [EnableQuery]
        public ActionResult<IQueryable<PayAppHistoryVw>> Get()
        {
            //auth check
            /*
            if (User.Identity.IsServiceUser())
            {
                return null;
            }
            */
            var model = _context.PayAppHistoryVws.OrderBy(h => h.PayAppHistoryId);
            if (model == null)
                return BadRequest();

            return Ok(model);
        }

        [EnableQuery]
        public ActionResult<PayAppHistoryVw> Get([FromRoute] int key)
        {
            //auth check
            /*
            if (User.Identity.IsServiceUser())
            {
                return null;
            }
            */
            var model = _context.PayAppHistoryVws.FirstOrDefault(h => h.PayAppHistoryId == key);
            if (model == null)
                return NotFound();

            return Ok(model);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] PayAppHistoryVw model)
        {
            //auth check
            /*
            if (User.Identity.IsServiceUser())
            {
                return BadRequest();
            }
            */

            if (!ModelState.IsValid){
                return BadRequest(ModelState);
            }

            // Validate that the PayApp exists
            var payAppExists = await _context.PayApps
                .AnyAsync(pa => pa.PayAppId == model.PayAppId);

            if (!payAppExists)
            {
                return BadRequest($"PayApp with ID {model.PayAppId} does not exist.");
            }

            var dbModel = _mapper.Map<PayAppHistory>(model);

            dbModel.CreateDt = DateTimeOffset.UtcNow;
            dbModel.LastUpdateDt = DateTimeOffset.UtcNow;
            dbModel.LastUpdateUser = "deki@steeleconsult.com";

            _context.PayAppHistories.Add(dbModel);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException?.Message ?? "No inner exception";
                return BadRequest($"Database error: {ex.Message}. Inner: {innerMessage}");
            }

            var resultModel = _mapper.Map<PayAppHistoryVw>(dbModel);
            return Created(resultModel);
        }

        public async Task<IActionResult> Patch(int key, [FromBody] PayAppHistoryVw patch)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, "PATCH operation is not implemented.");

        }

        public async Task<IActionResult> Delete(int key)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, "DELETE operation is not implemented.");

        }

            
    }
}