using Company.Application.Login.Views;
using Company.Application.Share.Prism;
using Company.Core.Extension;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;
using System.Reflection;

namespace Company.Application.Login
{
    [Module(ModuleName = ModuleNames.ApplicationLoginModule, OnDemand = false)]
   
    public class ApplicationLoginModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeExposedServiceTypesForAssembly(Assembly.GetExecutingAssembly());
            containerProvider.Resolve<IRegionManager>().RequestNavigate(RegionNames.MainRegion, nameof(LoginView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterExposedServiceTypesForAssembly(Assembly.GetExecutingAssembly());
            containerRegistry.RegisterForNavigation<LoginView>();
            
        }
    }
}