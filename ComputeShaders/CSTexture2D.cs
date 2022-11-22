using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;


namespace ComputeShaders
{
    /// <summary>
    /// The class that holds the texture data. This class can be created in ComputeShader class
    /// </summary>
    public class CSTexture2D : IDisposable
    {
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        public IntPtr TextureNativePointer { get => Texture.NativePointer; }
        public IntPtr DeviceNativePointer { get => Texture.Device.NativePointer; }

        /// <summary>
        /// The SharpDX Direct3D 11 texture
        /// </summary>
        public Texture2D Texture { get; private set; }
        internal Texture2D stagingTexture { get; private set; }
        internal UnorderedAccessView UnorderedAccessView { get; private set; }
        internal int FormatSizeInBytes { get; }

        /// <summary>
        /// The ability to read/write the texture in the cpu
        /// </summary>
        public bool CPU_ReadWrite { get => stagingTexture != null; }
        /// <summary>
        /// The width of the texture
        /// </summary>
        public int Width { get => Texture.Description.Width; }
        /// <summary>
        /// The height of the texture
        /// </summary>
        public int Height { get => Texture.Description.Height; }
        /// <summary>
        /// The format of the texture
        /// </summary>
        public TextureFormat Format { get => (TextureFormat)Texture.Description.Format; }

        internal CSTexture2D(Device device, int width, int height, Texture2DDescription description)
        {
            Texture2DDescription dDescription = description;
            dDescription.Width = width;
            dDescription.Height = height;
            FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes(description.Format);

            Texture = new Texture2D(device, dDescription);
        }
        internal CSTexture2D(Device device, Bitmap bitmap, Texture2DDescription description)
        {
            // souce: https://stackoverflow.com/questions/36068631/sharpdx-3-0-2-d3d11-how-to-load-texture-from-file-and-make-it-to-work-in-shade

            Bitmap temp;
            description.Width = bitmap.Width;
            description.Height = bitmap.Height;
            FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes(description.Format);

            Texture = new Texture2D(device, description, Utilities.GetReversedBitmap(bitmap, out temp));
            temp.Dispose();
        }
        internal CSTexture2D(Device device, IntPtr dataPointer, Texture2DDescription description)
        {
            // souce: https://stackoverflow.com/questions/36068631/sharpdx-3-0-2-d3d11-how-to-load-texture-from-file-and-make-it-to-work-in-shade

            FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes(description.Format);

            Texture = new Texture2D(device, description, new DataRectangle(dataPointer, description.Width * FormatSizeInBytes));

        }
        internal CSTexture2D(Texture2D texture)
        {
            Texture = texture;
            FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes(texture.Description.Format);
        }

        /// <summary>
        /// Create a new texture using a texture pointer
        /// </summary>
        /// <param name="nativePointer">The texture pointer</param>
        /// <param name="format">The format of the texture</param>
        public CSTexture2D(IntPtr nativePointer, TextureFormat format)
        {
            Texture = new Texture2D(nativePointer);
            FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes((SharpDX.DXGI.Format)format);
        }

        /// <summary>
        /// Connects this texture to another compute shader so that data can read/write between the texture and the compute shader or any texture connected to it directly. NOTE: after calling this function if any changes occured to the texture or the shared version then Flush() should be called on the changed texture
        /// </summary>
        /// <param name="shader">The compute shader to connect with</param>
        /// <returns></returns>
        public CSTexture2D Share(ComputeShader shader)
        {
            //source: https://stackoverflow.com/questions/41625272/direct3d11-sharing-a-texture-between-devices-black-texture
            SharpDX.DXGI.Resource copy = Texture.QueryInterface<SharpDX.DXGI.Resource>();
            IntPtr sharedHandle = copy.SharedHandle;
            return new CSTexture2D(shader.device.OpenSharedResource<Texture2D>(sharedHandle));
        }
        /// <summary>
        /// Connects this texture to another texture so that data can read/write between the texture and the other texture or any texture connected to it directly. NOTE: after calling this function if any changes occured to the texture or the shared version then Flush() should be called on the changed texture
        /// </summary>
        /// <param name="anotherTexture">The another texture to connect with</param>
        /// <returns></returns>
        public CSTexture2D Share(CSTexture2D anotherTexture)
        {
            //source: https://stackoverflow.com/questions/41625272/direct3d11-sharing-a-texture-between-devices-black-texture
            SharpDX.DXGI.Resource copy = Texture.QueryInterface<SharpDX.DXGI.Resource>();
            IntPtr sharedHandle = copy.SharedHandle;
            return new CSTexture2D(anotherTexture.Texture.Device.OpenSharedResource<Texture2D>(sharedHandle));
        }
        /// <summary>
        /// Connects this texture to another texture so that data can read/write between the texture and the other texture or any texture connected to it directly. NOTE: after calling this function if any changes occured to the texture or the shared version then Flush() should be called on the changed texture
        /// </summary>
        /// <param name="devicePointer">The another texture device to connect with</param>
        /// <returns></returns>
        public CSTexture2D Share(IntPtr devicePointer)
        {
            //source: https://stackoverflow.com/questions/41625272/direct3d11-sharing-a-texture-between-devices-black-texture
            SharpDX.DXGI.Resource copy = Texture.QueryInterface<SharpDX.DXGI.Resource>();
            IntPtr sharedHandle = copy.SharedHandle;
            Device dev = new Device(devicePointer);
            return new CSTexture2D(dev.OpenSharedResource<Texture2D>(sharedHandle));
        }

