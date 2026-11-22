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

namespace Company.Core
{
    /// <summary>
    /// MyPopup.xaml 的交互逻辑
    /// </summary>
    /// 

    public partial class MyPopup : Popup
    {

        //可调参数
        private static readonly TimeSpan _minVisibleTime = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan _fadeInTime = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan _fadeOutTime = TimeSpan.FromMilliseconds(500);
        //单例状态
        private static MyPopup? _dialog;
        //实例状态
        private DispatcherTimer? _hideTimer;
        private int _generation;//每present一次+1，用来作废上一轮的回调
        private int _pendingToken;//当前计时器属于哪一轮
        public MyPopup()
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
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(MyPopup), new PropertyMetadata(string.Empty));
        /// <summary>
        /// L0 契约层
        /// </summary>
        /// <param name="message"></param>
        /// <param name="duration"></param>
        public static void Show(string message, TimeSpan duration)
        {
            if (!TryNormalizeParameters(message, duration, out var text, out var timespan)) return;
            RunOnUiThread(() => { ShowCore(text, timespan); });
        }

        private static void ShowCore(string text, TimeSpan timespan)
        {
            var popup = _dialog ??= new MyPopup();
            popup.Present(text, timespan);
        }
        /// <summary>
        /// 呈现
        /// </summary>
        /// <param name="text"></param>
        /// <param name="timespan"></param>
        private void Present(string text, TimeSpan timespan)
        {
            var token = ++_generation;//0.清场地。作废上一轮所有延迟回调 ？如何实现？

            Message = text;//1.内容
            ResetVisualState();//2.复位：停表，停动画，不透明度回1
            ApplyPlacement();//3.应用定位

            var wasOpen = IsOpen;//留存上一轮状态？
            IsOpen = true;//4.打开

            if (!wasOpen && this.Child is UIElement child) Animate(child, 0, 1, _fadeInTime, null);//5.淡入。若已开着，别闪。

            ScheduleHide(token, timespan);//6.安排隐藏
        }

        private void ScheduleHide(int token, TimeSpan timespan)
        {
            if (_hideTimer==null) 
            {
                _hideTimer = new DispatcherTimer(DispatcherPriority.Normal);
                _hideTimer.Tick += OnHideTick;
            }
            _hideTimer.Stop();
            _hideTimer.Interval= timespan;
            _pendingToken = token;
            _hideTimer.Start();
        }

        private void OnHideTick(object? sender, EventArgs e)
        {
            _hideTimer?.Stop();
            if (_pendingToken != _generation) return;//这条消息已被新的show取代
            BeginHide();
            
        }

        private void BeginHide()
        {
            if (this.Child is not UIElement child)
            {
                this.IsOpen = false;
                return;
            }
            var token = _generation;
            Animate(child, child.Opacity, 0, _fadeOutTime,
                () =>
                {
                    if (token != _generation) return;//淡出途中又来了新消息
                    child.BeginAnimation(OpacityProperty, null);
                    child.Opacity = 0;
                    this.IsOpen = false;//动画结束才关窗。
                }
                );
        }

        private static void Animate(UIElement target, double from, double to, TimeSpan duration, Action? onCompleted)
        {
            var animation = new DoubleAnimation(from, to, new Duration(duration))
            {
                EasingFunction=new QuadraticEase
                {
                    EasingMode=to < from ? EasingMode.EaseIn : EasingMode.EaseOut
                }
            };
            if (onCompleted is not null) animation.Completed += (_, __) => onCompleted();
           target.BeginAnimation(OpacityProperty, animation,HandoffBehavior.SnapshotAndReplace);
        }

       

        private void ApplyPlacement()
        {
            var owner = FindOwnerWindow();//owner有两种情况，一个是有合适的window返回值，另一个是null.有WINDOW返回值时再分了两种情况
            //有Window,用相对定位安排成功，就用相对定位。否则向下走绝对定位。
            if (owner != null && TryPlaceRelativeTo(owner)) return;

            //没有Window，走绝对定位
            PlaceOnWorkArea();//兜底：窗口不可用时，按工作区绝对定位。
        }

        private void PlaceOnWorkArea()
        {
            var workArea=SystemParameters.WorkArea;//DIP,主屏
            Size size = this.MeasureContent();

            this.PlacementTarget = null;
            this.Placement = PlacementMode.Absolute;
            this.HorizontalOffset = workArea.Left + Math.Max(0, workArea.Width - size.Width)/2;
            this.VerticalOffset=workArea.Top+Math.Max(0,workArea.Height- size.Height)/2;

        }
        /// <summary>
        /// 未打开时，Popup不在可视树里，只能手动Measure拿期望尺寸
        /// </summary>
        /// <returns></returns>
        private Size MeasureContent()
        {
            if (this.Child is not UIElement child) return Size.Empty;
            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return child.DesiredSize;
        }

        private bool TryPlaceRelativeTo(Window owner)
        {
            //还没布局完或最小化时，相对布局没有意义，交给兜底
            if (owner.ActualWidth <= 0 || owner.ActualHeight <= 0) return false;
            if (owner.WindowState == WindowState.Minimized) return false;
            
            this.PlacementTarget = owner;
            this.Placement = PlacementMode.Center;
            this.HorizontalOffset = 0;
            this.VerticalOffset = owner.ActualHeight / 3.0;//居中再往下挪三分之一窗高
            return true;
        }

        private static Window? FindOwnerWindow()
        {
            var app = Application.Current;
            if (app == null) return null;

            var activeWindow = app.Windows.OfType<Window>().FirstOrDefault(w => w.IsVisible && w.IsActive);
            if (activeWindow != null) return activeWindow;//找到可见并活跃的窗口。

            var main = app.MainWindow;
            return (main is not null && main.IsVisible) ? main : null;

        }

        /// <summary>
        /// L5 视觉复位。
        /// </summary>
        private void ResetVisualState()
        {
            _hideTimer?.Stop();
            if (this.Child is UIElement child)
            {
                child.BeginAnimation(OpacityProperty, null);//去掉动画才可以改值。
                child.Opacity = 1.0;
            }
        }

        /// <summary>
        /// L1 尝试规整参数
        /// </summary>
        /// <param name="message"></param>
        /// <param name="duration"></param>
        /// <param name="text"></param>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        private static bool TryNormalizeParameters(string message, TimeSpan duration, out string text, out TimeSpan timeSpan)
        {
            text = message?.Trim() ?? string.Empty;
            timeSpan = duration > _minVisibleTime ? duration : _minVisibleTime;
            return text.Length > 0;
        }

        /// <summary>
        /// L2 确保委托在UI线程运行
        /// </summary>
        /// <param name="action"></param>
        private static void RunOnUiThread(Action action)
        {
            var app = Application.Current;
            if (app == null) return;

            var dispatcher = app.Dispatcher;
            if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished) return;

            if (dispatcher.CheckAccess()) action();
            else
            {
                dispatcher.BeginInvoke(DispatcherPriority.Normal, action);
            }

        }


    }
}
