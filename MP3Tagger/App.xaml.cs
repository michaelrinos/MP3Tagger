using Core;
using Core.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MP3Tagger.NewFolder;
using MP3Tagger.ViewModels;
using System;
using System.IO;
using System.Text;
using System.Windows;

namespace MP3Tagger {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public IServiceProvider ServiceProvider { get; set; }
        public IConfiguration Configuration { get; private set; }

        protected override void OnStartup (StartupEventArgs eventArgs)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true);
            
            Configuration = builder.Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            ServiceProvider = serviceCollection.BuildServiceProvider();
            var mainWindow = ServiceProvider.GetService<MainWindow>();
            mainWindow.Show();
        }
        private void ConfigureServices(IServiceCollection services)
        {
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
            services.AddTransient(typeof(MainWindow));
            services.AddScoped<ISqlDataProvider, SqlDataProvider>();
            services.AddScoped<IMusicService, MusicService>();

            services.AddTransient<MusicEditorViewModel>();
            services.AddTransient<MainViewModel>();

        }
    }
}
