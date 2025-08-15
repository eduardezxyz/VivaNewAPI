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
using NewVivaApi.Authentication.Models;
using NewVivaApi.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add Identity DbContext (separate from business DbContext)
builder.Services.AddDbContext<NewVivaApi.Authentication.Models.IdentityDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add controllers
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

builder.Services.AddScoped<NewVivaApi.Authentication.AuthService>();
builder.Services.AddScoped<NewVivaApi.Services.AspNetUserService>();

// Configure Identity to use the separate IdentityDbContext
builder.Services.AddIdentity<ApplicationUser, Role>()
    .AddEntityFrameworkStores<NewVivaApi.Authentication.Models.IdentityDbContext>()
    .AddDefaultTokenProviders();

// builder.Services.AddIdentity<NewVivaApi.Authentication.Models.ApplicationUser, IdentityRole>()
//     .AddEntityFrameworkStores<AppDbContext>()
//     .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
        {
            // options.DefaultAuthenticateScheme = "JWT_OR_COOKIE";
            // options.DefaultChallengeScheme = "JWT_OR_COOKIE";
            // options.DefaultScheme = "JWT_OR_COOKIE";
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
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
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "NewViva API", Version = "v1" });
    
    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});
builder.Services.AddAutoMapper(typeof(AutoMapperConfig));
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

NewVivaApi.Extensions.ServiceLocator.Current = app.Services;

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

    builder.EntitySet<UserProfilesVw>("UserProfiles")
        .EntityType.HasKey(up => up.UserId);

    builder.EntitySet<PayAppPaymentsVw>("PayAppPayments")
        .EntityType.HasKey(p => p.PaymentId);

    return builder.GetEdmModel();
}
