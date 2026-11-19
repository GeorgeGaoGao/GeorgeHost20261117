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

        public static IContainerExtension Container { get; private set; }
        public static IRegionManager RegionManager { get; private set; }
        public static IEventAggregator EventAggregator { get; private set; }
        public static IDialogService DialogService { get; private set; }
        public static IModuleManager ModuleManager { get; private set; }
    }
}