        /// <summary>
        /// Enables the ability to read/write the texture using cpu. Enabling it has the advantages:
        /// <br>- to read the texture raw data using GetRawDataIntPtr function.</br>
        /// <br>- to write to the texture raw data using WriteToRawData function.</br>
        /// <br>and has the disadvantages:</br>
        /// <br>- may decrease the performance.</br>
        /// <br>- increase the memory usage to almost the double.</br>
        /// </summary>
        public void EnableCPU_ReadWrite()
        {
            if (CPU_ReadWrite)
                return;

            stagingTexture = new Texture2D(Texture.Device, new Texture2DDescription()
            {
                ArraySize = Texture.Description.ArraySize,
                CpuAccessFlags = Texture.Description.CpuAccessFlags,
                BindFlags = BindFlags.None,
                Format = Texture.Description.Format,
                Height = Texture.Description.Height,
                Width = Texture.Description.Width,
                MipLevels = Texture.Description.MipLevels,
                Usage = ResourceUsage.Staging,
                SampleDescription = Texture.Description.SampleDescription,
                OptionFlags = ResourceOptionFlags.None,
            });
        }
        /// <summary>
        /// Disables the ability to read/write the texture using cpu. Disables it has the advantages (if cpu read/write was enabled):
        /// <br>- may increase the performance.</br>
        /// <br>- decrease the memory usage to almost the half.</br>
        /// <br>and has the disadvantages:</br>
        /// <br>- can not read the texture raw data using GetRawDataIntPtr function.</br>
        /// <br>- can not write to the texture raw data using WriteToRawData function.</br>
        /// </summary>
        public void DisablesCPU_ReadWrite()
        {
            if (!CPU_ReadWrite)
                return;

            stagingTexture.Dispose();
            stagingTexture = null;
        }

        /// <summary>
        /// Copy the contents of the texture to another texture.
        /// NOTE: CPU read/write ability must be enabled for both textures, both textures must have exact same dimensions, and this function is so slow compared to using shared textures.
        /// </summary>
        /// <param name="destination">The texture to copy to.</param>
        public void CopyToTexture(CSTexture2D destination)
        {
            if (destination.Texture.Device != Texture.Device)
            {
                destination.WriteToRawData(desBox =>
                {
                    ReadFromRawData(scrBox =>
                    {
                        CopyMemory(desBox.DataPointer, scrBox.DataPointer, (uint)(scrBox.RowPitch * Texture.Description.Height));
                    });
                });
            }
            else
            {
                Texture.Device.ImmediateContext.CopyResource(Texture, destination.Texture);
            }
        }

        /// <summary>
        /// Write to the texture raw data (using only cpu) by an write function.
        /// NOTE: the data box pointer is aligned to 16 bytes. Check: https://learn.microsoft.com/en-us/windows/win32/api/d3d11/ns-d3d11-d3d11_mapped_subresource
        /// </summary>
        /// <param name="writeAction"></param>
        public void WriteToRawData(Action<TextureDataBox> writeAction)
        {
            if (!CPU_ReadWrite)
            {
                throw new Exception("Cannot use WriteToRawData because CPU read/write ability is disabled. To enable it call EnableCPU_ReadWrite function.");
            }

            Texture.Device.ImmediateContext.CopyResource(Texture, stagingTexture);

            TextureDataBox box = new TextureDataBox(Texture.Device.ImmediateContext.MapSubresource(stagingTexture, 0, MapMode.Write, MapFlags.None));

            try
            {
                writeAction(box);
            }
            finally
            {
                Texture.Device.ImmediateContext.UnmapSubresource(stagingTexture, 0);
            }

            Texture.Device.ImmediateContext.CopyResource(stagingTexture, Texture);
        }
        /// <summary>
        /// Reads through the raw data using 'readAction'.
        /// NOTE: all the reading process must ONLY be done inside 'readAction' function.
        /// </summary>
        /// <returns></returns>
        public void ReadFromRawData(Action<TextureDataBox> readAction)
        {
            if (!CPU_ReadWrite)
            {
                throw new Exception("Cannot use GetRawDataIntPtr because CPU read/write ability is disabled. To enable it call EnableCPU_ReadWrite function.");
            }

            Texture.Device.ImmediateContext.CopyResource(Texture, stagingTexture);

            TextureDataBox box = new TextureDataBox(Texture.Device.ImmediateContext.MapSubresource(stagingTexture, 0, MapMode.Read, MapFlags.None));

            try
            {
                readAction(box);
            }
            finally
            {
                Texture.Device.ImmediateContext.UnmapSubresource(stagingTexture, 0);
            }
        }

        /// <summary>
        /// Sends queued-up commands in the command buffer to the graphics processing unit (GPU). It is used after updating a shared texture
        /// </summary>
        public void Flush()
        {
            Texture.Device.ImmediateContext.Flush();
        }

        internal UnorderedAccessView CreateUAV(Device device)
        {
            UnorderedAccessView = new UnorderedAccessView(device, Texture, new UnorderedAccessViewDescription()
            {
                Format = Texture.Description.Format,
                Dimension = UnorderedAccessViewDimension.Texture2D,
                Texture2D = { MipSlice = 0 }
            });

            return UnorderedAccessView;
        }

        /// <summary>
        /// Dispose the unmanneged data to prevent memory leaks. This function must be called after finishing using the texture.
        /// </summary>
        public void Dispose()
        {
            Texture.Dispose();

            if (stagingTexture != null)
                stagingTexture.Dispose();

            if (UnorderedAccessView != null)
                UnorderedAccessView.Dispose();
        }
    }
}
