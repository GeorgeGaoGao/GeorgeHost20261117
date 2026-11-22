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
    public partial class GeorgePopup : System.Windows.Controls.Primitives.Popup
    {
        // ---------- 字段 ----------
        private static GeorgePopup? _instance;   // L3 单例
        private DispatcherTimer? _hideTimer;     // L7
        private int _generation;                 // L7 最新代号
        private int _pendingToken;               // L7 待触发计时器的号

        private static readonly TimeSpan MinVisible = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan FadeInTime = TimeSpan.FromMilliseconds(150);
        private static readonly TimeSpan FadeOutTime = TimeSpan.FromMilliseconds(220);

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string),
                typeof(GeorgePopup), new PropertyMetadata(string.Empty));

        public string Message
        {
            get => (string?)GetValue(MessageProperty) ?? string.Empty;
            set => SetValue(MessageProperty, value);
        }

        // ================= 编排层：一次写对 =================

        // L0 唯一入口
        public static void Show(string? message, TimeSpan duration)
            => RunOnUiThread(() => ShowCore(message, duration));

        // L3 组装
        private static void ShowCore(string? message, TimeSpan duration)
        {
            if (!TryNormalize(message, duration, out var text, out var shownFor))
                return;

            _instance ??= new GeorgePopup();     // 必须在 UI 线程建，否则孤儿 HWND
            _instance.Message = text;
            _instance.Present(text, shownFor);
        }

        // L4 编排：顺序即契约
        private void Present(string text, TimeSpan duration)
        {
            var token = ++_generation;           // ① 领号

            ResetVisualState();                  // ② 清残留
            ApplyPlacement();                    // ③ 量尺寸+定坐标
            IsOpen = true;                       // ④ 打开

            if (Child is UIElement content)      // ⑤ 淡入
                Animate(content, 0, 1, FadeInTime, null);

            ScheduleHide(token, duration);       // ⑥ 计时
        }

        // L5 定位（调用链先钉死，算法留空）
        private void ApplyPlacement()
        {
            var owner = FindOwnerWindow();
            if (owner is not null && TryPlaceRelativeTo(owner)) return;
            PlaceOnWorkArea();
        }

        // ================= 叶子层：先写笨实现 =================

        // L1 输入闸门
        private static bool TryNormalize(string? message, TimeSpan duration,
                                         out string text, out TimeSpan shownFor)
        {
            text = message ?? string.Empty;      // 【笨】先全盘接受
            shownFor = duration;
            return true;
        }

        // L2 线程闸门
        private static void RunOnUiThread(Action action)
        {
            action();                            // 【笨】先假设总在 UI 线程
        }

        // L5 叶子
        private static Window? FindOwnerWindow() => null;                    // 【笨】
        private bool TryPlaceRelativeTo(Window owner) => false;              // 【笨】
        private void PlaceOnWorkArea() { }                                   // 【笨】用默认位置
        private Size MeasureContent() => Size.Empty;                         // 【笨】

        // L6 视觉
        private void ResetVisualState()
        {
            if (Child is UIElement c) c.Opacity = 1;   // 【笨】只保证不透明
        }

        private static void Animate(UIElement target, double from, double to,
                                    TimeSpan duration, Action? onCompleted)
        {
            target.Opacity = to;                 // 【笨】直接跳，先不淡
            onCompleted?.Invoke();
        }

        // L7 时序
        private void ScheduleHide(int token, TimeSpan duration)
        {
            _pendingToken = token;               // 【笨】登记了但先不启动
        }

        private void OnHideTick(object? sender, EventArgs e) { }
        private void BeginHide(int token) { }
    }




    /*

    /// <summary>
    /// GeorgePopup.xaml 的交互逻辑
    /// </summary>
    public partial class GeorgePopup : Popup
    {
        // ===== 可调参数 =====
        private static readonly TimeSpan MinVisible = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan FadeInTime = TimeSpan.FromMilliseconds(1000);
        private static readonly TimeSpan FadeOutTime = TimeSpan.FromMilliseconds(1000);

        // ===== 单例状态 =====
        private static GeorgePopup? _instance;

        // ===== 实例状态 =====
        private DispatcherTimer? _hideTimer;
        private int _generation;    // 每 Present 一次 +1，用来作废上一轮的回调
        private int _pendingToken;  // 当前计时器属于哪一轮

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(GeorgePopup),
                new PropertyMetadata(string.Empty));

        public GeorgePopup()
        {
            InitializeComponent();
            DataContext = this;
            StaysOpen = true;   // 提示窗不该抢焦点、也不该点外面就消失
        }

        // ---------------- L0：对外契约 ----------------
        public static void Show(string? message, TimeSpan duration)
        {
            if (!TryNormalize(message, duration, out var text, out var display)) return;
            RunOnUiThread(() => ShowCore(text, display));
        }

        // ---------------- L1：参数守卫（纯函数） ----------------
        private static bool TryNormalize(string? message, TimeSpan duration,
                                         out string text, out TimeSpan display)
        {
            text = message?.Trim() ?? string.Empty;
            display = duration < MinVisible ? MinVisible : duration;
            return text.Length > 0;   // 空消息不显示
        }

        // ---------------- L2：宿主/线程守卫 ----------------
        private static void RunOnUiThread(Action action)
        {
            var app = Application.Current;
            if (app is null) return;                       // 没有 Application 就没有消息循环

            var dispatcher = app.Dispatcher;
            if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished) return;

            if (dispatcher.CheckAccess()) action();
            else dispatcher.BeginInvoke(DispatcherPriority.Normal, action);
        }

        // ---------------- L3：实例 ----------------
        private static void ShowCore(string text, TimeSpan display)
        {
            var popup = _instance ??= new GeorgePopup();
            popup.Present(text, display);
        }

        // ---------------- L4：编排 =---------------
        private void Present(string text, TimeSpan display)
        {
            var token = ++_generation;   // 作废上一轮所有延迟回调

            Message = text;              // 4.1 内容
            ResetVisualState();          // 4.2 复位：停表、清动画、不透明度回 1
            ApplyPlacement();            // 4.3 定位

            var wasOpen = IsOpen;        // 4.4 打开
            IsOpen = true;

            if (!wasOpen && Child is UIElement child)   // 4.5 淡入（已开着就别闪）
                Animate(child, 0, 1, FadeInTime, null);

            ScheduleHide(token, display); // 4.6 计时
        }

        // ---------------- L5：定位策略 ----------------
        private void ApplyPlacement()
        {
            var owner = FindOwnerWindow();
            if (owner is not null && TryPlaceRelativeTo(owner)) return;

            PlaceOnWorkArea();   // 兜底：窗口不可用时按工作区绝对定位
        }

        private static Window? FindOwnerWindow()
        {
            var app = Application.Current;
            if (app is null) return null;

            var active = app.Windows.OfType<Window>()
                                      .FirstOrDefault(w => w.IsActive && w.IsVisible);
            if (active is not null) return active;

            var main = app.MainWindow;
            return (main is not null && main.IsVisible) ? main : null;
        }

        private bool TryPlaceRelativeTo(Window owner)
        {
            // 还没布局完 / 最小化时，相对定位没有意义 → 交给兜底
            if (owner.ActualWidth <= 0 || owner.ActualHeight <= 0) return false;
            if (owner.WindowState == WindowState.Minimized) return false;

            PlacementTarget = owner;
            Placement = PlacementMode.Center;
            HorizontalOffset = 0;
            VerticalOffset = owner.ActualHeight / 3.0;  // 居中再往下挪 1/3 窗高
            return true;
        }

        private void PlaceOnWorkArea()
        {
            var workArea = SystemParameters.WorkArea;   // DIP，主屏
            var size = MeasureContent();

            PlacementTarget = null;
            Placement = PlacementMode.Absolute;
            HorizontalOffset = workArea.Left + Math.Max(0, workArea.Width - size.Width) / 2;
            VerticalOffset = workArea.Top + Math.Max(0, workArea.Height - size.Height) / 2;
        }

        /// <summary>未打开时 Popup 不在可视树里，只能手动 Measure 拿期望尺寸。</summary>
        private Size MeasureContent()
        {
            if (Child is not FrameworkElement child) return Size.Empty;
            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return child.DesiredSize;
        }

        // ---------------- L6：视觉层 ----------------
        private void ResetVisualState()
        {
            _hideTimer?.Stop();

            if (Child is UIElement child)
            {
                child.BeginAnimation(UIElement.OpacityProperty, null); // 先摘动画
                child.Opacity = 1.0;                                   // 再设基础值
            }
        }

        private static void Animate(UIElement target, double from, double to,
                                    TimeSpan duration, Action? onCompleted)
        {
            var animation = new DoubleAnimation(from, to, new Duration(duration))
            {
                EasingFunction = new QuadraticEase
                {
                    EasingMode = to < from ? EasingMode.EaseIn : EasingMode.EaseOut
                }
            };

            if (onCompleted is not null)
                animation.Completed += (_, __) => onCompleted();

            target.BeginAnimation(UIElement.OpacityProperty, animation,
                                  HandoffBehavior.SnapshotAndReplace);
        }

        // ---------------- L7：计时层 ----------------
        private void ScheduleHide(int token, TimeSpan display)
        {
            if (_hideTimer is null)
            {
                _hideTimer = new DispatcherTimer(DispatcherPriority.Normal);
                _hideTimer.Tick += OnHideTick;
            }

            _hideTimer.Stop();
            _hideTimer.Interval = display;
            _pendingToken = token;
            _hideTimer.Start();
        }

        private void OnHideTick(object? sender, EventArgs e)
        {
            _hideTimer?.Stop();
            if (_pendingToken != _generation) return;  // 这条消息已被新的 Show 取代
            BeginHide();
        }

        private void BeginHide()
        {
            if (Child is not UIElement child)
            {
                IsOpen = false;
                return;
            }

            var token = _generation;
            Animate(child, child.Opacity, 0, FadeOutTime, () =>
            {
                if (token != _generation) return;      // 淡出途中又来了新消息

                child.BeginAnimation(UIElement.OpacityProperty, null);
                child.Opacity = 0;
                IsOpen = false;                        // 动画结束才关窗
            });
        }
    }


    */



}
