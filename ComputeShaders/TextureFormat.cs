using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputeShaders
{
    /// <summary>
    /// The possible formats
    /// </summary>
    public enum TextureFormat
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        Unknown = 0,

        R32G32B32A32_Typeless = 1,
        R32G32B32A32_Float = 2,
        R32G32B32A32_UInt = 3,
        R32G32B32A32_SInt = 4,
        R32G32B32_Typeless = 5,
        R32G32B32_Float = 6,
        R32G32B32_UInt = 7,
        R32G32B32_SInt = 8,
        R16G16B16A16_Typeless = 9,
        R16G16B16A16_Float = 10, // 0x0000000A
        R16G16B16A16_UNorm = 11, // 0x0000000B
        R16G16B16A16_UInt = 12, // 0x0000000C
        R16G16B16A16_SNorm = 13, // 0x0000000D
        R16G16B16A16_SInt = 14, // 0x0000000E
        R32G32_Typeless = 15, // 0x0000000F
        R32G32_Float = 16, // 0x00000010
        R32G32_UInt = 17, // 0x00000011
        R32G32_SInt = 18, // 0x00000012
        R32G8X24_Typeless = 19, // 0x00000013
        D32_Float_S8X24_UInt = 20, // 0x00000014
        R32_Float_X8X24_Typeless = 21, // 0x00000015
        X32_Typeless_G8X24_UInt = 22, // 0x00000016
        R10G10B10A2_Typeless = 23, // 0x00000017
        R10G10B10A2_UNorm = 24, // 0x00000018
        R10G10B10A2_UInt = 25, // 0x00000019
        R11G11B10_Float = 26, // 0x0000001A
        R8G8B8A8_Typeless = 27, // 0x0000001B
        R8G8B8A8_UNorm = 28, // 0x0000001C
        R8G8B8A8_UNorm_SRgb = 29, // 0x0000001D
        R8G8B8A8_UInt = 30, // 0x0000001E
        R8G8B8A8_SNorm = 31, // 0x0000001F
        R8G8B8A8_SInt = 32, // 0x00000020
        R16G16_Typeless = 33, // 0x00000021
        R16G16_Float = 34, // 0x00000022
        R16G16_UNorm = 35, // 0x00000023
        R16G16_UInt = 36, // 0x00000024
        R16G16_SNorm = 37, // 0x00000025
        R16G16_SInt = 38, // 0x00000026
        R32_Typeless = 39, // 0x00000027
        D32_Float = 40, // 0x00000028
        R32_Float = 41, // 0x00000029
        R32_UInt = 42, // 0x0000002A
        R32_SInt = 43, // 0x0000002B
        R24G8_Typeless = 44, // 0x0000002C
        D24_UNorm_S8_UInt = 45, // 0x0000002D
        R24_UNorm_X8_Typeless = 46, // 0x0000002E
        X24_Typeless_G8_UInt = 47, // 0x0000002F
        R8G8_Typeless = 48, // 0x00000030
        R8G8_UNorm = 49, // 0x00000031
        R8G8_UInt = 50, // 0x00000032
        R8G8_SNorm = 51, // 0x00000033
        R8G8_SInt = 52, // 0x00000034
        R16_Typeless = 53, // 0x00000035
        R16_Float = 54, // 0x00000036
        D16_UNorm = 55, // 0x00000037
        R16_UNorm = 56, // 0x00000038
        R16_UInt = 57, // 0x00000039
        R16_SNorm = 58, // 0x0000003A
        R16_SInt = 59, // 0x0000003B
        R8_Typeless = 60, // 0x0000003C
        R8_UNorm = 61, // 0x0000003D
        R8_UInt = 62, // 0x0000003E
        R8_SNorm = 63, // 0x0000003F
        R8_SInt = 64, // 0x00000040
        A8_UNorm = 65, // 0x00000041
        R1_UNorm = 66, // 0x00000042
        R9G9B9E5_Sharedexp = 67, // 0x00000043
        R8G8_B8G8_UNorm = 68, // 0x00000044
        G8R8_G8B8_UNorm = 69, // 0x00000045
        BC1_Typeless = 70, // 0x00000046
        BC1_UNorm = 71, // 0x00000047
        BC1_UNorm_SRgb = 72, // 0x00000048
        BC2_Typeless = 73, // 0x00000049
        BC2_UNorm = 74, // 0x0000004A
        BC2_UNorm_SRgb = 75, // 0x0000004B
        BC3_Typeless = 76, // 0x0000004C
        BC3_UNorm = 77, // 0x0000004D
        BC3_UNorm_SRgb = 78, // 0x0000004E
        BC4_Typeless = 79, // 0x0000004F
        BC4_UNorm = 80, // 0x00000050
        BC4_SNorm = 81, // 0x00000051
        BC5_Typeless = 82, // 0x00000052
        BC5_UNorm = 83, // 0x00000053
        BC5_SNorm = 84, // 0x00000054
        B5G6R5_UNorm = 85, // 0x00000055
        B5G5R5A1_UNorm = 86, // 0x00000056
        B8G8R8A8_UNorm = 87, // 0x00000057
        B8G8R8X8_UNorm = 88, // 0x00000058
        R10G10B10_Xr_Bias_A2_UNorm = 89, // 0x00000059
        B8G8R8A8_Typeless = 90, // 0x0000005A
        B8G8R8A8_UNorm_SRgb = 91, // 0x0000005B
        B8G8R8X8_Typeless = 92, // 0x0000005C
        B8G8R8X8_UNorm_SRgb = 93, // 0x0000005D
        BC6H_Typeless = 94, // 0x0000005E
        BC6H_Uf16 = 95, // 0x0000005F
        BC6H_Sf16 = 96, // 0x00000060
        BC7_Typeless = 97, // 0x00000061
        BC7_UNorm = 98, // 0x00000062
        BC7_UNorm_SRgb = 99, // 0x00000063
        AYUV = 100, // 0x00000064
        Y410 = 101, // 0x00000065
        Y416 = 102, // 0x00000066
        NV12 = 103, // 0x00000067
        P010 = 104, // 0x00000068
        P016 = 105, // 0x00000069
        Opaque420 = 106, // 0x0000006A
        YUY2 = 107, // 0x0000006B
        Y210 = 108, // 0x0000006C
        Y216 = 109, // 0x0000006D
        NV11 = 110, // 0x0000006E
        AI44 = 111, // 0x0000006F
        IA44 = 112, // 0x00000070
        P8 = 113, // 0x00000071
        A8P8 = 114, // 0x00000072
        B4G4R4A4_UNorm = 115, // 0x00000073
        P208 = 130, // 0x00000082
        V208 = 131, // 0x00000083
        V408 = 132, // 0x00000084
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
