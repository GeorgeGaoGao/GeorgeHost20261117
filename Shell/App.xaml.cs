using Company.Application.Share.Prism;
using Company.Core;
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
            Container.Resolve<IModuleManager>().LoadModule(ModuleNames.ApplicationMainModule);

        }
        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            base.ConfigureModuleCatalog(moduleCatalog);
            moduleCatalog.AddModule(
                new ModuleInfo()
                {
                    ModuleName = ModuleNames.CoreModule,
                    ModuleType = typeof(CoreModule).AssemblyQualifiedName,
                    InitializationMode = InitializationMode.OnDemand
                }
                );
        }
        protected override IModuleCatalog CreateModuleCatalog()
        {

            return new DirectoryModuleCatalog() { ModulePath = ModuleNames.ModulePath };
        }
    }
}
