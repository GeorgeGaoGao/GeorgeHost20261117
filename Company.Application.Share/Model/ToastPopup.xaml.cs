using ReactiveUI.Primitives.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ReactiveUI;
using System.Reactive.Linq;

namespace Company.Applications.Share.Model
{
    /// <summary>
    /// 轻量级 Toast 提示：显示后停留 2 秒，然后淡出。
    /// </summary>
    public partial class ToastPopup : UserControl
    {
        // 依赖属性一：显示的信息
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(
                nameof(Message),
                typeof(string),
                typeof(ToastPopup),
                new PropertyMetadata(string.Empty));

        // 依赖属性二：显示的不透明度
        // 注意：不能命名为 Opacity，因为 UIElement 已经定义了 Opacity 依赖属性，会冲突。
        public static readonly DependencyProperty ToastOpacityProperty =
            DependencyProperty.Register(
                nameof(ToastOpacity),
                typeof(double),
                typeof(ToastPopup),
                new PropertyMetadata(1.0));

        private static readonly TimeSpan DisplayDuration = TimeSpan.FromSeconds(2);   // 停留 2 秒
        private static readonly TimeSpan FadeOutDuration = TimeSpan.FromMilliseconds(400); // 淡出时长
        private static readonly TimeSpan FadeInDuration = TimeSpan.FromMilliseconds(150); // 淡入时长

        private IDisposable _hideSubscription;   // 保存定时器订阅，便于取消

        public ToastPopup()
        {
            InitializeComponent();
            // 让控件内部（包括 Popup 内的）绑定都指向控件自身。
            // Popup 的内容处于独立的可视化树，用 DataContext 继承比 RelativeSource/ElementName 更可靠。
            DataContext = this;
        }

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public double ToastOpacity
        {
            get => (double)GetValue(ToastOpacityProperty);
            set => SetValue(ToastOpacityProperty, value);
        }

        /// <summary>显示一条提示，2 秒后自动淡出。</summary>
        public void Show(string message)
        {
            // 取消上一次尚未触发的定时器（防止连续调用时旧定时器把新提示关掉）
            _hideSubscription?.Dispose();

            Message = message;
            RootPopup.IsOpen = true;

            FadeIn();

            // ReactiveUI 生态的 Rx：2 秒后触发隐藏
            _hideSubscription = Observable
                .Timer(DisplayDuration)
                .Subscribe(_ => FadeOut());
        }

        private void FadeIn()
        {
            BeginAnimation(ToastOpacityProperty, null); // 清掉上次的动画
            ToastOpacity = 0.0;
            Animate(0.0, 1.0, FadeInDuration, null);
        }

        private void FadeOut()
        {
            Animate(ToastOpacity, 0.0, FadeOutDuration, () =>
            {
                RootPopup.IsOpen = false;

                // 清除动画并复位，供下一次显示
                BeginAnimation(ToastOpacityProperty, null);
                ToastOpacity = 1.0;
            });
        }

        private void Animate(double from, double to, TimeSpan duration, Action completed)
        {
            var animation = new DoubleAnimation(from, to, duration)
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            if (completed != null)
                animation.Completed += (_, _) => completed();

            BeginAnimation(ToastOpacityProperty, animation);
        }

        // 控件从界面卸载时（如页面导航走）清理资源，避免定时器在背后触发
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _hideSubscription?.Dispose();
            RootPopup.IsOpen = false;
        }
    }
}
