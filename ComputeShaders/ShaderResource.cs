using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;

namespace ComputeShaders
{
    /// <summary>
    /// A base class for all shader Resources (exept constant buffer).
    /// </summary>
    public class ShaderResource<T> : IDisposable where T : Resource
    {
        internal T resource;
        internal T stagingResource;
        internal UnorderedAccessView unorderedAccessView;

        /// <summary>
        /// The ability to read/write the resource raw data in the cpu
        /// </summary>
        public bool CPU_ReadWrite { get => stagingResource != null; }

        internal virtual ShaderResource<T> CreateSharedResource(T resource)
        {
            return new ShaderResource<T>()
            {
                resource = resource,
            };
        }

        /// <summary>
        /// Connects this resource to another compute shader so that data can read/write between the resource and the compute shader or any resource connected to it directly. NOTE: after calling this function if any changes occured to the resource or the shared version then Flush() must be called on the changed resource.
        /// </summary>
        /// <param name="shader">The compute shader to connect with.</param>
        /// <returns></returns>
        public ShaderResource<T> Share(ComputeShader shader)
        {
            //source: https://stackoverflow.com/questions/41625272/direct3d11-sharing-a-texture-between-devices-black-texture
            SharpDX.DXGI.Resource copy = resource.QueryInterface<SharpDX.DXGI.Resource>();
            IntPtr sharedHandle = copy.SharedHandle;
            return CreateSharedResource(shader.device.OpenSharedResource<T>(sharedHandle));
        }
        /// <summary>
        /// Connects this resource to another resource so that data can read/write between the resource and the other resource or any resource connected to it directly. NOTE: after calling this function if any changes occured to the resource or the shared version then Flush() must be called on the changed resource.
        /// </summary>
        /// <param name="another">The another shader resource to connect with</param>
        /// <returns></returns>
        public ShaderResource<T> Share<T2>(ShaderResource<T2> another) where T2 : Resource
        {
            //source: https://stackoverflow.com/questions/41625272/direct3d11-sharing-a-texture-between-devices-black-texture
            SharpDX.DXGI.Resource copy = resource.QueryInterface<SharpDX.DXGI.Resource>();
            IntPtr sharedHandle = copy.SharedHandle;
            return CreateSharedResource(another.resource.Device.OpenSharedResource<T>(sharedHandle));
        }
        /// <summary>
        /// Connects this resource to a Direct3D 11 device so that data can read/write between the resource and the other resource or any resource connected to it directly. NOTE: after calling this function if any changes occured to the resource or the shared version then Flush() must be called on the changed resource.
        /// </summary>
        /// <param name="devicePointer">The Direct3D 11 device to connect with</param>
        /// <returns></returns>
        public ShaderResource<T> Share(IntPtr devicePointer)
        {
            //source: https://stackoverflow.com/questions/41625272/direct3d11-sharing-a-texture-between-devices-black-texture
            SharpDX.DXGI.Resource copy = resource.QueryInterface<SharpDX.DXGI.Resource>();
            IntPtr sharedHandle = copy.SharedHandle;
            Device device = new Device(devicePointer);
            return CreateSharedResource(device.OpenSharedResource<T>(sharedHandle));
        }

        /// <summary>
        /// Sends queued-up commands in the command buffer to the graphics processing unit (GPU). It is used after updating a shared resource.
        /// </summary>
        public void Flush()
        {
            resource.Device.ImmediateContext.Flush();
        }

        /// <summary>
        /// Returns the size of the resource in bytes.
        /// </summary>
        /// <returns></returns>
        internal virtual uint GetResourceSize()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Copy the contents from this resource to another resource.
        /// NOTE: In case that the 2 resources connect to different shaders, CPU read/write ability must be enabled for both resources. Both resource must have exact same dimensions. This function is so slow compared to other methods like using shared resources.
        /// </summary>
        /// <param name="destination">The resource to copy to.</param>
        public void CopyTo(ShaderResource<T> destination)
        {
            if (destination.resource.Device != resource.Device)
            {
                destination.WriteToRawData(desBox =>
                {
                    ReadFromRawData(scrBox =>
                    {
                        Utilities.CopyMemory(desBox.DataPointer, scrBox.DataPointer, GetResourceSize());
                    });
                });
            }
            else
            {
                resource.Device.ImmediateContext.CopyResource(resource, destination.resource);
            }
        }

