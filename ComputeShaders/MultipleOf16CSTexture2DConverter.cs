using System;
using System.Runtime.InteropServices;
using System.Drawing;
using SharpDX.Direct3D11;
using System.Drawing.Imaging;

namespace ComputeShaders
{
    /// <summary>
    /// A class that converts CSTexture2D to Bitmap
    /// </summary>
    public class MultipleOf16CSTexture2DConverter
    {
        Bitmap finalBitmap;
        Graphics bitmapGraphics;

        int formateSizeInBytes;
        int originalWidth;
        int originalHeight;

        /// <summary>
        /// Creates a new CSTexture2D to Bitmap converter. NOTE: NOTE: if your texture width can be a multiple of 16, then make it a multiple of 16 and use MultipleOf16CSTexture2DConverter for better performance
        /// </summary>
        public MultipleOf16CSTexture2DConverter(CSTexture2D texture2D)
        {
            formateSizeInBytes = texture2D.FormatSizeInBytes;
            originalWidth = texture2D.Width;
            originalHeight = texture2D.Height;

            if ((int)Math.Ceiling(texture2D.Width / 16f) * 16 != originalWidth)
                throw new ArgumentException($"The texture width: {texture2D.Width} isn't a multiple of 16");

            finalBitmap = new Bitmap(originalWidth, originalHeight, TextureFormatHelper.ConvertFormatToBitmap(texture2D.Format));
            bitmapGraphics = Graphics.FromImage(finalBitmap);
        }

        /// <summary>
        /// Copy the information from "source" to a bitmap
        /// </summary>
        /// <param name="source">The CSTexture2D to copy from</param>
        public Bitmap Convert(CSTexture2D source)
        {
            BitmapData bitData = finalBitmap.LockBits(new Rectangle(0, 0, finalBitmap.Width, finalBitmap.Height), ImageLockMode.WriteOnly, finalBitmap.PixelFormat);

            try
            {
                source.ReadFromRawData(data =>
                {
                    Utilities.CopyMemory(bitData.Scan0, data.DataPointer, (uint)(originalWidth * originalHeight * formateSizeInBytes));
                });
            }
            finally
            {
                finalBitmap.UnlockBits(bitData);
            }

            return finalBitmap;
        }

        /// <summary>
        /// Dispose the unmanneged data to prevent memory leaks. This function must be called after finishing using this.
        /// </summary>
        public void Dispose()
        {
            finalBitmap.Dispose();
            bitmapGraphics.Dispose();
        }
    }
}
