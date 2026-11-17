using Company.Application.Share.Prism;
using ControlzEx.Theming;
using MahApps.Metro.Controls;
using Shell.ViewModels;
using System.Windows;

namespace Shell.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            ThemeManager.Current.ChangeTheme(this, "Dark.Red");
        }

        private void LaunchGitHubSite(object sender, RoutedEventArgs e)
        {

        }

        private void DeployCupCakes(object sender, RoutedEventArgs e)
        {

        }
    }
}
