using Company.Core.IOC;
using ReactiveUI;
using ReactiveUI.Binding;
using ReactiveUI.SourceGenerators;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Company.Applications.Main.Models
{
    /// <summary>
    /// 继承了一个带观察者的接口，别的模块就可以从IOC中以接口的方法，得到当前类型的实例
    /// </summary>
    [ExposedService(Lifetime.Singleton, typeof(IMouseWorkModeProvider), typeof(IZoomModeProvider))]
    public partial class MainModel : ReactiveObject, IMouseWorkModeProvider, IZoomModeProvider
    {
        /// <summary>
        /// 鼠标工作模式
        /// </summary>
        [Reactive]
        public partial MouseWorkMode MouseWorkMode { get; set; }
        /// <summary>
        /// 鼠标工作模式的观察者
        /// </summary>
        public IObservable<MouseWorkMode> MouseWorkObservable { get; }
        [Reactive]
        public partial ZoomMode ZoomMode { get; set; }

        public IObservable<ZoomMode> ZoomModeObservable => this.WhenAnyValue(x => x.ZoomMode);

        public MainModel()
        {
            ZoomMode = ZoomMode.Uniform;
            MouseWorkMode= MouseWorkMode.默认操作;
            MouseWorkObservable = this.WhenAnyValue(x => x.MouseWorkMode);
        }
    }

    public interface IMouseWorkModeProvider { }
    public interface IZoomModeProvider { }
    public enum MouseWorkMode
    {
        默认操作,
        其它操作
    }
    public enum ZoomMode
    {
        Uniform,
        Maximum,
    }
}
