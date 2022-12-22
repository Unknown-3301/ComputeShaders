using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;
using System.Drawing;

namespace ComputeShaders
{
    /// <summary>
    /// The main class for running compute shaders (Direct3D 11)
    /// </summary>
    public class CSDevice : IDisposable
    {
        internal Device device;

        /// <summary>
        /// The <see cref="Device"/> native pointer.
        /// </summary>
        public IntPtr DeviceNativePointer { get => device.NativePointer; }

        /// <summary>
        /// The shader that the device currently use. This is the shader that will run upon calling <see cref="Dispatch(int, int, int)"/>
        /// <br>To change it use <see cref="SetComputeShader(ComputeShader)"/>.</br>
        /// </summary>
        public ComputeShader CurrentShader { get; private set; }

        /// <summary>
        /// Creates a new device.
        /// </summary>
        /// <param name="gpuAdapterIndex">The index of the adapter to enumerate.</param>
        /// <param name="gpuCreationgFlag">Describes parameters that are used to create a device.</param>
        public CSDevice(int gpuAdapterIndex = 0, CSDeviceCreationFlags gpuCreationgFlag = CSDeviceCreationFlags.None)
        {
            try
            {
                SharpDX.DXGI.Adapter1 gpu;
                using (var factory = new SharpDX.DXGI.Factory1())
                {
                    gpu = factory.GetAdapter1(gpuAdapterIndex);
                }

                device = new Device(gpu, (DeviceCreationFlags)gpuCreationgFlag);
            }
            catch
            {
                throw new Exception("ERROR: Failed to create Device.");
            }
        }
        internal CSDevice(IntPtr deviceNativePointer)
        {
            device = new Device(deviceNativePointer);
        }

        /// <summary>
        /// Creates a compute shader connected to this device.
        /// </summary>
        /// <param name="shaderByteCode">The byte code of the compiled shader. You can get the compiled byte array of a shader file using <see cref="ComputeShader.CompileComputeShader(string, string, string)"/></param>
        public ComputeShader CreateComputeShader(byte[] shaderByteCode)
        {
            return new ComputeShader(shaderByteCode, this);
        }
        /// <summary>
        /// Creates a compute shader connected to this device.
        /// </summary>
        /// <param name="shaderName">The path to the compute shader (relative to the solution this code is in)</param>
        /// <param name="entryPoint">The main kernel function of the shader</param>
        /// <param name="targetProfile">The type and version of the shader. default = cs_4_0. The type and version of the shader. default = cs_5_0. (cs is for Compute shader) (5_0 is for shader model 5.0)</param>
        public ComputeShader CreateComputeShader(string shaderName, string entryPoint = "CSMain", string targetProfile = "cs_5_0")
        {
            return new ComputeShader(shaderName, this, entryPoint, targetProfile);
        }

