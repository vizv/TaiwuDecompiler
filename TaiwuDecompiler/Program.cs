using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.Metadata;
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
        private const string ORIGINAL_FILENAME = "Assembly-CSharp";

        /// <summary>
        /// 脱壳后的 Assembly 文件名。
        /// </summary>
        private const string UNPACKED_FILENAME_TEMPLATE = "Assembly-CSharp-{0}";

        /// <summary>
        /// 反编译器入口方法。
        /// </summary>
        /// <param name="args">参数列表（未使用）</param>
        static void Main(string[] args)
        {
            string gamePath = GetSteamGameInstallLocation(STEAM_GAME_ID);
            Console.WriteLine(string.Format("使用游戏路径: {0}", gamePath));

            string originalAssemblyPath = GetAssemblyPath(gamePath);
            string originalAssemblyHash = HashUtility.GetFileSHA256(originalAssemblyPath);
            Console.WriteLine(string.Format("游戏 Assembly 散列: {0}", originalAssemblyHash));

            string monoLibraryPath = GetMonoPath(gamePath);
            byte[] unpackedAssemblyData = AssemblyUtility.LoadImage(monoLibraryPath, originalAssemblyPath);
            string UNPACKED_FILENAME = string.Format(UNPACKED_FILENAME_TEMPLATE, originalAssemblyHash);
            string unpackedAssemblyPath = originalAssemblyPath.Replace(ORIGINAL_FILENAME, UNPACKED_FILENAME);
            Console.WriteLine(string.Format("写入脱壳后的 Assembly 到: {0}", unpackedAssemblyPath));
            File.WriteAllBytes(unpackedAssemblyPath, unpackedAssemblyData);

            string decompiledDirectory = Path.Combine(gamePath, "TaiwuDecompiler-" + UNPACKED_FILENAME);
            Console.WriteLine("\n开始反编译……");
            DecompileAssembly(unpackedAssemblyPath, decompiledDirectory);

            // TODO: 清理非游戏文件
            Console.WriteLine(string.Format("完成！反编译源码已保存至：{0}", decompiledDirectory));
            Console.WriteLine("按任意键退出……");
            Console.ReadKey();
        }

        /// <summary>
        /// 从注册表获取特定 Steam 游戏的安装路径。
        /// </summary>
        /// <param name="gameId">Steam 游戏 ID</param>
        /// <returns>安装路径</returns>
        private static string GetSteamGameInstallLocation(uint gameId)
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
        private static string GetAssemblyPath(string baseDirectory)
        {
            const string MANAGED_DIR = "Managed";
            string[] matches = Directory.GetDirectories(baseDirectory, MANAGED_DIR, SearchOption.AllDirectories);
            if (matches.Length != 1)
            {
                string message = matches.Length == 0 ? "没有找到 {0} 目录" : "找到多个 {0} 目录";
                throw new Exception(string.Format(message, MANAGED_DIR));
            }
            return Path.Combine(matches[0], ORIGINAL_FILENAME + ".dll");
        }

        /// <summary>
        /// 获取 Mono 动态链接库的路径。
        /// </summary>
        /// <param name="baseDirectory">搜索基础路径</param>
        /// <returns>Mono 动态链接库的路径</returns>
        private static string GetMonoPath(string baseDirectory)
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

        /// <summary>
        /// 使用 ILSpy 反编译 Assembly。
        /// </summary>
        /// <param name="assemblyPath">Assembly 路径</param>
        /// <param name="outputPath">输出路径</param>
        private static void DecompileAssembly(string assemblyPath, string outputPath)
        {
            if (Directory.Exists(outputPath)) Directory.Delete(outputPath, true);
            Directory.CreateDirectory(outputPath);

            WholeProjectDecompiler decompiler = new WholeProjectDecompiler();

            var module = new PEFile(assemblyPath);
            decompiler.AssemblyResolver = new UniversalAssemblyResolver(assemblyPath, false, module.Reader.DetectTargetFrameworkId(assemblyPath));
            decompiler.DecompileProject(module, outputPath);
        }
    }
}
