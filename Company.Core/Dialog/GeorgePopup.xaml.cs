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
    /// GeorgePopup.xaml 的交互逻辑
    /// </summary>
    public partial class GeorgePopup : Popup
    {
        //=================可调参数=====================
        //最小显示时长，淡入时长 淡出时长
        private static readonly TimeSpan MinVisibleTime = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan FadeInTime = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan FadeOutTime = TimeSpan.FromMilliseconds(500);

        //=================弹窗单例状态=====================
        //单例，静态懒加载
        private static GeorgePopup? _georgePopup;

        //=================实例状态=====================
        //轮次计数 延迟动作轮次计数暂存 隐藏所需的定时器
        private int _generation;
        private int _pendingToken;
        private DispatcherTimer? _hideTimer;

        //=================依赖属性=====================

        public string Information
        {
            get { return (string)GetValue(InformationProperty); }
            set { SetValue(InformationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Information.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InformationProperty =
            DependencyProperty.Register(nameof(Information), typeof(string), typeof(GeorgePopup), new PropertyMetadata(string.Empty));


        //=================对外契约=====================
        //统一用Show(information,timespan) 尝试规整参数 在UI线程运行核心功能ShowCore
        public static void Show(string information, TimeSpan timespan)
        {
            if (!TryNormalizeParameters(information, timespan, out string message, out TimeSpan duration)) return;
            RunOnUiThread(() => ShowCore(message, duration));
        }


        //=================尝试规整参数=====================
        public static bool TryNormalizeParameters(string information, TimeSpan timespan, out string message, out TimeSpan duration)
        {
            message = information?.Trim() ?? string.Empty;
            duration = timespan > MinVisibleTime ? timespan : MinVisibleTime;
            return message.Length > 0;

        }
        //=================在UI线程运行=====================
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


        //=================ShowCore=====================
        //建立实例，调用present
        private static void ShowCore(string message, TimeSpan duration)
        {
            if (_georgePopup == null) _georgePopup = new GeorgePopup();
            if (_georgePopup.Child is not UIElement child) return;
            _georgePopup.Information = message;
            _georgePopup.Present(duration);
        }



        //=================编排=====================
        //视觉复位 确定显示位置 动画实现 计划隐藏
        private void Present(TimeSpan duration)
        {
            if (Child is not UIElement child) return;

            var token = ++_generation;

            ResetVisualState();
            DecidePlacement();

            var wasOpen = IsOpen;
            if (!wasOpen)
            {
                child.Opacity = 0;
                IsOpen = true;
                Animate(child, 0, 1, FadeInTime);
            }
            else
            {
                IsOpen = true;
            }

            SheduleHide(token,duration);
        }



        //=================视觉复位=====================
        private void ResetVisualState()
        {
            if (Child is not UIElement child) return;
            _hideTimer?.Stop();
            child.BeginAnimation(OpacityProperty, null);
            child.Opacity = 1;
        }
        //=================确定显示位置=====================
        private void DecidePlacement()
        {
            var app = Application.Current;
            if (app == null) return;

            Window? owner = FindOwnerWindow();

            if (owner != null && TryPlaceRelativeToWindow(owner)) return;

            PlaceAbsoluteToWorkArea();
        }

        private Window? FindOwnerWindow()
        {
            var app = Application.Current;
            if (app == null) return null;

            var active = app.Windows.OfType<Window>().Where(w => w.IsVisible && w.IsActive).FirstOrDefault();
            if (active != null) return active;

            var main = app.MainWindow;
            if (main != null && main.IsVisible && main.WindowState != WindowState.Minimized) return main;
            else return null;
        }

        private bool TryPlaceRelativeToWindow(Window owner)
        {
            if (owner == null) return false;
            if (Child is not UIElement child) return false;

            PlacementTarget = owner;
            Placement = PlacementMode.Center;
            HorizontalOffset = 0;
            VerticalOffset = owner.ActualHeight / 5.0;
            return true;
        }

        private void PlaceAbsoluteToWorkArea()
        {
            if (Child is not UIElement child) return;

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

        //=================动画实现=====================
        private void Animate(UIElement target, double from, double to, TimeSpan timeSpan, Action? action = null)
        {
            if (target is not UIElement) return;
            var animation = new DoubleAnimation(from, to, new Duration(timeSpan))
            {
                EasingFunction = new QuadraticEase()
                {
                    EasingMode = from > to ? EasingMode.EaseIn : EasingMode.EaseOut
                }
            };
            if (action != null)
            {
                animation.Completed += (s, e) => action();
            }
            target.BeginAnimation(OpacityProperty, animation);
        }

       

        //=================计划隐藏=====================
        private void SheduleHide(int token, TimeSpan duration)
        {
            _pendingToken = token;//Tick事件处理方法中需要用到token，但不方便传过去，于是用更高层变量暂存起来
            if (_hideTimer==null)
            {
                _hideTimer = new DispatcherTimer(DispatcherPriority.Normal);
                _hideTimer.Tick += OnHideTick;
            }
            _hideTimer.Interval = duration;
            _hideTimer.Start();
        }

        private void OnHideTick(object? sender, EventArgs e)
        {
            _hideTimer?.Stop();
            if (_generation != _pendingToken) return;//说明有新的present，放弃未完成动作
            BeginHide();
        }

        private void BeginHide()
        {
            var token = _generation;
            if (Child is not UIElement child) return;
            Animate(child, 1, 0, FadeOutTime, () =>
            {
                if (_generation != token) return;//说明有新的present,放弃未完成动作
                child.BeginAnimation(OpacityProperty, null);
                child.Opacity = 0;
                IsOpen = false;
                PlacementTarget = null;
            });
        }

        public GeorgePopup()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
