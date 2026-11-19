using Company.Application.Share.Event;
using Company.Application.Share.Model;
using Company.Core.IOC;
using Prism.Commands;
using Prism.Mvvm;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Company.Application.Login.ViewModels
{
    public class LoginViewModel : ReactiveObject
    {
        [Reactive]
        public CurrentUser CurrentUser { get; set; } = new CurrentUser();
        public ICommand LoginCommand { get; set; }
        public LoginViewModel()
        {
            LoginCommand = new DelegateCommand(OnLoginCommand);
        }
          

        private void OnLoginCommand()
        {
            if (string.IsNullOrEmpty(CurrentUser.Name) || string.IsNullOrEmpty(CurrentUser.Password))
            {
                return;
            }
            if (CurrentUser.Name == "Admin" && CurrentUser.Password == "123456")
            {
                PrismProvider.EventAggregator.GetEvent<LoginSuccessEvent>().Publish(CurrentUser);
            }
        }
    }
    
}