        /// <summary>
        /// Creates a new texture connected to this device.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="format">the format of the texture.</param>
        /// <param name="allowSharing">Determines whether the created texture can be shared, if true then <see cref="CSTexture2D.Share(CSDevice)"/> can be called.</param>
        /// <returns></returns>
        public CSTexture2D CreateTexture2D(int width, int height, TextureFormat format, bool allowSharing = false)
        {
            Texture2DDescription description = new Texture2DDescription()
            {
                ArraySize = 1,
                CpuAccessFlags = CpuAccessFlags.Read | CpuAccessFlags.Write,
                BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
                Format = (SharpDX.DXGI.Format)format,
                Width = width,
                Height = height,
                MipLevels = 1, //for some reason if this is 0 it will not work
                OptionFlags = allowSharing ? ResourceOptionFlags.Shared : ResourceOptionFlags.None,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            };

            return new CSTexture2D(this, width, height, description);
        }
        /// <summary>
        /// Creates a new texture connected to this device containing <paramref name="bitmap"/> data.
        /// </summary>
        /// <param name="bitmap">The bitmap.</param>
        /// <param name="allowSharing">Determines whether the created texture can be shared, if true then <see cref="CSTexture2D.Share(CSDevice)"/> can be called.</param>
        /// <returns></returns>
        public CSTexture2D CreateTexture2D(Bitmap bitmap, bool allowSharing = false)
        {
            TextureFormat format = TextureFormatHelper.ConvertBitmapToFormat(bitmap.PixelFormat);
            if (format == TextureFormat.Unknown)
            {
                throw new ArgumentException("The bitmap format is not supported. Make sure its format is either Format32bppArgb or Format64bppArgb");
            }

            Texture2DDescription description = new Texture2DDescription()
            {
                ArraySize = 1,
                CpuAccessFlags = CpuAccessFlags.Read | CpuAccessFlags.Write,
                BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
                Format = (SharpDX.DXGI.Format)format,
                Width = bitmap.Width,
                Height = bitmap.Height,
                MipLevels = 1, //for some reason if this is 0 it will not work
                OptionFlags = allowSharing ? ResourceOptionFlags.Shared : ResourceOptionFlags.None,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            };

            return new CSTexture2D(this, bitmap, description);
        }
        /// <summary>
        /// Creates a new texture connected to this device containing the image from <paramref name="imageRelativePath"/> data.
        /// </summary>
        /// <param name="imageRelativePath">The relative path to the image file (relative to the solution this code is in).</param>
        /// <param name="allowSharing">Determines whether the created texture can be shared, if true then <see cref="CSTexture2D.Share(CSDevice)"/> can be called.</param>
        /// <returns></returns>
        public CSTexture2D CreateTexture2D(string imageRelativePath, bool allowSharing = false)
        {
            using (Bitmap b = Utilities.GetFlippedBitmap(imageRelativePath))
            {
                return CreateTexture2D(b, allowSharing);
            }
        }
        /// <summary>
        /// Creates a new texture connected to this device containing the data stored in <paramref name="dataPointer"/>.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="format">the format of the texture.</param>
        /// <param name="dataPointer">The pointer that holds raw texture data</param>
        /// <param name="allowSharing">Determines whether the created texture can be shared, if true then <see cref="CSTexture2D.Share(CSDevice)"/> can be called.</param>
        /// <returns></returns>
        public CSTexture2D CreateTexture2D(int width, int height, TextureFormat format, IntPtr dataPointer, bool allowSharing = false)
        {
            Texture2DDescription description = new Texture2DDescription()
            {
                ArraySize = 1,
                CpuAccessFlags = CpuAccessFlags.Read | CpuAccessFlags.Write,
                BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
                Format = (SharpDX.DXGI.Format)format,
                Height = height,
                Width = width,
                MipLevels = 1, //for some reason if this is 0 it will not work
                OptionFlags = allowSharing ? ResourceOptionFlags.Shared : ResourceOptionFlags.None,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            };

            return new CSTexture2D(this, dataPointer, description);
        }

        /// <summary>
        /// Creates a new texture connected to this device.
        /// </summary>
        /// <param name="width">The width of the texture array.</param>
        /// <param name="height">The height of the texture array.</param>
        /// <param name="numberOfTextures">The number of texture in the texture array.</param>
        /// <param name="format">the format of the texture array.</param>
        /// <param name="allowSharing">Determines whether the created texture array can be shared, if true then <see cref="CSTexture2DArray.Share(CSDevice)"/> can be called.</param>
        /// <returns></returns>
        public CSTexture2DArray CreateTexture2DArray(int width, int height, int numberOfTextures, TextureFormat format, bool allowSharing = false)
        {
            Texture2DDescription description = new Texture2DDescription()
            {
                ArraySize = numberOfTextures,
                CpuAccessFlags = CpuAccessFlags.Read | CpuAccessFlags.Write,
                BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
                Format = (SharpDX.DXGI.Format)format,
                Width = width,
                Height = height,
                MipLevels = 1, //for some reason if this is 0 it will not work
                OptionFlags = allowSharing ? ResourceOptionFlags.Shared : ResourceOptionFlags.None,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            };

            return new CSTexture2DArray(this, width, height, numberOfTextures, description);
        }
        /// <summary>
        /// Creates a new texture connected to this device where every slice in the texture array contains the data from its equivalent slice from <paramref name="bitmaps"/>.
        /// </summary>
        /// <param name="allowSharing">Determines whether the created texture can be shared, if true then <see cref="CSTexture2DArray.Share(CSDevice)"/> can be called.</param>
        /// <param name="bitmaps">The bitmaps (all with the same width, height, and format).</param>
        /// <returns></returns>
        public CSTexture2DArray CreateTexture2DArray(bool allowSharing = false, params Bitmap[] bitmaps)
        {
            TextureFormat format = TextureFormatHelper.ConvertBitmapToFormat(bitmaps[0].PixelFormat);
            if (format == TextureFormat.Unknown)
            {
                throw new ArgumentException("The bitmap format is not supported. Make sure its format is either Format32bppArgb or Format64bppArgb");
            }

            Texture2DDescription description = new Texture2DDescription()
            {
                ArraySize = bitmaps.Length,
                CpuAccessFlags = CpuAccessFlags.Read | CpuAccessFlags.Write,
                BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
                Format = (SharpDX.DXGI.Format)format,
                Width = bitmaps[0].Width,
                Height = bitmaps[0].Height,
                MipLevels = 1, //for some reason if this is 0 it will not work
                OptionFlags = allowSharing ? ResourceOptionFlags.Shared : ResourceOptionFlags.None,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            };

            return new CSTexture2DArray(this, bitmaps, description);
        }
        /// <summary>
        /// Creates a new texture connected to this device where every slice in the texture array contains the data from its equivalent slice from <paramref name="dataPointers"/>.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="format">the format of the texture.</param>
        /// <param name="allowSharing">Determines whether the created texture can be shared, if true then <see cref="CSTexture2DArray.Share(CSDevice)"/> can be called.</param>
        /// <param name="dataPointers">The pointer that holds raw texture data.</param>
        /// <returns></returns>
        public CSTexture2DArray CreateTexture2DArray(int width, int height, TextureFormat format, bool allowSharing = false, params IntPtr[] dataPointers)
        {
            Texture2DDescription description = new Texture2DDescription()
            {
                ArraySize = dataPointers.Length,
                CpuAccessFlags = CpuAccessFlags.Read | CpuAccessFlags.Write,
                BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
                Format = (SharpDX.DXGI.Format)format,
                Height = height,
                Width = width,
                MipLevels = 1, //for some reason if this is 0 it will not work
                OptionFlags = allowSharing ? ResourceOptionFlags.Shared : ResourceOptionFlags.None,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            };

            return new CSTexture2DArray(this, dataPointers, description);
        }

