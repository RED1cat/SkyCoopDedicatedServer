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
        const int symbolLimit = 90;
        const int symbolHeight = 18;
        const int lineLimit = 25;
        public static MouseState mouseState;
        static bool textBoxHasFocus = false;
        static StringBuilder textBoxDisplayCharacters = new StringBuilder();
        static LinkedList<string> textBuffer = new LinkedList<string>();
        static bool mouseIsClick = false;
        static bool keyBackSpaceIsClick = false;
        static bool keyEnterIsClick = false;
        static bool cursorBlink = false;
        static float currentTime = 0f;
        public static void AddLine(string line)
        {
            if(line.Length >= symbolLimit)
            {
                string curLine = "";
                while(line != "")
                {
                    if(curLine.Length <= symbolLimit)
                    {
                        curLine += line[0];
                        line = line.Remove(0, 1);
                    }
                    else
                    {
                        textBuffer.AddLast(curLine);
                        curLine = "";
                    }
                }
                textBuffer.AddLast(curLine);
            }
            else
            {
                textBuffer.AddLast(line);
            }
        }
        static void ReadLine()
        {
            if(textBoxDisplayCharacters.Length != 0)
            {
                string lime = textBoxDisplayCharacters.ToString();
                if (lime.Contains('\r'))
                {
                    lime = lime.Replace("\r", "");
                }
                Logger.Log("[Console] " + lime);
                Logger.Log("[Console] " + Shared.ExecuteCommand(lime));
                textBoxDisplayCharacters.Clear();
            }
        }
        public static void Updata(GameTime gameTime)
        {
            mouseState = Mouse.GetState();
            if(mouseState.LeftButton == ButtonState.Pressed && mouseIsClick == false)
            {
                CheckClickOnMyBox(mouseState.Position, true, new Rectangle(0, 440, 800, 40));
                mouseIsClick = true;
            }
            else if(mouseState.LeftButton == ButtonState.Released)
            {
                mouseIsClick= false;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Back) && keyBackSpaceIsClick == false && textBoxHasFocus == true && textBoxDisplayCharacters.Length > 0)
            {
                textBoxDisplayCharacters.Remove(textBoxDisplayCharacters.Length - 1, 1);
                keyBackSpaceIsClick = true;
            }
            else if (Keyboard.GetState().IsKeyUp(Keys.Back))
            {
                keyBackSpaceIsClick = false;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Enter) && keyEnterIsClick == false && textBoxHasFocus == true)
            {
                ReadLine();
                keyEnterIsClick = true;
            }
            else if (Keyboard.GetState().IsKeyUp(Keys.Enter))
            {
                keyEnterIsClick = false;
            }
        }
        public static void Draw(SpriteBatch _spriteBatch, GameTime gameTime)
        {
            _spriteBatch.Draw(MyMod.fontBg, new Vector2(0, 440), new Rectangle(0, 0, 800, 40), Color.White);

            currentTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (currentTime >= 0.2f)
            {
                currentTime -= 0.2f;
                if (textBoxHasFocus)
                {
                    cursorBlink = !cursorBlink;
                }
            }
            if(cursorBlink) 
            {
                textBoxDisplayCharacters.Append('_');
                _spriteBatch.DrawString(MyMod.font, textBoxDisplayCharacters, new Vector2(10, 448), Color.White);
                textBoxDisplayCharacters.Remove(textBoxDisplayCharacters.Length - 1, 1);
            }
            else
            {
                _spriteBatch.DrawString(MyMod.font, textBoxDisplayCharacters, new Vector2(10, 448), Color.White);
            }

            if (textBuffer.Count != 0)
            {
                int index = 0;
                if (textBuffer.Count >= lineLimit)
                {
                    textBuffer.RemoveFirst();
                }
                foreach (string line in textBuffer)
                {
                    _spriteBatch.DrawString(MyMod.font, line, new Vector2(5, symbolHeight * index), Color.White);
                    index++;
                }
            }
        }
        public static void RegisterFocusedButtonForTextInput(System.EventHandler<TextInputEventArgs> method)
        {
            MyMod.gw.TextInput += method;
        }
        public static void UnRegisterFocusedButtonForTextInput(System.EventHandler<TextInputEventArgs> method)
        {
            MyMod.gw.TextInput -= method;
        }
        public static void CheckClickOnMyBox(Point mouseClick, bool isClicked, Rectangle r)
        {
            if (r.Contains(mouseClick) && isClicked)
            {
                textBoxHasFocus = !textBoxHasFocus;
                if (textBoxHasFocus)
                    RegisterFocusedButtonForTextInput(OnInput);
                else
                    UnRegisterFocusedButtonForTextInput(OnInput);
            }
        }
        public static void OnInput(object sender, TextInputEventArgs e)
        {
            if (char.IsLetterOrDigit(e.Character) || char.IsSymbol(e.Character) || char.IsPunctuation(e.Character) || char.IsWhiteSpace(e.Character))
            {
                textBoxDisplayCharacters.Append(e.Character);
            }
        }
        
    }

}
