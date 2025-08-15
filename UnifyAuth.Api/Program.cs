using Application.Common.Mappings;
using Application.Common.Validators;
using Application.Interfaces.Services;
using Application.Services;
using FluentValidation;
using Infrastructure.Common.IdentityModels;
using Infrastructure.Common.Mappings;
using Infrastructure.Extensions;
using Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Exceptions;
using UnifyAuth.Api.Handlers;
using UnifyAuth.Api.Middlewares;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

//Serilog Configuration
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.WithExceptionDetails();
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Infrastructure services
builder.Services.AddInfrastructureServices(builder.Configuration);

// Application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddValidatorsFromAssembly(typeof(RegisterDtoValidator).Assembly);

//AutoMapper configurations
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<UserProfile>();
    cfg.AddProfile<IdentityUserProfile>();
});

// Identity configuration
builder.Services.AddIdentity<IdentityUserModel, IdentityRole<Guid>>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;
    options.User.RequireUniqueEmail = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 3;
})
.AddEntityFrameworkStores<UnifyAuthContext>()
.AddDefaultTokenProviders();
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(2);
});

//Cors configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

//Authentication configuration
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

//Exception handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseCors("AllowDev");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
