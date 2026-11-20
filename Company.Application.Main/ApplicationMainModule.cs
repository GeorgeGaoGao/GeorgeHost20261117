using Company.Applications.Main.Views;
using Company.Applications.Share.Prism;
using Company.Core.Extension;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;
using System.Reflection;

namespace Company.Application.Main
{
    [Module(ModuleName =ModuleNames.ApplicationMainModule,OnDemand =true)]
    [ModuleDependency(ModuleNames.ApplicationLoginModule)]
    public class ApplicationMainModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            //containerProvider.Resolve<IRegionManager>().RegisterViewWithRegion<MainView>(RegionNames.MainRegion);
            containerProvider.Resolve<IRegionManager>().RequestNavigate(RegionNames.MainRegion, nameof(MainView));

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterExposedServiceTypesForAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.RegisterForNavigation<MainView>();
        }
    }
}