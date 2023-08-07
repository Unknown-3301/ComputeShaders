using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using SharpDX.DXGI;

namespace ComputeShaders
{
    /// <summary>
    /// A helper class for TextureFormat
    /// </summary>
    public static class TextureFormatHelper
    {
        /// <summary>
        /// Converts a TextureFormat enum to its equivalent in PixelFormat. NOTE: if no equivalent is found it will return PixelFormat.Undefined
        /// </summary>
        /// <param name="format">The format</param>
        /// <returns></returns>
        public static PixelFormat ConvertFormatToBitmap(TextureFormat format)
        {
            switch(format)
            {
                case TextureFormat.Unknown:
                    return PixelFormat.Undefined;

                case TextureFormat.R16G16B16A16_UNorm:
                    return PixelFormat.Format64bppArgb;

                case TextureFormat.R8G8B8A8_UNorm:
                    return PixelFormat.Format32bppArgb;

                default:
                    return PixelFormat.Undefined;
            }
        }

        /// <summary>
        /// Converts a PixelFormat enum to its equivalent in TextureFormat. NOTE: if no equivalent is found it will return TextureFormat.Unknown
        /// </summary>
        /// <param name="pixelFormat">The format</param>
        /// <returns></returns>
        public static TextureFormat ConvertBitmapToFormat(PixelFormat pixelFormat)
        {
            switch(pixelFormat)
            {
                case PixelFormat.Undefined:
                    return TextureFormat.Unknown;

                case PixelFormat.Format32bppArgb:
                    return TextureFormat.R8G8B8A8_UNorm;

                case PixelFormat.Format64bppArgb:
                    return TextureFormat.R16G16B16A16_UNorm;

                default:
                    return TextureFormat.Unknown;
            }
        }

        /// <summary>
        /// Returns the size of <paramref name="format"/> in bytes.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static int Size(this TextureFormat format)
        {
            return ((Format)format).SizeOfInBytes();
        }
    }
}
