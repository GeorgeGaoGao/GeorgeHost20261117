using System;
using System.Collections.Generic;
using System.Text;

namespace Company.Core.IOC
{
    [ExposedService(Lifetime=Lifetime.Singleton,AutoInitialize =true)]
    public sealed class PrismProvider
    {
        public PrismProvider(IContainerExtension container, 
            IRegionManager regionManager, 
            IEventAggregator eventAggregator, 
            IDialogService dialogService, 
            IModuleManager moduleManager)
        {
            Container = container;
            RegionManager = regionManager;
            EventAggregator = eventAggregator;
            DialogService = dialogService;
            ModuleManager = moduleManager;
        }

        public IContainerExtension Container { get; }
        public IRegionManager RegionManager { get; }
        public IEventAggregator EventAggregator { get;  }
        public IDialogService DialogService { get;  }
        public IModuleManager ModuleManager { get;  }
    }
}
