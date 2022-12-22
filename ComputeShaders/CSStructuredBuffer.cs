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
    public class CSStructuredBuffer<T> : ShaderResource<Buffer> where T : struct
    {
        internal int numberOfElements { get; private set; }
        internal int elementSizeInBytes { get; private set; }

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

        internal CSStructuredBuffer(CSDevice device, T[] array, int eachElementSizeInBytes, bool allowShare)
        {
            elementSizeInBytes = eachElementSizeInBytes;
            numberOfElements = array.Length;
            int arraySize = array.Length * eachElementSizeInBytes;
            Device = device;

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

                resource = new Buffer(device.device, stream, new BufferDescription()
                {
                    SizeInBytes = arraySize,
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                    OptionFlags = ResourceOptionFlags.BufferStructured | (allowShare ? ResourceOptionFlags.Shared : ResourceOptionFlags.None),
                    StructureByteStride = eachElementSizeInBytes,
                });
            }
        }
        internal CSStructuredBuffer(CSDevice device, List<T> array, int eachElementSizeInBytes, bool allowShare)
        {
            elementSizeInBytes = eachElementSizeInBytes;
            numberOfElements = array.Count;
            int arraySize = array.Count * eachElementSizeInBytes;
            Device = device;

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
                    resource = new Buffer(device.device, stream, new BufferDescription()
                    {
                        SizeInBytes = arraySize,
                        Usage = ResourceUsage.Default,
                        BindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                        OptionFlags = ResourceOptionFlags.BufferStructured | (allowShare ? ResourceOptionFlags.Shared : ResourceOptionFlags.None),
                        StructureByteStride = eachElementSizeInBytes,
                    });

                }
                catch
                {
                    throw new System.Exception("ERROR while creating a structuredBuffer. NOTE: make sure your data size (int bytes) is a multiple of 16");
                }
            }
        }
        internal CSStructuredBuffer(Buffer buffer, CSDevice device, int numberOfElements, int elementSizeInBytes)
        {
            this.numberOfElements = numberOfElements;
            this.elementSizeInBytes = elementSizeInBytes;
            Device = device;

            resource = buffer;

            elements = new T[numberOfElements];
            EnableCPU_Raw_ReadWrite();
            GetData(ref _elements);
        }
        internal CSStructuredBuffer(ShaderResource<Buffer> buffer, int numberOfElements, int elementSizeInBytes)
        {
            this.numberOfElements = numberOfElements;
            this.elementSizeInBytes = elementSizeInBytes;
            Device = buffer.Device;

            resource = buffer.resource;

            elements = new T[numberOfElements];
            EnableCPU_Raw_ReadWrite();
            GetData(ref _elements);
        }

        /// <summary>
        /// Updates the data stored in the buffer using the cpu. Note that <see cref="SetData(T[])"/> is faster than <see cref="ShaderResource{T}.WriteToRawData(System.Action{TextureDataBox})"/> only on small scales. 
        /// It is recommended to use <see cref="ShaderResource{T}.WriteToRawData(System.Action{TextureDataBox})"/> instead of <see cref="SetData(T[])"/> if the buffer size in bytes is bigger than ~1,200,000.
        /// </summary>
        /// <param name="array">The new data</param>
        public void SetData(T[] array)
        {
            resource.Device.ImmediateContext.UpdateSubresource(array, resource);
        }
        /// <summary>
        /// Updates the data stored in the buffer using the cpu.
        /// </summary>
        /// <param name="list">The new data</param>
        [System.Obsolete("This function is so slow. Try SetData(T[] array) instead.")]
        public void SetData(List<T> list)
        {
            list.CopyTo(elements);

            resource.Device.ImmediateContext.UpdateSubresource(elements, resource);
        }
        /// <summary>
        /// Copy the data in the buffer to the array using the cpu.
        /// </summary>
        /// <param name="array">the array to copy to</param>
        public void GetData(ref T[] array)
        {
            if (!CPU_ReadWrite)
            {
                throw new System.Exception("Cannot use GetData() because CPU read/write ability is disabled. To enable it call EnableCPU_Raw_ReadWrite function.");
            }

            resource.Device.ImmediateContext.CopyResource(resource, stagingResource);
            DataBox dataBox = resource.Device.ImmediateContext.MapSubresource(stagingResource, 0, MapMode.Read, MapFlags.None);

            GCHandle handle;
            SharpDX.Utilities.CopyMemory(Utilities.GetIntPtr(array, out handle), dataBox.DataPointer, numberOfElements * elementSizeInBytes);
            handle.Free();

            stagingResource.Device.ImmediateContext.UnmapSubresource(stagingResource, 0);
        }
        /// <summary>
        /// Copy the data in the buffer to the list using the cpu.
        /// NOTE: it's prefered to use the GetData(ref T[] array) because using a list (this function) is alot slower
        /// </summary>
        /// <param name="list">the list to copy to</param>
        [System.Obsolete("This function is so slow. Try GetData(ref T[] array) instead.")]
        public void GetData(ref List<T> list)
        {
            if (!CPU_ReadWrite)
            {
                throw new System.Exception("Cannot use GetData() because CPU read/write ability is disabled. To enable it call EnableCPU_Raw_ReadWrite function.");
            }

            resource.Device.ImmediateContext.CopyResource(resource, stagingResource);
            DataBox dataBox = stagingResource.Device.ImmediateContext.MapSubresource(stagingResource, 0, MapMode.Read, MapFlags.None);

            GCHandle handle;
            SharpDX.Utilities.CopyMemory(Utilities.GetIntPtr(elements, out handle), dataBox.DataPointer, numberOfElements * elementSizeInBytes);
            handle.Free();

            list = new List<T>(elements);

            stagingResource.Device.ImmediateContext.UnmapSubresource(stagingResource, 0);
        }

        /// <summary>
        /// Updates the whole resource with the raw data from <paramref name="dataPointer"/>.
        /// </summary>
        /// <param name="dataPointer">The pointer to the raw data.</param>
        public override void UpdateSubresource(System.IntPtr dataPointer)
        {
            Device.device.ImmediateContext.UpdateSubresource(new DataBox(dataPointer, Length * elementSizeInBytes, Length * elementSizeInBytes), resource);
        }

        internal override uint GetResourceSize()
        {
            return (uint)(numberOfElements * elementSizeInBytes);
        }
        internal override ShaderResource<Buffer> CreateSharedResource(Buffer resource, CSDevice device)
        {
            return new CSStructuredBuffer<T>(resource, device, numberOfElements, ElementSizeInBytes);
        }

        /// <summary>
        /// Enables the ability to read/write the resource raw data using cpu. Enabling it has the advantages:
        /// <br>- to read the resource raw data using <see cref="ShaderResource{T}.ReadFromRawData(Action{TextureDataBox})"/>.</br>
        /// <br>- to write to the resource raw data using <see cref="ShaderResource{T}.WriteToRawData(Action{TextureDataBox})"/> function.</br>
        /// <br>and has the disadvantages:</br>
        /// <br>- may decrease the performance.</br>
        /// <br>- increase the memory usage to almost the double.</br>
        /// </summary>
        public override void EnableCPU_Raw_ReadWrite()
        {
            if (CPU_ReadWrite)
                return;

            stagingResource = new Buffer(resource.Device, new BufferDescription()
            {
                SizeInBytes = elementSizeInBytes * numberOfElements,
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = elementSizeInBytes,
                CpuAccessFlags = CpuAccessFlags.Read | CpuAccessFlags.Write
            });
        }

        /// <summary>
        /// Connects this resource to another device so that data can read/write between the resource and the device or any resource connected to it directly. 
        /// <br>NOTE: after calling this function if any changes occured to the resource or the shared version then <see cref="ShaderResource{T}.Flush"/> must be called on the changed resource.</br>
        /// </summary>
        /// <param name="device">The device to connect with.</param>
        /// <returns></returns>
        public new CSStructuredBuffer<T> Share(CSDevice device)
        {
            return new CSStructuredBuffer<T>(base.Share(device), numberOfElements, elementSizeInBytes);
        }
        /// <summary>
        /// Connects this resource to another resource so that data can read/write between the resource and the other resource or any resource connected to it directly. 
        /// <br>NOTE: after calling this function if any changes occured to the resource or the shared version then <see cref="ShaderResource{T}.Flush"/> must be called on the changed resource.</br>
        /// </summary>
        /// <param name="another">The another shader resource to connect with</param>
        /// <returns></returns>
        public new CSStructuredBuffer<T> Share<T2>(ShaderResource<T2> another) where T2 : Resource
        {
            return new CSStructuredBuffer<T>(base.Share(another), numberOfElements, elementSizeInBytes);
        }
        /// <summary>
        /// Connects this resource to a Direct3D 11 device so that data can read/write between the resource and the other resource or any resource connected to it directly. 
        /// <br>NOTE: after calling this function if any changes occured to the resource or the shared version then <see cref="ShaderResource{T}.Flush"/> must be called on the changed resource.</br>/// </summary>
        /// <param name="devicePointer">The Direct3D 11 device to connect with</param>
        /// <returns></returns>
        public new CSStructuredBuffer<T> Share(System.IntPtr devicePointer)
        {
            return new CSStructuredBuffer<T>(base.Share(devicePointer), numberOfElements, elementSizeInBytes);
        }

        internal UnorderedAccessView CreateUAV(Device device)
        {
            unorderedAccessView = new UnorderedAccessView(device, resource, new UnorderedAccessViewDescription()
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

    }
}
