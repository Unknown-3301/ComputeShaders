using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Drawing;
using System.Drawing.Imaging;

namespace ComputeShaders
{
    /// <summary>
    /// A class to store random functions used in the library.
    /// </summary>
    public static class Utilities
    {
        //public unsafe static void CopyMemory(IntPtr dest, IntPtr src, uint count)
        //{
        //    Buffer.MemoryCopy((void*)src, (void*)dest, count, count);
        //}

        /// <summary>
        /// Copies the data from the scr pointer to the dest pointer.
        /// </summary>
        /// <param name="dest">Destination pointer.</param>
        /// <param name="src">Source pointer.</param>
        /// <param name="count">Data length in bytes.</param>
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        /// <summary>
        /// Access the bitmap raw data (using <see cref="Bitmap.LockBits(Rectangle, ImageLockMode, PixelFormat)"/>).
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="accessAction">The action of accessing. The raw data can only be accessed inside this action.</param>
        /// <param name="accessMode">The mode for accessing.</param>
        public static void AccessRawData(this Bitmap bitmap, Action<BitmapData> accessAction, ImageLockMode accessMode)
        {
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), accessMode, bitmap.PixelFormat);

            try
            {
                accessAction(data);
            }
            catch
            {
                bitmap.UnlockBits(data);
            }

            bitmap.UnlockBits(data);
        }

        /// <summary>
        /// Return the address of the object
        /// </summary>
        /// <param name="obj">The object</param>
        /// <param name="handle">The handle that should be disposed (handle.Free()) after finishing using the address</param>
        /// <returns></returns>
        public static IntPtr GetIntPtr(object obj, out GCHandle handle)
        {
            // source: https://stackoverflow.com/questions/537573/how-to-get-intptr-from-byte-in-c-sharp
            handle = GCHandle.Alloc(obj, GCHandleType.Pinned);
            IntPtr intPtr = handle.AddrOfPinnedObject();

            return intPtr;
        }
        /// <summary>
        /// Get the data of a image in the path but upsidedown and the red and blue channels are swapped.
        /// </summary>
        /// <param name="imagePath">The path to the image.</param>
        /// <returns></returns>
        public static Bitmap GetFlippedBitmap(string imagePath)
        {

            // source: https://www.codeproject.com/Questions/167235/How-to-swap-Red-and-Blue-channels-on-bitmap

            Bitmap newBitmap;

            using (Bitmap bitmap = new Bitmap(imagePath))
            {
                newBitmap = new Bitmap(bitmap.Width, bitmap.Height);

                var imageAttr = new ImageAttributes();
                imageAttr.SetColorMatrix(new ColorMatrix(
                                             new[]
                                                 {
                                                 new[] {0.0F, 0.0F, 1.0F, 0.0F, 0.0F},
                                                 new[] {0.0F, 1.0F, 0.0F, 0.0F, 0.0F},
                                                 new[] {1.0F, 0.0F, 0.0F, 0.0F, 0.0F},
                                                 new[] {0.0F, 0.0F, 0.0F, 1.0F, 0.0F},
                                                 new[] {0.0F, 0.0F, 0.0F, 0.0F, 1.0F}
                                                 }
                                             ));

                GraphicsUnit pixel = GraphicsUnit.Pixel;
                using (Graphics g = Graphics.FromImage(newBitmap))
                {
                    g.DrawImage(bitmap, Rectangle.Round(bitmap.GetBounds(ref pixel)), 0, 0, bitmap.Width, bitmap.Height,
                                GraphicsUnit.Pixel, imageAttr);
                }

                newBitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            }

            return newBitmap;
        }

        /// <summary>
        /// Gets the private variable from and object Casted
        /// </summary>
        /// <typeparam name="T">Object type that contains the private variable</typeparam>
        /// <typeparam name="D">The type of the private variable</typeparam>
        /// <param name="variableName">The name of the private variable</param>
        /// <param name="obj">The object that contains the private variable</param>
        /// <returns></returns>
        public static D GetPrivteVariableDataCasted<T, D>(string variableName, T obj)
        {
            return (D)typeof(T).GetField(variableName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
        }
        /// <summary>
        /// Gets the private variable from and object Uncasted
        /// </summary>
        /// <typeparam name="T">Object type that contains the private variable</typeparam>
        /// <typeparam name="D">The type of the private variable</typeparam>
        /// <param name="variableName">The name of the private variable</param>
        /// <param name="obj">The object that contains the private variable</param>
        /// <returns></returns>
        public static object GetPrivteVariableDataUncasted<T, D>(string variableName, T obj)
        {
            return typeof(T).GetField(variableName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
        }
      
        /// <summary>
        /// returns the object from <paramref name="intPtr"/>.
        /// </summary>
        /// <typeparam name="T">The object type</typeparam>
        /// <param name="intPtr">the pointer to the object data in memory</param>
        /// <returns></returns>
        public static T GetObjectFromIntPtr<T>(IntPtr intPtr)
        {
            // source: https://stackoverflow.com/questions/17339928/c-sharp-how-to-convert-object-to-intptr-and-back
            GCHandle handle = (GCHandle)intPtr;
            T t = (T)handle.Target;
            handle.Free();
            return t;
        }
    }
}
