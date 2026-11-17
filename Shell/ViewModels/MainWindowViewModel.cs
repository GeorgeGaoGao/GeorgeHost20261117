using Company.Application.Share.Prism;
using Prism.Modularity;
using Prism.Mvvm;
using Prism.Navigation.Regions;

namespace Shell.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        
        private string _title = "Prism Application";

        public MainWindowViewModel()
        {
            
        }

        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

       
    }
}
