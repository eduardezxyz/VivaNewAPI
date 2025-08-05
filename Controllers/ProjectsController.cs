using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData;
using NewVivaApi.Data;
using NewVivaApi.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NewVivaApi.Controllers
{
    public class ProjectsController : ODataController
    {
        private readonly AppDbContext _context;

        public ProjectsController(AppDbContext context)
        {
            _context = context;
        }

        [EnableQuery]
        public IQueryable<ProjectsVw> GetProjects()
        {
            // Simple: just return all projects from the view
            return _context.ProjectsVws.OrderBy(p => p.ProjectName);
        }

        [EnableQuery]
        public SingleResult<ProjectsVw> GetProject(int key)
        {
            // Find a specific project by key
            var result = _context.ProjectsVws.Where(p => p.ProjectId == key);
            return SingleResult.Create(result);
        }

        

    }
}