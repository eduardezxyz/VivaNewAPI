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
using NewVivaApi.Authentication.Models;
using NewVivaApi.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json");
}

// Register AppDbContext with the DI container
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add Identity DbContext (separate from business DbContext)
builder.Services.AddDbContext<NewVivaApi.Authentication.Models.IdentityDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure Identity to use the separate IdentityDbContext
builder.Services.AddIdentity<ApplicationUser, Role>()
    .AddEntityFrameworkStores<NewVivaApi.Authentication.Models.IdentityDbContext>()
    .AddDefaultTokenProviders();

// ----------------------
// 1. Configure CORS
// ----------------------
var MyAllowAngularApp = "_myAllowAngularApp";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowAngularApp,
        policy =>
        {
            policy.WithOrigins("http://localhost:4200") // Angular app origin
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials(); // 👈 important
        });
});

// ----------------------
// Add Controllers + OData
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
        .SetMaxTop(100)
        .AddRouteComponents("odata", GetEdmModel()));

// ----------------------
// Services
// ----------------------
builder.Services.AddScoped<AuthService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<NewVivaApi.Services.AspNetUserService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<FinancialSecurityService>();



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
// Swagger
// ----------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "NewVivaApi", Version = "v1" });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http, // change from ApiKey → Http
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by your JWT token"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddAutoMapper(typeof(AutoMapperConfig));
builder.Services.AddHttpContextAccessor();


var app = builder.Build();
NewVivaApi.Extensions.ServiceLocator.Current = app.Services;

// ----------------------
// Middleware Pipeline
// ----------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable CORS **before** authentication & authorization
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

    builder.EntitySet<ProjectsVw>("Projects").EntityType.HasKey(p => p.ProjectId);
    builder.EntitySet<PayAppsVw>("PayApps").EntityType.HasKey(p => p.PayAppId);
    builder.EntitySet<SubcontractorsVw>("Subcontractors").EntityType.HasKey(s => s.SubcontractorId);
    builder.EntitySet<SubcontractorProjectsVw>("SubcontractorProjects").EntityType.HasKey(sp => sp.SubcontractorProjectId);
    builder.EntitySet<GeneralContractorsVw>("GeneralContractors").EntityType.HasKey(g => g.GeneralContractorId);
    builder.EntitySet<PayAppHistoryVw>("PayAppHistory").EntityType.HasKey(h => h.PayAppHistoryId);
    builder.EntitySet<UserProfilesVw>("UserProfiles").EntityType.HasKey(up => up.UserId);
    builder.EntitySet<DocumentsVw>("Documents").EntityType.HasKey(d => d.DocumentId);

    return builder.GetEdmModel();
}
