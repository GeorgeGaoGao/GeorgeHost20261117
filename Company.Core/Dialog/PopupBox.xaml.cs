using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
        private static PopupBox? _dialog;
        private static DispatcherTimer? _hideTimer;
        public PopupBox()
        {
            InitializeComponent();
            this.DataContext = this;
        }


        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Message.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(PopupBox), new PropertyMetadata(string.Empty));

        public static void ShowPopup(string message, int seconds = 2)
        {
            Show(message, TimeSpan.FromSeconds(Math.Max(1, seconds)));
        }
        /// <summary>
        /// 显示提示。可从任意线程、任意时机调用（包括还没有任何窗口的时候）。
        /// </summary>
        /// <param name="message"></param>
        /// <param name="timeSpan"></param>
        private static void Show(string message, TimeSpan timeSpan)
        {
            if (message == null) return;
            if (timeSpan <= TimeSpan.Zero) timeSpan = TimeSpan.FromSeconds(2);
            var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished) return;
            //确保在正确的线程上调用核心功能。
            if (dispatcher.CheckAccess())
            {
                ShowCore(message, timeSpan);
            }
            else
            {
                dispatcher.BeginInvoke(new Action(
                    () =>
                    {
                        ShowCore(message, timeSpan);
                    }
                    ), DispatcherPriority.Normal);
            }
        }

        private static void ShowCore(string message, TimeSpan timeSpan)
        {
            try
            {
                var popup = _dialog ??=new PopupBox();
                if (popup.Child is not UIElement child) return;
                child.BeginAnimation(OpacityProperty, null);
                child.Opacity = 1.0;

                popup.Message = message;
                Place(popup, child);
                popup.IsOpen = true;

                var timer = _hideTimer ??= new DispatcherTimer();
                timer.Stop();
                timer.Interval = timeSpan;
                timer.Tick -= OnHideTick;
                timer.Tick += OnHideTick;
                timer.Start();
            }
            catch (Exception ex)
            {
                // 提示框失败不应影响业务，但必须留下痕迹，方便定位 XamlParseException 之类的问题
                Debug.WriteLine("[MyPopup] show failed: " + ex);
            }

        }

        private static void Place(PopupBox popup, UIElement child)
        {
            var owner = FindOwnerWindow();
            if (owner!=null)
            {
                popup.PlacementTarget = owner;
                popup.Placement = PlacementMode.Center;
                popup.VerticalOffset = owner.ActualHeight > 0 ? owner.ActualHeight / 3.0 : 0;
                return;
            }
            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var size = child.DesiredSize;
            var area = SystemParameters.WorkArea;
            popup.PlacementTarget = null;
            popup.Placement = PlacementMode.AbsolutePoint;
            popup.HorizontalOffset = area.Left + Math.Max(0, area.Width - size.Width)/2.0;
            popup.VerticalOffset=area.Top+Math.Max(0,area.Height - size.Height)/3.0;

        }

        private static Window? FindOwnerWindow()
        {
            var app=Application.Current;
            if (app == null) return null;
            
            var windows=app.Windows.OfType<Window>().Where(w=>w.IsVisible).ToList();
            return windows.FirstOrDefault(w=>w.IsActive)??(app.MainWindow!=null&&app.MainWindow.IsVisible?app.MainWindow:null);


        }

        private static void OnHideTick(object? sender, EventArgs e)
        {
            _hideTimer?.Stop();
            if (_dialog.Child is not UIElement child) return;

            var fade = new DoubleAnimation(child.Opacity, 0, TimeSpan.FromMilliseconds(1000));

            child.BeginAnimation(OpacityProperty, fade);
            fade.Completed += (s, e) =>
            {
                child.BeginAnimation(OpacityProperty, null);                
                child.Opacity = 1.0;
               
                if (_dialog == null) return;
                _dialog.IsOpen = false;
                _dialog.Message = string.Empty;
            };
        }

       
    }
}