        /// <summary>
        /// Creates a new constant buffer connected to this device that stores <paramref name="data"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data">The data to be stored in the buffer.</param>
        /// <param name="dataSizeInBytes">The size of the data in bytes.</param>
        /// <returns></returns>
        public CSCBuffer<T> CreateBuffer<T>(T data, int dataSizeInBytes) where T : struct
        {
            return new CSCBuffer<T>(this, data, dataSizeInBytes);
        }

        /// <summary>
        /// Creates a new StructuredBuffer connected to this device containing <paramref name="array"/> data.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array">The array to be stored in the buffer.</param>
        /// <param name="eachElementSizeInBytes">The size of an element in <paramref name="array"/> in bytes.</param>
        /// <param name="allowSharing">Determines whether the created texture can be shared, if true then <see cref="CSStructuredBuffer{T}.Share(CSDevice)"/> can be called.</param>
        /// <returns></returns>
        public CSStructuredBuffer<T> CreateStructuredBuffer<T>(T[] array, int eachElementSizeInBytes, bool allowSharing = false) where T : struct
        {
            return new CSStructuredBuffer<T>(this, array, eachElementSizeInBytes, allowSharing);
        }
        /// <summary>
        /// Creates a new StructuredBuffer connected to this device containing <paramref name="list"/> data.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list to be stored.</param>
        /// <param name="eachElementSizeInBytes">The size of an element in <paramref name="list"/> in bytes.</param>
        /// <param name="allowSharing">Determines whether the created texture can be shared, if true then <see cref="CSStructuredBuffer{T}.Share(CSDevice)"/> can be called.</param>
        /// <returns></returns>
        public CSStructuredBuffer<T> CreateStructuredBuffer<T>(List<T> list, int eachElementSizeInBytes, bool allowSharing = false) where T : struct
        {
            return new CSStructuredBuffer<T>(this, list, eachElementSizeInBytes, allowSharing);
        }

