using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Core.MVVM.Interfaces;
using PasswordManager.Core.MVVM;
using PasswordManager.Core.Services;
using PasswordManager.Data.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using PasswordManager.Core.Services.Interfaces;
using PasswordManager.App.Services;
using PasswordManager.App.ViewModels;
using PasswordManager.App.Views;
using PasswordManager.Data.Repositories.Interfaces;
using PasswordManager.Data.Repositories;
using PasswordManager.Data.Context;

namespace PasswordManager.App
{
        public partial class App : Application
        {
                private ServiceProvider _serviceProvider;

                public App()
                {
                        // We'll initialize services in OnStartup instead
                }

                private async Task ConfigureServicesAsync(ServiceCollection services)
                {
                        // Get Azure Key Vault configuration
                        string keyVaultUrl = ConfigurationManager.AppSettings["KeyVaultUrl"];
                        string tenantId = ConfigurationManager.AppSettings["TenantId"];
                        string clientId = ConfigurationManager.AppSettings["ClientId"];
                        string clientSecret = ConfigurationManager.AppSettings["ClientSecret"];

                        // Create and add Key Vault service
                        var keyVaultService = new KeyVaultService(keyVaultUrl, tenantId, clientId, clientSecret);
                        services.AddSingleton<IKeyVaultService>(keyVaultService);

                        // Get encryption key from Key Vault
                        string encryptionKey = await keyVaultService.GetSecretAsync("EncryptionKey");

                        // Add data layer services with the retrieved encryption key
                        services.AddPasswordManagerDataServices(encryptionKey);

                        // Register application services
                        services.AddSingleton<IPasswordStrengthService, PasswordStrengthService>();
                        services.AddSingleton<IDialogService, DialogService>();
                        services.AddSingleton<INavigationService>(sp => new NavigationService(sp));

                        // Register ViewModels
                        services.AddTransient<LoginViewModel>();
                        services.AddTransient<MainViewModel>();
                        services.AddTransient<DashboardViewModel>();
                        services.AddTransient<PasswordManagementViewModel>();
                        services.AddTransient<AdminDashboardViewModel>();
                        services.AddTransient<TwoFactorSetupViewModel>();
                        services.AddTransient<PasswordGroupManagementViewModel>();
                        services.AddTransient<SecurityManagementViewModel>();

                        // Register Views
                        services.AddTransient<LoginWindow>();
                        services.AddTransient<MainWindow>();
                }

                protected override async void OnStartup(StartupEventArgs e)
                {
                        base.OnStartup(e);

                        try
                        {
                                var services = new ServiceCollection();
                                await ConfigureServicesAsync(services);
                                _serviceProvider = services.BuildServiceProvider();

                                var loginViewModel = _serviceProvider.GetRequiredService<LoginViewModel>();
                                var loginWindow = new LoginWindow(loginViewModel);
                                loginWindow.Show();
                                MainWindow = loginWindow;
                        }
                        catch (Exception ex)
                        {
                                MessageBox.Show($"Failed to initialize application: {ex.Message}",
                                              "Startup Error",
                                              MessageBoxButton.OK,
                                              MessageBoxImage.Error);
                                Shutdown(-1);
                        }
                }

                protected override void OnExit(ExitEventArgs e)
                {
                        base.OnExit(e);
                        _serviceProvider?.Dispose();
                }
        }
}
