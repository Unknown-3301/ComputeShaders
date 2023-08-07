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
    /// The class that holds the texture array data.
    /// </summary>
    public class CSTexture2DArray : ShaderResource<Texture2D>
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
        /// The number of textures in the texture array
        /// </summary>
        public int Textures { get => resource.Description.ArraySize; }
        /// <summary>
        /// The format of the texture
        /// </summary>
        public TextureFormat Format { get => (TextureFormat)resource.Description.Format; }

        internal CSTexture2DArray(CSDevice device, int width, int height, int numberOfTextures, Texture2DDescription description)
        {
            Texture2DDescription dDescription = description;
            dDescription.Width = width;
            dDescription.Height = height;
            dDescription.ArraySize = numberOfTextures;
            FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes(description.Format);
            Device = device;

            resource = new Texture2D(device.device, dDescription);
        }
        internal CSTexture2DArray(CSDevice device, IEnumerable<Bitmap> bitmaps, Texture2DDescription description)
        {
            // souce: https://stackoverflow.com/questions/36068631/sharpdx-3-0-2-d3d11-how-to-load-texture-from-file-and-make-it-to-work-in-shade

            List<DataRectangle> rectangles = new List<DataRectangle>();
            Device = device;

            foreach (Bitmap bitmap in bitmaps)
            {
                description.Width = bitmap.Width;
                description.Height = bitmap.Height;
                FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes(description.Format);

                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

                rectangles.Add(new DataRectangle(data.Scan0, data.Stride));

                bitmap.UnlockBits(data);
            }

            resource = new Texture2D(device.device, description, rectangles.ToArray());
        }
        internal CSTexture2DArray(CSDevice device, IntPtr[] slicesDataPointers, Texture2DDescription description)
        {
            // souce: https://stackoverflow.com/questions/36068631/sharpdx-3-0-2-d3d11-how-to-load-texture-from-file-and-make-it-to-work-in-shade

            FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes(description.Format);
            Device = device;

            DataRectangle[] rectangles = new DataRectangle[slicesDataPointers.Length];
            for (int i = 0; i < rectangles.Length; i++)
            {
                rectangles[i] = new DataRectangle(slicesDataPointers[i], description.Width * FormatSizeInBytes);
            }

            resource = new Texture2D(device.device, description, rectangles);

        }
        internal CSTexture2DArray(Texture2D texture, CSDevice device)
        {
            Device = device;
            resource = texture;
            FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes(texture.Description.Format);
        }

        /// <summary>
        /// Create a new texture array using a texture array pointer
        /// </summary>
        /// <param name="nativePointer">The texture array pointer</param>
        /// <param name="format">The format of the texture</param>
        public CSTexture2DArray(IntPtr nativePointer, TextureFormat format)
        {
            resource = new Texture2D(nativePointer);
            Device = new CSDevice(resource.Device.NativePointer);
            FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes((SharpDX.DXGI.Format)format);
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

        /// <summary>
        /// Copy the contents of this resource to <paramref name="destination"/>.
        /// NOTE: In case that the 2 resources connect to different device, CPU read/write ability must be enabled for both resources. Both resource must have exact same dimensions. When the two resources connect to different devices, this function is so slow compared to other methods like using shared resources.
        /// </summary>
        /// <param name="destination"></param>
        public void CopyTo(CSTexture2DArray destination)
        {
            if (destination.Device != Device)
            {
                destination.AccessRawData(desBox =>
                {
                    AccessRawData(scrBox =>
                    {
                        Utilities.CopyMemory(desBox.DataPointer, scrBox.DataPointer, (uint)(Math.Ceiling(Width / 16f) * 16 * Height * Textures * FormatSizeInBytes));
                    }, CPUAccessMode.Read);
                }, CPUAccessMode.Write);
            }
            else
            {
                resource.Device.ImmediateContext.CopyResource(resource, destination.resource);
            }
        }

        /// <summary>
        /// Copies an entire slice (a texture from the texutre array) to <paramref name="destination"/>.
        /// </summary>
        /// <param name="destination">The texture to copy to.</param>
        /// <param name="sliceIndex">The index to the slice to copy from.</param>
        public void CopySliceTo(CSTexture2D destination, int sliceIndex)
        {
            Device.device.ImmediateContext.CopySubresourceRegion(resource, sliceIndex, null, destination.resource, 0);
        }

        /// <summary>
        /// Copies a slice (a texture from the texutre array), starting from (<paramref name="scrStartX"/>, <paramref name="scrStartY"/>) to <paramref name="destination"/> starting from (<paramref name="dstStartX"/>, <paramref name="dstStartY"/>).
        /// </summary>
        /// <param name="destination">The texture to copy to.</param>
        /// <param name="sliceIndex">The index to the slice to copy from.</param>
        /// <param name="copyWidth">The number of pixels to copy along the x-axis.</param>
        /// <param name="copyHeight">The number of pixels to copy along the y-axis.</param>
        /// <param name="scrStartX">The x index to start copying from.</param>
        /// <param name="scrStartY">The y index to start copying from.</param>
        /// <param name="dstStartX">The x index to start copying to.</param>
        /// <param name="dstStartY">The y index to start copying to.</param>
        public void CopySliceTo(CSTexture2D destination, int sliceIndex, int copyWidth, int copyHeight, int scrStartX = 0, int scrStartY = 0, int dstStartX = 0, int dstStartY = 0)
        {
            Device.device.ImmediateContext.CopySubresourceRegion(resource, sliceIndex, new ResourceRegion(scrStartX, scrStartY, 0, copyWidth + scrStartX, copyHeight + scrStartY, 1), destination.resource, 0, dstStartX, dstStartY);
        }

        /// <summary>
        /// Gives the cpu access to the raw data stored in a slice (a single texture in the texture array) in the resource (by mapping https://learn.microsoft.com/en-us/windows/win32/api/d3d11/ns-d3d11-d3d11_mapped_subresource). During mapping:
        /// <br>- GPU access to the resource is denied.</br>
        /// <br>- The size of the first dimension of the resource (length in buffer, width in texture...) in bytes is rounded to the closest multiple of 16 that is larger than or equal to the original size.</br>
        /// <br>Note that <see cref="ShaderResource{T}.CPU_ReadWrite"/> must be true, and that all the accessing can only be done inside <paramref name="accessAction"/>..</br>
        /// </summary>
        /// <param name="accessAction">The action to access the data.</param>
        /// <param name="mode">The mode of access.</param>
        /// <param name="sliceIndex">The index of the texture (slice) in the texture array.</param>
        public void AccessSliceRawData(Action<TextureDataBox> accessAction, CPUAccessMode mode, int sliceIndex)
        {
            if (!CPU_ReadWrite)
            {
                throw new Exception("Cannot use AccessSliceRawData because CPU read/write ability is disabled. To enable it call EnableCPU_ReadWrite function.");
            }

            resource.Device.ImmediateContext.CopyResource(resource, stagingResource);

            int mipSize;
            TextureDataBox box = new TextureDataBox(resource.Device.ImmediateContext.MapSubresource(stagingResource, 0, sliceIndex, (MapMode)mode, MapFlags.None, out mipSize));

            try
            {
                accessAction(box);
            }
            finally
            {
                resource.Device.ImmediateContext.UnmapSubresource(stagingResource, 0);
            }

            resource.Device.ImmediateContext.CopyResource(stagingResource, resource);
        }

        /// <summary>
        /// Creates a resource that Gives its device (<paramref name="device"/>) access to a shared resource (this resource) created on a different device (this resource's device). In other words, it creates a resource with <paramref name="device"/> that is connected to this resource's device through this resource. There are important notes regarding shared resources:
        /// <br>- If any of the two shared resources (the result resource and this resource) is updated, <see cref="CSDevice.Flush()"/> must be called.</br>
        /// <br>- In some cases, updating a shared resource and using <see cref="CSDevice.Flush()"/> might causes problems if that shared resource was used with an asynchronous function. For example, when <see cref="CSDevice.Flush()"/> is used then <see cref="ShaderResource{T}.CopyResource(ShaderResource{T})"/> is called using that shared resources. In such cases, it is adviced to call <see cref="CSDevice.Synchronize()"/> afterwards.</br>
        /// </summary>
        /// <param name="device">another device.</param>
        /// <returns></returns>
        public CSTexture2DArray Share(CSDevice device) => new CSTexture2DArray(CreateSharedResource(device.device), device);
        /// <summary>
        /// Creates a resource that Gives its device (<paramref name="devicePointer"/>) access to a shared resource (this resource) created on a different device (this resource's device). In other words, it creates a resource with <paramref name="devicePointer"/> that is connected to this resource's device through this resource. There are important notes regarding shared resources:
        /// <br>- If any of the two shared resources (the result resource and this resource) is updated, <see cref="CSDevice.Flush()"/> must be called.</br>
        /// <br>- In some cases, updating a shared resource and using <see cref="CSDevice.Flush()"/> might causes problems if that shared resource was used with an asynchronous function. For example, when <see cref="CSDevice.Flush()"/> is used then <see cref="ShaderResource{T}.CopyResource(ShaderResource{T})"/> is called using that shared resources. In such cases, it is adviced to call <see cref="CSDevice.Synchronize()"/> afterwards.</br>
        /// </summary>
        /// <param name="devicePointer">another device.</param>
        /// <returns></returns>
        public CSTexture2DArray Share(IntPtr devicePointer) => new CSTexture2DArray(CreateSharedResource(new Device(devicePointer)), new CSDevice(devicePointer));

        /// <inheritdoc/>
        public override void UpdateSubresource(IntPtr dataPointer)
        {
            int rowPitch = Width * FormatSizeInBytes;
            int slicePitch = rowPitch * Height;

            Device.device.ImmediateContext.UpdateSubresource(new DataBox(dataPointer, rowPitch, slicePitch), resource);
        }

        /// <inheritdoc/>
        internal override UnorderedAccessView CreateUAV()
        {
            unorderedAccessView = new UnorderedAccessView(Device.device, resource, new UnorderedAccessViewDescription()
            {
                Texture2DArray = new UnorderedAccessViewDescription.Texture2DArrayResource()
                {
                    ArraySize = Textures,
                    FirstArraySlice = 0,
                    MipSlice = 0, //if didn't work try 1
                },
                Dimension = UnorderedAccessViewDimension.Texture2DArray,
                Format = resource.Description.Format,
            });

            return unorderedAccessView;
        }
    }
}
