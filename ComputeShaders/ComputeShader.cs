using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using System.Drawing;
using System.Drawing.Imaging;

namespace ComputeShaders
{
    // The main source for the ILMerge info: https://stackoverflow.com/questions/2556048/how-to-integrate-ilmerge-into-visual-studio-build-process-to-merge-assemblies

    //After some amount of testing for the memory limit the results are:
    //The resource limit mentioned in https://learn.microsoft.com/en-us/windows/win32/direct3d11/overviews-direct3d-11-resources-limits
    //which is min(max(128mb, VRam), 2gb) means that the size total of ALL resources created (even if from different devices) COMBINED could not be above 2gb
    //yes COMBINED

    /// <summary>
    /// The main class for using compute shader class (Direct3D 11)
    /// </summary>
    public class ComputeShader
    {
        /// <summary>
        /// The device connected to this compute shader.
        /// </summary>
        public CSDevice Device { get; }

        internal SharpDX.Direct3D11.ComputeShader computeShader;

        internal ComputeShader(byte[] shaderByteCode, CSDevice device)
        {
            Device = device;
            computeShader = new SharpDX.Direct3D11.ComputeShader(device.device, shaderByteCode);
        }
        internal ComputeShader(string shaderName, CSDevice device, string entryPoint = "CSMain", string targetProfile = "cs_5_0")
        {
            Device = device;
            computeShader = new SharpDX.Direct3D11.ComputeShader(device.device, CompileComputeShader(shaderName, entryPoint, targetProfile));
        }

        /// <summary>
        /// Compile the compute shader.
        /// </summary>
        /// <param name="shaderName">The path to the compute shader (relative to the solution this code is in)</param>
        /// <param name="entryPoint">The main kernel function of the shader (1 kernel function for every compute shader class)</param>
        /// <param name="targetProfile">The type and version of the shader. default = cs_5_0. (cs is for Compute shader) (5_0 is for shader model 5.0)</param>
        /// <param name="flags">The for the shader.</param>
        /// <returns>Returns the byte code of the compiled shader</returns>
        public static byte[] CompileComputeShader(string shaderName, string entryPoint = "CSMain", string targetProfile = "cs_5_0", ShaderFlags flags = ShaderFlags.OptimizationLevel3)
        {
            try
            {
                using (CompilationResult result = ShaderBytecode.CompileFromFile(shaderName, entryPoint, targetProfile))
                {
                    return result.Bytecode;
                }
            }
            catch
            {
                throw new System.Exception("ERROR: failed to compile a compute shader.");
            }
        }

        /// <summary>
        /// Dispose the unmanneged data to prevent memory leaks. This function must be called after finishing using this.
        /// </summary>
        public void Dispose()
        {
            computeShader.Dispose();
        }
    }
}
