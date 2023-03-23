using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkyCoopDedicatedServerGraphBuilder
{
    internal class CustomConsole
    {
        const int symbolLimit = 92;
        const int symbolHeight = 19;
        const int lineLimit = 23;
        const int lineBufferLimit = 75;
        const int commandLineBufferLimit = 10;
        static int consolePosition = 0;
        static int commandHistoryIndex = 0;
        static MouseState mouseState;
        static bool textBoxHasFocus = false;
        static StringBuilder textBoxDisplayCharacters = new StringBuilder();
        static List<Line> lineBuffer = new List<Line>();
        static List<string> commandLineBuffer = new List<string>();
        static bool mouseIsClick = false;
        static bool keyBackSpaceIsClick = false;
        static bool keyEnterIsClick = false;
        static bool keyPageUpIsClick = false;
        static bool keyPageDownIsClick = false;
        static bool keyUpIsClick = false;
        static bool keyDownIsClick = false;
        static bool cursorBlink = false;
        static float currentTime = 0f;
        
        private class Line
        {
            public string line { get; set; }
            public Color color { get; set; }
        }
        public class Logger
        {
            public static void Log(string message, LoggerColor lColor = LoggerColor.White)
            {
                Color xnaColor = ColorToXna(lColor);
                string consoleLog = $"[{DateTime.Now.ToString() + '.' + DateTime.Now.Millisecond.ToString()}] {message}";

                AddLine(consoleLog, xnaColor);
            }
            public enum LoggerColor
            {
                Red,
                Green,
                Blue,
                Yellow,
                Magenta,
                White,
            }
            public static Color ColorToXna(LoggerColor lColor)
            {
                switch (lColor)
                {
                    case LoggerColor.Red:
                        return Color.Red;
                    case LoggerColor.Green:
                        return Color.Green;
                    case LoggerColor.Blue:
                        return Color.Blue;
                    case LoggerColor.Yellow:
                        return Color.Yellow;
                    case LoggerColor.Magenta:
                        return Color.Magenta;
                    case LoggerColor.White:
                        return Color.White;
                    default:
                        return Color.White;
                }
            }
        }

        public static void AddLine(string line, Color color)
        {
            line = line.Replace("\r", "");
            if(lineBuffer.Count >= lineBufferLimit)
            {
                lineBuffer.RemoveAt(0);
                if(line.Length > symbolLimit )
                {
                    consolePosition -= line.Length / symbolLimit;
                }
                else
                {
                    consolePosition--;
                }
            }

            if (line.Length > symbolLimit || line.Contains('\n'))
            {
                string curLine = "";
                char charItem;
                foreach (char item in line)
                {
                    charItem = item;
                    if ((char.IsLetterOrDigit(charItem) || char.IsSymbol(charItem) || char.IsPunctuation(charItem) || char.IsWhiteSpace(charItem)) == false)
                    {
                        charItem = '?';
                    }
                    if (curLine.Length == symbolLimit || item == '\n')
                    {
                        if(item == '\n')
                        {
                            charItem = new char();
                        }
                        Line lineToAdd1 = new Line();
                        lineToAdd1.color = color;
                        lineToAdd1.line = curLine;
                        if (lineBuffer.Count >= lineLimit)
                        {
                            consolePosition++;
                        }
                        lineBuffer.Add(lineToAdd1);
                        curLine = "";
                    }
                    else
                    {
                        curLine += charItem;
                    }
                }
                Line lineToAdd2 = new Line();
                lineToAdd2.line = curLine;
                lineToAdd2.color = color;
                if (lineBuffer.Count >= lineLimit)
                {
                    consolePosition++;
                }
                lineBuffer.Add(lineToAdd2);
            }
            else
            {
                Line lineToAdd3 = new Line();
                lineToAdd3.line = line;
                lineToAdd3.color = color;
                if (lineBuffer.Count >= lineLimit)
                {
                    consolePosition++;
                }
                lineBuffer.Add(lineToAdd3);
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
                if (lime.Contains('/'))
                {
                    Logger.Log($"[Console] {lime}", Logger.LoggerColor.Yellow);
                    Logger.Log($"[Console] {XnaMain.ExecuteCommand(lime.Replace("/", ""))}", Logger.LoggerColor.Yellow);
                }
                else
                {
                    Logger.Log("[Console] Use /command", Logger.LoggerColor.Yellow);
                }

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

            UpdateConsolekey();
        }
        public static void Draw(SpriteBatch _spriteBatch, GameTime gameTime)
        {
            _spriteBatch.Draw(XnaMain.fontBg, new Vector2(0, 440), new Rectangle(0, 0, 800, 40), Color.White);

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
            if(cursorBlink && textBoxHasFocus) 
            {
                textBoxDisplayCharacters.Append('_');
                _spriteBatch.DrawString(XnaMain.font, textBoxDisplayCharacters, new Vector2(10, 448), Color.White);
                textBoxDisplayCharacters.Remove(textBoxDisplayCharacters.Length - 1, 1);
            }
            else
            {
                _spriteBatch.DrawString(XnaMain.font, textBoxDisplayCharacters, new Vector2(10, 448), Color.White);
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
                boundery = start + lineLimit;

                if(boundery > lineBuffer.Count)
                {
                    boundery = lineBuffer.Count;
                }
            }

            int index = 0;
            for (int i = start; i < boundery; i++)
            {
                string line = lineBuffer.ElementAt(i).line;
                Color color = lineBuffer.ElementAt(i).color;
                _spriteBatch.DrawString(XnaMain.font, line, new Vector2(5, symbolHeight * index), color);
                index++;
            }
        }
        public static void RegisterFocusedButtonForTextInput(System.EventHandler<TextInputEventArgs> method)
        {
            XnaMain.gw.TextInput += method;
        }
        public static void UnRegisterFocusedButtonForTextInput(System.EventHandler<TextInputEventArgs> method)
        {
            XnaMain.gw.TextInput -= method;
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
        private static void UpdateConsolekey()
        {
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
                if (consolePosition > 0)
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
                if (consolePosition < lineBuffer.Count - lineLimit && lineBuffer.Count + 1 >= lineLimit)
                {
                    consolePosition++;
                }
                keyPageDownIsClick = true;
            }
            else if (Keyboard.GetState().IsKeyUp(Keys.PageDown))
            {
                keyPageDownIsClick = false;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Down) && keyDownIsClick == false)
            {
                if (commandLineBuffer.Count > 0)
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
                keyDownIsClick = true;
            }
            else if (Keyboard.GetState().IsKeyUp(Keys.Down))
            {
                keyDownIsClick = false;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Up) && keyUpIsClick == false)
            {
                if (commandLineBuffer.Count > 0)
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
                keyUpIsClick = true;
            }
            else if (Keyboard.GetState().IsKeyUp(Keys.Up))
            {
                keyUpIsClick = false;
            }
        }
    }

}
