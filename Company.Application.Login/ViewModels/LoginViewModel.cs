using Company.Applications.Share.Event;
using Company.Applications.Share.Model;
using Company.Core.Dialog;
using Company.Core.IOC;
using Prism.Commands;
using Prism.Mvvm;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Company.Applications.Login.ViewModels
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
                PopupBox.ShowPopup("用户名或密码不能为空");
                return;
            }
            if (CurrentUser.Name == "Admin" && CurrentUser.Password == "123456")
            {
                PrismProvider.EventAggregator.GetEvent<LoginSuccessEvent>().Publish(CurrentUser);
            }
            else
            {
                //PopupBox.ShowPopup("用户名或密码错误");
                //GeorgePopup.Show("用户名或密码错误",TimeSpan.FromSeconds(2));
                //MyPopup.ShowPopup("用户名或密码错误");
                //DSPopup.Show("用户名或密码错误",TimeSpan.FromSeconds(2));
                //ToastPopup.Show("用户名或密码错误",TimeSpan.FromSeconds(2));
                DSPopup.Show("用户名或密码错误",TimeSpan.FromSeconds(2));
            }
        }
    }
    
}
