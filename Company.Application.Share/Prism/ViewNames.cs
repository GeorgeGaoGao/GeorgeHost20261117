using System;
using System.Collections.Generic;
using System.Text;

namespace Company.Application.Share.Prism
{
    /// <summary>
    /// 与ModuleNames和RegionNames不同，那两个是人为命的名，需要各处统一。
    /// 而viewNames是各个类的名字，应当根据类名可以自动变化才合适。所以不要用这个类。
    /// 凡是用到某个view类名的地方，就用nameof来表达，避免view改名时不能自动改。
    /// </summary>
    public class ViewNames
    {
       
    }
}
