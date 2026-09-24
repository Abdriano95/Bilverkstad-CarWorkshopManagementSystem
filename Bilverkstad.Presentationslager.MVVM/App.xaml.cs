using Bilverkstad.Affärslager;
using Bilverkstad.Datalager;
using Bilverkstad.Presentationslager.MVVM.Services;
using Bilverkstad.Presentationslager.MVVM.ViewModels;
using Bilverkstad.Presentationslager.MVVM.Views.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows;

namespace Bilverkstad.Presentationslager.MVVM
{
    
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;
        private IConfiguration? _configuration;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Load configuration
            LoadConfiguration();
            
            // Initialize database
            InitializeDatabase();
            
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            try
            {
                ShowInitialWindow(Get_serviceProvider());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while starting the application: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }

        private void LoadConfiguration()
        {
            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                _configuration = builder.Build();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while loading configuration: {ex.Message}\n\nMake sure appsettings.json exists in the application directory.", 
                    "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }

        private void InitializeDatabase()
        {
            try
            {
                var connectionString = _configuration?.GetConnectionString("BilverkstadDatabase");
                
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Connection string 'BilverkstadDatabase' not found in appsettings.json");
                }

                // All data access through UnitOfWork uses this connection from now on
                BilverkstadContext.DefaultConnectionString = connectionString;

                using var context = new BilverkstadContext(connectionString);
                
                // Ensure database is created and apply migrations
                context.Database.Migrate();
                
                // Seed the database with initial data
                BilverkstadSeed.Populate(context);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while initializing the database: {ex.Message}\n\nInner Exception: {ex.InnerException?.Message}", 
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IWindowService, WindowService>();
            services.AddSingleton<IUserMessageService, UserMessageService>();
            services.AddSingleton<AnställdController>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<LoginWindow>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<MainWindow>();
        }

        private IServiceProvider? Get_serviceProvider()
        {
            return _serviceProvider;
        }

        private void ShowInitialWindow(IServiceProvider? _serviceProvider)
        {
            var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
            loginWindow.Show();
        }
    }

}
