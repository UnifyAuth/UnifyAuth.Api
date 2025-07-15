using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Infrastructure.Common.Mappings;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Extensions
{
    public static class ServiceRegistration
    {
        public static void AddInfrastructureServices(this IServiceCollection services)
        {
            // Repositories
            services.AddScoped<IUserRepository, UserRepository>();

            //Services
            services.AddTransient<IEMailService, EmailService>();
            services.AddScoped<IEmailTokenService, EmailTokenService>();
        }
    }
}
