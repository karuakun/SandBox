using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using OpenSilverApplication1.ViewModels;

namespace OpenSilverApplication1
{
    public sealed partial class App : Application
    {
        public App()
        {
            Services = ConfigureServices();

            InitializeComponent();
            Window.Current.Content = Services.GetService<Views.MainPage>();
        }

        public new static App Current => (App)Application.Current;

        public IServiceProvider Services { get; }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddHttpClient();

            services.AddTransient<Views.MainPage>();
            services.AddTransient<MainViewModel>();
            
            return services.BuildServiceProvider();
        }
    }
}
