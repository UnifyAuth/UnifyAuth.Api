using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Infrastructure.Persistence.Context;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SendGrid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Extensions
{
    public static class ServiceRegistration
    {
        public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Database Context
            services.AddDbContext<UnifyAuthContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            });

            // Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            //Services
            services.AddScoped<IEmailTokenService, EmailTokenService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IPasswordService, PasswordService>();
            services.AddScoped<ITwoFactorService, TwoFactorService>();

            // SendGrid DI implementation
            services.AddSingleton<ISendGridClient>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var apiKey = config["SendGrid:ApiKey"];
                var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(10) // Set a timeout for SendGrid requests
                };
                return new SendGridClient(apiKey);
            });
            services.AddTransient<IEMailService, EmailService>();
        }
    }
}
