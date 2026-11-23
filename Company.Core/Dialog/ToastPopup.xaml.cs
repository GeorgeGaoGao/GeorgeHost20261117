using System;
using System.Collections.Generic;
using System.DirectoryServices;
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
    /// ToastPopup.xaml 的交互逻辑
    /// </summary>
    public partial class ToastPopup : Popup
    {

        //===============可调参数======================
        //不变，事实上是常量，用PASCAL命名。
        private static readonly TimeSpan MinVisible = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan FadeInTime = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan FadeOutTime = TimeSpan.FromMilliseconds(500);
        //===============单例状态======================
        private static ToastPopup? _dialog;
        //===============实例状态======================
        private DispatcherTimer? _hideTimer;
        private int _generation;//每present一次加一
        private int _pendingToken;//当前计时器属于哪一轮

        //===============依赖属性======================
        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Message.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(ToastPopup), new PropertyMetadata(string.Empty));


        public ToastPopup()
        {
            InitializeComponent();
            DataContext = this;
        }
        //===============对外契约======================
        public static void Show(string message, TimeSpan timeSpan)
        {
            if (!TryNormalizeParameters(message, timeSpan, out string text, out TimeSpan duration)) return;
            RunOnUiThread(() => ShowCore(text, duration));
        }
        //===============参数规整======================
        private static bool TryNormalizeParameters(string message, TimeSpan timeSpan, out string text, out TimeSpan duration)
        {
            text = message;
            duration = timeSpan;
            return true;
        }
        //===============UI线程======================
        private static void RunOnUiThread(Action action)
        {
            action();
        }

        //===============单例实例======================

        private static void ShowCore(string text, TimeSpan duration)
        {
            var popup = _dialog ??= new ToastPopup();
            popup.Present(text, duration);//开始用实例引用实例方法。
        }
        //===============编排方法======================

        private void Present(string text, TimeSpan duration)
        {
            var token = ++_generation;
            this.Message = text;
            ResetVisualState();
            ApplyPlacement();
            var wasOpen = IsOpen;
            IsOpen = true;

            if (!wasOpen && Child is UIElement child) Animate(child, 0, 1, FadeInTime, null);//已打开就不淡入。不处于打开，就开淡入。
            ScheduleHide(token, duration);
        }

       



        //===============视觉复位======================
        private void ResetVisualState()
        {
            if (this.Child is not UIElement child) return;
            child.BeginAnimation(OpacityProperty, null);
            child.Opacity = 1.0;
        }

        //===============安排位置======================
        /// <summary>
        /// 有窗口，就相对窗口安排。没窗口，就绝对定位。
        /// </summary>
        private void ApplyPlacement()
        {
            Window? owner = FindOwnerWindow();
            if (owner != null && this.TryPlaceRelativeTo(owner)) return;

            this.PlaceOnWorkArea();
        }
        /// <summary>
        /// 找到可附着的窗口。活动的可见的，主窗口，null
        /// </summary>
        /// <returns></returns>
        private static Window? FindOwnerWindow()//不涉及实例，可以写成静态。
        {
            var app = Application.Current;
            if (app == null) return null;

            var active = app.Windows.OfType<Window>().Where(w => w.IsActive && w.IsVisible).FirstOrDefault();
            if (active != null) return active;

            var main = app.MainWindow;
            return (main is not null && main.IsVisible) ? main : null;
        }
        /// <summary>
        /// 相对窗口安排。
        /// </summary>
        /// <param name="owner"></param>
        /// <returns></returns>
        private bool TryPlaceRelativeTo(Window owner)
        {
            //没有布局完成时，实际宽高为零。
            //最小化时也无法根据它来安排位置。
            if (owner.ActualWidth <= 0 || owner.ActualHeight <= 0) return false;
            if (owner.WindowState == WindowState.Minimized) return false;

            this.PlacementTarget = owner;
            this.Placement = PlacementMode.Center;
            this.HorizontalOffset = 0;
            this.VerticalOffset = owner.ActualHeight / 3.0;
            return true;
        }
        /// <summary>
        /// 工作区绝对定位安排。
        /// </summary>
        private void PlaceOnWorkArea()
        {
            var workArea = SystemParameters.WorkArea;
            Size size = this.MeasureContent();

            this.PlacementTarget = null;
            this.Placement = PlacementMode.Center;
            this.HorizontalOffset = Math.Max(0, workArea.Width - size.Width);
            this.VerticalOffset = Math.Max(0, workArea.Height - size.Height);

        }
        /// <summary>
        /// 测量控件内容的大小
        /// </summary>
        /// <returns>返回内容的大小</returns>
        /// <exception cref="NotImplementedException">未实现</exception>
        private Size MeasureContent()
        {
            if (Child is not UIElement child) return Size.Empty;
            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return child.DesiredSize;

        }
        //===============淡入淡出======================
        private static void Animate(UIElement target, double from, double to, TimeSpan duration, Action? onCompleted)
        {
            var animation = new DoubleAnimation(from, to, duration)
            {
                EasingFunction = new QuadraticEase()
                {
                    EasingMode = from > to ? EasingMode.EaseOut : EasingMode.EaseIn//Easein缓动发生在开头，easeout缓动发生在结尾。
                }
            };
            if (onCompleted != null) animation.Completed += (s, e) => onCompleted();
            target.BeginAnimation(OpacityProperty, animation);
        }
        //===============计时隐藏======================
        private void ScheduleHide(int token, TimeSpan duration)
        {
            if (_hideTimer==null)
            {
                _hideTimer = new DispatcherTimer(DispatcherPriority.Normal);
                _hideTimer.Tick += OnHideTick;//在创建时加上事件处理方法，只用加这一次。
            }
            _hideTimer.Stop();
            _hideTimer.Interval = duration;
            _pendingToken = token;
            _hideTimer.Start();
        }

        private void OnHideTick(object? sender, EventArgs e)
        {
            _hideTimer.Stop();
            if (_pendingToken != _generation) return;//这条消息已被新的SHOW取代，为什么？
            BeginHide();
        }

        private void BeginHide()
        {
            if (this.Child is not UIElement child)
            {
                IsOpen = false;
                return;
            }
            var token = _generation;
            Animate(child, 1, 0, FadeOutTime, () =>
            {
                if (token != _generation) return;//淡出途中又来了新消息。
                child.BeginAnimation(OpacityProperty, null);
                child.Opacity = 0;
                IsOpen = false;//动画结束才关窗。

            });
        }
    }
}
