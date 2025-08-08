using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using NewVivaApi.Data;
using NewVivaApi.Models;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.OData.Routing;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    })
    .AddOData(options => options
        .Select()
        .Filter()
        .OrderBy()
        .Expand()
        .Count()
        .SetMaxTop(100)
        .AddRouteComponents("odata", GetEdmModel()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(typeof(AutoMapperConfig));

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// EDM Model for OData
static IEdmModel GetEdmModel()
{
    var builder = new ODataConventionModelBuilder();
    
    builder.EntitySet<ProjectsVw>("Projects")
        .EntityType.HasKey(p => p.ProjectId);
        
    builder.EntitySet<PayAppsVw>("PayApps")
        .EntityType.HasKey(p => p.PayAppId);

    builder.EntitySet<SubcontractorsVw>("Subcontractors")
        .EntityType.HasKey(s => s.SubcontractorId);

    builder.EntitySet<SubcontractorProjectsVw>("SubcontractorProjects")
        .EntityType.HasKey(sp => sp.SubcontractorProjectId);
    
    builder.EntitySet<GeneralContractorsVw>("GeneralContractors")
        .EntityType.HasKey(g => g.GeneralContractorId);

    builder.EntitySet<PayAppHistoryVw>("PayAppHistory")
        .EntityType.HasKey(h => h.PayAppHistoryId);

    builder.EntitySet<UserProfilesVw>("UserProfiles")
        .EntityType.HasKey(up => up.UserId);

    builder.EntitySet<DocumentsVw>("Documents")
        .EntityType.HasKey(d => d.DocumentId);

    return builder.GetEdmModel();
}
