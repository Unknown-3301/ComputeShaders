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
    public abstract class ShaderResource<T> : IDisposable where T : Resource
    {
        internal T resource;
        internal T stagingResource;
        internal UnorderedAccessView unorderedAccessView;

        /// <summary>
        /// The native pointer to the sharpDX resource.
        /// </summary>
        public IntPtr ResourceNativePointer { get => resource.NativePointer; }
        /// <summary>
        /// The device connected to this resource.
        /// </summary>
        public CSDevice Device { get; protected set; }

        /// <summary>
        /// The ability to read/write the resource raw data by cpu. When true, it enables the cpu to read/write the resource raw data. However, enabling it increases memory usage to almost double.
        /// </summary>
        public bool CPU_ReadWrite { get => stagingResource != null; }


        internal T CreateSharedResource(Device device)
        {
            //source: https://stackoverflow.com/questions/41625272/direct3d11-sharing-a-texture-between-devices-black-texture
            SharpDX.DXGI.Resource copy = resource.QueryInterface<SharpDX.DXGI.Resource>();
            IntPtr sharedHandle = copy.SharedHandle;
            return device.OpenSharedResource<T>(sharedHandle);
        }

        /// <summary>
        /// Gives the cpu access to the raw data stored in the resource (by mapping https://learn.microsoft.com/en-us/windows/win32/api/d3d11/ns-d3d11-d3d11_mapped_subresource). During mapping:
        /// <br>- GPU access to the resource is denied.</br>
        /// <br>- The size of the first dimension of the resource (length in buffer, width in texture...) in bytes is rounded to the closest multiple of 16 that is larger than or equal to the original size.</br>
        /// <br>Note that <see cref="CPU_ReadWrite"/> must be true, and that all the accessing can only be done inside <paramref name="accessAction"/>..</br>
        /// </summary>
        /// <param name="accessAction">The action to access the data.</param>
        /// <param name="mode">The mode of access.</param>
        public void AccessRawData(Action<TextureDataBox> accessAction, CPUAccessMode mode)
        {
            if (!CPU_ReadWrite)
            {
                throw new Exception("Cannot use AccessRawData because CPU read/write ability is disabled. To enable it call EnableCPU_Raw_ReadWrite function.");
            }

            resource.Device.ImmediateContext.CopyResource(resource, stagingResource);

            TextureDataBox box = new TextureDataBox(resource.Device.ImmediateContext.MapSubresource(stagingResource, 0, (MapMode)mode, MapFlags.None));

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
        /// An asynchronous call that copies the entire contents of the source resource to the destination resource using the GPU. This call has a few restrictions, the source resource (this) and <paramref name="destination"/>:
        /// <br>- Must be different resources.</br>
        /// <br>- Must share the same <see cref="Device"/>.</br>
        /// <br>- Must be the same type.</br>
        /// <br>- Must have identical dimensions (including width, height, depth, and size as appropriate).</br>
        /// <br>- Must have compatible <see cref="TextureFormat"/> (if they were textures), which means the formats must be identical or at least from the same type group. For example, a <see cref="TextureFormat.R32G32B32_Float"/> texture can be copied to a <see cref="TextureFormat.R32G32B32_UInt"/> texture since both of these formats are in the  <see cref="TextureFormat.R32G32B32_Typeless"/> group. </br>
        /// </summary>
        /// <param name="destination"></param>
        public void CopyResource(ShaderResource<T> destination)
        {
            //See this: https://learn.microsoft.com/en-us/windows/win32/direct3d10/d3d10-graphics-programming-guide-resources-mapping
            
            Device.device.ImmediateContext.CopyResource(resource, destination.resource);
        }

        /// <summary>
        /// Updates the resource with the raw data from <paramref name="dataPointer"/> using the cpu.
        /// </summary>
        /// <param name="dataPointer">The pointer containing</param>
        public abstract void UpdateSubresource(IntPtr dataPointer);
        /// <summary>
        /// Enables <see cref="CPU_ReadWrite"/>.
        /// </summary>
        public abstract void EnableCPU_Raw_ReadWrite();
        /// <summary>
        /// Creates an <see cref="UnorderedAccessView"/> for the resource.
        /// </summary>
        internal abstract UnorderedAccessView CreateUAV();

        /// <summary>
        /// Disables <see cref="CPU_ReadWrite"/>.
        /// </summary>
        public void DisablesCPU_Raw_ReadWrite()
        {
            if (!CPU_ReadWrite)
                return;

            stagingResource.Dispose();
            stagingResource = null;
        }

        /// <inheritdoc/>
        public virtual void Dispose()
        {
            resource.Dispose();
            stagingResource?.Dispose();
            unorderedAccessView?.Dispose();
        }

        /// <summary>
        /// Returns whether this shaderResource and <paramref name="other"/> share the same native d3d11 resource.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool SameNativeResource(ShaderResource<T> other) => ResourceNativePointer == other.ResourceNativePointer;
    }
}
