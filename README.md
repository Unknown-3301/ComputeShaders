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
After creating the compute shader we need to create its resources which are Textures, Texture Arrays, Structured Buffers, Constant Buffers. Every resource is created using a compute shader.
```
//as example
ComputeShader shader = new ComputeShader(compiledArray);

CSTexture2D texture = shader.CreateTexture2D(...);
CSTexture2DArray textureArray = shader.CreateTexture2DArray(...);
CSCBuffer<...> sBuffer = shader.CreateStructuredBuffer<...>(...);
CSStructuredBuffer<...> cBuffer = shader.CreateBuffer<...>(...);
```
**Textures:** you can create either an empty texture or a texture with data stored in it.
```
CSTexture2D texture = shader.CreateTexture2D(width, height, textureFormat); //create an empty texture
CSTexture2D texture2 = shader.CreateTexture2D(bitmap); //create a texture with the data from the bitmap

string imagePath = @"C:\Users\....\Images\image.png";
CSTexture2D texture3 = shader.CreateTexture2D(path); //create a texture with the data from the image in the path
CSTexture2D texture4 = shader.CreateTexture2D(width, height, textureFormat, pointer); //create a texture with the data from the pointer
```
**Texture Arrays:** Creating it is very similar to creating a texture.
```
CSTexture2D texture = shader.CreateTexture2D(width, height, numberOfTextures, textureFormat); //create an empty texture
CSTexture2D texture2 = shader.CreateTexture2D(bitmaps); //create a texture with the data from the bitmaps array
CSTexture2D texture3 = shader.CreateTexture2D(width, height, textureFormat, pointers); //create a texture with the data from the pointers array, where is every pointer refers to a texture raw data
```
