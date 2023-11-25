using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;


namespace ComputeShaders
{
    /// <summary>
    /// The class that holds the texture data.
    /// </summary>
    public class CSTexture2D : ShaderResource<Texture2D>
    {
        internal int FormatSizeInBytes { get; }

        /// <summary>
        /// The width of the texture.
        /// </summary>
        public int Width { get => resource.Description.Width; }
        /// <summary>
        /// The height of the texture.
        /// </summary>
        public int Height { get => resource.Description.Height; }
        /// <summary>
        /// The format of the texture.
        /// </summary>
        public TextureFormat Format { get => (TextureFormat)resource.Description.Format; }

        internal CSTexture2D(CSDevice device, int width, int height, Texture2DDescription description)
        {
            Texture2DDescription dDescription = description;
            dDescription.Width = width;
            dDescription.Height = height;
            FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes(description.Format);
            Device = device;

            resource = new Texture2D(device.device, dDescription);
        }
        internal CSTexture2D(CSDevice device, Bitmap bitmap, Texture2DDescription description)
        {
            // souce: https://stackoverflow.com/questions/36068631/sharpdx-3-0-2-d3d11-how-to-load-texture-from-file-and-make-it-to-work-in-shade

            description.Width = bitmap.Width;
            description.Height = bitmap.Height;
            FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes(description.Format);
            Device = device;

            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

            try
            {
                resource = new Texture2D(device.device, description, new DataRectangle(data.Scan0, data.Stride));
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
            
        }
        internal CSTexture2D(CSDevice device, IntPtr dataPointer, Texture2DDescription description)
        {
            // souce: https://stackoverflow.com/questions/36068631/sharpdx-3-0-2-d3d11-how-to-load-texture-from-file-and-make-it-to-work-in-shade

            FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes(description.Format);

            Device = device;
            resource = new Texture2D(device.device, description, new DataRectangle(dataPointer, description.Width * FormatSizeInBytes));

        }
        internal CSTexture2D(Texture2D texture, CSDevice device)
        {
            Device = device;
            resource = texture;
            FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes(texture.Description.Format);
        }

        /// <summary>
        /// Create a new CStexture2D instance using a direct3D11 texture pointer.
        /// </summary>
        /// <param name="nativePointer">The texture pointer.</param>
        public CSTexture2D(IntPtr nativePointer)
        {
            resource = new Texture2D(nativePointer);
            Device = new CSDevice(resource.Device.NativePointer);
            FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes(resource.Description.Format);
        }

        /// <summary>
        /// Copy the contents of this resource to <paramref name="destination"/>.
        /// NOTE: In case that the 2 resources connect to different device, CPU read/write ability must be enabled for both resources. Both resource must have exact same dimensions. When the two resources connect to different devices, this function is so slow compared to other methods like using shared resources.
        /// </summary>
        /// <param name="destination"></param>
        public void CopyTo(CSTexture2D destination)
        {
            if (!destination.Device.SameNativeDevice(Device))
            {
                destination.AccessRawData(desBox =>
                {
                    AccessRawData(scrBox =>
                    {
                        if (desBox.RowPitch == scrBox.RowPitch)
                            Utilities.CopyMemory(desBox.DataPointer, scrBox.DataPointer, (uint)(desBox.RowPitch * Height * FormatSizeInBytes));
                        else
                        {
                            IntPtr src = scrBox.DataPointer;
                            IntPtr dst = desBox.DataPointer;

                            for (int y = 0; y < Height; y++)
                            {
                                Utilities.CopyMemory(dst, src, (uint)(Width * FormatSizeInBytes));

                                src = IntPtr.Add(src, scrBox.RowPitch);
                                dst = IntPtr.Add(dst, desBox.RowPitch);
                            }
                        }
                    
                    }, CPUAccessMode.Read);
                }, CPUAccessMode.Write);
            }
            else
            {
                resource.Device.ImmediateContext.CopyResource(resource, destination.resource);
            }
        }

        /// <summary>
        /// Copies data to <paramref name="destination"/>.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="srcX">The x-coordinate of the pixel to start copy from.</param>
        /// <param name="srcY">The y-coordinate of the pixel to start copy from.</param>
        /// <param name="dstX">The x-coordinate of the pixel to start copy to.</param>
        /// <param name="dstY">The y-coordinate of the pixel to start copy to.</param>
        /// <param name="widthCount">the width of the region to copy in pixels.</param>
        /// <param name="heightCount">the height of the region to copy in pixels.</param>
        public void CopyTo(CSTexture2D destination, int srcX, int srcY, int dstX, int dstY, int widthCount, int heightCount)
        {
            if (srcX < 0 || srcY < 0 || dstX < 0 || dstY < 0 || srcX >= Width || srcY >= Height || dstX >= destination.Width || dstY >= destination.Height)
                throw new IndexOutOfRangeException();

            if (widthCount < 0 || heightCount < 0 || srcX + widthCount > Width || srcY + heightCount > Height || dstX + widthCount > destination.Width || dstY + heightCount > destination.Height)
                throw new ArgumentOutOfRangeException();

            if (!destination.Device.SameNativeDevice(Device))
            {
                destination.AccessRawData(desBox =>
                {
                    AccessRawData(scrBox =>
                    {
                        if (desBox.RowPitch == scrBox.RowPitch && widthCount == Width)
                            Utilities.CopyMemory(desBox.DataPointer, scrBox.DataPointer, (uint)(desBox.RowPitch * heightCount * FormatSizeInBytes));
                        else
                        {
                            IntPtr src = IntPtr.Add(scrBox.DataPointer, srcX * FormatSizeInBytes + srcY * scrBox.RowPitch);
                            IntPtr dst = IntPtr.Add(desBox.DataPointer, dstX * FormatSizeInBytes + dstY * desBox.RowPitch);

                            for (int y = 0; y < heightCount; y++)
                            {
                                Utilities.CopyMemory(dst, src, (uint)(widthCount * FormatSizeInBytes));

                                src = IntPtr.Add(src, scrBox.RowPitch);
                                dst = IntPtr.Add(dst, desBox.RowPitch);
                            }
                        }

                    }, CPUAccessMode.Read);
                }, CPUAccessMode.Write);
            }
            else
            {
                resource.Device.ImmediateContext.CopySubresourceRegion(resource, 0, new ResourceRegion(srcX, srcY, 0, srcX + widthCount, srcY + heightCount, 1), destination.resource, 0, dstX, dstY);
            }
        }

        /// <summary>
        /// Creates a resource that Gives its device (<paramref name="device"/>) access to a shared resource (this resource) created on a different device (this resource's device). In other words, it creates a resource with <paramref name="device"/> that is connected to this resource's device through this resource. There are important notes regarding shared resources:
        /// <br>- If any of the two shared resources (the result resource and this resource) is updated, <see cref="CSDevice.Flush()"/> must be called.</br>
        /// <br>- In some cases, updating a shared resource and using <see cref="CSDevice.Flush()"/> might causes problems if that shared resource was used with an asynchronous function. For example, when <see cref="CSDevice.Flush()"/> is used then <see cref="ShaderResource{T}.CopyResource(ShaderResource{T})"/> is called using that shared resources. In such cases, it is adviced to call <see cref="CSDevice.Synchronize()"/> afterwards.</br>
        /// </summary>
        /// <param name="device">another device.</param>
        /// <returns></returns>
        public CSTexture2D Share(CSDevice device) => new CSTexture2D(CreateSharedResource(device.device), device);
        /// <summary>
        /// Creates a resource that Gives its device (<paramref name="devicePointer"/>) access to a shared resource (this resource) created on a different device (this resource's device). In other words, it creates a resource with <paramref name="devicePointer"/> that is connected to this resource's device through this resource. There are important notes regarding shared resources:
        /// <br>- If any of the two shared resources (the result resource and this resource) is updated, <see cref="CSDevice.Flush()"/> must be called.</br>
        /// <br>- In some cases, updating a shared resource and using <see cref="CSDevice.Flush()"/> might causes problems if that shared resource was used with an asynchronous function. For example, when <see cref="CSDevice.Flush()"/> is used then <see cref="ShaderResource{T}.CopyResource(ShaderResource{T})"/> is called using that shared resources. In such cases, it is adviced to call <see cref="CSDevice.Synchronize()"/> afterwards.</br>
        /// </summary>
        /// <param name="devicePointer">another device.</param>
        /// <returns></returns>
        public CSTexture2D Share(IntPtr devicePointer) => new CSTexture2D(CreateSharedResource(new Device(devicePointer)), new CSDevice(devicePointer));

        /// <inheritdoc/>
        public override void UpdateSubresource(IntPtr dataPointer)
        {
            int rowPitch = Width * FormatSizeInBytes;
            int slicePitch = rowPitch * Height;

            Device.device.ImmediateContext.UpdateSubresource(new DataBox(dataPointer, rowPitch, slicePitch), resource);
        }

        /// <inheritdoc/>
        public override void EnableCPU_Raw_ReadWrite()
        {
            if (CPU_ReadWrite)
                return;

            stagingResource = new Texture2D(resource.Device, new Texture2DDescription()
            {
                ArraySize = resource.Description.ArraySize,
                CpuAccessFlags = CpuAccessFlags.Write | CpuAccessFlags.Read,
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

        /// <inheritdoc/>
        internal override UnorderedAccessView CreateUAV()
        {
            unorderedAccessView = new UnorderedAccessView(Device.device, resource, new UnorderedAccessViewDescription()
            {
                Format = resource.Description.Format,
                Dimension = UnorderedAccessViewDimension.Texture2D,
                Texture2D = { MipSlice = 0 }
            });

            return unorderedAccessView;
        }
    }
}
