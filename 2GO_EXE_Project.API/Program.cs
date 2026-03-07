using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.OpenApi.Models;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.BAL.Services;
using _2GO_EXE_Project.BAL.Settings;
using _2GO_EXE_Project.DAL.Context;
using _2GO_EXE_Project.DAL.Repositories.Implementations;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Npgsql;
using PayOS;
using PayOS.Models;
using Microsoft.AspNetCore.Http;
using _2GO_EXE_Project.BAL.Validation;
using _2GO_EXE_Project.DAL.Entities;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);
var corsName = "AllowAll";

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();
if (corsOrigins.Length == 0)
{
    var envOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS");
    if (!string.IsNullOrWhiteSpace(envOrigins))
    {
        corsOrigins = envOrigins
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }
}
if (corsOrigins.Length == 0)
{
    corsOrigins = new[] { "http://localhost:5173" };
}

builder.Services.AddCors(p => p.AddPolicy(name: corsName, policy =>
{
    policy.WithOrigins(corsOrigins)
        .AllowCredentials()
        .AllowAnyMethod()
        .AllowAnyHeader();
}));

// CORS policy for webhooks (no credentials needed)
builder.Services.AddCors(p => p.AddPolicy("WebhookPolicy", policy =>
{
    policy.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader();
}));

// Add services to the container.
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<GmailEmailSettings>(builder.Configuration.GetSection("Gmail"));
builder.Services.Configure<GeminiSettings>(builder.Configuration.GetSection("Gemini"));
builder.Services.Configure<_2GO_EXE_Project.BAL.Settings.PaymentGatewaySettings>(builder.Configuration.GetSection("PaymentGateway"));
builder.Services.Configure<PayOSSettings>(builder.Configuration.GetSection("PayOS"));
builder.Services.Configure<GhnSettings>(builder.Configuration.GetSection("GHN"));
builder.Services.Configure<_2GO_EXE_Project.BAL.Settings.CloudinarySettings>(builder.Configuration.GetSection("Cloudinary"));
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IModeratorService, ModeratorService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IBankService, BankService>();
builder.Services.AddScoped<ISubCategoryService, SubCategoryService>();
builder.Services.AddScoped<IListingService, ListingService>();
builder.Services.AddScoped<ISellerListingService, SellerListingService>();
builder.Services.AddScoped<IAdminListingService, AdminListingService>();
builder.Services.AddScoped<IModeratorListingService, ModeratorListingService>();
builder.Services.AddScoped<ISavedListingService, SavedListingService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAdminOrderService, AdminOrderService>();
builder.Services.AddScoped<IOrderTransactionService, OrderTransactionService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentGateway, HmacPaymentGateway>();
builder.Services.AddHttpClient<IPayosPaymentGateway, PayosPaymentGateway>();
builder.Services.AddScoped<IEscrowService, EscrowService>();
builder.Services.AddScoped<IShippingService, ShippingService>();
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IDistrictService, DistrictService>();
builder.Services.AddScoped<IWardService, WardService>();
builder.Services.AddHttpClient<IGhnShippingService, GhnShippingService>();
builder.Services.AddHttpClient<IGeminiService, GeminiService>();
builder.Services.AddScoped<IAiListingService, AiListingService>();
builder.Services.AddScoped<IAiQualityCheckService, AiQualityCheckService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IModerationService, ModerationService>();
builder.Services.AddScoped<IUserRiskInfoService, UserRiskInfoService>();
builder.Services.AddScoped<IUserPrecheckService, UserPrecheckService>();
builder.Services.AddScoped<INoteGenerationService, NoteGenerationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IChatbotService, ChatbotService>();
builder.Services.AddScoped<IMarketPriceProvider, MarketPriceProvider>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
builder.Services.AddScoped<IAdminSubscriptionPlanService, AdminSubscriptionPlanService>();
builder.Services.AddScoped<IAdminMarketPriceService, AdminMarketPriceService>();
builder.Services.AddScoped<IAdminPayoutService, AdminPayoutService>();
builder.Services.AddHostedService<EscrowExpiryService>();
builder.Services.AddHostedService<EscrowPayoutRetryService>();
// Configure PayOS
builder.Services.AddKeyedSingleton("OrderClient", (serviceProvider, key) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    return new PayOSClient(new PayOSOptions
    {
        ClientId = config["PayOS:ClientId"] ?? Environment.GetEnvironmentVariable("PAYOS_CLIENT_ID"),
        ApiKey = config["PayOS:ApiKey"] ?? Environment.GetEnvironmentVariable("PAYOS_API_KEY"),
        ChecksumKey = config["PayOS:ChecksumKey"] ?? Environment.GetEnvironmentVariable("PAYOS_CHECKSUM_KEY"),
    });
});
builder.Services.AddScoped<IPayOSService, PayOSService>();

