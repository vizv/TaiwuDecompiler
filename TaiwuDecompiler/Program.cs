using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using System;
using System.Collections.Generic;
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
        /// 游戏目录名。
        /// </summary>
        readonly static string GAME_DIRECTORY = "The Scroll Of Taiwu";

        /// <summary>
        /// 游戏文件名。
        /// </summary>
        readonly static string GAME_NAME = $"{GAME_DIRECTORY} Alpha V1.0";

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
            (string gamePath, string originalAssemblyPath, string monoLibraryPath) = GetSteamGameInstallation(STEAM_GAME_ID);

            if (gamePath == null)
            {
                Console.Error.WriteLine("错误：搜索游戏路径失败，请将反编译器目录拷贝至游戏路径后重试。");
                Environment.Exit(1);
            }

            Console.WriteLine($"使用游戏路径: {gamePath}");
            Console.WriteLine($"使用 Assembly: {originalAssemblyPath}");
            Console.WriteLine($"使用 Mono 库: {monoLibraryPath}");
            Console.WriteLine();

            string originalAssemblyHash = HashUtility.GetFileSHA256(originalAssemblyPath);
            Console.WriteLine($"游戏 Assembly 散列: {originalAssemblyHash}");

            byte[] unpackedAssemblyData = AssemblyUtility.LoadImage(monoLibraryPath, originalAssemblyPath);
            string UNPACKED_FILENAME = string.Format(UNPACKED_FILENAME_TEMPLATE, originalAssemblyHash);
            string unpackedAssemblyPath = originalAssemblyPath.Replace(ORIGINAL_FILENAME, UNPACKED_FILENAME);
            Console.WriteLine($"写入脱壳后的 Assembly 到: {unpackedAssemblyPath}");
            File.WriteAllBytes(unpackedAssemblyPath, unpackedAssemblyData);

            string decompiledDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TaiwuDecompiler-" + UNPACKED_FILENAME);
            Console.WriteLine("\n开始反编译……");
            DecompileAssembly(unpackedAssemblyPath, decompiledDirectory);

            // FIXME: 未来重写 WholeProjectDecompiler，导出合适的工程文件。
            Console.WriteLine("\n执行清理……");
            CleanupSource(decompiledDirectory);

            Console.WriteLine($"完成！反编译源码已保存至：{decompiledDirectory}");
            Console.WriteLine("按任意键退出……");
            Console.ReadKey();
        }

        /// <summary>
        /// 获取通过 Steam 所安装的特定 Unity 游戏路径。
        /// </summary>
        /// <param name="gameId">Steam 游戏 ID（可为空）</param>
        /// <returns>安装路径、Assembly 路径、Mono 库路径</returns>
        private static (string, string, string) GetSteamGameInstallation(uint gameId = 0)
        {
            string steamGameRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App " + gameId;
            string steamGameInstallLocationName = "InstallLocation";

            string installLocationFromRegistry = RegistryUtility.LocalMachine.GetString(steamGameRegistryKey, steamGameInstallLocationName);
            string currentLocation = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".."));

            List<string> installationLocationCandidatesList = new List<string> {
                installLocationFromRegistry,
                @"C:\Program Files (x86)\Steam\steamapps\common\The Scroll Of Taiwu",
                @"C:\Program Files\Steam\steamapps\common\The Scroll Of Taiwu",
                currentLocation,
            };

            foreach (string installationLocationCandidate in installationLocationCandidatesList)
            {
                try
                {
                    string assemblyPathCandidate = GetAssemblyPath(installationLocationCandidate);
                    string monoPathCandidate = GetMonoPath(installationLocationCandidate);

                    if (File.Exists(assemblyPathCandidate) && File.Exists(monoPathCandidate))
                    {
                        return (installationLocationCandidate, assemblyPathCandidate, monoPathCandidate);
                    }
                }
                catch
                {
                    continue;
                }
            }

            return (null, null, null);
        }

        /// <summary>
        /// 获取 Assembly 的路径。
        /// </summary>
        /// <param name="baseDirectory">搜索基础路径</param>
        /// <returns>原始 Assembly 的路径</returns>
        private static string GetAssemblyPath(string baseDirectory)
        {
            return $@"{baseDirectory}\{GAME_NAME}_Data\Managed\{ORIGINAL_FILENAME}.dll";
        }

        /// <summary>
        /// 获取 Mono 动态链接库的路径。
        /// </summary>
        /// <param name="baseDirectory">搜索基础路径</param>
        /// <returns>Mono 动态链接库的路径</returns>
        private static string GetMonoPath(string baseDirectory)
        {
            return $@"{baseDirectory}\Mono\EmbedRuntime\mono.dll";
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

        /// <summary>
        /// 清理大部分项目文件，仅保留跟游戏相关的文件。
        /// </summary>
        /// <param name="sourceDirectory">源码目录</param>
        private static void CleanupSource(string sourceDirectory)
        {
            string[] entries = Directory.GetFileSystemEntries(sourceDirectory);
            foreach (var entry in entries)
            {
                if (Directory.Exists(entry)) Directory.Delete(entry, true);
                if (File.Exists(entry) && entry.EndsWith(".csproj")) File.Delete(entry);
            }
        }
    }
}
