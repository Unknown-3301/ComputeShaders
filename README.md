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

**Structured Buffers:** To create it you need the array (or list) of data and the size of each element in bytes. Data type can only be struct.
```
//as example
struct Arrow
{ 
  float positionX;
  float positionY;
  float directionX;
  float directionY;
}

Arrow[] arrows = new Arrow[100];
CSStructuredBuffer<Arrow> arrowsBuffer = shader.CreateStructuredBuffer<Arrow>(arrows, sizeof(float) * 4);
```

**Constant Buffer:** To create it you need the data and its size in bytes. It is important to make sure the size of the data is a multiple of 16 (16, 32, 48...). In Structured Buffers this is not required.
```
struct Point
{
  float positionX;
  float positionY;
  
  //this is to make the size of the struct a multiple of 16
  //the size of a float number is 4 bytes so the total size of this struct is 16 (which is a multiple of 16)
  float DummyVar1;
  float DummyVar2;
}

Point p = new Point();
CSCBuffer<Point> pointBuffer = shader.CreateBuffer<Point>(p, sizeof(float) * 4);
```
Now we need to connect the resources to the shader.
```
shader.SetRWTexture2D(texture, 0); //in shader the variable is (example) RWTexture2D<float4> Input : register(u0);
shader.SetRWTexture2DArray(textureArray, 1); //in shader the variable is (example) RWTexture2DArray<float4> Input2 : register(u1);
shader.SetRWStructuredBuffer(sBuffer, 2); //in shader the variable is (example) RWStructuredBuffer<float> floats : register(u2);
shader.SetBuffer(cBuffer, 0); //in shader the variable is (example) cbuffer Info : register(b0) { ... }
```
After that the shader can be run now. It is the same way as unity
```
//as example
shader.Dispatch(width / 8f, 1, 1);
```

## Sharing Shader Resources
Resources are created through a shader which means they are connected to the that shader and ONLY that shader. Connecting a resource to a shader means that this resource can be used in that shader. This means that if we have 2 textures (example) from 2 different shaders, we can not transfer data between the textures using the GPU.!
[Sharing1](https://user-images.githubusercontent.com/39702846/203797105-8d1c5189-a170-420b-b8b4-ed48c141e7cf.png)
