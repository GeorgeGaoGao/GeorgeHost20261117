using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Company.Core.Dialog
{
    /// <summary>
    /// PopupBox.xaml 的交互逻辑
    /// </summary>
    public partial class PopupBox : Popup
    {
        public PopupBox()
        {
            InitializeComponent();
        }


        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }


        // Using a DependencyProperty as the backing store for Message.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(PopupBox), new PropertyMetadata(string.Empty));

        private static PopupBox _dialog;
        private static DispatcherTimer _hideTimer;
        private static DispatcherTimer _fadeTimer;


        public static void ShowPopup(string message, Window owner = null, int seconds = 1)
        {
            Application.Current.Dispatcher.Invoke(
                () =>
                {
                    if (_dialog == null)
                    {
                        _dialog = new PopupBox();
                        _hideTimer = new DispatcherTimer();
                        _fadeTimer = new DispatcherTimer();
                    }
                    owner ??= Application.Current.MainWindow;

                }

                );
        }
       
    }
}
