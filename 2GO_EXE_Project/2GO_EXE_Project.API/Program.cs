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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<GmailEmailSettings>(builder.Configuration.GetSection("Gmail"));
builder.Services.Configure<_2GO_EXE_Project.BAL.Settings.PaymentGatewaySettings>(builder.Configuration.GetSection("PaymentGateway"));
builder.Services.Configure<MomoSettings>(builder.Configuration.GetSection("Momo"));
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IModeratorService, ModeratorService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ISubCategoryService, SubCategoryService>();
builder.Services.AddScoped<IListingService, ListingService>();
builder.Services.AddScoped<ISellerListingService, SellerListingService>();
builder.Services.AddScoped<IAdminListingService, AdminListingService>();
builder.Services.AddScoped<IModeratorListingService, ModeratorListingService>();
builder.Services.AddScoped<ISavedListingService, SavedListingService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentGateway, HmacPaymentGateway>();
builder.Services.AddHttpClient<IMomoPaymentGateway, MomoPaymentGateway>();
builder.Services.AddScoped<IEscrowService, EscrowService>();
builder.Services.AddScoped<IShippingService, ShippingService>();
builder.Services.AddScoped<IReportService, ReportService>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    var jwtSection = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection.Issuer,
        ValidAudience = jwtSection.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection.Secret)),
        ClockSkew = TimeSpan.Zero,
        NameClaimType = JwtRegisteredClaimNames.Sub,
        RoleClaimType = ClaimTypes.Role
    };
});

// Initialize Firebase Admin (for verifying ID tokens from Firebase Auth)
var firebaseSection = builder.Configuration.GetSection("Firebase");
if (FirebaseApp.DefaultInstance == null)
{
    var credentialPath = firebaseSection["CredentialsPath"];
    var projectId = firebaseSection["ProjectId"];
#pragma warning disable CS0618 // FromFile is marked obsolete in this version; acceptable for setup
    GoogleCredential credential;
    var envPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
    if (!string.IsNullOrWhiteSpace(credentialPath) && File.Exists(credentialPath))
    {
        credential = GoogleCredential.FromFile(credentialPath);
    }
    else if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
    {
        credential = GoogleCredential.FromFile(envPath);
    }
    else
    {
        throw new InvalidOperationException("Firebase credentials not configured. Set Firebase:CredentialsPath or GOOGLE_APPLICATION_CREDENTIALS to a valid service account json.");
    }
#pragma warning restore CS0618

    FirebaseApp.Create(new AppOptions
    {
        Credential = credential,
        ProjectId = string.IsNullOrWhiteSpace(projectId) ? null : projectId
    });
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
