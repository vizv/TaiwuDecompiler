using System;
using System.IO;
using TaiwuDecompiler.Utilities;

namespace TaiwuDecompiler
{
    /// <summary>
    /// 反编译器主类。
    /// </summary>
    class Program
    {
        /// <summary>
        /// 太吾绘卷的 Steam 游戏 ID，用于通过注册表获取游戏路径。
        /// </summary>
        private const int STEAM_GAME_ID = 838350;

        /// <summary>
        /// 原始的 Assembly 文件名。
        /// </summary>
        private const string ORIGINAL_FILENAME = "Assembly-CSharp.dll";

        /// <summary>
        /// 脱壳后的 Assembly 文件名。
        /// </summary>
        private const string UNPACKED_FILENAME = "Assembly-CSharp-{0}.dll";

        /// <summary>
        /// 反编译器入口方法。
        /// </summary>
        /// <param name="args">参数列表（未使用）</param>
        static void Main(string[] args)
        {
            string gamePath = getSteamGameInstallLocation(STEAM_GAME_ID);
            Console.Error.WriteLine(string.Format("游戏路径: {0}", gamePath)); // FIXME: Debug
            string originalAssemblyPath = getAssemblyPath(gamePath);
            string originalAssemblyHash = HashUtility.GetFileSHA256(originalAssemblyPath);
            Console.Error.WriteLine(string.Format("Assembly 散列: {0}", originalAssemblyHash)); // FIXME: Debug
            string monoLibraryPath = getMonoPath(gamePath);
            byte[] unpackedAssemblyData = AssemblyUtility.LoadImage(monoLibraryPath, originalAssemblyPath);
            Console.Error.WriteLine(string.Format("脱壳后的 Assembly 大小: {0}", unpackedAssemblyData.Length)); // FIXME: Debug
            string unpackedAssemblyPath = originalAssemblyPath.Replace(ORIGINAL_FILENAME, string.Format(UNPACKED_FILENAME, originalAssemblyHash));
            Console.Error.WriteLine(string.Format("保存脱壳后的 Assembly 到: {0}", unpackedAssemblyPath)); // FIXME: Debug
            File.WriteAllBytes(unpackedAssemblyPath, unpackedAssemblyData);
            // TODO: 调用 dnspy（使用参数：--spaces 2 --no-tokens）去反编译 Assembly-CSharp-Unpacked-<SHA256>.dll，并输出到临时目录
            // TODO: 清理非游戏文件
            Console.ReadKey(); // FIXME: Debug
        }

        /// <summary>
        /// 从注册表获取特定 Steam 游戏的安装路径。
        /// </summary>
        /// <param name="gameId">Steam 游戏 ID</param>
        /// <returns>安装路径</returns>
        private static string getSteamGameInstallLocation(uint gameId)
        {
            string steamGameRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App " + gameId;
            string steamGameInstallLocationName = "InstallLocation";

            string installLocation = RegistryUtility.LocalMachine.GetString(steamGameRegistryKey, steamGameInstallLocationName);
            return installLocation;
        }

        /// <summary>
        /// 获取 Assembly 的路径。
        /// </summary>
        /// <param name="baseDirectory">搜索基础路径</param>
        /// <returns>原始 Assembly 的路径</returns>
        private static string getAssemblyPath(string baseDirectory)
        {
            const string MANAGED_DIR = "Managed";
            string[] matches = Directory.GetDirectories(baseDirectory, MANAGED_DIR, SearchOption.AllDirectories);
            if (matches.Length != 1)
            {
                string message = matches.Length == 0 ? "没有找到 {0} 目录" : "找到多个 {0} 目录";
                throw new Exception(string.Format(message, MANAGED_DIR));
            }
            return Path.Combine(matches[0], ORIGINAL_FILENAME);
        }

        /// <summary>
        /// 获取 Mono 动态链接库的路径。
        /// </summary>
        /// <param name="baseDirectory">搜索基础路径</param>
        /// <returns>Mono 动态链接库的路径</returns>
        private static string getMonoPath(string baseDirectory)
        {
            const string MONO_LIBRARY_NAME = "mono.dll";
            string[] matches = Directory.GetFiles(baseDirectory, MONO_LIBRARY_NAME, SearchOption.AllDirectories);
            if (matches.Length != 1)
            {
                string message = matches.Length == 0 ? "没有找到 {0}" : "找到多个 {0}";
                throw new Exception(string.Format(message, MONO_LIBRARY_NAME));
            }
            return matches[0];
        }
    }
}