        /// <summary>
        /// Write to the resource raw data (using only cpu) by an write function.
        /// NOTE: the data box pointer is aligned to 16 bytes. Check: https://learn.microsoft.com/en-us/windows/win32/api/d3d11/ns-d3d11-d3d11_mapped_subresource
        /// </summary>
        /// <param name="writeAction"></param>
        public void WriteToRawData(Action<TextureDataBox> writeAction)
        {
            if (!CPU_ReadWrite)
            {
                throw new Exception("Cannot use WriteToRawData because CPU read/write ability is disabled. To enable it call EnableCPU_Raw_ReadWrite function.");
            }

            resource.Device.ImmediateContext.CopyResource(resource, stagingResource);

            TextureDataBox box = new TextureDataBox(resource.Device.ImmediateContext.MapSubresource(stagingResource, 0, MapMode.Write, MapFlags.None));

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
        /// Reads through the raw data using 'readAction'.
        /// NOTE: all the reading process must ONLY be done inside 'readAction' function. Also, the data box pointer is aligned to 16 bytes. Check: https://learn.microsoft.com/en-us/windows/win32/api/d3d11/ns-d3d11-d3d11_mapped_subresource
        /// </summary>
        /// <returns></returns>
        public void ReadFromRawData(Action<TextureDataBox> readAction)
        {
            if (!CPU_ReadWrite)
            {
                throw new Exception("Cannot use GetRawDataIntPtr because CPU read/write ability is disabled. To enable it call EnableCPU_Raw_ReadWrite function.");
            }

            resource.Device.ImmediateContext.CopyResource(resource, stagingResource);

            TextureDataBox box = new TextureDataBox(resource.Device.ImmediateContext.MapSubresource(stagingResource, 0, MapMode.Read, MapFlags.None));

            try
            {
                readAction(box);
            }
            finally
            {
                resource.Device.ImmediateContext.UnmapSubresource(stagingResource, 0);
            }
        }


        /// <summary>
        /// Enables the ability to read/write the resource raw data using cpu. Enabling it has the advantages:
        /// <br>- to read the resource raw data using GetRawDataIntPtr function.</br>
        /// <br>- to write to the resource raw data using WriteToRawData function.</br>
        /// <br>and has the disadvantages:</br>
        /// <br>- may decrease the performance.</br>
        /// <br>- increase the memory usage to almost the double.</br>
        /// </summary>
        public virtual void EnableCPU_Raw_ReadWrite()
        {
            if (CPU_ReadWrite)
                return;

            throw new NotImplementedException();
        }
        /// <summary>
        /// Disables the ability to read/write the resource raw data using cpu. Disables it has the advantages (if cpu read/write was enabled):
        /// <br>- may increase the performance.</br>
        /// <br>- decrease the memory usage to almost the half.</br>
        /// <br>and has the disadvantages:</br>
        /// <br>- can not read the resource raw data using GetRawDataIntPtr function.</br>
        /// <br>- can not write to the resource raw data using WriteToRawData function.</br>
        /// </summary>
        public void DisablesCPU_Raw_ReadWrite()
        {
            if (!CPU_ReadWrite)
                return;

            stagingResource.Dispose();
            stagingResource = null;
        }

        /// <summary>
        /// Dispose the unmanneged data to prevent memory leaks. This function must be called after finishing using the resource.
        /// </summary>
        public void Dispose()
        {
            resource.Dispose();

            if (stagingResource != null)
                stagingResource.Dispose();

            if (unorderedAccessView != null)
                unorderedAccessView.Dispose();
        }
    }
}
