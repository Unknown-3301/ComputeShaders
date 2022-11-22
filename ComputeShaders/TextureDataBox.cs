using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputeShaders
{
    /// <summary>
    /// A struct that provides an access to texture data pointers
    /// </summary>
    public struct TextureDataBox
    {
        /// <summary>
        /// The data pointer. It is aligned to 16 bytes
        /// </summary>
        public IntPtr DataPointer { get; internal set; }
        /// <summary>
        /// Gets the number of bytes per row.
        /// </summary>
        public int RowPitch { get; internal set; }
        /// <summary>
        /// Gets the number of bytes per slice (for a 3D texture, a slice is a 2D image)
        /// </summary>
        public int SlicePitch { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether this instance is empty.
        /// </summary>
        /// <value><c>true</c> if this instance is empty; otherwise, <c>false</c>.</value>
        public bool IsEmpty
        {
            get
            {
                return DataPointer == IntPtr.Zero && RowPitch == 0 && SlicePitch == 0;
            }
        }

        internal TextureDataBox(SharpDX.DataBox dataBox)
        {
            DataPointer = dataBox.DataPointer;
            RowPitch = dataBox.RowPitch;
            SlicePitch = dataBox.SlicePitch;
        }
    }
}
