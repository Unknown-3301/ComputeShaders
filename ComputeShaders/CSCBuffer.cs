using SharpDX;
using SharpDX.Direct3D11;

namespace ComputeShaders
{
    /// <summary>
    /// A class that stores data for cbuffer (in the compute shader)
    /// </summary>
    public class CSCBuffer<T> : System.IDisposable where T : struct
    {
        public System.IntPtr BufferNativePointer { get => buffer.NativePointer; }
        internal Buffer buffer { get; private set; }

        internal CSCBuffer(Device device, T dataStruct, int sizeInBytes)
        {
            using (DataStream data = new DataStream(sizeInBytes, true, true))
            {
                data.Write(dataStruct);
                data.Position = 0;

                try
                {
                    buffer = new Buffer(device, data, new BufferDescription()
                    {
                        SizeInBytes = sizeInBytes,
                        Usage = ResourceUsage.Default,
                        BindFlags = BindFlags.ConstantBuffer,
                    });
                }
                catch
                {
                    throw new System.Exception("ERROR while creating a buffer. NOTE: make sure your data size (int bytes) is a multiple of 16");
                }
            }
        }
        internal CSCBuffer(Buffer buffer)
        {
            this.buffer = buffer;
        }

        /// <summary>
        /// Updates the buffer data
        /// </summary>
        /// <param name="newData">the new data</param>
        public void UpdateBuffer(T newData)
        {
            //souce: https://gamedev.stackexchange.com/questions/77582/how-can-i-create-a-buffer-suitable-for-dynamic-updates-in-sharpdx
            buffer.Device.ImmediateContext.UpdateSubresource(ref newData, buffer);
        }

        /// <summary>
        /// Connects this buffer to another compute shader so that data can read/write between the buffer and the compute shader or any buffer connected to it directly. NOTE: after calling this function if any changes occured to the buffer or the shared version then Flush() should be called on the changed buffer
        /// </summary>
        /// <param name="shader">The compute shader to connect with</param>
        /// <returns></returns>
        public CSCBuffer<T> Share(ComputeShader shader)
        {
            //source: https://stackoverflow.com/questions/41625272/direct3d11-sharing-a-texture-between-devices-black-texture
            SharpDX.DXGI.Resource copy = buffer.QueryInterface<SharpDX.DXGI.Resource>();
            System.IntPtr sharedHandle = copy.SharedHandle;
            return new CSCBuffer<T>(shader.device.OpenSharedResource<Buffer>(sharedHandle));
        }

        /// <summary>
        /// Dispose the unmanneged data to prevent memory leaks. This function must be called after finishing using this.
        /// </summary>
        public void Dispose()
        {
            buffer.Dispose();
        }
    }
}
