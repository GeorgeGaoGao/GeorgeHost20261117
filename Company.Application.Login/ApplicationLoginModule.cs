using Company.Application.Login.Views;
using Company.Application.Share.Prism;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;

namespace Company.Application.Login
{
    [Module(ModuleName = ModuleNames.ApplicationLoginModule, OnDemand = false)]
   
    public class ApplicationLoginModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.Resolve<IRegionManager>().RequestNavigate(RegionNames.MainRegion, nameof(LoginView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<LoginView>();
            
        }
    }
}