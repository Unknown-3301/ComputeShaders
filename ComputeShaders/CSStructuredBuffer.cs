using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;


namespace ComputeShaders
{
    /// <summary>
    /// A class that stores an array of data for RWStructuredBuffer (in the compute shader)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CSStructuredBuffer<T> : System.IDisposable where T : struct
    {
        public System.IntPtr BufferNativePointer { get => buffer.NativePointer; }
        public System.IntPtr DeviceNativePointer { get => buffer.Device.NativePointer; }

        internal Buffer buffer { get; private set; }
        internal Buffer stagingBuffer { get; private set; }
        internal UnorderedAccessView unorderedAccessView;

        internal int numberOfElements { get; private set; }
        internal int elementSizeInBytes { get; private set; }

        /// <summary>
        /// The ability to read the buffer in the cpu
        /// </summary>
        public bool CPU_Read { get => stagingBuffer != null; }
        /// <summary>
        /// The number of elements in the buffer.
        /// </summary>
        public int Length { get => numberOfElements; }
        /// <summary>
        /// The size of each element in the buffer in bytes.
        /// </summary>
        public int ElementSizeInBytes { get => elementSizeInBytes; }

        T[] _elements;
        internal T[] elements { get => _elements; private set { _elements = value; } }

        internal CSStructuredBuffer(Device device, T[] array, int eachElementSizeInBytes, bool allowShare)
        {
            elementSizeInBytes = eachElementSizeInBytes;
            numberOfElements = array.Length;
            int arraySize = array.Length * eachElementSizeInBytes;

            if (array == null)
            {
                throw new System.Exception();
            }

            elements = new T[array.Length];

            using (DataStream stream = new DataStream(arraySize, true, true))
            {
                for (int i = 0; i < array.Length; i++)
                {
                    stream.Write(array[i]);
                    elements[i] = array[i];
                }
                stream.Position = 0;

                buffer = new Buffer(device, stream, new BufferDescription()
                {
                    SizeInBytes = arraySize,
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                    OptionFlags = ResourceOptionFlags.BufferStructured | (allowShare ? ResourceOptionFlags.Shared : ResourceOptionFlags.None),
                    StructureByteStride = eachElementSizeInBytes,
                });

                stagingBuffer = new Buffer(buffer.Device, new BufferDescription()
                {
                    SizeInBytes = elementSizeInBytes * numberOfElements,
                    CpuAccessFlags = CpuAccessFlags.Read | CpuAccessFlags.Write,
                    Usage = ResourceUsage.Staging,
                    BindFlags = BindFlags.None,
                    OptionFlags = ResourceOptionFlags.None,
                    StructureByteStride = eachElementSizeInBytes,
                });
            }
        }
        internal CSStructuredBuffer(Device device, List<T> array, int eachElementSizeInBytes, bool allowShare)
        {
            elementSizeInBytes = eachElementSizeInBytes;
            numberOfElements = array.Count;
            int arraySize = array.Count * eachElementSizeInBytes;

            if (array == null)
            {
                throw new System.Exception();
            }

            elements = new T[array.Count];

            using (DataStream stream = new DataStream(arraySize, true, true))
            {
                for (int i = 0; i < array.Count; i++)
                {
                    stream.Write(array[i]);
                    elements[i] = array[i];
                }
                stream.Position = 0;

                try
                {
                    buffer = new Buffer(device, stream, new BufferDescription()
                    {
                        SizeInBytes = arraySize,
                        Usage = ResourceUsage.Default,
                        BindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                        OptionFlags = ResourceOptionFlags.BufferStructured | (allowShare ? ResourceOptionFlags.Shared : ResourceOptionFlags.None),
                        StructureByteStride = eachElementSizeInBytes,
                    });

                    stagingBuffer = new Buffer(buffer.Device, new BufferDescription()
                    {
                        SizeInBytes = elementSizeInBytes * numberOfElements,
                        Usage = ResourceUsage.Staging,
                        BindFlags = BindFlags.None,
                        OptionFlags = ResourceOptionFlags.None,
                        StructureByteStride = elementSizeInBytes,
                        CpuAccessFlags = CpuAccessFlags.Read
                    });
                }
                catch
                {
                    throw new System.Exception("ERROR while creating a structuredBuffer. NOTE: make sure your data size (int bytes) is a multiple of 16");
                }
            }
        }
        internal CSStructuredBuffer(Buffer buffer, int numberOfElements, int elementSizeInBytes)
        {
            this.numberOfElements = numberOfElements;
            this.elementSizeInBytes = elementSizeInBytes;

            this.buffer = buffer;
            stagingBuffer = new Buffer(buffer.Device, new BufferDescription()
            {
                SizeInBytes = elementSizeInBytes * numberOfElements,
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = elementSizeInBytes,
                CpuAccessFlags = CpuAccessFlags.Read
            });

            elements = new T[numberOfElements];
            GetData(ref _elements);
        }

        /// <summary>
        /// Updates the data stored in the buffer
        /// </summary>
        /// <param name="array">The new data</param>
        public void SetData(T[] array)
        {
            buffer.Device.ImmediateContext.UpdateSubresource(array, buffer);
        }
        /// <summary>
        /// Updates the data stored in the buffer
        /// </summary>
        /// <param name="list">The new data</param>
        [System.Obsolete("This function is so slow. Try SetData(T[] array) instead.")]
        public void SetData(List<T> list)
        {
            list.CopyTo(elements);

            buffer.Device.ImmediateContext.UpdateSubresource(elements, buffer);
        }
        /// <summary>
        /// Copy the data in the buffer to the array
        /// </summary>
        /// <param name="array">the array to copy to</param>
        public void GetData(ref T[] array)
        {
            buffer.Device.ImmediateContext.CopyResource(buffer, stagingBuffer);
            DataBox dataBox = stagingBuffer.Device.ImmediateContext.MapSubresource(stagingBuffer, 0, MapMode.Read, MapFlags.None);

            GCHandle handle;
            SharpDX.Utilities.CopyMemory(Utilities.GetIntPtr(array, out handle), dataBox.DataPointer, numberOfElements * elementSizeInBytes);
            handle.Free();

            stagingBuffer.Device.ImmediateContext.UnmapSubresource(stagingBuffer, 0);
        }
        /// <summary>
        /// Copy the data in the buffer to the list.
        /// NOTE: it's prefered to use the GetData(ref T[] array) because using a list (this function) is alot slower
        /// </summary>
        /// <param name="list">the list to copy to</param>
        [System.Obsolete("This function is so slow. Try GetData(ref T[] array) instead.")]
        public void GetData(ref List<T> list)
        {
            buffer.Device.ImmediateContext.CopyResource(buffer, stagingBuffer);
            DataBox dataBox = stagingBuffer.Device.ImmediateContext.MapSubresource(stagingBuffer, 0, MapMode.Read, MapFlags.None);

            GCHandle handle;
            SharpDX.Utilities.CopyMemory(Utilities.GetIntPtr(elements, out handle), dataBox.DataPointer, numberOfElements * elementSizeInBytes);
            handle.Free();

            list = new List<T>(elements);

            stagingBuffer.Device.ImmediateContext.UnmapSubresource(stagingBuffer, 0);
        }

        /// <summary>
        /// Enables the ability to read the buffer using cpu. Enabling it has the advantages:
        /// <br>- to read the buffer raw data using GetData function.</br>
        /// <br>and has the disadvantages:</br>
        /// <br>- may decrease the performance.</br>
        /// <br>- increase the memory usage to almost the double.</br>
        /// </summary>
        public void EnableCPU_Read()
        {
            if (CPU_Read)
                return;

            stagingBuffer = new Buffer(buffer.Device, new BufferDescription()
            {
                SizeInBytes = elementSizeInBytes * numberOfElements,
                CpuAccessFlags = CpuAccessFlags.Read,
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = elementSizeInBytes,
            });
        }
        /// <summary>
        /// Disables the ability to read the buffer using cpu. Disables it has the advantages (if cpu read was enabled):
        /// <br>- may increase the performance.</br>
        /// <br>- decrease the memory usage to almost the half.</br>
        /// <br>and has the disadvantages:</br>
        /// <br>- can not read the buffer raw data using GetData function.</br>
        /// </summary>
        public void DisablesCPU_Read()
        {
            if (!CPU_Read)
                return;

            stagingBuffer.Dispose();
            stagingBuffer = null;
        }

        /// <summary>
        /// Connects this buffer to another compute shader so that data can read/write between the buffer and the compute shader or any buffer connected to it directly. NOTE: after calling this function if any changes occured to the buffer or the shared version then Flush() should be called on the changed buffer
        /// </summary>
        /// <param name="shader">The compute shader to connect with</param>
        /// <returns></returns>
        public CSStructuredBuffer<T> Share(ComputeShader shader)
        {
            //source: https://stackoverflow.com/questions/41625272/direct3d11-sharing-a-texture-between-devices-black-texture
            SharpDX.DXGI.Resource copy = buffer.QueryInterface<SharpDX.DXGI.Resource>();
            System.IntPtr sharedHandle = copy.SharedHandle;
            return new CSStructuredBuffer<T>(shader.device.OpenSharedResource<Buffer>(sharedHandle), numberOfElements, elementSizeInBytes);
        }
        /// <summary>
        /// Connects this buffer to another buffer so that data can read/write between the buffer and the other buffer or any buffer connected to it directly. NOTE: after calling this function if any changes occured to the buffer or the shared version then Flush() should be called on the changed buffer
        /// </summary>
        /// <param name="anotherBuffer">The another buffer to connect with</param>
        /// <returns></returns>
        public CSStructuredBuffer<T> Share<D>(CSStructuredBuffer<D> anotherBuffer) where D : struct
        {
            //source: https://stackoverflow.com/questions/41625272/direct3d11-sharing-a-texture-between-devices-black-texture
            SharpDX.DXGI.Resource copy = buffer.QueryInterface<SharpDX.DXGI.Resource>();
            System.IntPtr sharedHandle = copy.SharedHandle;
            return new CSStructuredBuffer<T>(anotherBuffer.buffer.Device.OpenSharedResource<Buffer>(sharedHandle), numberOfElements, elementSizeInBytes);
        }
        public CSStructuredBuffer<T> Share(System.IntPtr devicePointer, int numberOfElements, int elementSizeInBytes)
        {
            //source: https://stackoverflow.com/questions/41625272/direct3d11-sharing-a-texture-between-devices-black-texture
            SharpDX.DXGI.Resource copy = buffer.QueryInterface<SharpDX.DXGI.Resource>();
            System.IntPtr sharedHandle = copy.SharedHandle;
            Device dev = new Device(devicePointer);
            return new CSStructuredBuffer<T>(dev.OpenSharedResource<SharpDX.Direct3D11.Buffer>(sharedHandle), numberOfElements, elementSizeInBytes);
        }

        /// <summary>
        /// Sends queued-up commands in the command buffer to the graphics processing unit (GPU). It is used after updating a shared buffer
        /// </summary>
        public void Flush()
        {
            buffer.Device.ImmediateContext.Flush();
        }

        internal UnorderedAccessView CreateUAV(Device device)
        {
            unorderedAccessView = new UnorderedAccessView(device, buffer, new UnorderedAccessViewDescription()
            {
                Dimension = UnorderedAccessViewDimension.Buffer,
                Buffer = new UnorderedAccessViewDescription.BufferResource()
                {
                    ElementCount = numberOfElements,
                    Flags = UnorderedAccessViewBufferFlags.Counter,
                },
                Format = SharpDX.DXGI.Format.Unknown,
            });

            return unorderedAccessView;
        }

        /// <summary>
        /// Dispose the unmanneged data to prevent memory leaks. This function must be called after finishing using this.
        /// </summary>
        public void Dispose()
        {
            buffer.Dispose();
            stagingBuffer.Dispose();

            if (unorderedAccessView != null)
                unorderedAccessView.Dispose();
        }
    }
}