// Configure payOS for transfer controller
builder.Services.AddKeyedSingleton("TransferClient", (sp, key) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new PayOSClient(new PayOSOptions
    {
        ClientId = config["PayOS:PayoutClientId"] ?? Environment.GetEnvironmentVariable("PAYOS_PAYOUT_CLIENT_ID"),
        ApiKey = config["PayOS:PayoutApiKey"] ?? Environment.GetEnvironmentVariable("PAYOS_PAYOUT_API_KEY"),
        ChecksumKey = config["PayOS:PayoutChecksumKey"] ?? Environment.GetEnvironmentVariable("PAYOS_PAYOUT_CHECKSUM_KEY"),
        LogLevel = LogLevel.Debug,
    });
});
builder.Services.AddScoped<ITransferService, TransferService>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

var connectionString = builder.Configuration.GetConnectionString("PostgreSqlConnection") ?? builder.Configuration.GetValue<string>("ConnectionStrings__PostgreSqlConnection");
var sanitizedConnectionString = SanitizeConnectionString(connectionString);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    var jwtSection = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
    var JwtIssuer = jwtSection.Issuer ?? builder.Configuration.GetValue<string>("Jwt__Issuer");
    var JwtAudience = jwtSection.Audience ?? builder.Configuration.GetValue<string>("Jwt__Audience");
    var JwtSecret = jwtSection.Secret ?? builder.Configuration.GetValue<string>("Jwt__Secret") ?? string.Empty;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = JwtIssuer,
        ValidAudience = JwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret)),
        ClockSkew = TimeSpan.Zero,
        NameClaimType = JwtRegisteredClaimNames.Sub,
        RoleClaimType = ClaimTypes.Role
    };
});

// Initialize Firebase Admin (for verifying ID tokens from Firebase Auth)
var firebaseSection = builder.Configuration.GetSection("Firebase"); 
if (FirebaseApp.DefaultInstance == null)
{    var credentialPath = firebaseSection["CredentialsPath"] ?? builder.Configuration.GetValue<string>("Firebase__CredentialsPath");
    var projectId = firebaseSection["ProjectId"] ?? builder.Configuration.GetValue<string>("Firebase__ProjectId");
    var credentialJson = firebaseSection["CredentialsJson"]
        ?? builder.Configuration.GetValue<string>("Firebase__CredentialsJson")
        ?? builder.Configuration.GetValue<string>("firebase-credentials");
    var envCredentialPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
#pragma warning disable CS0618 // FromFile is marked obsolete in this version; acceptable for setup
    GoogleCredential? credential = null;
    if (!string.IsNullOrWhiteSpace(credentialJson))
    {
        credential = GoogleCredential.FromJson(credentialJson);
    }
    else if (!string.IsNullOrWhiteSpace(credentialPath) && File.Exists(credentialPath))
    {
        credential = GoogleCredential.FromFile(credentialPath);
    }
    else if (!string.IsNullOrWhiteSpace(envCredentialPath) && File.Exists(envCredentialPath))
    {
        credential = GoogleCredential.FromFile(envCredentialPath);
    }
    else
    {
        var logger = LoggerFactory.Create(logging => logging.AddConsole()).CreateLogger("FirebaseInit");
        logger.LogWarning("Firebase credentials not configured. Firebase is disabled. Set Firebase:CredentialsJson, Firebase:CredentialsPath, or GOOGLE_APPLICATION_CREDENTIALS to a valid service account json.");
    }
#pragma warning restore CS0618

    if (credential != null)
    {
        FirebaseApp.Create(new AppOptions
        {
            Credential = credential,
            ProjectId = string.IsNullOrWhiteSpace(projectId) ? null : projectId
        });
    }
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "2GO_EXE_Project API", Version = "v1" });
    c.EnableAnnotations();
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

var app = builder.Build();
app.Logger.LogInformation("Database connection: {ConnectionString}", sanitizedConnectionString);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("EnableSwaggerInProduction"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(corsName);

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (ValidationException ex)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        // Use ex.Message since ValidationException does not have an Errors property
        await context.Response.WriteAsJsonAsync(new { errors = new[] { ex.Message } });
    }
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static string SanitizeConnectionString(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return "<empty>";
    }

    try
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (!string.IsNullOrWhiteSpace(builder.Password))
        {
            builder.Password = "****";
        }
        return builder.ToString();
    }
    catch
    {
        return "<invalid connection string>";
    }
}

public interface IListingCommentRepository : IGenericRepository<ListingComment>
{
    Task<ListingComment?> GetByIdWithDetailsAsync(long commentId, CancellationToken cancellationToken = default);
    Task<(int Total, IReadOnlyList<ListingComment> Items)> GetByListingIdAsync(
        long listingId, 
        int skip, 
        int take, 
        CancellationToken cancellationToken = default);
}


