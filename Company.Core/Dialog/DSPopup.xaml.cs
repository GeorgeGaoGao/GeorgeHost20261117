using System;
using System.Collections.Generic;
using System.Numerics;
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
    /// DSPopup.xaml 的交互逻辑
    /// </summary>
    public partial class DSPopup : Popup
    {
        //===================可调参数=======================
        private static readonly TimeSpan MinVisible = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan FadeInTime = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan FadeOutTime = TimeSpan.FromMilliseconds(500);
        //===================单例状态=======================
        private static DSPopup? _dialog;
        //===================实例状态=======================
        private int _generation;
        private int _pendingToken;
        private DispatcherTimer? _hideTimer;
        //===================依赖属性=======================


        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Message.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(DSPopup), new PropertyMetadata(string.Empty));



        public DSPopup()
        {
            InitializeComponent();
            this.DataContext = this;
            StaysOpen = true;//不会因范围外占击鼠标而关闭。
        }
        //===================对外契约=======================
        /// <summary>
        /// 对外契约
        /// </summary>
        /// <param name="message">显示信息</param>
        /// <param name="timeSpan">完全显示时间</param>
        public static void Show(string message, TimeSpan timeSpan)
        {
            if (!TryNormalizeParameters(message, timeSpan, out string text, out TimeSpan duration)) return;
            RunOnUiThread(() => ShowCore(text, duration));
        }
        //===================尝试规整参数=======================
        private static bool TryNormalizeParameters(string message, TimeSpan timeSpan, out string text, out TimeSpan duration)
        {
            text = message?.Trim() ?? string.Empty;
            duration = timeSpan > MinVisible ? timeSpan : MinVisible;
            return text.Length > 0;
        }

        //===================UI线程运行=======================
        private static void RunOnUiThread(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

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

        //===================建立实例=======================
        private static void ShowCore(string text, TimeSpan duration)
        {
            var dialog = _dialog ??= new DSPopup();//UI线程运行保证了这个实例由UI线程创建，谁创建谁使用。
            if (dialog.Child is not UIElement) return;//这个判断放在这里比放在present里更好，
                                                      //简单直接。语义上也说得通，创建实例后补足信息。之后的present只
                                                      //用编排处理方法就行了。

            dialog.Message = text;//补足实例的显示信息。

            dialog.Present(duration);//后续不再需要用到text,后续方法都是实例方法，不用担心实例为空。
        }
        //===================编排=======================
        private void Present(TimeSpan duration)
        {
            var token = ++_generation;//每次展示取得令牌，记录轮次。作废上一轮所有延迟回调。

            ResetVisualState();//视觉复位。
            DecidePlacement();//确定展示位置。

            var wasOpen = IsOpen;//记录当前打开状态，后期用于判断是否需要执行淡入。

            if (!wasOpen && Child is UIElement child) 
            {
                child.Opacity = 0;
                IsOpen = true;
                Animate(child, 0, 1, FadeInTime);
            }
            IsOpen = true;
            ScheduleHide(token,duration);//按计划淡出
        }

        //===================视觉复位=======================

        private void ResetVisualState()
        {
            _hideTimer?.Stop();//如果不停，会如何？它有可能让本方法的结果作废吗？如果能，那就必须停。

            if (Child is not UIElement child) return;//作为一个独立可用的方法，这是它操作的前提，即便冗余，也是值得的。
            child.BeginAnimation(OpacityProperty, null);
            child.Opacity = 1.0;
        }

        //===================确定展示位置=======================

        private void DecidePlacement()
        {
            Window? owner = FindOwnerWindow();
            if (owner != null && TryPlaceRelativeToWindow(owner)) return;
          
            PlaceAbsoluteToWorkArea();
           
        }

        private Window? FindOwnerWindow()
        {
            var app = Application.Current;
            if (app == null) return null;//WPF世界不存在

            var active=app.Windows.OfType<Window>().Where(w => w.IsVisible && w.IsActive).FirstOrDefault();
            if (active != null) return active;//WPF世界里激活的窗口

            var main = app.MainWindow;
            return (main!=null&&main.IsVisible&&main.WindowState!=WindowState.Minimized) ? main:null;//主窗口,且没有最小化
           
        }
        private bool TryPlaceRelativeToWindow(Window? owner)
        {
            if (owner == null||!owner.IsVisible||owner.WindowState==WindowState.Minimized) return false;

            PlacementTarget = owner;
            Placement = PlacementMode.Center;
            HorizontalOffset = 0;
            VerticalOffset = owner.ActualHeight / 3.0;
            return true;
        }

        private void PlaceAbsoluteToWorkArea()
        {
            var workArea = SystemParameters.WorkArea;
            var size=MeasureContent();

            PlacementTarget = null;
            Placement = PlacementMode.Absolute;
            HorizontalOffset =workArea.Left+ Math.Max(0, workArea.Width - size.Width)/2.0;
            VerticalOffset =workArea.Top+ Math.Max(0,workArea.Height-size.Height)/2.0;
        }

        private Size MeasureContent()
        {
            if (Child is not UIElement child) return Size.Empty;
            
            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return child.DesiredSize;
        }

        //===================动画淡入淡出=======================

        private void Animate(UIElement target, double from, double to,TimeSpan duration, Action? onCompleted=null)
        {
            var animation = new DoubleAnimation(from, to,new Duration(duration))
            {
                EasingFunction = new QuadraticEase()
                {
                    EasingMode = from > to ? EasingMode.EaseOut : EasingMode.EaseIn//让明暗对比强烈处更平缓
                }
            };
            if (onCompleted != null) animation.Completed += (s, e) => onCompleted();
           
            target.BeginAnimation(OpacityProperty, animation);
        }
        //===================计划淡出=======================
        private void ScheduleHide(int token, TimeSpan duration)
        {
            if (_hideTimer == null)
            {
                _hideTimer = new DispatcherTimer(DispatcherPriority.Normal);
                _hideTimer.Tick += OnHideTick;
            }
            _hideTimer.Stop();
            _hideTimer.Interval = duration;
            _pendingToken = token;//延迟动作开始之前，记录token值。后续真正想比较的仍是token与_generation，
                                  //但困难在于这个token无法传递到OnHideTick中，所以用_pendingToken保存一下。
            _hideTimer.Start();
        }

        private void OnHideTick(object? sender, EventArgs e)
        {
            _hideTimer?.Stop();//只需要用时钟的第一次间隔。
            if (_generation != _pendingToken) return;//计时器开始了，经历了interval的延时时间，
                                                     //需要与出发时比较。现在_generation有新值了，
                                                     //说明有新的present执行，把上次未完的延迟动作丢弃。

            BeginHide();
        }

        private void BeginHide()
        {
            if (Child is not UIElement child) //防御性编程
            {
                IsOpen = false;
                return;
            }
            var token = _generation;//开始延时动作前，先记录令牌，用于动画结束后的处理方法中。
            Animate(child, 1, 0, FadeOutTime,
                () =>
                {
                    if (token!=_generation ) return;//令牌值改变了，说明又有新消息了，放弃掉未完成的延时动作。
                    child.BeginAnimation(OpacityProperty, null);
                    child.Opacity = 0;
                    IsOpen = false;
                    PlacementTarget = null;//释放对窗口的引用。
                }
            
                );//此处的onCompleted方法中，需要能用上token，所以用lambda更简单，否则需要把token作为参数带过去。还需要用上child.
        }
    }
}
