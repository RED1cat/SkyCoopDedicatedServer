using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SkyCoop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DedicatedServer
{
    internal class CustomConsole
    {
        const int symbolLimit = 90;
        const int symbolHeight = 18;
        const int lineLimit = 24;
        const int lineBufferLimit = 75;
        const int commandLineBufferLimit = 10;
        static int consolePosition = 0;
        static int commandHistoryIndex = 0;
        static MouseState mouseState;
        static bool textBoxHasFocus = false;
        static StringBuilder textBoxDisplayCharacters = new StringBuilder();
        static List<string> lineBuffer = new List<string>();
        static List<string> commandLineBuffer = new List<string>();
        static bool mouseIsClick = false;
        static bool keyBackSpaceIsClick = false;
        static bool keyEnterIsClick = false;
        static bool keyPageUpIsClick = false;
        static bool keyPageDownIsClick = false;
        static bool keyUpIsClick = false;
        static bool keyDownIsClick = false;
        static bool keyCopyIsClicked = false;
        static bool keyPasteIsClicked = false;
        static bool cursorBlink = false;
        static float currentTime = 0f;
        public static void AddLine(string line)
        {
            bool needScroll = true;
            if (lineBuffer.Count >= lineBufferLimit)
            {
                lineBuffer.RemoveAt(0);
                needScroll = false;

            }
            if (line.Length >= symbolLimit)
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
                        lineBuffer.Add(curLine);
                        curLine = "";
                    }
                }
                lineBuffer.Add(curLine);
            }
            else
            {
                lineBuffer.Add(line);
            }
            if(needScroll) 
            {
                if (lineBuffer.Count + 1 > lineLimit)
                {
                    consolePosition++;
                }
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

                if(commandLineBuffer.Count - 1> commandLineBufferLimit)
                {
                    commandLineBuffer.RemoveAt(0);
                }
                commandLineBuffer.Add(lime);

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

            if (Keyboard.GetState().IsKeyDown(Keys.PageUp) && keyPageUpIsClick == false)
            {
                if(consolePosition > 0)
                {
                    consolePosition--;
                }
                keyPageUpIsClick = true;
            }
            else if (Keyboard.GetState().IsKeyUp(Keys.PageUp))
            {
                keyPageUpIsClick = false;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.PageDown) && keyPageDownIsClick == false)
            {
                if(consolePosition < lineBuffer.Count - lineLimit && lineBuffer.Count + 1 >= lineLimit)
                {
                    consolePosition++;
                }
                keyPageDownIsClick = true;
            }
            else if (Keyboard.GetState().IsKeyUp(Keys.PageDown))
            {
                keyPageDownIsClick = false;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Up) && keyUpIsClick == false)
            {
                if(commandLineBuffer.Count > 0)
                {
                    if (commandHistoryIndex > 0)
                    {
                        commandHistoryIndex--;
                    }
                    else
                    {
                        commandHistoryIndex = commandLineBuffer.Count - 1;
                    }
                    textBoxDisplayCharacters.Clear();
                    textBoxDisplayCharacters.Append(commandLineBuffer.ElementAt(commandHistoryIndex));
                }
                keyUpIsClick = true;
            }
            else if (Keyboard.GetState().IsKeyUp(Keys.Up))
            {
                keyUpIsClick = false;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Down) && keyDownIsClick == false)
            {
                if(commandLineBuffer.Count > 0)
                {
                    if (commandHistoryIndex < commandLineBuffer.Count - 1)
                    {
                        commandHistoryIndex++;
                    }
                    else
                    {
                        commandHistoryIndex = 0;
                    }
                    textBoxDisplayCharacters.Clear();
                    textBoxDisplayCharacters.Append(commandLineBuffer.ElementAt(commandHistoryIndex));
                }
                keyDownIsClick = true;
            }
            else if (Keyboard.GetState().IsKeyUp(Keys.Down))
            {
                keyDownIsClick = false;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.LeftControl))
            {
                if (Keyboard.GetState().IsKeyDown(Keys.C) && keyCopyIsClicked == false)
                {
                    AddLine("Не забуть, что забыл, добавить копирование, потому что нету форм и КЛИПОРТА");
                    keyCopyIsClicked = true;
                }
                else if (Keyboard.GetState().IsKeyUp(Keys.C))
                {
                    keyCopyIsClicked = false;
                }
                if (Keyboard.GetState().IsKeyDown(Keys.V) && keyPasteIsClicked == false)
                {
                    AddLine("Не забуть, что забыл, добавить вставку, потому что нету форм и КЛИПОРТА");
                    keyPasteIsClicked = true;
                }
                else if (Keyboard.GetState().IsKeyUp(Keys.V))
                {
                    keyPasteIsClicked = false;
                }
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
                    keyBackSpaceIsClick = false;
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

            int boundery;
            int start;

            if (lineBuffer.Count < lineLimit)
            {
                boundery = lineBuffer.Count;
                start = 0;
            }
            else
            {
                start = consolePosition;
                boundery = start+lineLimit;

                if(boundery > lineBuffer.Count)
                {
                    boundery = lineBuffer.Count;
                }
            }

            int index = 0;
            for (int i = start; i < boundery; i++)
            {
                string line = lineBuffer.ElementAt(i);
                _spriteBatch.DrawString(MyMod.font, line, new Vector2(5, symbolHeight * index), Color.White);
                index++;
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
