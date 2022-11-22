using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputeShaders
{
    [Flags]
    public enum CSDeviceCreationFlags
    {
        SingleThreaded = 1,
        Debug = 2,
        SwitchToRef = 4,
        PreventThreadingOptimizations = 8,
        BgraSupport = 32, // 0x00000020
        Debuggable = 64, // 0x00000040
        PreventAlteringLayerSettingsFromRegistry = 128, // 0x00000080
        DisableGpuTimeout = 256, // 0x00000100
        VideoSupport = 2048, // 0x00000800
        None = 0,
    }
}
