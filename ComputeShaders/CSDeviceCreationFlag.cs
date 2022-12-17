using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputeShaders
{
    /// <summary>
    /// Describes parameters that are used to create a device. see https://learn.microsoft.com/en-us/windows/win32/api/d3d11/ne-d3d11-d3d11_create_device_flag for more information.
    /// </summary>
    [Flags]
    public enum CSDeviceCreationFlags
    {
        /// <summary>
        /// Use this flag if your application will only call methods of Direct3D 11 interfaces from a single thread. By default, the ID3D11Device object is thread-safe.
        /// By using this flag, you can increase performance. However, if you use this flag and your application calls methods of Direct3D 11 interfaces from multiple threads, undefined behavior might result.
        /// </summary>
        SingleThreaded = 1,
        /// <summary>
        /// Creates a device that supports the debug layer.
        /// To use this flag, you must have D3D11*SDKLayers.dll installed; otherwise, device creation fails. To get D3D11_1SDKLayers.dll, install the SDK for Windows 8.
        /// To see the debug information while using compute shaders use "Immediate Window" in visual studio.
        /// </summary>
        Debug = 2,
        /// <summary>
        /// This flag is not supported in Direct3D 11.
        /// </summary>
        SwitchToRef = 4,
        /// <summary>
        /// Prevents multiple threads from being created. When this flag is used with a Windows Advanced Rasterization Platform (WARP) device, no additional threads will be created by WARP
        /// and all rasterization will occur on the calling thread. This flag is not recommended for general use. See remarks.
        /// </summary>
        PreventThreadingOptimizations = 8,
        /// <summary>
        /// Creates a device that supports BGRA formats (<see cref="TextureFormat.B8G8R8A8_UNorm"/> and <see cref="TextureFormat.B8G8R8A8_UNorm_SRgb"/>). All 10level9 and higher hardware with WDDM 1.1+ drivers support BGRA formats.
        /// </summary>
        BgraSupport = 32, // 0x00000020
        /// <summary>
        /// This value is not supported until Direct3D 11.1
        /// </summary>
        Debuggable = 64, // 0x00000040
        /// <summary>
        /// This value is not supported until Direct3D 11.1
        /// </summary>
        PreventAlteringLayerSettingsFromRegistry = 128, // 0x00000080
        /// <summary>
        /// This value is not supported until Direct3D 11.1.
        /// </summary>
        DisableGpuTimeout = 256, // 0x00000100
        /// <summary>
        /// This value is not supported until Direct3D 11.1.
        /// </summary>
        VideoSupport = 2048, // 0x00000800
        /// <summary>
        /// None
        /// </summary>
        None = 0,
    }
}
