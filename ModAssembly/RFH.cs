using System;
using System.Reflection;
using Cysharp.Threading.Tasks;

namespace Add_Custom_Teleport_Point
{
    // 反射获取私有变量、属性、方法
    // ai写的，我做了一点修改，不好用，但能用
    // GetPrivateProperty 获取私有属性
    // GetPrivateMethod 获取私有方法
    // GetFieldValue 获取私有变量
    // SetFieldValue 设置私有变量的值
    // InvokePrivateMethod 执行私有函数
    public static class RFH
    {
        public static Delegate[] GetRegisteredDelegates(Type type, string eventName,object? instance = null)
        {
            FieldInfo field = type.GetField(eventName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (field == null) return new Delegate[0];
            var eventDelegate = field.GetValue(instance) as Delegate;
            if (eventDelegate == null) return new Delegate[0];
            return eventDelegate.GetInvocationList();
        }
        
        public static FieldInfo GetPrivateField(Type type, string fieldName)
        {
            return type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        }
        
        public static PropertyInfo GetPrivateProperty(Type type, string propertyName)
        {
            return type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        }
        
        public static MethodInfo GetPrivateMethod(Type type, string methodName)
        {
            return type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        }

        public static object? GetFieldValue(object obj, string fieldName)
        {
            FieldInfo field = GetPrivateField(obj.GetType(), fieldName);
            return field?.GetValue(obj);
        }

        public static void SetFieldValue(object obj, string fieldName, object value)
        {
            FieldInfo field = GetPrivateField(obj.GetType(), fieldName);
            field?.SetValue(obj, value);
        }

        public static void InvokePrivateMethod(object obj, string methodName, params object[] parameters)
        {
            MethodInfo method = GetPrivateMethod(obj.GetType(), methodName);
            if (method == null) throw new Exception("no method");
            object[] finalParameters = HandleDefaultParameters(method, parameters);
            method.Invoke(obj, finalParameters);
        }

        public static T InvokePrivateMethod<T>(object obj, string methodName, params object[] parameters)
        {
            MethodInfo method = GetPrivateMethod(obj.GetType(), methodName);
            if (method == null) throw new Exception("no method");
            object[] finalParameters = HandleDefaultParameters(method, parameters);
            return (T)method.Invoke(obj, finalParameters);
        }

        public static UniTask InvokePrivateMethodUniTask(object obj, string methodName, params object[] parameters)
        {
            MethodInfo method = GetPrivateMethod(obj.GetType(), methodName);
            if (method == null) throw new Exception("no method");
            object[] finalParameters = HandleDefaultParameters(method, parameters);
            var result = method.Invoke(obj, finalParameters);
            if (result is UniTask uniTask) return uniTask;
            else throw new Exception("not UniTask");
        }

        public static UniTask<T?> InvokePrivateMethodUniTask<T>(object obj, string methodName, params object[] parameters)
        {
            MethodInfo method = GetPrivateMethod(obj.GetType(), methodName);
            if (method == null) throw new Exception("no method");
            object[] finalParameters = HandleDefaultParameters(method, parameters);
            var result = method.Invoke(obj, finalParameters);
            if (result is UniTask<T?> uniTaskT) return uniTaskT;
            if (result is UniTask uniTask) throw new Exception("not UniTask<T?> instead UniTask");
            else throw new Exception("not UniTask<T?>");
        }

        public static object[] HandleDefaultParameters(MethodInfo method, object[] providedParams)
        {
            ParameterInfo[] methodParams = method.GetParameters();
            // 如果提供的参数数量已经匹配，直接返回
            if (providedParams != null && providedParams.Length == methodParams.Length)
            {
                return providedParams;
            }

            // 创建包含默认值的完整参数数组
            object[] finalParams = new object[methodParams.Length];

            for (int i = 0; i < methodParams.Length; i++)
            {
                if (providedParams != null && i < providedParams.Length)
                {
                    // 使用提供的参数
                    finalParams[i] = providedParams[i];
                }
                else
                {
                    // 使用参数的默认值
                    if (methodParams[i].DefaultValue != DBNull.Value)
                    {
                        finalParams[i] = methodParams[i].DefaultValue;
                    }
                    else
                    {
                        // 没有默认值，使用类型默认值
                        finalParams[i] = GetDefaultValue(methodParams[i].ParameterType)!;
                    }
                }
            }

            return finalParams;
        }

        public static object? GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        public static bool HasMethod(Type type, string methodName)
        {
            return type.GetMethod(methodName, Type.EmptyTypes) != null;
        }
    }
    }
