using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using uMarket.Server.Data;
using uMarket.Server.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Enable console logging for diagnostics
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Set log level based on environment
if (builder.Environment.IsProduction())
{
    builder.Logging.SetMinimumLevel(LogLevel.Warning);
}
else
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}

// Configure Kestrel for production (Azure handles this) or development
if (!builder.Environment.IsProduction())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        // Development: Listen on localhost only for security
        options.ListenLocalhost(5000);
    });
}

// Add services to the container
builder.Services.AddControllers();

// Only add OpenAPI in development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi();
}

// Configure CORS based on environment
builder.Services.AddCors(options =>
{
    if (builder.Environment.IsProduction())
    {
        // Production: Restrict to your actual mobile app or specific origins
        options.AddPolicy("Production", policy =>
        {
            policy
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowAnyOrigin(); // TODO: Replace with specific mobile app origin when available
                // For MAUI apps, you might need AllowAnyOrigin since mobile apps don't have a fixed origin
        });
    }
    else
    {
        // Development: Permissive for local testing
        options.AddPolicy("Development", policy =>
        {
            policy
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowAnyOrigin();
        });
    }
});

// Configure EF Core with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        }));

// Configure ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = false;
    
    // Production-ready password requirements
    if (builder.Environment.IsProduction())
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
    }
    else
    {
        // Relaxed for development
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    }
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// JWT Configuration - Must be set in appsettings or Azure App Settings
var jwtKey = builder.Configuration["Jwt:Key"] 
    ?? throw new InvalidOperationException("JWT Key not configured. Set Jwt:Key in appsettings or environment variables.");
var issuer = builder.Configuration["Jwt:Issuer"] 
    ?? throw new InvalidOperationException("JWT Issuer not configured.");
var audience = builder.Configuration["Jwt:Audience"] 
    ?? throw new InvalidOperationException("JWT Audience not configured.");

// JWT Authentication
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
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(2)
    };
    
    // Allow SignalR to receive access token from query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"].FirstOrDefault();
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Configure cookie authentication to return 401/403 for API requests
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization();
builder.Services.AddSignalR();

var app = builder.Build();

// Request logging - only in development
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        Console.WriteLine($"[Request] {context.Request.Method} {context.Request.Path}");
        await next();
    });

    // Debug endpoints - ONLY in development
    app.MapPost("/debug/ping", () => Results.NoContent());
    app.MapGet("/debug/routes", (EndpointDataSource ds) =>
    {
        var list = ds.Endpoints.Select(e => e.DisplayName ?? e.ToString()).ToList();
        return Results.Ok(list);
    });
}

// Apply EF Core migrations and seed roles
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var strategy = context.Database.CreateExecutionStrategy();

        strategy.Execute(() =>
        {
            // Apply migrations
            context.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully");

            // Seed roles
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            string[] roles = new[] { "Estudiante", "Vendedor" };
            foreach (var role in roles)
            {
                var exists = roleManager.RoleExistsAsync(role).GetAwaiter().GetResult();
                if (!exists)
                {
                    roleManager.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
                    logger.LogInformation("Created role: {Role}", role);
                }
            }
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database migration or role seeding");
        throw;
    }
}

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    // Production: Force HTTPS
    app.UseHttpsRedirection();
}

// Use appropriate CORS policy
var corsPolicy = app.Environment.IsProduction() ? "Production" : "Development";
app.UseCors(corsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<uMarket.Server.Hubs.ChatHub>("/hubs/chat");

app.Run();
