using System;
using System.IO;
using System.Security.Cryptography;

namespace TaiwuDecompiler.Utilities
{
    /// <summary>
    /// 散列计算实用工具类。
    /// </summary>
    class HashUtility
    {
        /// <summary>
        /// 计算一个文件的 SHA256 散列值。
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>十六进制字符串表示的该文件的 SHA256 散列值</returns>
        public static string GetFileSHA256(string path)
        {
            using (SHA256 digester = SHA256.Create())
            using (FileStream fileStream = File.OpenRead(path))
            {
                byte[] hash = digester.ComputeHash(fileStream);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }
    }
}
