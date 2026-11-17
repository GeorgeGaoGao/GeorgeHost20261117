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
        public const string ApplicationLoginModule = "ApplicationLoginModule";
    }
}
