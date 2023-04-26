using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;

namespace ComputeShaders.Diagnostics
{
    /// <summary>
    /// A class to measure the execution time of commands done on the GPU (like Dispatch on compute shaders).
    /// </summary>
    public class GPUTimeMeasurer : IDisposable
    {
        private Query disjoint;
        private Query start;
        private Query end;

        private Device device;

        /// <summary>
        /// Creates a new measurment instance.
        /// </summary>
        /// <param name="device">ComputeShaders D3D11 device. It must be disposed of manually.</param>
        public GPUTimeMeasurer(Device device)
        {
            this.device = device;
            InitQueries();
        }
        /// <summary>
        /// Creates a new measurment instance.
        /// </summary>
        /// <param name="device">SharpDX D3D11 device. It must be disposed of manually.</param>
        public GPUTimeMeasurer(CSDevice device)
        {
            this.device = new Device(device.DeviceNativePointer);
            InitQueries();
        }

        /// <summary>
        /// Measures the time it takes to execute <paramref name="gpuAction"/>. Note that if this is done in <see cref="Windows.WindowForm"/> it is prefered to do it once every frame or less.
        /// </summary>
        /// <param name="gpuAction"></param>
        /// <returns></returns>
        public GPUTimeMeasurerData Measure(Action gpuAction)
        {
            device.ImmediateContext.Begin(disjoint);
            device.ImmediateContext.End(start);

            gpuAction();

            device.ImmediateContext.End(end);
            device.ImmediateContext.End(disjoint);

            while (!device.ImmediateContext.IsDataAvailable(start)) { }
            ulong startTicks = device.ImmediateContext.GetData<ulong>(start);

            while (!device.ImmediateContext.IsDataAvailable(end)) { }
            ulong endTicks = device.ImmediateContext.GetData<ulong>(end);

            while (!device.ImmediateContext.IsDataAvailable(disjoint)) { }
            QueryDataTimestampDisjoint dataDisjoint = device.ImmediateContext.GetData<QueryDataTimestampDisjoint>(disjoint);

            return new GPUTimeMeasurerData()
            {
                Reliable = !dataDisjoint.Disjoint,
                Time = (endTicks - startTicks) / (double)dataDisjoint.Frequency * 1000000,
            };
        }

        /// <summary>
        /// Disposes the unmanaged data. 
        /// </summary>
        public void Dispose()
        {
            disjoint.Dispose();
            start.Dispose();
            end.Dispose();
        }

        private void InitQueries()
        {
            disjoint = new Query(device, new QueryDescription()
            {
                Type = QueryType.TimestampDisjoint,
                Flags = QueryFlags.None,
            });
            start = new Query(device, new QueryDescription()
            {
                Type = QueryType.Timestamp,
                Flags = QueryFlags.None,
            });
            end = new Query(device, new QueryDescription()
            {
                Type = QueryType.Timestamp,
                Flags = QueryFlags.None,
            });
        }
    }
}