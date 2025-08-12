using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using NewVivaApi.Data;
using NewVivaApi.Models;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.OData.Routing;
using NewVivaApi.Services;

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

// 1. Configure Authentication with JWT Bearer
builder.Services.AddAuthentication(options =>
		{
			options.DefaultAuthenticateScheme = "JWT_OR_COOKIE";
			options.DefaultChallengeScheme = "JWT_OR_COOKIE";
			options.DefaultScheme = "JWT_OR_COOKIE";
		})

// Adding Jwt Bearer  
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:ValidAudience"],
        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))
    };
});

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(typeof(AutoMapperConfig));
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 2. Add authentication middleware
app.UseAuthentication();

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
