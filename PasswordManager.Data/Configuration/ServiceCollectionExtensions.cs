using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Core.Services.Interfaces;
using PasswordManager.Core.Services;
using PasswordManager.Data.Context;
using PasswordManager.Data.Repositories.Interfaces;
using PasswordManager.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

namespace PasswordManager.Data.Configuration
{
        public static class ServiceCollectionExtensions
        {
                public static IServiceCollection AddPasswordManagerDataServices(
                    this IServiceCollection services,
                    string encryptionKey)
                {
                        // Initialize Entity Framework
                        Database.SetInitializer<PasswordManagerEntities>(null);

                        // Register DbContext
                        services.AddScoped<PasswordManagerEntities>();

                        // Register Core Services
                        services.AddSingleton<IEncryptionService>(_ =>
                            new EncryptionService(encryptionKey));
                        services.AddSingleton<ISecurityService, SecurityService>();

                        // Register Repositories
                        services.AddScoped<IUserRepository, UserRepository>();
                        services.AddScoped<IStoredPasswordRepository, StoredPasswordRepository>();
                        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
                        services.AddScoped<ILoginAttemptRepository, LoginAttemptRepository>();
                        services.AddScoped<IPasswordGroupRepository, PasswordGroupRepository>();
                        services.AddScoped<ISecurityManagementRepository, SecurityManagementRepository>();

                        return services;
                }
        }
}