using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using System.Drawing;
using System.Drawing.Imaging;

namespace ComputeShaders
{
    // The main source for the ILMerge info: https://stackoverflow.com/questions/2556048/how-to-integrate-ilmerge-into-visual-studio-build-process-to-merge-assemblies

    /// <summary>
    /// The main class for using compute shader class (Direct3D 11)
    /// </summary>
    public class ComputeShader
    {
        public IntPtr DeviceNativePointer { get => device.NativePointer; }
        internal Device device;
        internal SharpDX.Direct3D11.ComputeShader computeShader;

        /// <summary>
        /// Creates a compute shader class
        /// </summary>
        /// <param name="shaderByteCode">The byte code of the compiled shader</param>
        /// <param name="gpuAdapterIndex">The adapter (gpu) index. default: 0</param>
        /// <param name="gpuCreationgFlag"></param>
        public ComputeShader(byte[] shaderByteCode, int gpuAdapterIndex = 0, CSDeviceCreationFlags gpuCreationgFlag = CSDeviceCreationFlags.Debug)
        {
            device = CreateDevice(gpuAdapterIndex, gpuCreationgFlag);
            computeShader = new SharpDX.Direct3D11.ComputeShader(device, shaderByteCode);

            device.ImmediateContext.ComputeShader.Set(computeShader);
        }
        /// <summary>
        /// Creates a compute shader class
        /// </summary>
        /// <param name="shaderName">The path to the compute shader (relative to the solution this code is in)</param>
        /// <param name="entryPoint">The main kernel function of the shader</param>
        /// <param name="targetProfile">The type and version of the shader. default = cs_4_0. The type and version of the shader. default = cs_5_0. (cs is for Compute shader) (5_0 is for shader model 5.0)</param>
        /// <param name="gpuAdapterIndex">The adapter (gpu) index. default: 0</param>
        /// <param name="gpuCreationgFlag"></param>
        public ComputeShader(string shaderName, string entryPoint = "CSMain", string targetProfile = "cs_5_0", int gpuAdapterIndex = 0, CSDeviceCreationFlags gpuCreationgFlag = CSDeviceCreationFlags.Debug)
        {
            device = CreateDevice(gpuAdapterIndex, gpuCreationgFlag);
            computeShader = new SharpDX.Direct3D11.ComputeShader(device, CompileComputeShader(shaderName, entryPoint, targetProfile));

            device.ImmediateContext.ComputeShader.Set(computeShader);
        }
        /// <summary>
        /// Creates a compute shader and connect it to the device (from the device native pointer)
        /// </summary>
        /// <param name="shaderByteCode">The byte code of the compiled shader</param>
        /// <param name="deviceNativePointer">The native pointer for a Direct3D 11 device</param>
        public ComputeShader(byte[] shaderByteCode, IntPtr deviceNativePointer)
        {
            device = new Device(deviceNativePointer);
            computeShader = new SharpDX.Direct3D11.ComputeShader(device, shaderByteCode);

            device.ImmediateContext.ComputeShader.Set(computeShader);
        }

        /// <summary>
        /// Creates a new texture connected to this compute shader (sharing the same Direct3D 11 device)
        /// </summary>
        /// <param name="width">The width of the texture</param>
        /// <param name="height">The height of the texture</param>
        /// <param name="format">the format of the texture</param>
        /// <param name="allowSharing">Determines whether the created texture can be shared, if true then CSTexture2D.Share() can be called</param>
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

            return new CSTexture2D(device, width, height, description);
        }
        /// <summary>
        /// Creates a new texture (connected to this compute shader) having the bitmap information
        /// </summary>
        /// <param name="bitmap">The bitmap</param>
        /// <param name="allowSharing">Determines whether the created texture can be shared, if true then CSTexture2D.Share() can be called</param>
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

