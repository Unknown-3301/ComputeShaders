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

        internal CSStructuredBuffer(CSDevice device, System.IntPtr pointer, int length, int eachElementSizeInBytes, bool allowShare)
        {
            elementSizeInBytes = eachElementSizeInBytes;
            numberOfElements = length;
            int arraySize = length * eachElementSizeInBytes;
            Device = device;

            using (DataStream stream = new DataStream(arraySize, true, true))
            {
                byte[] bytes = new byte[arraySize];

                stream.Write(pointer, 0, arraySize);

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


            using (DataStream stream = new DataStream(arraySize, true, true))
            {
                byte[] bytes = new byte[arraySize];
                GCHandle arrayHandle;

                System.IntPtr arrayPointer = Utilities.GetIntPtr(array, out arrayHandle);
                stream.Write(arrayPointer, 0, arraySize);

                arrayHandle.Free();

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

            using (DataStream stream = new DataStream(arraySize, true, true))
            {
                byte[] bytes = new byte[arraySize];
                GCHandle arrayHandle;

                System.IntPtr arrayPointer = Utilities.GetIntPtr(Utilities.GetPrivteVariableDataUncasted<List<T>, T[]>("_items", array), out arrayHandle);
                stream.Write(arrayPointer, 0, arraySize);

                arrayHandle.Free();

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
        }

        /// <summary>
        /// Updates the data stored in the buffer using the cpu. Note that <see cref="SetData(T[])"/> is faster than <see cref="ShaderResource{T}.AccessRawData(System.Action{TextureDataBox}, CPUAccessMode)"/> only on small scales. 
        /// It is recommended to use <see cref="ShaderResource{T}.AccessRawData(System.Action{TextureDataBox}, CPUAccessMode)"/> instead of <see cref="SetData(T[])"/> if the buffer size in bytes is bigger than ~1,200,000.
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
        public void SetData(List<T> list)
        {
            resource.Device.ImmediateContext.UpdateSubresource(Utilities.GetPrivteVariableDataCasted<List<T>, T[]>("_items", list), resource);
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
        /// NOTE: <see cref="List{T}.Count"/> of <paramref name="list"/> must be bigger than or equal to <see cref="Length"/>
        /// </summary>
        /// <param name="list">the list to copy to</param>
        public void GetData(ref List<T> list)
        {
            if (!CPU_ReadWrite)
            {
                throw new System.Exception("Cannot use GetData() because CPU read/write ability is disabled. To enable it call EnableCPU_Raw_ReadWrite function.");
            }

            resource.Device.ImmediateContext.CopyResource(resource, stagingResource);
            DataBox dataBox = stagingResource.Device.ImmediateContext.MapSubresource(stagingResource, 0, MapMode.Read, MapFlags.None);

            GCHandle handle;
            SharpDX.Utilities.CopyMemory(Utilities.GetIntPtr(Utilities.GetPrivteVariableDataCasted<List<T>, T[]>("_items", list), out handle), dataBox.DataPointer, numberOfElements * elementSizeInBytes);
            handle.Free();

            stagingResource.Device.ImmediateContext.UnmapSubresource(stagingResource, 0);
        }

        /// <inheritdoc/>
        public override void UpdateSubresource(System.IntPtr dataPointer)
        {
            Device.device.ImmediateContext.UpdateSubresource(new DataBox(dataPointer, Length * elementSizeInBytes, Length * elementSizeInBytes), resource);
        }

        /// <summary>
        /// Copy the contents of this resource to <paramref name="destination"/>.
        /// NOTE: In case that the 2 resources connect to different device, CPU read/write ability must be enabled for both resources. Both resource must have exact same dimensions. When the two resources connect to different devices, this function is so slow compared to other methods like using shared resources.
        /// </summary>
        /// <param name="destination"></param>
        public void CopyTo(CSStructuredBuffer<T> destination)
        {
            if (destination.Device != Device)
            {
                destination.AccessRawData(desBox =>
                {
                    AccessRawData(scrBox =>
                    {
                        Utilities.CopyMemory(desBox.DataPointer, scrBox.DataPointer, (uint)(Length * elementSizeInBytes));
                    }, CPUAccessMode.Read);
                }, CPUAccessMode.Write);
            }
            else
            {
                resource.Device.ImmediateContext.CopyResource(resource, destination.resource);
            }
        }

        /// <inheritdoc/>
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
        /// Creates a resource that Gives its device (<paramref name="device"/>) access to a shared resource (this resource) created on a different device (this resource's device). In other words, it creates a resource with <paramref name="device"/> that is connected to this resource's device through this resource. There are important notes regarding shared resources:
        /// <br>- If any of the two shared resources (the result resource and this resource) is updated, <see cref="CSDevice.Flush()"/> must be called.</br>
        /// <br>- In some cases, updating a shared resource and using <see cref="CSDevice.Flush()"/> might causes problems if that shared resource was used with an asynchronous function. For example, when <see cref="CSDevice.Flush()"/> is used then <see cref="ShaderResource{T}.CopyResource(ShaderResource{T})"/> is called using that shared resources. In such cases, it is adviced to call <see cref="CSDevice.Synchronize()"/> afterwards.</br>
        /// </summary>
        /// <param name="device">another device.</param>
        /// <returns></returns>
        public CSStructuredBuffer<T> Share(CSDevice device) => new CSStructuredBuffer<T>(CreateSharedResource(device.device), device, Length, ElementSizeInBytes);
        /// <summary>
        /// Creates a resource that Gives its device (<paramref name="devicePointer"/>) access to a shared resource (this resource) created on a different device (this resource's device). In other words, it creates a resource with <paramref name="devicePointer"/> that is connected to this resource's device through this resource. There are important notes regarding shared resources:
        /// <br>- If any of the two shared resources (the result resource and this resource) is updated, <see cref="CSDevice.Flush()"/> must be called.</br>
        /// <br>- In some cases, updating a shared resource and using <see cref="CSDevice.Flush()"/> might causes problems if that shared resource was used with an asynchronous function. For example, when <see cref="CSDevice.Flush()"/> is used then <see cref="ShaderResource{T}.CopyResource(ShaderResource{T})"/> is called using that shared resources. In such cases, it is adviced to call <see cref="CSDevice.Synchronize()"/> afterwards.</br>
        /// </summary>
        /// <param name="devicePointer">another device.</param>
        /// <returns></returns>
        public CSStructuredBuffer<T> Share(System.IntPtr devicePointer) => new CSStructuredBuffer<T>(CreateSharedResource(new Device(devicePointer)), new CSDevice(devicePointer), Length, ElementSizeInBytes);


        /// <inheritdoc/>
        internal override UnorderedAccessView CreateUAV()
        {
            unorderedAccessView = new UnorderedAccessView(Device.device, resource, new UnorderedAccessViewDescription()
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
