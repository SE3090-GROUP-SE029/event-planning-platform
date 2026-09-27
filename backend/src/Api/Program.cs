using System.Text;
using Application.Common.Interfaces;
using Application.Dtos.Events;
using Application.Services.Auth;
using Application.Services.Admin;
using Application.Services.Bookings;
using Application.Services.Events;
using Application.Services.Quotations;
using Application.Services.Scheduling;
using Application.Services.Test;
using Application.Services.Vendors;
using Application.Services.Planning;
using Application.Services.Validation;
using Application.Validators.Events;
using Api.GuestManagement;
using FluentValidation;
using Infrastructure.Auth;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Infrastructure.Services.Planning;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Plan It API",
        Version = "v1",
        Description = "Event Planning Platform API"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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

// Test feature service (correct namespace and class name)
builder.Services.AddScoped<ITestService, TestService>();

// C4 Guest Management — AI, email, registration, invitation, QR services
builder.Services.AddGuestManagement(builder.Configuration);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured."),
        npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

// JWT configuration (required by dev authentication)
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("The Jwt configuration section is missing.");
builder.Services.Configure<AdminSeedOptions>(builder.Configuration.GetSection("AdminSeed"));
builder.Services.Configure<AgenticAiOptions>(builder.Configuration.GetSection("AgenticAI"));
var agenticAiOptions = builder.Configuration.GetSection("AgenticAI").Get<AgenticAiOptions>()
    ?? new AgenticAiOptions();

// Dev repository and service registrations
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IVendorRepository, VendorRepository>();
builder.Services.AddScoped<IVendorOfferingRepository, VendorOfferingRepository>();
builder.Services.AddScoped<IVendorGalleryImageRepository, VendorGalleryImageRepository>();
builder.Services.AddScoped<IVendorAvailabilityRepository, VendorAvailabilityRepository>();
builder.Services.AddScoped<IVendorMarketplaceRepository, VendorMarketplaceRepository>();
builder.Services.AddScoped<IQuotationRepository, QuotationRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<IVendorImageStorage>(_ =>
{
    var webRoot = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
    Directory.CreateDirectory(Path.Combine(webRoot, "uploads", "vendors"));
    return new LocalVendorImageStorage(webRoot);
});
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IVendorService, VendorService>();
builder.Services.AddScoped<IVendorOfferingService, VendorOfferingService>();
builder.Services.AddScoped<IVendorGalleryService, VendorGalleryService>();
builder.Services.AddScoped<IVendorAvailabilityService, VendorAvailabilityService>();
builder.Services.AddScoped<IVendorMarketplaceService, VendorMarketplaceService>();
builder.Services.AddScoped<IQuotationService, QuotationService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IAdminEventService, AdminEventService>();
builder.Services.AddScoped<IAdminReadRepository, AdminReadRepository>();
builder.Services.AddScoped<IAdminReadService, AdminReadService>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IEventPlanDraftRepository, EventPlanDraftRepository>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICoordinatorPlanValidationService, CoordinatorPlanValidationService>();
builder.Services.AddScoped<IPlanGenerationService, PlanGenerationService>();
builder.Services.AddScoped<IPlanDecisionService, PlanDecisionService>();
builder.Services.AddScoped<IAgenticAiClient, AgenticAiClient>();
builder.Services.AddHttpClient("AgenticAI", client =>
{
    client.BaseAddress = new Uri(agenticAiOptions.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(agenticAiOptions.TimeoutSeconds);
});
builder.Services.AddScoped<IValidator<CreateEventRequest>, CreateEventRequestValidator>();

// Component 3: Scheduling Registrations
builder.Services.AddScoped<IScheduleRepository, ScheduleRepository>();
builder.Services.AddScoped<ConflictDetectionService>();
builder.Services.AddScoped<ScheduleService>();

// Component 3: Scheduling Registrations
builder.Services.AddScoped<IScheduleRepository, ScheduleRepository>();
builder.Services.AddScoped<ConflictDetectionService>();
builder.Services.AddScoped<ScheduleService>();


builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("ADMIN"));
    options.AddPolicy("EventPlannerOnly", p => p.RequireRole("EVENT_PLANNER"));
    options.AddPolicy("VendorOnly", p => p.RequireRole("VENDOR"));
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 2 * 1024 * 1024;
    options.ValueLengthLimit = 2 * 1024 * 1024;
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});
var app = builder.Build();

app.UseCors("AllowAll");

var webRootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(Path.Combine(webRootPath, "uploads", "vendors"));
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(webRootPath),
    RequestPath = ""
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("./v1/swagger.json", "Plan It API V1");
        c.RoutePrefix = "swagger";
    });
}

// Avoid HTTPS redirects on the local http profile (causes browser "Network Error" on uploads).
var httpsPort = app.Configuration["ASPNETCORE_HTTPS_PORT"];
if (!string.IsNullOrWhiteSpace(httpsPort))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Admin user seeding on startup (skips gracefully when AdminSeed:Password is not configured)
// NOTE: Requires human review — touches auth/user database state on application startup.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = services.GetRequiredService<AppDbContext>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher>();
        var adminSeedOptions = services.GetRequiredService<IOptions<AdminSeedOptions>>();
        await AdminSeeder.SeedAdminAsync(db, passwordHasher, adminSeedOptions.Value, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the admin user.");
    }
}

app.Run();
