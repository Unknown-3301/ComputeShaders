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
            List<Bitmap> temps = new List<Bitmap>();
            Device = device;

            foreach (Bitmap bitmap in bitmaps)
            {
                description.Width = bitmap.Width;
                description.Height = bitmap.Height;
                FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes(description.Format);

                Bitmap temp;
                rectangles.Add(Utilities.GetReversedBitmap(bitmap, out temp));
                temps.Add(temp);
            }

            resource = new Texture2D(device.device, description, rectangles.ToArray());
            temps.ForEach(x => x.Dispose());
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
        internal CSTexture2DArray(ShaderResource<Texture2D> shaderResource)
        {
            Device = shaderResource.Device;
            resource = shaderResource.resource;
            stagingResource = shaderResource.stagingResource;
            FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes(resource.Description.Format);
        }

        /// <summary>
        /// Create a new texture array using a texture array pointer
        /// </summary>
        /// <param name="nativePointer">The texture array pointer</param>
        /// <param name="format">The format of the texture</param>
        public CSTexture2DArray(IntPtr nativePointer, TextureFormat format)
        {
            resource = new Texture2D(nativePointer);
            FormatSizeInBytes = SharpDX.DXGI.FormatHelper.SizeOfInBytes((SharpDX.DXGI.Format)format);
        }

        internal override uint GetResourceSize()
        {
            return (uint)(Width * Height * Textures * FormatSizeInBytes);
        }
        internal override ShaderResource<Texture2D> CreateSharedResource(Texture2D resource, CSDevice device)
        {
            return new CSTexture2DArray(resource, device);
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
        /// Connects this resource to another device so that data can read/write between the resource and the device or any resource connected to it directly. 
        /// <br>NOTE: after calling this function if any changes occured to the resource or the shared version then <see cref="ShaderResource{T}.Flush"/> must be called on the changed resource.</br>
        /// </summary>
        /// <param name="device">The device to connect with.</param>
        /// <returns></returns>
        public new CSTexture2DArray Share(CSDevice device)
        {
            return new CSTexture2DArray(base.Share(device));
        }
        /// <summary>
        /// Connects this resource to another resource so that data can read/write between the resource and the other resource or any resource connected to it directly. 
        /// <br>NOTE: after calling this function if any changes occured to the resource or the shared version then <see cref="ShaderResource{T}.Flush"/> must be called on the changed resource.</br>
        /// </summary>
        /// <param name="another">The another shader resource to connect with</param>
        /// <returns></returns>
        public new CSTexture2DArray Share<T>(ShaderResource<T> another) where T : Resource
        {
            return new CSTexture2DArray(base.Share(another));
        }
        /// <summary>
        /// Connects this resource to a Direct3D 11 device so that data can read/write between the resource and the other resource or any resource connected to it directly. 
        /// <br>NOTE: after calling this function if any changes occured to the resource or the shared version then <see cref="ShaderResource{T}.Flush"/> must be called on the changed resource.</br>/// </summary>
        /// <param name="devicePointer">The Direct3D 11 device to connect with</param>
        /// <returns></returns>
        public new CSTexture2DArray Share(IntPtr devicePointer)
        {
            return new CSTexture2DArray(base.Share(devicePointer));
        }

        /// <summary>
        /// Write to the a slice (a slice from texture array means a single texture2D) raw data (using only cpu) by an write function.
        /// NOTE: the data box pointer is aligned to 16 bytes. Check: https://learn.microsoft.com/en-us/windows/win32/api/d3d11/ns-d3d11-d3d11_mapped_subresource
        /// </summary>
        /// <param name="writeAction"></param>
        /// <param name="sliceIndex">The index of the texture (slice) in the texture array.</param>
        public void WriteToSliceRawData(Action<TextureDataBox> writeAction, int sliceIndex)
        {
            if (!CPU_ReadWrite)
            {
                throw new Exception("Cannot use WriteToRawData because CPU read/write ability is disabled. To enable it call EnableCPU_ReadWrite function.");
            }

            resource.Device.ImmediateContext.CopyResource(resource, stagingResource);

            int mipSize;
            TextureDataBox box = new TextureDataBox(resource.Device.ImmediateContext.MapSubresource(stagingResource, 0, sliceIndex, MapMode.Write, MapFlags.None, out mipSize));

            try
            {
                writeAction(box);
            }
            finally
            {
                resource.Device.ImmediateContext.UnmapSubresource(stagingResource, 0);
            }

            resource.Device.ImmediateContext.CopyResource(stagingResource, resource);
        }
        /// <summary>
        /// Reads through the slice (a slice from texture array means a single texture2D) raw data using 'readAction'.
        /// NOTE: all the reading process must ONLY be done inside 'readAction' function. Also, the data box pointer is aligned to 16 bytes. Check: https://learn.microsoft.com/en-us/windows/win32/api/d3d11/ns-d3d11-d3d11_mapped_subresource
        /// </summary>
        /// <param name="readAction"></param>
        /// <param name="sliceIndex">The index of the texture (slice) in the texture array.</param>
        public void ReadFromSliceRawData(Action<TextureDataBox> readAction, int sliceIndex)
        {
            if (!CPU_ReadWrite)
            {
                throw new Exception("Cannot use GetRawDataIntPtr because CPU read/write ability is disabled. To enable it call EnableCPU_ReadWrite function.");
            }

            resource.Device.ImmediateContext.CopyResource(resource, stagingResource);

            int mipSize;
            TextureDataBox box = new TextureDataBox(resource.Device.ImmediateContext.MapSubresource(stagingResource, 0, sliceIndex, MapMode.Read, MapFlags.None, out mipSize));

            try
            {
                readAction(box);
            }
            finally
            {
                resource.Device.ImmediateContext.UnmapSubresource(stagingResource, 0);
            }
        }


        internal UnorderedAccessView CreateUAV(Device device)
        {
            unorderedAccessView = new UnorderedAccessView(device, resource, new UnorderedAccessViewDescription()
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
