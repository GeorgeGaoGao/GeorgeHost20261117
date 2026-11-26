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
using System.Windows.Media.Animation;
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
        //================可调参数=========================
        private static readonly TimeSpan MinVisible = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan FadeInTime = TimeSpan.FromMilliseconds(5000);
        private static readonly TimeSpan FadeOutTime = TimeSpan.FromMilliseconds(5000);
        //=================单例状态=========================
        private static MyPopup? _dialog;
        //================实例状态=========================
        private DispatcherTimer? _hideTimer;
        private int _generation;
        private int _pendingToken;
        //================依赖属性=========================


        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Message.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(MyPopup), new PropertyMetadata(string.Empty));


        //================对外契约=========================
        public static void Show(string message, TimeSpan timeSpan)
        {
            if (!TryNormalizeParameters(message, timeSpan, out string text, out TimeSpan duration)) return;
            RunOnUiThread(() => ShowCore(text, duration));
        }
        //================规整参数=========================

        private static bool TryNormalizeParameters(string message, TimeSpan timeSpan, out string text, out TimeSpan duration)
        {
            text = message?.Trim() ?? string.Empty;
            duration = timeSpan > MinVisible ? timeSpan : MinVisible;
            return text.Length > 0;
        }
        //================UI线程运行=========================


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
        //================创建实例=========================

        private static void ShowCore(string text, TimeSpan duration)
        {
            var popup = _dialog ??= new MyPopup();
            popup.Message = text;
            popup.Present(duration);
        }
        //================编排=========================

        private void Present(TimeSpan duration)
        {
            var token = ++_generation;

            ResetVisualState();
            DecidePlacement();

            var wasOpen = IsOpen;
            IsOpen = true;

            if (!wasOpen && Child is UIElement child) Animate(child, 0, 1, FadeInTime);
            ScheduleHide(token,duration);
        }



        //================恢复视觉=========================

        private void ResetVisualState()
        {
            _hideTimer?.Stop();
            if (Child is not UIElement child) return;
            child.BeginAnimation(OpacityProperty, null);
            child.Opacity = 1;
        }
        //================决定位置=========================

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
            if (main != null && main.IsVisible && main.WindowState != WindowState.Minimized) return main;
            else return null;
        }

        private bool TryPlaceRelativeToWindow(Window owner)
        {
            if (owner == null) return false;

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
            HorizontalOffset = workArea.Left + Math.Max(0, (workArea.Width - size.Width) / 2.0);
            VerticalOffset = workArea.Top + Math.Max(0, (workArea.Height - size.Height) / 2.0);

        }

        private Size MeasureContent()
        {
            if (Child is not UIElement child) return Size.Empty;
            Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return child.DesiredSize;
        }

        //================淡入淡出=========================

        private void Animate(UIElement target, double from, double to, TimeSpan timeSpan, Action? onCompleted=null)
        {
            if (Child is not UIElement child) return;
            var animation = new DoubleAnimation(from, to, timeSpan)
            {
                EasingFunction = new QuadraticEase()
                {
                    EasingMode = from > to ?EasingMode.EaseOut: EasingMode.EaseIn
                }
            };
            if (onCompleted != null)
            {
                animation.Completed += (s, e) => onCompleted();
            }
            child.BeginAnimation(OpacityProperty, animation);

        }
        //================计划隐藏=========================

        private void ScheduleHide(int token, TimeSpan duration)
        {
            _pendingToken = token;
            if (_hideTimer==null)
            {
                _hideTimer = new DispatcherTimer(DispatcherPriority.Normal);
                _hideTimer.Tick += OnHideTick;
            }
            _hideTimer.Interval= duration;
            _hideTimer.Start();
        }

        private void OnHideTick(object? sender, EventArgs e)
        {
            _hideTimer?.Stop();
            if (_generation != _pendingToken) return;//在等待interval期间发生了新的present
            BeginHide();
        }

        private void BeginHide()
        {
            if (Child is not UIElement child) return;
            var token = _generation;
            Animate(child, 1, 0, FadeOutTime,
                () =>
                {
                    if (_generation != token) return;//在等待动画完成过程中发生了新的presetn
                    child.BeginAnimation(OpacityProperty, null);
                    child.Opacity = 0;
                    IsOpen = false;
                }
                );
        }


        //================可调参数=========================
        //================可调参数=========================
        //================可调参数=========================
        //================可调参数=========================
        //================可调参数=========================
        public MyPopup()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
