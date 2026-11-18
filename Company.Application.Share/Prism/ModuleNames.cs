using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Company.Application.Share.Prism
{
    public class ModuleNames
    {
        //public const string ModulePath = @".\Modules";
        public static readonly string ModulePath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules");
        //ModuleName是人为指定的，在[Module]中指定的，只要特性中不改，就能正确对应到模块，
        //即使模块类的name改变也没关系。所以用const string是好用的。
        public const string ApplicationMainModule = "ApplicationMainModule";
        public const string ApplicationLoginModule = "ApplicationLoginModule";
        public const string CoreModule = "CoreModule";
    }
}
