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
    /// BusyIndicator.xaml 的交互逻辑
    /// </summary>


    /*
     * 需求（R1–R10）
R1 契约：静态方法 BusyIndicator.Begin(string? message = null) 与 BusyIndicator.End()，可在任意线程调用。
R2 计数：Begin 加一，End 减一。只有计数回到 0 才可能隐藏。End 多于 Begin 时忽略，计数不得为负。
R3 延迟出现：第一次 Begin 后 300ms 内就归零的，从头到尾不显示。
R4 最短显示：一旦显示，从出现那一刻起至少 800ms 才允许开始淡出。
R5 动画：淡入 150ms，淡出 250ms。已显示时再 Begin，不重播淡入，只更新文案。
R6 中断：淡出过程中来 Begin，必须取消淡出、立即恢复完全可见，且那次淡出不得再关窗。
R7 线程：非 UI 线程调用自动切到 UI 线程；无 Application 或已关闭时静默返回。
R8 定位：有活动窗口则相对窗口正中；没有则屏幕工作区正中。
R9 文案：空白文案用 "正在处理…"；已显示时新文案立即生效。
R10 收尾：关窗后不留下还在跑的计时器或动画。
不要求转圈动画，视觉上有个圆角卡片 + 文案就够。不要加新控件，就用你手上这套。

动手前，先在注释里回答这 6 个问题
计数为 0、1、2 时，各有哪些延迟动作可能在执行？分别是什么？
“延迟出现”的计时器在什么时候必须作废？作废的手段是什么？
你的“代号”在什么时机递增？为什么不是每次 Begin 都递增？
最短显示时间用时间戳算，还是用一个“到点才允许隐藏”的计时器 gate？各自的失效条件是什么？
淡出被打断时，除了改透明度，还有哪两处状态必须处理？
哪一处代码必须判 End 是否多余？如果漏了会怎样？


    测试用例 Begin(); 100ms; End();	全程不出现
     */

    /// <summary>
    /// 全局忙碌提示。Begin/End 成对调用，支持嵌套；延迟出现 + 最短显示，避免闪烁。
    /// </summary>
    public partial class BusyIndicator : Popup
    {
        //===============可调参数======================
        private static readonly TimeSpan ShowDelay = TimeSpan.FromMilliseconds(300);
        private static readonly TimeSpan MinVisible = TimeSpan.FromMilliseconds(800);
        private static readonly TimeSpan FadeInTime = TimeSpan.FromMilliseconds(150);
        private static readonly TimeSpan FadeOutTime = TimeSpan.FromMilliseconds(250);
        private const string FallbackText = "正在处理…";

        //===============单例状态======================
        private static BusyIndicator? _instance;

        //===============实例状态======================
        private int _count;                                   // 还没 End 的 Begin 数
        private int _generation;                              // 计数跨越 0 时翻页
        private DispatcherTimer? _timer;                      // 同一时刻只排一个延迟动作
        private readonly Stopwatch _visibleClock = new();     // 单调时钟，算最短显示   这是字段，所以不会每次都new.如果用=>，那就变成了属性，也就是方法，那就会每次都new

        //===============依赖属性======================
        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(BusyIndicator),
                new PropertyMetadata(FallbackText));

        public BusyIndicator()
        {
            InitializeComponent();
            DataContext = this;
        }

        //===============对外契约======================
        public static void Begin(string? message = null)
        {
            RunOnUiThread(() => Instance.BeginCore(message));
        }

        public static void End()
        {
            RunOnUiThread(() => Instance.EndCore());
        }

        private static BusyIndicator Instance => _instance ??= new BusyIndicator();

        //===============UI线程======================
        private static void RunOnUiThread(Action action)
        {
            var app = Application.Current;
            if (app == null) return;
            var dispatcher = app.Dispatcher;
            if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished) return;

            if (dispatcher.CheckAccess()) action();
            else dispatcher.BeginInvoke(action, DispatcherPriority.Normal);
        }

        //===============编排：开始======================
        private void BeginCore(string? message)
        {
            Message = string.IsNullOrWhiteSpace(message) ? FallbackText : message.Trim();//每次进来都会更新方案，
                                                                                         //这个方案在哪里会被显示？后面的open(token)是唯一显示的地方。
                                                                                         //_count记录使用者人数。只有等于0时才可以开窗
                                                                                         //IsOpen负责开窗。
                                                                                         //
            if (_count > 0)
            {
                _count++;               // 已有一轮在跑：只更新文案，不动计时器   _count++，只登记多了一个使用者。
                return;
            }

            _count = 1;
            var token = ++_generation;  // 0 → 1：新的一轮，作废上一轮所有在途回调。
                                        // 因为每一个延迟回调里都会判断_generation是否还等于当初记下的token，不等就会return
                                        // 所以这里++_generation就会让那些延迟动作不再完全执行。

            if (IsOpen)
            {
                RestoreVisible();       // 正在淡出：拉回来    现在的使用者是第一人，但之前的动作没结束，所以恢复全显示，但方案已修改。
                return;
            }
            StartTimer(ShowDelay, token, () => Open(token));//这里唯一可能显示文案，第一人，且已无开窗，才能进入，开启定时器，准备显示。

            /*
             * if (_count > 0) { _count++; return; }        // 是新一轮还是加入
if (IsOpen) { RestoreVisible(); return; }    // 上一轮在收尾吗
if (--_count > 0) return;                    // 我是最后一个吗
if (!IsOpen) return;                         // 到底显示过没有
             */

        }

        //===============编排：结束======================
        private void EndCore()
        {
            if (_count == 0) return;    // 多余的 End：忽略，计数不允许为负
            if (--_count > 0) return;   // 还有人没收工
            //以上两条，就只保留了一个可能性就是最初_count==1,只有一个使用者的情况。在这种情况下，_count==0

            var token = ++_generation;  // 1 → 0：本轮结束
            CancelTimer();              // 作废那个还没到点的“延迟显示”

            if (!IsOpen) return;        // 从头到尾没显示过，什么都不用做

            var remaining = MinVisible - _visibleClock.Elapsed;
            if (remaining <= TimeSpan.Zero) BeginHide(token);
            else StartTimer(remaining, token, () => BeginHide(token));
        }

        //===============真正显示======================
        private void Open(int token)
        {
            if (token != _generation) return;       // 等待期间已经 End 了
            if (Child is not UIElement child) return;

            ApplyPlacement();
            child.BeginAnimation(OpacityProperty, null);
            child.Opacity = 0;                      // 先归零再开窗，避免第一帧闪一下

            IsOpen = true;
            _visibleClock.Restart();
            Animate(child, 0, 1, FadeInTime, null);
        }

        private void RestoreVisible()
        {
            if (Child is not UIElement child) return;
            child.BeginAnimation(OpacityProperty, null);   // 掐掉淡出
            child.Opacity = 1;
        }

        private void BeginHide(int token)
        {
            if (token != _generation) return;
            if (Child is not UIElement child)
            {
                IsOpen = false;
                return;
            }

            Animate(child, 1, 0, FadeOutTime, () =>
            {
                if (token != _generation) return;   // 淡出途中又 Begin 了
                child.BeginAnimation(OpacityProperty, null);
                child.Opacity = 0;
                IsOpen = false;
                _visibleClock.Reset();
            });
        }

        //===============计时器======================
        private void StartTimer(TimeSpan after, int token, Action action)
        {
            CancelTimer();
            var timer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = after };//每次都开启一个新的定时器。
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                if (_timer != timer) return;        // 已被更晚的安排替换  //_timer这时不确定是null吗？
                _timer = null;
                if (token != _generation) return;   // 这一轮已作废
                action();//计时器的TICK决定了要不要走后续处理，后续具体处理不是starttimer关心的事。
            };
            _timer = timer;
            timer.Start();
        }

        private void CancelTimer()
        {
            _timer?.Stop();
            _timer = null;
        }

        //===============定位======================
        private void ApplyPlacement()
        {
            var owner = FindOwnerWindow();
            if (owner != null
                && owner.ActualWidth > 0
                && owner.ActualHeight > 0
                && owner.WindowState != WindowState.Minimized)
            {
                PlacementTarget = owner;
            }
            else
            {
                PlacementTarget = null;   // 没有目标时相对屏幕居中
            }

            Placement = PlacementMode.Center;
            HorizontalOffset = 0;         // Center 模式下，居中就是零偏移
            VerticalOffset = 0;
        }

        private static Window? FindOwnerWindow()
        {
            var app = Application.Current;
            if (app == null) return null;

            var active = app.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive && w.IsVisible);
            if (active != null) return active;

            var main = app.MainWindow;
            return (main is not null && main.IsVisible) ? main : null;
        }

        //===============动画======================
        private static void Animate(UIElement target, double from, double to, TimeSpan duration, Action? onCompleted)
        {
            var animation = new DoubleAnimation(from, to, duration)
            {
                EasingFunction = new QuadraticEase
                {
                    EasingMode = from < to ? EasingMode.EaseOut : EasingMode.EaseIn
                }
            };
            if (onCompleted != null) animation.Completed += (s, e) => onCompleted();
            target.BeginAnimation(OpacityProperty, animation);
        }
    }
}
