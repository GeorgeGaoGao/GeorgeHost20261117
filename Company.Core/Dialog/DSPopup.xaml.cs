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
    /// 全局复用的轻量提示框。
    ///
    /// 相对最初版本的关键改动：
    /// 1. 文本绑定改成构造函数里显式 Source，不再依赖 RelativeSource FindAncestor；
    /// 2. 不再依赖 Application.Current / MainWindow：没有 WPF 窗口时退化为屏幕坐标，保证一定显示；
    /// 3. 淡出用 DoubleAnimation，重复调用会打断上一次动画并复位透明度；
    /// 4. 定时器在 UI 线程懒创建，不是静态字段初始化；
    /// 5. 失败不再静默：写 Debug 输出（XAML 解析失败、Child 为空等）。
    /// </summary>
    public partial class DSPopup : Popup
    {
        private static DSPopup? _dialog;
        private static DispatcherTimer? _hideTimer;
        public DSPopup()
        {
            InitializeComponent();
            // 显式 Source：Popup 的内容会被托管到独立窗口，FindAncestor/DataContext 的解析链路
            // 依赖逻辑树细节；这里直接指定源，任何宿主下都能显示文字。
            MessageText.SetBinding(TextBlock.TextProperty, new Binding(nameof(Message)) { Source = this });
        }
        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(DSPopup),
                new PropertyMetadata(string.Empty));
        // 兼容旧调用：DSPopup.ShowPopup("保存成功", 2)
        public static void ShowPopup(string message, int seconds = 2)
        {
            Show(message, TimeSpan.FromSeconds(Math.Max(1, seconds)));
        }
        /// <summary>
        /// 显示提示。可从任意线程、任意时机调用（包括还没有任何窗口的时候）。
        /// </summary>
        /// <param name="message">提示文本。</param>
        /// <param name="duration">停留时长（含淡出前的全显示时间）。</param>
        public static void Show(string message, TimeSpan duration)
        {
            if (string.IsNullOrEmpty(message)) return;
            if (duration <= TimeSpan.Zero) duration = TimeSpan.FromSeconds(2);
            // Application 可能为 null（WinForms 宿主 / 控制台 / 单元测试）
            var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished) return;
            if (dispatcher.CheckAccess())
            {
                ShowCore(message, duration);
            }
            else
            {
                // 异步切线程：不阻塞调用方，也不会和 UI 线程互等
                dispatcher.BeginInvoke(new Action(() => ShowCore(message, duration)), DispatcherPriority.Normal);
            }
        }
        private static void ShowCore(string message, TimeSpan duration)
        {
            try
            {
                var popup = _dialog ??= new DSPopup();
                if (popup.Child is not UIElement child)
                {
                    // XAML 没有正确设置 Child（例如 x:Class / Build Action 配错）时最容易踩到
                    Debug.WriteLine("[DSPopup] Child is null, check DSPopup.xaml build action (should be Page).");
                    return;
                }
                // 打断上一次尚未结束的淡出，并把透明度复位，否则新消息会半透明地"闪一下"
                child.BeginAnimation(UIElement.OpacityProperty, null);
                child.Opacity = 1.0;
                popup.Message = message;
                Place(popup, child);
                popup.IsOpen = true;
                var timer = _hideTimer ??= new DispatcherTimer();
                timer.Stop();
                timer.Interval = duration;
                timer.Tick -= OnHideTick;
                timer.Tick += OnHideTick;
                timer.Start();
            }
            catch (Exception ex)
            {
                // 提示框失败不应影响业务，但必须留下痕迹，方便定位 XamlParseException 之类的问题
                Debug.WriteLine("[DSPopup] show failed: " + ex);
            }
        }
        /// <summary>
        /// 有 WPF 窗口就居中对齐它；没有窗口（WinForms 宿主、启动早期、设计器、单测）
        /// 就退化为屏幕坐标，保证 Popup 一定能定位、一定看得见。
        /// </summary>
        private static void Place(Popup popup, UIElement child)
        {
            var owner = FindOwnerWindow();
            if (owner != null)
            {
                popup.PlacementTarget = owner;
                popup.Placement = PlacementMode.Center;
                popup.VerticalOffset = owner.ActualHeight > 0 ? owner.ActualHeight / 3.0 : 0.0;
                return;
            }
            // 离线量一次拿真实尺寸（内容是自适应的）
            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var size = child.DesiredSize;
            var area = SystemParameters.WorkArea;
            popup.PlacementTarget = null;
            popup.Placement = PlacementMode.AbsolutePoint;
            popup.HorizontalOffset = area.Left + Math.Max(0, (area.Width - size.Width) / 2.0);
            popup.VerticalOffset = area.Top + Math.Max(0, (area.Height - size.Height) / 3.0);
        }
        private static Window? FindOwnerWindow()
        {
            var app = Application.Current;
            if (app is null) return null;
            var windows = app.Windows.OfType<Window>().Where(w => w.IsVisible).ToList();
            return windows.FirstOrDefault(w => w.IsActive)
                ?? (app.MainWindow != null && app.MainWindow.IsVisible ? app.MainWindow : null);
        }
        private static void OnHideTick(object? sender, EventArgs e)
        {
            _hideTimer?.Stop();
            if (_dialog?.Child is not UIElement child) return;
            var fade = new DoubleAnimation(child.Opacity, 0.0, TimeSpan.FromMilliseconds(250));
            fade.Completed += (_, _) =>
            {
                child.BeginAnimation(UIElement.OpacityProperty, null);
                child.Opacity = 1.0;
                if (_dialog is null) return;
                _dialog.IsOpen = false;
                _dialog.Message = string.Empty;
            };
            child.BeginAnimation(UIElement.OpacityProperty, fade);
        }
    }
}
