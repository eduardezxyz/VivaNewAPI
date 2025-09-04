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
using NewVivaApi.Extensions;
using Microsoft.AspNetCore.OData.Query.Validator;

namespace NewVivaApi.Controllers.Odata
{
    // [Authorize]
    public class PayAppHistoryController : ODataController
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ODataValidationSettings _validationSettings;


        public PayAppHistoryController(AppDbContext context, IMapper mapper, ODataValidationSettings validationSettings)
        {
            _context = context;
            _mapper = mapper;
            _validationSettings = validationSettings;

        }


        private IQueryable<PayAppHistoryVw>? GetSecureModel()
        {
            if (User.Identity.IsServiceUser())
            {
                return null;
            }

            if (User.Identity.IsVivaUser())
            {
                return _context.PayAppHistoryVws;
            }
            else if (User.Identity.IsGeneralContractor())
            {
                int? currentGeneralContractorId = User.Identity.GetGeneralContractorId();
                return _context.PayAppHistoryVws.Where(payApp =>
                    payApp.LowestPermToView == "General Contractor");
            }
            // SRS: Commented this out just in case they want the Sub Contractors to have access to the History again (11/8/19)
            // else if (User.Identity.IsSubContractor())
            // {
            //     int? currentSubcontractorId = User.Identity.GetSubcontractorID();
            //     return _context.PayAppHistory_vw.Where(payApp =>
            //         payApp.LowestPermToView == "Subcontractor");
            // }
            else
            {
                return null;
            }
        }

        [EnableQuery]
        public ActionResult<IQueryable<PayAppHistoryVw>> Get(ODataQueryOptions<PayAppHistoryVw> queryOptions)
        {
            if (User.Identity.IsServiceUser())
            {
                return null;
            }

            try
            {
                queryOptions.Validate(_validationSettings);

            }
            catch (ODataException ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok(GetSecureModel());
        }

        [EnableQuery]
        public ActionResult<PayAppHistoryVw> Get([FromRoute] int key, ODataQueryOptions<PayAppHistoryVw> queryOptions)
        {

            if (User.Identity.IsServiceUser())
            {
                return BadRequest();
            }

            try
            {
                queryOptions.Validate(_validationSettings);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

            var model = GetSecureModel().FirstOrDefault(h => h.PayAppHistoryId == key);
            if (model == null)
                return NotFound();

            return Ok(model);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] PayAppHistoryVw model)
        {
            if (User.Identity.IsServiceUser())
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
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

            TryValidateModel(dbModel);
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.PayAppHistories.Add(dbModel);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var exceptionFormatter = new DbEntityValidationExceptionFormatter(ex);
                return BadRequest(exceptionFormatter.Message);
            }

            var resultModel = _mapper.Map<PayAppHistoryVw>(dbModel);
            return Created(resultModel);
        }


        [HttpPatch]
        public async Task<IActionResult> Patch(int key, [FromBody] PayAppHistoryVw patch)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, "PATCH operation is not implemented.");

        }


        [HttpDelete("{key}")]
        public async Task<IActionResult> Delete(int key)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, "DELETE operation is not implemented.");

        }


    }
}