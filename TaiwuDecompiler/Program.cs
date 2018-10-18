using System;
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
        /// 反编译器入口方法。
        /// </summary>
        /// <param name="args">参数列表（未使用）</param>
        static void Main(string[] args)
        {
            string gamePath = getSteamGameInstallLocation(STEAM_GAME_ID);
            Console.Error.WriteLine(string.Format("Game Path: {0}", gamePath)); // FIXME: Debug
            // TODO: 计算 Assembly-CSharp.dll 的 SHA256 值
            // TODO: 脱壳 Assembly-CSharp.dll，并存至 Assembly-CSharp-Unpacked-<SHA256>.dll
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
    }
}
