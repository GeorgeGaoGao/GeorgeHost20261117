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
    /// MyPopup.xaml 的交互逻辑
    /// </summary>
    public partial class MyPopup : Popup
    {
        //=================可调参数=========================
        private static readonly TimeSpan MinVisible = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan FadeInTime=TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan FadeOutTime=TimeSpan.FromMilliseconds(500);
        //=================单例状态=========================
        private static MyPopup? _dialog;
        //=================实例状态=========================
        private int _generation;
        private int _pendingToken;
        private DispatcherTimer _hideTimer;
        //=================依赖属性=========================


        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Message.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(MyPopup), new PropertyMetadata(string.Empty));


        //=================对外契约=========================
        public static void Show(string message, TimeSpan duration)
        {
            if (!TryNormalizeParameters(message, duration, out string text, out TimeSpan timeSpan)) return;
            RunOnUiThread(() => ShowCore(text, timeSpan));
        }
        //=================尝试规整参数=========================
        private static bool TryNormalizeParameters(string message, TimeSpan duration, out string text, out TimeSpan timeSpan)
        {
            text = message?.Trim() ?? string.Empty;
            timeSpan = duration > MinVisible ? duration : MinVisible;
            return text.Length > 0;
        }

        
        //=================UI线程运行=========================

        private static void RunOnUiThread(Action action)
        {
            if (action == null) return;

            var app = Application.Current;
            if (app == null) return;

            var dispatcher = app.Dispatcher;
            if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished) return;

            if (dispatcher.CheckAccess()) action();
            else
            {
                dispatcher.BeginInvoke(action, DispatcherPriority.Normal);
            }
        }
        //=================建立实例=========================
        private static void ShowCore(string text, TimeSpan timeSpan)
        {
            var popup = _dialog ??= new MyPopup();
            if (popup.Child is not UIElement child) return;
            popup.Message = text;
            child.Opacity = 0;
            popup.IsOpen = false;
            popup.StaysOpen = true;

            popup.Present(timeSpan);
        }
        //=================编排=========================
        private void Present(TimeSpan timeSpan)
        {
            var token = ++_generation;

            ResetVisualState();
            DecidePlacement();

            IsOpen = true;
            Animate(0, 1, FadeInTime, onCompleted);
            ScheduleHide(token,timeSpan);
        }
        //=================恢复视觉=========================

        private void ResetVisualState()
        {
            if (Child is not UIElement child) return;

            child.BeginAnimation(OpacityProperty, null);
            child.Opacity = 0;
        }

        //=================决定位置=========================

        private void DecidePlacement()
        {
            Window? owner = FindOwnerWindow();

            if (owner != null && TryPlaceRelativeToWindow(owner)) return;

            PlaceAbsoluteOnWorkArea();
            
        }

        private Window? FindOwnerWindow()
        {
            var app = Application.Current;
            if (app == null) return null;

            var active = app.Windows.OfType<Window>().Where(w => w.IsVisible && w.IsActive).FirstOrDefault();
            return active;

            var main = app.MainWindow;
            if (main != null && main.WindowState != WindowState.Minimized && main.IsVisible) return main;
            else
            {
                return null;
            }
           
        }

        private bool TryPlaceRelativeToWindow(Window owner)
        {
            if (owner == null) return false;
            if (Child is not UIElement child) return false;

            PlacementTarget = owner;
            Placement = PlacementMode.Center;
            HorizontalOffset = 0;
            VerticalOffset = owner.ActualHeight / 3.0;
            return true;
        }

        private void PlaceAbsoluteOnWorkArea()
        {
            var workArea = SystemParameters.WorkArea;
            Size size = MeasureContent();

            PlacementTarget = null;
            Placement = PlacementMode.Absolute;
            HorizontalOffset = workArea.Left + Math.Max(0, (workArea.Width - size.Width) / 2);
            VerticalOffset=workArea.Top+Math.Max(0,(workArea.Height - size.Height)/2);
        }

        private Size MeasureContent()
        {
            if (Child is not UIElement child) return Size.Empty;
            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return child.DesiredSize;
        }

        //=================淡入淡出=========================

        private void Animate(int v1, int v2, TimeSpan fadeInTime, object onCompleted)
        {
            throw new NotImplementedException();
        }

        //=================计时隐藏=========================
        private void ScheduleHide(int token, TimeSpan timeSpan)
        {
            throw new NotImplementedException();
        }



        
        //=================可调参数=========================
        //=================可调参数=========================
        //=================可调参数=========================
        //=================可调参数=========================
        public MyPopup()
        {
            InitializeComponent();
        }
    }
}
