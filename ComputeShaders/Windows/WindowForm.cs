using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using SharpDX.Windows;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;

namespace ComputeShaders.Windows
{
    /// <summary>
    /// A basic class to create a window. It wraps and simplifies the usage of both <see cref="RenderForm"/> and <see cref="SwapChain"/>.
    /// </summary>
    public class WindowForm : IDisposable
    {
        /// <summary>
        /// The width of the window form.
        /// </summary>
        public int Width { get; private set; }
        /// <summary>
        /// The Height of the window form.
        /// </summary>
        public int Height { get; private set; }
        /// <summary>
        /// The format of the pixels in the window form.
        /// </summary>
        public TextureFormat Format { get; private set; }
        /// <summary>
        /// The title of the window form.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// The device the window form uses.
        /// </summary>
        public CSDevice Device { get; private set; }
        /// <summary>
        /// SharpDX RenderForm. This is used for more control over the window form.
        /// </summary>
        public RenderForm WindowRenderForm { get; private set; }
        /// <summary>
        /// The output texture used to draw on the window form. Also, it is the texture that will be passed as input in the 'updateAction' method when calling <see cref="Run(Action{CSTexture2D})"/>
        /// </summary>
        public CSTexture2D Output { get; private set; }

        private SharpDX.Direct3D11.Device device;
        private RenderTargetView targetView;
        private SwapChain swapChain;
        private Texture2D outputTexture;
        private Action<CSTexture2D> updateMethod;

        /// <summary>
        /// Creates a new window form. Note that the window will not run until <see cref="Run"/> is called
        /// </summary>
        /// <param name="width">The width of the window form.</param>
        /// <param name="height">The Height of the window form.</param>
        /// <param name="format">The format of the pixels in the window form.</param>
        /// <param name="title">The title of the window form.</param>
        public WindowForm(int width, int height, TextureFormat format, string title)
        {
            Width = width;
            Height = height;
            Title = title;
            Format = format;

            WindowRenderForm = new RenderForm(Title);
            WindowRenderForm.Width = Width;
            WindowRenderForm.Height = Height;
            WindowRenderForm.BackColor = Color.Black;

            ModeDescription backBufferDesc = new ModeDescription(Width, Height, new Rational(60, 1), (Format)format);
            SwapChainDescription swapChainDesc = new SwapChainDescription()
            {
                ModeDescription = backBufferDesc,
                SampleDescription = new SampleDescription(1, 0),
                Usage = Usage.RenderTargetOutput,
                BufferCount = 1,
                OutputHandle = WindowRenderForm.Handle,
                IsWindowed = true
            };


            SharpDX.Direct3D11.Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, swapChainDesc, out device, out swapChain);

            targetView = new RenderTargetView(device, swapChain.GetBackBuffer<Texture2D>(0));

            Device = new CSDevice(device.NativePointer);

            Output = Device.CreateTexture2D(Width, Height, format, true);
            outputTexture = new Texture2D(Output.ResourceNativePointer);
        }

        /// <summary>
        /// Runs the window form. Note that once this function is called, the cpu will not continue past the function until the window is closed (though <paramref name="updateAction"/> will still be called evey frame).
        /// </summary>
        /// <param name="updateAction">The drawing action to update the window form evey frame.</param>
        public void Run(Action<CSTexture2D> updateAction)
        {
            updateMethod = updateAction;
            RenderLoop.Run(WindowRenderForm, Update);
        }

        private void Update()
        {
            updateMethod(Output);

            device.ImmediateContext.CopyResource(outputTexture, targetView.Resource);

            swapChain.Present(1, PresentFlags.None);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Device.Dispose();
            WindowRenderForm.Dispose();
            swapChain.Dispose();
            Output.Dispose();
            targetView.Dispose();
        }
    }
}
