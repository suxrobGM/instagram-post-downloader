using Prism;
using Prism.Ioc;
using Prism.Unity;
using InstagramPostDownloader.Views;

namespace InstagramPostDownloader
{
    public partial class App : PrismApplication
    {
        public static string AndroidExternalDirectory;

        public App(IPlatformInitializer initializer = null) : base(initializer)
        {
            InitializeComponent();
            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.Register<MainPage>();
        }

        protected override void OnInitialized()
        {
            
        }
    }
}
