using SharpDX;
using SharpDX.Direct3D11;

namespace ComputeShaders
{
    /// <summary>
    /// A class that stores data for cbuffer (in the compute shader)
    /// </summary>
    public class CSCBuffer<T> : System.IDisposable where T : struct
    {
        /// <summary>
        /// The device connected to this resource.
        /// </summary>
        public CSDevice Device { get; private set; }

        internal Buffer buffer { get; private set; }

        internal CSCBuffer(CSDevice device, T dataStruct, int sizeInBytes)
        {
            using (DataStream data = new DataStream(sizeInBytes, true, true))
            {
                data.Write(dataStruct);
                data.Position = 0;
                Device = device;

                try
                {
                    buffer = new Buffer(device.device, data, new BufferDescription()
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
        /// Dispose the unmanneged data to prevent memory leaks. This function must be called after finishing using this.
        /// </summary>
        public void Dispose()
        {
            buffer.Dispose();
        }
    }
}
