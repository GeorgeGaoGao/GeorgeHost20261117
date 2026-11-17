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
        /// <summary>
        /// 获取type上标注的所有ExposedServiceAttribute实例。正常为一个，但也有多个的可能。也可能没有。
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static IEnumerable<ExposedServiceAttribute> GetExposedServices(Type type)
        {
            return type.GetCustomAttributes<ExposedServiceAttribute>();
        }
        public static void RegisterAssembly(this IContainerRegistry containerRegistry, Assembly assembly)
        {
            var list = assembly.GetTypes();
            foreach (var type in list)
            {
                RegisterAssembly(containerRegistry, type);
            }
        }

        private static void RegisterAssembly(IContainerRegistry containerRegistry, Type type)
        {
            var attributeList = GetExposedServices(type);
            if (attributeList == null) return;
           
            foreach (var attribute in attributeList)
            {
                if (attribute.Lifetime==Lifetime.Singleton)//注册单例
                {
                    containerRegistry.RegisterSingleton(type);
                }
                foreach (var IType in attribute.Types)
                {
                    if (attribute.Lifetime==Lifetime.Singleton)
                    {
                        containerRegistry.RegisterSingleton(IType, type);//以接口注册单例
                    }
                    else if (attribute.Lifetime==Lifetime.Transient)
                    {
                        containerRegistry.Register(IType,type);//以接口注册多例
                    }
                }
            }
        }
    }
}
