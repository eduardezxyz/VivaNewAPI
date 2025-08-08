using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;
using NewVivaApi.Models;
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

namespace NewVivaApi.Controllers.OData
{
    //[Authorize]
    public class DocumentsController : ODataController
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        //private readonly ODataValidationSettings _validationSettings = new ODataValidationSettings();

        public DocumentsController(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // private IQueryable<DocumentsVw> GetSecureModel()
        // {
        //     return _context.DocumentsVw
        //         .Where(d => d.DeleteDt == null)
        //         .OrderBy(s => s.FileName);
        // }

        [EnableQuery]
        public ActionResult<IQueryable<DocumentsVw>> Get()
        {
            // if (User.Identity?.IsServiceUser() == true)
            // {
            //     return BadRequest();
            // }

            var model = _context.DocumentsVws.OrderBy(d => d.DocumentId);
            if (model == null)
                return BadRequest();

            return Ok(model);
        }

        [EnableQuery]
        public ActionResult<DocumentsVw> Get([FromRoute] int key)
        {
            // if (User.Identity?.IsServiceUser() == true)
            // {
            //     return BadRequest();
            // }

            var model = _context.DocumentsVws.FirstOrDefault(d => d.DocumentId == key);
            if (model == null)
                return NotFound();

            return Ok(model);
        }

        public IActionResult Post([FromBody] DocumentsVw model)
        {
            // if (User.Identity?.IsServiceUser() == true)
            // {
            //     return BadRequest();
            // }

            return StatusCode(501); //no implementation in the original code
        }

        public IActionResult Patch([FromRoute] int key, [FromBody] Delta<DocumentsVw> patch)
        {
            // if (User.Identity?.IsServiceUser() == true)
            // {
            //     return BadRequest();
            // }

            return StatusCode(501); //no implementation in the original code
        }

        public async Task<IActionResult> Delete([FromRoute] int key)
        {
            // if (User.Identity?.IsServiceUser() == true)
            // {
            //     return BadRequest();
            // }

            var model = await _context.Documents
                .FirstOrDefaultAsync(s => s.DocumentId == key && s.DeleteDt == null);
            
            if (model == null)
                return NotFound();

            model.DeleteDt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}