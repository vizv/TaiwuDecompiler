using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TaiwuDecompiler.Utilities
{
    class AssemblyUtility
    {
        /// <summary>
        /// MonoImage 结构体。
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct MonoImage
        {
            public int refCount;
            public IntPtr rawDataHandle;
            public IntPtr rawData;
            public int rawDataLen;
        }

        /// <summary>
        /// Mono 的 Assembly 镜像加载函数。
        /// </summary>
        /// <param name="data">储存数据的指针</param>
        /// <param name="dataLen ">数据长度</param>
        /// <param name="needCopy">是否需要拷贝</param>
        /// <param name="status">储存加载状态的指针</param>
        /// <param name="refOnly">是否仅供引用</param>
        /// <param name="name">Assembly 的文件名</param>
        /// <returns></returns>
        [DllImport("mono.dll", EntryPoint = "mono_image_open_from_data_with_name", CharSet = CharSet.Ansi)]
        internal static extern IntPtr MonoOpenImage(
            IntPtr data,
            uint dataLen,
            bool needCopy,
            IntPtr status,
            bool refOnly,
            string name
        );

        /// <summary>
        /// Mono 的 Assembly 镜像加载函数。
        /// </summary>
        /// <param name="data">储存数据的指针</param>
        /// <param name="dataLen ">数据长度</param>
        /// <param name="needCopy">是否需要拷贝</param>
        /// <param name="status">储存加载状态的指针</param>
        /// <param name="refOnly">是否仅供引用</param>
        /// <param name="name">Assembly 的文件名</param>
        /// <returns></returns>
        [DllImport("mono-2.0-bdwgc.dll", EntryPoint = "mono_image_open_from_data_with_name", CharSet = CharSet.Ansi)]
        internal static extern IntPtr MonoBleedingEdgeOpenImage(
            IntPtr data,
            uint dataLen,
            bool needCopy,
            IntPtr status,
            bool refOnly,
            string name
        );

        /// <summary>
        /// Mono 的 Assembly 镜像加载函数。
        /// </summary>
        /// <param name="monoPath">Mono 库的路径</param>
        /// <param name="data">储存数据的指针</param>
        /// <param name="dataLen ">数据长度</param>
        /// <param name="needCopy">是否需要拷贝</param>
        /// <param name="status">储存加载状态的指针</param>
        /// <param name="refOnly">是否仅供引用</param>
        /// <param name="name">Assembly 的文件名</param>
        /// <returns></returns>
        internal static IntPtr OpenImage(
            string monoPath,
            IntPtr data,
            uint dataLen,
            bool needCopy,
            IntPtr status,
            bool refOnly,
            string name
        )
        {
            if (monoPath.EndsWith("mono-2.0-bdwgc.dll"))
            {
                return MonoBleedingEdgeOpenImage(data, dataLen, needCopy, status, refOnly, name);
            }
            return MonoOpenImage(data, dataLen, needCopy, status, refOnly, name);
        }

        /// <summary>
        /// 初始化 Mono 镜像列表。
        /// </summary>
        [DllImport("mono.dll", EntryPoint = "mono_images_init")]
        internal static extern void MonoInitImages();

        /// <summary>
        /// 初始化 Mono 镜像列表。
        /// </summary>
        [DllImport("mono-2.0-bdwgc.dll", EntryPoint = "mono_images_init")]
        internal static extern void MonoBleedingEdgeInitImages();

        internal static void InitImages(string monoPath)
        {
            if (monoPath.EndsWith("mono-2.0-bdwgc.dll"))
            {
                MonoBleedingEdgeInitImages();
                return;
            }
            MonoInitImages();
        }

        /// <summary>
        /// Win32 的库加载函数。
        /// </summary>
        /// <param name="dllPath">要加载的 DLL 路径</param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string dllPath);

        /// <summary>
        /// 使用 Mono 加载 Assembly 的镜像。
        /// </summary>
        /// <param name="monoPath">Mono 库的路径</param>
        /// <param name="assemblyPath">Assembly 镜像的路径</param>
        /// <returns>加载了数据的数组</returns>
        public static byte[] LoadImage(string monoPath, string assemblyPath)
        {
            IntPtr pData = IntPtr.Zero;

            try
            {
                // 准备可供调用 Unmanaged 代码的带有 Assembly 数据的指针
                byte[] assemblyData = File.ReadAllBytes(assemblyPath);
                pData = Marshal.AllocHGlobal(assemblyData.Length);
                Marshal.Copy(assemblyData, 0, pData, assemblyData.Length);

                // 加载 Mono 动态链接库并加载加壳过的 Assembly
                LoadLibrary(monoPath);
                InitImages(monoPath);
                var pImage = OpenImage(monoPath, pData, (uint)assemblyData.Length, false, IntPtr.Zero, false, assemblyPath);
                MonoImage loadedImage = (MonoImage)Marshal.PtrToStructure(pImage, typeof(MonoImage));

                // 准备用于储存 Managed 数据的数组，并写入已加载的数据
                byte[] dumpedData = new byte[loadedImage.rawDataLen];
                Marshal.Copy(loadedImage.rawData, dumpedData, 0, loadedImage.rawDataLen);
                return dumpedData;
            }
            finally
            {
                if (pData != IntPtr.Zero) Marshal.FreeHGlobal(pData);
            }

            // FIXME: 检查 MonoImage 是否有被释放？
        }
    }
}
