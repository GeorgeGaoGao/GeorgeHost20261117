using Company.Application.Share.Prism;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;
using Shell.Views;
using System.Windows;

namespace Shell
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {

        }
        protected override void OnInitialized()
        {
            base.OnInitialized();
            var regionManager = Container.Resolve<IRegionManager>();
            regionManager.RequestNavigate(RegionNames.MainRegion, "LoginView");

        }
        protected override IModuleCatalog CreateModuleCatalog()
        {
            return new DirectoryModuleCatalog() { ModulePath =ModuleNames.ModulePath};
        }
    }
}
