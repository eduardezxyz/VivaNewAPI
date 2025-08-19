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
using NewVivaApi.Authentication;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Identity;
using NewVivaApi.Authentication.Models;

var builder = WebApplication.CreateBuilder(args);

// ----------------------
// DbContext
// ----------------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, Role>()
    .AddEntityFrameworkStores<NewVivaApi.Authentication.Models.IdentityDbContext>()
    .AddDefaultTokenProviders();

// ----------------------
// CORS for Angular
// ----------------------
var MyAllowAngularApp = "_myAllowAngularApp";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowAngularApp,
        policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

// ----------------------
// Controllers + OData
// ----------------------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    })
    .AddOData(options => options
        .Select()
        .Filter()
        .OrderBy()
        .Expand()
        .Count()
        .SetMaxTop(1000) // merged from second file
        .AddRouteComponents("odata", GetEdmModel()));

// ----------------------
// Services
// ----------------------
builder.Services.AddScoped<AuthService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAutoMapper(typeof(AutoMapperConfig));

// ----------------------
// JWT Authentication
// ----------------------
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:ValidAudience"],
        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))
    };
});

builder.Services.AddAuthorization();

// ----------------------
// Swagger with JWT Support
// ----------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "NewVivaApi", Version = "v1" });

    // JWT auth in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by your JWT token"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ----------------------
// Middleware Pipeline
// ----------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable CORS before authentication
app.UseCors(MyAllowAngularApp);

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// ----------------------
// EDM Model for OData
// ----------------------
static IEdmModel GetEdmModel()
{
    var builder = new ODataConventionModelBuilder();

    // From first file
    builder.EntitySet<ProjectsVw>("Projects").EntityType.HasKey(p => p.ProjectId);
    builder.EntitySet<PayAppsVw>("PayApps").EntityType.HasKey(p => p.PayAppId);
    builder.EntitySet<SubcontractorsVw>("Subcontractors").EntityType.HasKey(s => s.SubcontractorId);
    builder.EntitySet<SubcontractorProjectsVw>("SubcontractorProjects").EntityType.HasKey(sp => sp.SubcontractorProjectId);
    builder.EntitySet<GeneralContractorsVw>("GeneralContractors").EntityType.HasKey(g => g.GeneralContractorId);
    builder.EntitySet<PayAppHistoryVw>("PayAppHistory").EntityType.HasKey(h => h.PayAppHistoryId);
    builder.EntitySet<UserProfilesVw>("UserProfiles").EntityType.HasKey(up => up.UserId);
    builder.EntitySet<DocumentsVw>("Documents").EntityType.HasKey(d => d.DocumentId);

    // Additional example from second file (already included above but keeping structure clean)
    // builder.EntitySet<ProjectsVw>("Projects").EntityType.HasKey(p => p.ProjectId);

    return builder.GetEdmModel();
}
