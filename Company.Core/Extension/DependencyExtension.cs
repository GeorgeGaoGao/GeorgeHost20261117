using Company.Core.IOC;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Company.Core.Extension
{
    public static class DependencyExtension
    {
        #region RegisterAssembly 
        /// <summary>
        /// IContainerRegistry的扩展方法。把程序集中的有ExposedServiceAttribute特性的所有类注册进容器。
        /// </summary>
        /// <param name="containerRegistry"></param>
        /// <param name="assembly"></param>
        public static void RegisterExposedServiceTypesForAssembly(this IContainerRegistry containerRegistry, Assembly assembly)
        {
            IEnumerable<Type> list = GetExposedServiceTypesFromAssembly(assembly);

            foreach (var type in list)
            {
                //把一个type注册进容器。
                RegisterOneType(containerRegistry, type);
            }
        }
        /// <summary>
        /// //找出assembly中所有加注了要求特性的类。
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        private static IEnumerable<Type> GetExposedServiceTypesFromAssembly(Assembly assembly)
        {
            
            return assembly.GetTypes().Where
                (t =>
                t != null
                && t.IsClass
                && !t.IsAbstract
                && t.CustomAttributes.Any(a => a.AttributeType == typeof(ExposedServiceAttribute)));
        }

        /// <summary>
        /// 把一个type注册进容器。
        /// </summary>
        /// <param name="containerRegistry"></param>
        /// <param name="type"></param>
        private static void RegisterOneType(IContainerRegistry containerRegistry, Type type)
        {
            //找出该类上所有的ExposedServiceAttribute实例。
            var attributeList = type.GetCustomAttributes<ExposedServiceAttribute>();
            if (attributeList == null) return;

            foreach (var attribute in attributeList)
            {
                if (attribute.Lifetime == Lifetime.Singleton)//注册单例
                {
                    containerRegistry.RegisterSingleton(type);
                }
                foreach (var IType in attribute.Types)
                {
                    if (attribute.Lifetime == Lifetime.Singleton)
                    {
                        containerRegistry.RegisterSingleton(IType, type);//以接口注册单例
                    }
                    else if (attribute.Lifetime == Lifetime.Transient)
                    {
                        containerRegistry.Register(IType, type);//以接口注册多例
                    }
                }
            }
        }
        #endregion

        #region InitializeAssembly
        /// <summary>
        /// IContainerProvider的扩展方法。初始化程序集中所有标注为ExposedServiceAttriubute特性的类，要求单例具自动加载AutoInitialize=true
        /// </summary>
        /// <param name="containerProvider"></param>
        /// <param name="assembly"></param>
        public static void InitializeExposedServiceTypesForAssembly(this IContainerProvider containerProvider,Assembly assembly)
        {
            IEnumerable<Type> typeList = GetExposedServiceTypesFromAssembly(assembly);
            foreach (var type in typeList)
            {
                InitializeOneType(containerProvider, type);
            }
        }
        /// <summary>
        /// InitializeAssembly的重载方法。把标注AutoInitialize特性的一个类初始化。
        /// </summary>
        /// <param name="containerProvider"></param>
        /// <param name="type"></param>
        /// <exception cref="NotImplementedException"></exception>
        private static void InitializeOneType(IContainerProvider containerProvider, Type type)
        {
            var attributeList = type.GetCustomAttributes<ExposedServiceAttribute>();
            foreach (var attribute in attributeList)
            {
                if (attribute.Lifetime == Lifetime.Singleton && attribute.AutoInitialize)
                {
                    containerProvider.Resolve(type);
                }
            }
        }

       #endregion
    }
}
