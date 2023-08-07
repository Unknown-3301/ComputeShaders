using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputeShaders
{
    /// <summary>
    /// An enum for the methods of accessing raw <see cref="ShaderResource{T}"/> data using the cpu.
    /// </summary>
    public enum CPUAccessMode
    {
        /// <summary>
        /// The cpu can only read the raw data.
        /// </summary>
        Read = 1,
        /// <summary>
        /// The cpu can only write to the raw data.
        /// </summary>
        Write = 2,
        /// <summary>
        /// The cpu can both read from the raw data and write to it.
        /// </summary>
        ReadWrite = 3,
    }
}
