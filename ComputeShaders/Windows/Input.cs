using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.DirectInput;

namespace ComputeShaders.Windows
{
    /// <summary>
    /// The class used to detect user input (mouse or keyboard).
    /// </summary>
    public class Input : IDisposable
    {
        private DirectInput directInput;

        private Mouse mouse;

        private MouseState currentMouseState;
        private MouseState previousMouseState;

        /// <summary>
        /// The change in the cursor position in the x-axis.
        /// </summary>
        public int MouseDeltaXPosition { get; private set; }
        /// <summary>
        /// The change in the cursor position in the y-axis.
        /// </summary>
        public int MouseDeltaYPosition { get; private set; }

        private Keyboard keyboard;

        private KeyboardState currentKeyBoardState;
        private KeyboardState previousKeyBoardState;

        /// <summary>
        /// Create a new Input instance. Note that in order for the instance to register any user data, <see cref="Update"/> must be called every frame.
        /// </summary>
        public Input()
        {
            directInput = new DirectInput();

            keyboard = new Keyboard(directInput);
            keyboard.Acquire();

            mouse = new Mouse(directInput);
            mouse.Acquire();
        }

        /// <summary>
        /// Updates the data about user input. This method should be called at the beginning of every frame.
        /// </summary>
        public void Update()
        {
            if (currentMouseState != null)
            {
                currentMouseState.Buttons.CopyTo(previousMouseState.Buttons, 0);
            }
            else
            {
                previousMouseState = new MouseState();
            }

            currentMouseState = mouse.GetCurrentState();

            MouseDeltaXPosition = currentMouseState.X;
            MouseDeltaYPosition = currentMouseState.Y;

            if (currentKeyBoardState != null)
            {
                List<Key> curList = currentKeyBoardState.PressedKeys;
                List<Key> preList = previousKeyBoardState.PressedKeys;
                int diff = curList.Count - preList.Count;

                if (diff > 0)
                {
                    for (int i = 0; i < diff; i++)
                    {
                        preList.Add(Key.Convert);
                    }
                }
                else if (diff < 0)
                {
                    for (int i = 0; i < -diff; i++)
                    {
                        preList.Remove(Key.K);
                    }
                }

                for (int i = 0; i < curList.Count; i++)
                {
                    preList[i] = curList[i];
                }
            }
            else
            {
                previousKeyBoardState = keyboard.GetCurrentState();
            }

            currentKeyBoardState = keyboard.GetCurrentState();
        }

        /// <summary>
        /// Returns if the mouse button have been pressed this frame.
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        public bool GetMouseButtonDown(MouseButton button)
        {
            int index = (int)button;

            return currentMouseState.Buttons[index] && !previousMouseState.Buttons[index];
        }
        /// <summary>
        /// Returns if the mouse button is pressed.
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        public bool GetMouseButton(MouseButton button)
        {
            int index = (int)button;

            return currentMouseState.Buttons[index];
        }
        /// <summary>
        /// Returns if the mouse button have been released this frame.
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        public bool GetMouseButtonUp(MouseButton button)
        {
            int index = (int)button;

            return !currentMouseState.Buttons[index] && previousMouseState.Buttons[index];
        }

        /// <summary>
        /// Returns if a key have been pressed this frame.
        /// </summary>
        /// <param name="key">The keyboard key.</param>
        /// <returns></returns>
        public bool GetKeyBoardButtonDown(Key key)
        {
            return currentKeyBoardState.IsPressed(key) && !previousKeyBoardState.IsPressed(key);
        }
        /// <summary>
        /// Returns if a key is pressed.
        /// </summary>
        /// <param name="key">The keyboard key.</param>
        /// <returns></returns>
        public bool GetKeyBoardButton(Key key)
        {
            return currentKeyBoardState.IsPressed(key);
        }
        /// <summary>
        /// Returns if a key have been released this frame.
        /// </summary>
        /// <param name="key">The keyboard key.</param>
        /// <returns></returns>
        public bool GetKeyBoardButtonUp(Key key)
        {
            return !currentKeyBoardState.IsPressed(key) && previousKeyBoardState.IsPressed(key);
        }

        /// <summary>
        /// Disposes the unmanaged data. 
        /// </summary>
        public void Dispose()
        {
            directInput.Dispose();
            keyboard.Dispose();
            mouse.Dispose();
        }
    }
}
