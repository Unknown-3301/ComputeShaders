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
    public class CSTexture2D : ShaderResource<Texture2D>
    {
        internal int FormatSizeInBytes { get; }

        /// <summary>
        /// The width of the texture
        /// </summary>
        public int Width { get => resource.Description.Width; }
        /// <summary>
        /// The height of the texture
        /// </summary>
        public int Height { get => resource.Description.Height; }
        /// <summary>
        /// The format of the texture
        /// </summary>
        public TextureFormat Format { get => (TextureFormat)resource.Description.Format; }

        internal CSTexture2D(Device device, int width, int height, Texture2DDescription description)
        {
            Texture2DDescription dDescription = description;
            dDescription.Width = width;
            dDescription.Height = height;
            FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes(description.Format);

            resource = new Texture2D(device, dDescription);
        }
        internal CSTexture2D(Device device, Bitmap bitmap, Texture2DDescription description)
        {
            // souce: https://stackoverflow.com/questions/36068631/sharpdx-3-0-2-d3d11-how-to-load-texture-from-file-and-make-it-to-work-in-shade

            Bitmap temp;
            description.Width = bitmap.Width;
            description.Height = bitmap.Height;
            FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes(description.Format);

            resource = new Texture2D(device, description, Utilities.GetReversedBitmap(bitmap, out temp));
            temp.Dispose();
        }
        internal CSTexture2D(Device device, IntPtr dataPointer, Texture2DDescription description)
        {
            // souce: https://stackoverflow.com/questions/36068631/sharpdx-3-0-2-d3d11-how-to-load-texture-from-file-and-make-it-to-work-in-shade

            FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes(description.Format);

            resource = new Texture2D(device, description, new DataRectangle(dataPointer, description.Width * FormatSizeInBytes));

        }
        internal CSTexture2D(Texture2D texture)
        {
            resource = texture;
            FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes(texture.Description.Format);
        }
        internal CSTexture2D(ShaderResource<Texture2D> shaderResource)
        {
            resource = shaderResource.resource;
            stagingResource = shaderResource.stagingResource;
            FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes(resource.Description.Format);
        }

        /// <summary>
        /// Create a new texture using a texture pointer
        /// </summary>
        /// <param name="nativePointer">The texture pointer</param>
        /// <param name="format">The format of the texture</param>
        public CSTexture2D(IntPtr nativePointer, TextureFormat format)
        {
            resource = new Texture2D(nativePointer);
            FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes((SharpDX.DXGI.Format)format);
        }

        internal override uint GetResourceSize()
        {
            return (uint)(Width * Height * FormatSizeInBytes);
        }
        internal override ShaderResource<Texture2D> CreateSharedResource(Texture2D resource)
        {
            return new CSTexture2D(resource);
        }

        /// <summary>
        /// Disables the ability to read/write the resource raw data using cpu. Disables it has the advantages (if cpu read/write was enabled):
        /// <br>- may increase the performance.</br>
        /// <br>- decrease the memory usage to almost the half.</br>
        /// <br>and has the disadvantages:</br>
        /// <br>- can not read the resource raw data using GetRawDataIntPtr function.</br>
        /// <br>- can not write to the resource raw data using WriteToRawData function.</br>
        /// </summary>
        public override void EnableCPU_Raw_ReadWrite()
        {
            if (CPU_ReadWrite)
                return;

            stagingResource = new Texture2D(resource.Device, new Texture2DDescription()
            {
                ArraySize = resource.Description.ArraySize,
                CpuAccessFlags = resource.Description.CpuAccessFlags,
                BindFlags = BindFlags.None,
                Format = resource.Description.Format,
                Height = resource.Description.Height,
                Width = resource.Description.Width,
                MipLevels = resource.Description.MipLevels,
                Usage = ResourceUsage.Staging,
                SampleDescription = resource.Description.SampleDescription,
                OptionFlags = ResourceOptionFlags.None,
            });
        }

        /// <summary>
        /// Connects this resource to another compute shader so that data can read/write between the resource and the compute shader or any resource connected to it directly. NOTE: after calling this function if any changes occured to the resource or the shared version then Flush() must be called on the changed resource.
        /// </summary>
        /// <param name="shader">The compute shader to connect with.</param>
        /// <returns></returns>
        public CSTexture2D Share(ComputeShader shader)
        {
            return new CSTexture2D(base.Share(shader));
        }
        /// <summary>
        /// Connects this resource to another resource so that data can read/write between the resource and the other resource or any resource connected to it directly. NOTE: after calling this function if any changes occured to the resource or the shared version then Flush() must be called on the changed resource.
        /// </summary>
        /// <param name="another">The another shader resource to connect with</param>
        /// <returns></returns>
        public CSTexture2D Share<T>(ShaderResource<T> another) where T : Resource
        {
            return new CSTexture2D(base.Share(another));
        }
        /// <summary>
        /// Connects this resource to a Direct3D 11 device so that data can read/write between the resource and the other resource or any resource connected to it directly. NOTE: after calling this function if any changes occured to the resource or the shared version then Flush() must be called on the changed resource.
        /// </summary>
        /// <param name="devicePointer">The Direct3D 11 device to connect with</param>
        /// <returns></returns>
        public CSTexture2D Share(IntPtr devicePointer)
        {
            return new CSTexture2D(base.Share(devicePointer));
        }

        internal UnorderedAccessView CreateUAV(Device device)
        {
            unorderedAccessView = new UnorderedAccessView(device, resource, new UnorderedAccessViewDescription()
            {
                Format = resource.Description.Format,
                Dimension = UnorderedAccessViewDimension.Texture2D,
                Texture2D = { MipSlice = 0 }
            });

            return unorderedAccessView;
        }
    }
}
