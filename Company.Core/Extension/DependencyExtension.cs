using Company.Core.IOC;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Company.Core.Extension
{
    public static class DependencyExtension
    {
        private static List<Type> GetTypes(Assembly assembly)
        {
            var list = assembly.GetTypes().Where(t => t != null && t.IsClass && !t.IsAbstract
            && t.CustomAttributes.Any(a => a.AttributeType == typeof(ExposedServiceAttribute))).ToList();

            return list;
        }
        private IEnumerable<ExposedServiceAttribute> GetExposedServices(Type type)
        {

        }
        public static void RegisterAssembly(IContainerRegistry containerRegistry, Assembly assembly)
        {
            var list = assembly.GetTypes();
            foreach (var type in list)
            {
                RegisterAssembly(containerRegistry, type);
            }
        }

        private static void RegisterAssembly(IContainerRegistry containerRegistry, Type type)
        {
            
        }
    }
}
