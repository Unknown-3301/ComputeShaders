using System.Collections;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ComputeShaders
{
    /// <summary>
    /// The class that holds the texture array data. This class can be created in ComputeShader class
    /// </summary>
    public class CSTexture2DArray : IDisposable
    {
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        public IntPtr TextureNativePointer { get => Texture.NativePointer; }
        /// <summary>
        /// The SharpDX Direct3D 11 texture
        /// </summary>
        public Texture2D Texture { get; private set; }
        internal Texture2D stagingTexture { get; private set; }
        internal UnorderedAccessView UnorderedAccessView { get; private set; }
        internal int FormatSizeInBytes { get; }

        private bool alreadyMapping;

        /// <summary>
        /// The width of the texture
        /// </summary>
        public int Width { get => Texture.Description.Width; }
        /// <summary>
        /// The height of the texture
        /// </summary>
        public int Height { get => Texture.Description.Height; }
        /// <summary>
        /// The number of textures in the texture array
        /// </summary>
        public int Textures { get => Texture.Description.ArraySize; }
        /// <summary>
        /// The format of the texture
        /// </summary>
        public TextureFormat Format { get => (TextureFormat)Texture.Description.Format; }

        internal CSTexture2DArray(Device device, int width, int height, int numberOfTextures, Texture2DDescription description)
        {
            Texture2DDescription dDescription = description;
            dDescription.Width = width;
            dDescription.Height = height;
            dDescription.ArraySize = numberOfTextures;
            FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes(description.Format);

            Texture = new Texture2D(device, dDescription);
        }
        internal CSTexture2DArray(Device device, IEnumerable<Bitmap> bitmaps, Texture2DDescription description)
        {
            // souce: https://stackoverflow.com/questions/36068631/sharpdx-3-0-2-d3d11-how-to-load-texture-from-file-and-make-it-to-work-in-shade

            List<DataRectangle> rectangles = new List<DataRectangle>();
            List<Bitmap> temps = new List<Bitmap>();

            foreach (Bitmap bitmap in bitmaps)
            {
                description.Width = bitmap.Width;
                description.Height = bitmap.Height;
                FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes(description.Format);

                Bitmap temp;
                rectangles.Add(Utilities.GetReversedBitmap(bitmap, out temp));
                temps.Add(temp);
            }

            Texture = new Texture2D(device, description, rectangles.ToArray());
            temps.ForEach(x => x.Dispose());
        }
        internal CSTexture2DArray(Device device, IntPtr[] slicesDataPointers, Texture2DDescription description)
        {
            // souce: https://stackoverflow.com/questions/36068631/sharpdx-3-0-2-d3d11-how-to-load-texture-from-file-and-make-it-to-work-in-shade

            FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes(description.Format);

            DataRectangle[] rectangles = new DataRectangle[slicesDataPointers.Length];
            for (int i = 0; i < rectangles.Length; i++)
            {
                rectangles[i] = new DataRectangle(slicesDataPointers[i], description.Width * FormatSizeInBytes);
            }

            Texture = new Texture2D(device, description, rectangles);

        }
        internal CSTexture2DArray(Texture2D texture)
        {
            Texture = texture;
            FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes(texture.Description.Format);
        }

        /// <summary>
        /// Create a new texture array using a texture array pointer
        /// </summary>
        /// <param name="nativePointer">The texture array pointer</param>
        /// <param name="format">The format of the texture</param>
        public CSTexture2DArray(IntPtr nativePointer, TextureFormat format)
        {
            Texture = new Texture2D(nativePointer);
            FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes((SharpDX.DXGI.Format)format);
        }

        /// <summary>
        /// Gets a pointer to the data contained in the texture array, and denies the GPU access to that texture. NOTE: after finishing using the pointer UnMap() must be called
        /// </summary>
        /// <param name="read">whether you can read from that pointer</param>
        /// <param name="write">whether you can write to that pointer</param>
        /// <returns></returns>
        public TextureDataBox Map(bool read, bool write)
        {
            if (stagingTexture == null)
            {
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

            Texture.Device.ImmediateContext.CopyResource(Texture, stagingTexture);

            if (!read && !write)
                throw new Exception("CSTexture ERROR: in Map(), read and write cannot both be false!");

            MapMode mode = read ? (write ? MapMode.ReadWrite : MapMode.Read) : MapMode.Write;

            alreadyMapping = true;

            return new TextureDataBox(Texture.Device.ImmediateContext.MapSubresource(stagingTexture, 0, mode, MapFlags.None));
        }
        /// <summary>
        /// Gets a pointer to the data contained in a slice (texture) in the texture array, and denies the GPU access to that texture. NOTE: after finishing using the pointer UnMap() must be called
        /// </summary>
        /// <param name="sliceIndex">The index for the texture in the texture array</param>
        /// <param name="read">whether you can read from that pointer</param>
        /// <param name="write">whether you can write to that pointer</param>
        /// <returns></returns>
        public TextureDataBox MapSlice(int sliceIndex, bool read, bool write)
        {
            if (stagingTexture == null)
            {
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

            Texture.Device.ImmediateContext.CopyResource(Texture, stagingTexture);

            if (!read && !write)
                throw new Exception("CSTexture ERROR: in Map(), read and write cannot both be false!");

            MapMode mode = read ? (write ? MapMode.ReadWrite : MapMode.Read) : MapMode.Write;

            alreadyMapping = true;

            int mipSize;
            return new TextureDataBox(Texture.Device.ImmediateContext.MapSubresource(stagingTexture, 0, sliceIndex, mode, MapFlags.None, out mipSize));
        }
        /// <summary>
        /// Invalidate the pointer to a resource and re-enable the GPU's access to that resource.
        /// </summary>
        public void UnMap()
        {
            alreadyMapping = false;
            Texture.Device.ImmediateContext.UnmapSubresource(stagingTexture, 0);
        }

        /// <summary>
        /// Copy the contents of the texture array to another texture array.
        /// NOTE: both textures must have exact same dimensions, and this function is so slow compared to using shared textures.
        /// </summary>
        /// <param name="destination">The texture array to copy to.</param>
        public void CopyToTexture(CSTexture2DArray destination)
        {
            if (destination.Texture.Device != Texture.Device)
            {
                TextureDataBox desBox = destination.Map(false, true);
                TextureDataBox scrBox = Map(true, false);

                CopyMemory(desBox.DataPointer, scrBox.DataPointer, (uint)(scrBox.RowPitch * Texture.Description.Height));
            }
            else
            {
                Texture.Device.ImmediateContext.CopyResource(Texture, destination.Texture);
            }
        }

            /// <summary>
            /// Connects this texture array to another compute shader so that data can read/write between the texture array and the compute shader or any texture connected to it directly. NOTE: after calling this function if any changes occured to the texture array or the shared version then Flush() should be called on the changed texture array
            /// </summary>
            /// <param name="shader">The compute shader to connect with</param>
            /// <returns></returns>
            public CSTexture2DArray Share(ComputeShader shader)
        {
            //source: https://stackoverflow.com/questions/41625272/direct3d11-sharing-a-texture-between-devices-black-texture
            SharpDX.DXGI.Resource copy = Texture.QueryInterface<SharpDX.DXGI.Resource>();
            IntPtr sharedHandle = copy.SharedHandle;
            return new CSTexture2DArray(shader.device.OpenSharedResource<Texture2D>(sharedHandle));
        }
        /// <summary>
        /// Connects this texture array to another texture so that data can read/write between the texture array and the other texture or any texture connected to it directly. NOTE: after calling this function if any changes occured to the texture array or the shared version then Flush() should be called on the changed texture array
        /// </summary>
        /// <param name="anotherTexture">The another texture to connect with</param>
        /// <returns></returns>
        public CSTexture2DArray Share(CSTexture2D anotherTexture)
        {
            //source: https://stackoverflow.com/questions/41625272/direct3d11-sharing-a-texture-between-devices-black-texture
            SharpDX.DXGI.Resource copy = Texture.QueryInterface<SharpDX.DXGI.Resource>();
            IntPtr sharedHandle = copy.SharedHandle;
            return new CSTexture2DArray(anotherTexture.Texture.Device.OpenSharedResource<Texture2D>(sharedHandle));
        }
        public CSTexture2DArray Share(IntPtr devicePointer)
        {
            //source: https://stackoverflow.com/questions/41625272/direct3d11-sharing-a-texture-between-devices-black-texture
            SharpDX.DXGI.Resource copy = Texture.QueryInterface<SharpDX.DXGI.Resource>();
            IntPtr sharedHandle = copy.SharedHandle;
            Device dev = new Device(devicePointer);
            return new CSTexture2DArray(dev.OpenSharedResource<Texture2D>(sharedHandle));
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
                Texture2DArray = new UnorderedAccessViewDescription.Texture2DArrayResource()
                {
                    ArraySize = Textures,
                    FirstArraySlice = 0,
                    MipSlice = 0, //if didn't work try 1
                },
                Dimension = UnorderedAccessViewDimension.Texture2DArray,
                Format = Texture.Description.Format,
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

            if (alreadyMapping)
            {
                UnMap();
            }
        }
    }
}
