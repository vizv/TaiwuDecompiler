using Microsoft.Win32;

namespace TaiwuDecompiler.Utilities
{
    /// <summary>
    /// 注册表实用工具。
    /// </summary>
    class RegistryUtility
    {
        /// <summary>
        /// 从注册表中读取键值。
        /// </summary>
        /// <typeparam name="T">值的类型</typeparam>
        /// <param name="baseKey">基础分支</param>
        /// <param name="subKey">子项</param>
        /// <param name="name">名称</param>
        /// <returns>对应键的值</returns>
        public static T GetValue<T>(RegistryKey baseKey, string subKey, string name)
        {
            RegistryKey fullKey = baseKey.OpenSubKey(subKey);
            if (fullKey == null) return default(T);
            return (T)fullKey.GetValue(name);
        }

        /// <summary>
        /// 注册表中本机项的实用工具。
        /// </summary>
        public static class LocalMachine
        {
            /// <summary>
            /// 从注册表中读取本机项的键值。
            /// </summary>
            /// <typeparam name="T">值的类型</typeparam>
            /// <param name="subKey">子项</param>
            /// <param name="name">名称</param>
            /// <returns>对应键的值</returns>
            public static T GetValue<T>(string subKey, string name)
            {
                RegistryKey localMachine = Registry.LocalMachine;
                return RegistryUtility.GetValue<T>(localMachine, subKey, name);
            }

            /// <summary>
            /// 从注册表中读取本机项的字符串键值。
            /// </summary>
            /// <param name="key">子项</param>
            /// <param name="name">名称</param>
            /// <returns>对应键的值</returns>
            public static string GetString(string key, string name)
            {
                return GetValue<string>(key, name);
            }
        }
    }
}
