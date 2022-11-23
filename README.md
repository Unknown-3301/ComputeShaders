# ComputeShaders
---

a library that wraps the [SharpDX](https://github.com/sharpdx/SharpDX) library to be used like compute shaders in Unity

## Usage
Before explaining, this does not contain the full details, but the important information. We first need to create a `ComputeShader` class either by having the byte array of the compiled shader or the path to the shader. You can get the byte array for a compiled shader through the static function `CompileComputeShader(string shaderName, string entryPoint = "CSMain", string targetProfile = "cs_5_0")` where **shaderName** is the path to the shader file, **entryPoint** is the main kernel function name in the said shader, and **targetProfile** is the type and version of the shader (cs is for Compute shader and 5_0 is for shader model 5.0).
```
//as example
string path = @"C:\Users\....\shadersFile\shader.hlsl";
byte[] compiledArray = ComputeShader.CompileComputeShader(path);

// if your main kernel function name is "CSMain" there is no need to input it
ComputeShader csMethod1 = new ComputeShader(path); 

ComputeShader csMethod2 = new ComputeShader(compiledArray);

```
After creating the compute shader we need to create its resources which are Textures, Structured Buffers, Constant Buffers.
### Textures: 
