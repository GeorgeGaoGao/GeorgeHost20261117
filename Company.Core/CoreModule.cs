using Company.Application.Share.Prism;
using Company.Core.Extension;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Company.Core
{
    [Module(ModuleName=ModuleNames.CoreModule)]
    public class CoreModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.InitializeExposedServiceTypesForAssembly(Assembly.GetExecutingAssembly());
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterExposedServiceTypesForAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
