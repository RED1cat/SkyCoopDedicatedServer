using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SkyCoop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DedicatedServer
{
    internal class CustomConsole
    {
        public static MouseState mouseState;
        static bool myBoxHasFocus = false;
        static StringBuilder myTextBoxDisplayCharacters = new StringBuilder();
        public static void Updata()
        {
            mouseState = Mouse.GetState();
            var isClicked = mouseState.LeftButton == ButtonState.Pressed;
            CheckClickOnMyBox(mouseState.Position, isClicked, new Rectangle(0, 0, 200, 200));
        }
        public static void Draw(SpriteBatch _spriteBatch)
        {
            _spriteBatch.Draw(MyMod.fontBg, new Vector2(0, 440), new Rectangle(0, 0, 800, 40), Color.White);

            _spriteBatch.DrawString(MyMod.font, myBoxHasFocus.ToString(), new Vector2(10, 100), Color.White);
            try
            {
                _spriteBatch.DrawString(MyMod.font, myTextBoxDisplayCharacters, new Vector2(10, 448), Color.White);
            }
            catch
            {
                myTextBoxDisplayCharacters.Remove(myTextBoxDisplayCharacters.Length - 1, 1);
            }
        }
        public static void RegisterFocusedButtonForTextInput(System.EventHandler<TextInputEventArgs> method)
        {
            gw.TextInput += method;
        }
        public static void UnRegisterFocusedButtonForTextInput(System.EventHandler<TextInputEventArgs> method)
        {
            gw.TextInput -= method;
        }
        public static void CheckClickOnMyBox(Point mouseClick, bool isClicked, Rectangle r)
        {
            if (r.Contains(mouseClick) && isClicked)
            {
                myBoxHasFocus = !myBoxHasFocus;
                if (myBoxHasFocus)
                    RegisterFocusedButtonForTextInput(OnInput);
                else
                    UnRegisterFocusedButtonForTextInput(OnInput);
            }
        }
        public static void OnInput(object sender, TextInputEventArgs e)
        {
            var k = e.Key;
            var c = e.Character;
            myTextBoxDisplayCharacters.Append(c);
            Console.WriteLine(myTextBoxDisplayCharacters);
        }
        
    }

}