            return new CSTexture2D(device, bitmap, description);
        }
        /// <summary>
        /// Creates a new texture (connected to this compute shader) having the image information
        /// </summary>
        /// <param name="imageRelativePath">The relative path to the image file (relative to the solution this code is in)</param>
        /// <param name="allowSharing">Determines whether the created texture can be shared, if true then CSTexture2D.Share() can be called</param>
        /// <returns></returns>
        public CSTexture2D CreateTexture2D(string imageRelativePath, bool allowSharing = false)
        {
            using (Bitmap b = new Bitmap(imageRelativePath))
            {
                return CreateTexture2D(b, allowSharing);
            }
        }
        /// <summary>
        /// Creates a new texture (connected to this compute shader) having the data pointer information
        /// </summary>
        /// <param name="width">The width of the texture</param>
        /// <param name="height">The height of the texture</param>
        /// <param name="format">the format of the texture</param>
        /// <param name="dataPointer">The pointer that holds raw texture data</param>
        /// <param name="allowSharing">Determines whether the created texture can be shared, if true then CSTexture2D.Share() can be called</param>
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

            return new CSTexture2D(device, dataPointer, description);
        }

        /// <summary>
        /// Creates a new texture connected to this compute shader (sharing the same Direct3D 11 device)
        /// </summary>
        /// <param name="width">The width of the texture array</param>
        /// <param name="height">The height of the texture array</param>
        /// <param name="numberOfTextures">The number of texture in the texture array</param>
        /// <param name="format">the format of the texture array</param>
        /// <param name="allowSharing">Determines whether the created texture array can be shared, if true then CSTexture2DArray.Share() can be called</param>
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

            return new CSTexture2DArray(device, width, height, numberOfTextures, description);
        }
        /// <summary>
        /// Creates a new texture (connected to this compute shader) having the bitmaps information
        /// </summary>
        /// <param name="allowSharing">Determines whether the created texture can be shared, if true then CSTexture2D.Share() can be called</param>
        /// <param name="bitmaps">The bitmaps (all with the same width, height, and format)</param>
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

            return new CSTexture2DArray(device, bitmaps, description);
        }
        /// <summary>
        /// Creates a new texture (connected to this compute shader) having the data pointer information
        /// </summary>
        /// <param name="width">The width of the texture</param>
        /// <param name="height">The height of the texture</param>
        /// <param name="format">the format of the texture</param>
        /// <param name="allowSharing">Determines whether the created texture can be shared, if true then CSTexture2D.Share() can be called</param>
        /// <param name="dataPointers">The pointer that holds raw texture data</param>
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

            return new CSTexture2DArray(device, dataPointers, description);
        }

        /// <summary>
        /// Creates a new CSCBuffer (connected to this compute shader) that stores data
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data">The data to be stored in the buffet. It must be a sturct.</param>
        /// <param name="dataSizeInBytes">The size of the data in bytes.</param>
        /// <returns></returns>
        public CSCBuffer<T> CreateBuffer<T>(T data, int dataSizeInBytes) where T : struct
        {
            return new CSCBuffer<T>(device, data, dataSizeInBytes);
        }

        /// <summary>
        /// Creates a new StructuredBuffer (connected to this compute shader) that stores read/write data array.
        /// Be aware that the maximum size for a buffer is 25% of your Vram or 2gb, which is smaller, so the maximum number of elements is (maximum size / sizeof(T)).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array">The array to be stored in the buffer.</param>
        /// <param name="eachElementSizeInBytes">The size of an element in the array in bytes.</param>
        /// <param name="allowSharing">Determines whether the created texture can be shared, if true then Share() can be called.</param>
        /// <returns></returns>
        public CSStructuredBuffer<T> CreateStructuredBuffer<T>(T[] array, int eachElementSizeInBytes, bool allowSharing = false) where T : struct
        {
            return new CSStructuredBuffer<T>(device, array, eachElementSizeInBytes, allowSharing);
        }
        /// <summary>
        /// Creates a new StructuredBuffer (connected to this compute shader) that stores read/write data list
        /// Be aware that the maximum size for a buffer is 25% of your Vram or 2gb, which is smaller, so the maximum number of elements is (maximum size / sizeof(T)).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list to be stored.</param>
        /// <param name="eachElementSizeInBytes">The size of an element in the list in bytes.</param>
        /// <param name="allowSharing">Determines whether the created texture can be shared, if true then Share() can be called.</param>
        /// <returns></returns>
        public CSStructuredBuffer<T> CreateStructuredBuffer<T>(List<T> list, int eachElementSizeInBytes, bool allowSharing = false) where T : struct
        {
            return new CSStructuredBuffer<T>(device, list, eachElementSizeInBytes, allowSharing);
        }

        /// <summary>
        /// connects the texture to a RWTexture2D in the computeShader. This can be used once so there is no need to call this function every frame or every time you change something in the texture.
        /// </summary>
        /// <param name="texture">The texture to be set.</param>
        /// <param name="register_uav_index">the register index of the texture in the compute shader.</param>
        /// <br>Example: RWTexture2D<float4> texture : register(u0);   its register index is 0</br>
        public void SetRWTexture2D(CSTexture2D texture, int register_uav_index)
        {
            UnorderedAccessView view = texture.UnorderedAccessView;
            if (view == null)
            {
                view = texture.CreateUAV(device);
            }

            device.ImmediateContext.ComputeShader.SetUnorderedAccessView(register_uav_index, view);
        }
        /// <summary>
        /// connects the texture to a RWTexture2DArray in the computeShader. This can be used once so there is no need to call this function every frame or every time you change something in the texture.
        /// </summary>
        /// <param name="textureArray">The texture to be set.</param>
        /// <param name="register_uav_index">the register index of the texture in the compute shader.</param>
        /// <br>Example: RWTexture2D<float4> texture : register(u0);   its register index is 0</br>
        public void SetRWTexture2DArray(CSTexture2DArray textureArray, int register_uav_index)
        {
            UnorderedAccessView view = textureArray.UnorderedAccessView;
            if (view == null)
            {
                view = textureArray.CreateUAV(device);
            }

            device.ImmediateContext.ComputeShader.SetUnorderedAccessView(register_uav_index, view);
        }
        /// <summary>
        /// connects the buffer to a cbuffer in the computeShader. This can be used once so there is no need to call this function every frame or every time you change something in the buffer.
        /// </summary>
        /// <param name="cSBuffer">The buffer to be set.</param>
        /// <param name="register_buffer_index">the register index of the buffer in the compute shader.</param>
        /// <br>Example: cbuffer buffer : register(b0);   its register index is 0</br>
        public void SetBuffer<T>(CSCBuffer<T> cSBuffer, int register_buffer_index) where T : struct
        {
            device.ImmediateContext.ComputeShader.SetConstantBuffer(register_buffer_index, cSBuffer.buffer);
        }
        /// <summary>
        /// connects the structured buffer to a RWStructuredBuffer in the computeShader. This can be used once so there is no need to call this function every frame or every time you change somthing in the structured buffer
        /// </summary>
        /// <param name="structuredBuffer">The structured buffer to be set.</param>
        /// <param name="register_uav_index">the register index of the buffer in the compute shader</param>
        /// <br>Example: RWStructuredBuffer<float2> sbuffer : register(u0);   its register index is 0</br>
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
        /// Runs the compute shader
        /// </summary>
        /// <param name="threadsX">Number of threads in the x-axis</param>
        /// <param name="threadsY">Number of threads in the y-axis</param>
        /// <param name="threadsZ">Number of threads in the z-axis</param>
        public void Dispatch(int threadsX, int threadsY, int threadsZ)
        {
            device.ImmediateContext.Dispatch(threadsX, threadsY, threadsZ);
        }

        /// <summary>
        /// Compile the compute shader.
        /// </summary>
        /// <param name="shaderName">The path to the compute shader (relative to the solution this code is in)</param>
        /// <param name="entryPoint">The main kernel function of the shader (1 kernel function for every compute shader class)</param>
        /// <param name="targetProfile">The type and version of the shader. default = cs_5_0. (cs is for Compute shader) (5_0 is for shader model 5.0)</param>
        /// <returns>Returns the byte code of the compiled shader</returns>
        public static byte[] CompileComputeShader(string shaderName, string entryPoint = "CSMain", string targetProfile = "cs_5_0")
        {
            try
            {
                using (CompilationResult result = ShaderBytecode.CompileFromFile(shaderName, entryPoint, targetProfile))
                {
                    return result.Bytecode;
                }
            }
            catch
            {
                throw new System.Exception("ERROR: failed to compile a compute shader.");
            }
        }
        /// <summary>
        /// Creates a Direct3D 11 device
        /// </summary>
        /// <param name="gpuAdapterIndex"></param>
        /// <param name="gpuCreationgFlag"></param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        internal static Device CreateDevice(int gpuAdapterIndex = 0, CSDeviceCreationFlags gpuCreationgFlag = CSDeviceCreationFlags.Debug | CSDeviceCreationFlags.Debuggable)
        {
            try
            {
                SharpDX.DXGI.Adapter1 gpu;
                using (var factory = new SharpDX.DXGI.Factory1())
                {
                    gpu = factory.GetAdapter1(gpuAdapterIndex);
                }

                return new SharpDX.Direct3D11.Device(gpu, (DeviceCreationFlags)gpuCreationgFlag);
            }
            catch
            {
                throw new System.Exception("ERROR: Failed to create Device. Check variables gpuAdapterIndex and gpuCreationgFlag");
            }
        }

        /// <summary>
        /// Dispose the unmanneged data to prevent memory leaks. This function must be called after finishing using this.
        /// </summary>
        public void Dispose()
        {
            device.Dispose();
            computeShader.Dispose();
        }
    }
}
