﻿using System;
using System.Runtime.InteropServices;
using System.Drawing;
using SharpDX.Direct3D11;
using System.Drawing.Imaging;

namespace ComputeShaders
{
    /// <summary>
    /// A class that converts CSTexture2D to Bitmap
    /// </summary>
    public class CSTexture2DConverter : IDisposable
    {
        Bitmap m16_bitmap;
        Bitmap finalBitmap;
        Graphics bitmapGraphics;

        int formateSizeInBytes;
        int originalWidth;
        int originalHeight;
        int editedWidth;

        /// <summary>
        /// Creates a new CSTexture2D to Bitmap converter. NOTE: NOTE: if your texture width can be a multiple of 16, then make it a multiple of 16 and use MultipleOf16CSTexture2DConverter for better performance
        /// </summary>
        public CSTexture2DConverter(CSTexture2D texture2D)
        {
            formateSizeInBytes = texture2D.FormatSizeInBytes;
            originalWidth = texture2D.Width;
            originalHeight = texture2D.Height;
            editedWidth = (int)Math.Ceiling(texture2D.Width / 16f) * 16;

            m16_bitmap = new Bitmap(editedWidth, originalHeight, TextureFormatHelper.ConvertFormatToBitmap(texture2D.Format));
            finalBitmap = new Bitmap(originalWidth, originalHeight, TextureFormatHelper.ConvertFormatToBitmap(texture2D.Format));
            bitmapGraphics = Graphics.FromImage(finalBitmap);
        }

        /// <summary>
        /// Copy the information from "source" to a bitmap
        /// </summary>
        /// <param name="source">The CSTexture2D to copy from</param>
        public Bitmap Convert(CSTexture2D source)
        {
            BitmapData bitData = m16_bitmap.LockBits(new Rectangle(0, 0, m16_bitmap.Width, m16_bitmap.Height), ImageLockMode.WriteOnly, m16_bitmap.PixelFormat);

            try
            {
                source.ReadFromRawData(data =>
                {
                    Utilities.CopyMemory(bitData.Scan0, data.DataPointer, (uint)(editedWidth * originalHeight * formateSizeInBytes));
                });
            }
            finally
            {
                m16_bitmap.UnlockBits(bitData);
            }

            //source: https://stackoverflow.com/questions/9616617/c-sharp-copy-paste-an-image-region-into-another-image
            bitmapGraphics.DrawImage(m16_bitmap, new Rectangle(0, 0, originalWidth, originalHeight), new Rectangle(0, 0, originalWidth, originalHeight), GraphicsUnit.Pixel);

            return finalBitmap;
        }

        /// <summary>
        /// Dispose the unmanneged data to prevent memory leaks. This function must be called after finishing using this.
        /// </summary>
        public void Dispose()
        {
            m16_bitmap.Dispose();
            finalBitmap.Dispose();
            bitmapGraphics.Dispose();
        }
    }
}
