using Company.Applications.Share.Event;
using Company.Applications.Share.Model;
using Company.Applications.Share.Prism;
using Company.Core.IOC;
using Prism.Commands;
using Prism.Events;
using Prism.Modularity;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using System;
using System.Windows.Input;

namespace Shell.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        
        private string _title = "Prism Application";

        public ICommand LoadedCommand { get; set; }

        public MainWindowViewModel(IModuleManager moduleManager,IRegionManager regionManager)
        {
            LoadedCommand = new DelegateCommand(OnLoadedCommand);
        }

        private void OnLoadedCommand()
        {
            PrismProvider.EventAggregator.GetEvent<LoginSuccessEvent>().Subscribe(OnReceiveLoginSuccessEvent, ThreadOption.UIThread);
        }

        private void OnReceiveLoginSuccessEvent(CurrentUser user)
        {
            PrismProvider.ModuleManager.LoadModule(ModuleNames.ApplicationMainModule);
        }

        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

       
    }
}
