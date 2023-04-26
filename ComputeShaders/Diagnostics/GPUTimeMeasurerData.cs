using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputeShaders.Diagnostics
{

    /// <summary>
    /// A struct that contains measurement data from <see cref="GPUTimeMeasurer"/>.
    /// </summary>
    public struct GPUTimeMeasurerData
    {
        /// <summary>
        /// Whether this data is reliable. If not, that means the data aren't accurate and need to be measured again.
        /// </summary>
        public bool Reliable;
        /// <summary>
        /// The time measured in microseconds (μs)
        /// </summary>
        public double Time;
    }
}