        /// <summary>
        /// connects the texture to a RWTexture2D in the current compute shader.
        /// This function is can be called only once, even if a new compute shader is set using <see cref="SetComputeShader(ComputeShader)"/>. The only case to call this again is if a new compute shader is set and the <paramref name="register_uav_index"/>
        /// in that new shader is different from the old shader, then this function must be called again with <paramref name="register_uav_index"/> be the new uav index.
        /// </summary>
        /// <param name="texture">The texture to be set.</param>
        /// <param name="register_uav_index">the register index of the texture in the compute shader.</param>
        public void SetRWTexture2D(CSTexture2D texture, int register_uav_index)
        {
            UnorderedAccessView view = texture.unorderedAccessView;
            if (view == null)
            {
                view = texture.CreateUAV(device);
            }

            device.ImmediateContext.ComputeShader.SetUnorderedAccessView(register_uav_index, view);

        }
        /// <summary>
        /// connects the texture to a RWTexture2DArray in the computeShader.
        /// This function is can be called only once, even if a new compute shader is set using <see cref="SetComputeShader(ComputeShader)"/>. The only case to call this again is if a new compute shader is set and the <paramref name="register_uav_index"/>
        /// in that new shader is different from the old shader, then this function must be called again with <paramref name="register_uav_index"/> be the new uav index.
        /// </summary>
        /// <param name="textureArray">The texture to be set.</param>
        /// <param name="register_uav_index">the register index of the texture in the compute shader.</param>
        public void SetRWTexture2DArray(CSTexture2DArray textureArray, int register_uav_index)
        {
            UnorderedAccessView view = textureArray.unorderedAccessView;
            if (view == null)
            {
                view = textureArray.CreateUAV(device);
            }

            device.ImmediateContext.ComputeShader.SetUnorderedAccessView(register_uav_index, view);
        }
        /// <summary>
        /// connects the buffer to a cbuffer in the computeShader. 
        /// This function is can be called only once, even if a new compute shader is set using <see cref="SetComputeShader(ComputeShader)"/>. The only case to call this again is if a new compute shader is set and the <paramref name="register_buffer_index"/>
        /// in that new shader is different from the old shader, then this function must be called again with <paramref name="register_buffer_index"/> be the new uav index.
        /// </summary>
        /// <param name="cSBuffer">The buffer to be set.</param>
        /// <param name="register_buffer_index">the register index of the buffer in the compute shader.</param>
        public void SetBuffer<T>(CSCBuffer<T> cSBuffer, int register_buffer_index) where T : struct
        {
            device.ImmediateContext.ComputeShader.SetConstantBuffer(register_buffer_index, cSBuffer.buffer);
        }
        /// <summary>
        /// connects the structured buffer to a RWStructuredBuffer in the computeShader.
        /// This function is can be called only once, even if a new compute shader is set using <see cref="SetComputeShader(ComputeShader)"/>. The only case to call this again is if a new compute shader is set and the <paramref name="register_uav_index"/>
        /// in that new shader is different from the old shader, then this function must be called again with <paramref name="register_uav_index"/> be the new uav index.
        /// </summary>
        /// <param name="structuredBuffer">The structured buffer to be set.</param>
        /// <param name="register_uav_index">the register index of the buffer in the compute shader</param>
        public void SetRWStructuredBuffer<T>(CSStructuredBuffer<T> structuredBuffer, int register_uav_index) where T : struct
        {
            UnorderedAccessView view = structuredBuffer.unorderedAccessView;
            if (view == null)
            {
                view = structuredBuffer.CreateUAV(device);
            }

            device.ImmediateContext.ComputeShader.SetUnorderedAccessView(register_uav_index, view);
        }


        /// <summary>
        /// Runs the current compute shader in use.
        /// <br>To know the current compute shader in use see <see cref="CurrentShader"/>.</br>
        /// </summary>
        /// <param name="threadsX">Number of threads in the x-axis</param>
        /// <param name="threadsY">Number of threads in the y-axis</param>
        /// <param name="threadsZ">Number of threads in the z-axis</param>
        public void Dispatch(int threadsX, int threadsY, int threadsZ)
        {
            device.ImmediateContext.Dispatch(threadsX, threadsY, threadsZ);
        }

        /// <summary>
        /// Sets the current compute shader in use to <paramref name="newShader"/>.
        /// </summary>
        /// <param name="newShader">The new compute shader to use.</param>
        public void SetComputeShader(ComputeShader newShader)
        {
            device.ImmediateContext.ComputeShader.Set(newShader.computeShader);
        }

        /// <summary>
        /// Disposes the unmanaged data. 
        /// </summary>
        public void Dispose()
        {
            device.Dispose();
        }

        /// <summary>
        /// If 2 devices have the same device native pointer
        /// </summary>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <returns></returns>
        public static bool operator ==(CSDevice d1, CSDevice d2) => d1.DeviceNativePointer == d2.DeviceNativePointer;
        /// <summary>
        /// If 2 devices do not have the same device native pointer
        /// </summary>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <returns></returns>
        public static bool operator !=(CSDevice d1, CSDevice d2) => d1.DeviceNativePointer != d2.DeviceNativePointer;
    }
}
