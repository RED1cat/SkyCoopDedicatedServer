using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SkyCoop;
using static SkyCoop.MyMod;

namespace DedicatedServer
{
    internal class XnaMain : Game
    {
        static float currentTime0 = 0f;
        static float CurrentTime1 = 0f;
        static float CurrentTime2 = 0f;
        public static SpriteFont font;
        public static Texture2D fontBg;
        public static GameWindow gw;

        public List<float> StatsGraphPoints = new List<float>();
        public List<string> StatsGraphDates = new List<string>();
        public static Vector2 GraphBgPossition = new Vector2(0, 0);
        public static Rectangle GraphBgRect = new Rectangle(0, 0, 0, 0);
        public Graph StatsGraph;

        private GraphicsDeviceManager _graphics;
        public static GraphicsDevice _graphicsDevice;
        private SpriteBatch _spriteBatch;
        private static SpriteBatch _spriteBatchStatic;
        private FrameCounter _frameCounter = new FrameCounter();
        public XnaMain()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            InactiveSleepTime = new TimeSpan(0);
        }
        public class PlayerOnlineTime
        {
            public int TotalPlayers = 0;
            public int Days = 0;
            public float Average()
            {
                return TotalPlayers / Days;
            }
            public PlayerOnlineTime(int p, int d)
            {
                TotalPlayers = p;
                Days = d;
            }
        }
        protected override void Initialize()
        {
            SkyCoop.MyMod.Initialize();
            base.Initialize();
        }
        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _spriteBatchStatic = _spriteBatch;
            _graphicsDevice = GraphicsDevice;
            gw = Window;
            font = Content.Load<SpriteFont>("font");
            fontBg = Content.Load<Texture2D>("fontBg");
        }
        public static void BuildGraph()
        {
            Program.xnaMain.StatsGraph = new Graph(_graphicsDevice, _spriteBatchStatic, new Point(740, 200));
            Program.xnaMain.StatsGraph.Position = new Vector2(30, 250);
            Program.xnaMain.StatsGraph.MaxValue = Server.MaxPlayers;
            Program.xnaMain.StatsGraph.Type = Graph.GraphType.Line;
            GraphBgPossition = new Vector2(Program.xnaMain.StatsGraph.Position.X, 50);
            GraphBgRect = new Rectangle(0, 0, Program.xnaMain.StatsGraph.Size.X, Program.xnaMain.StatsGraph.Size.Y + 198);
        }
        public void NewGraph(Dictionary<string, int> DayStat)
        {
            BuildGraph();
            StatsGraphPoints.Clear();
            StatsGraphDates.Clear();
            foreach (var item in DayStat)
            {
                StatsGraphPoints.Add(item.Value);
                StatsGraphDates.Add(item.Key);
            }
        }
        public void NewGraph(Dictionary<string, Dictionary<string, int>> DaysStat)
        {
            BuildGraph();
            Program.xnaMain.StatsGraphPoints.Clear();
            Program.xnaMain.StatsGraphDates.Clear();
            Dictionary<string, PlayerOnlineTime> Buffer = new Dictionary<string, PlayerOnlineTime>();
            foreach (var item in DaysStat)
            {
                Dictionary<string, int> Day = item.Value;
                string DayKey = item.Key;

                foreach (var item2 in Day)
                {
                    string TimeKey = item2.Key;
                    int Players = item2.Value;
                    if (Buffer.ContainsKey(TimeKey))
                    {
                        Buffer[TimeKey].TotalPlayers += Players;
                    }
                    else
                    {
                        Buffer.Add(TimeKey, new PlayerOnlineTime(Players, 1));
                    }
                }
            }
            foreach (var item in Buffer)
            {
                Program.xnaMain.StatsGraphPoints.Add(item.Value.Average());
                Program.xnaMain.StatsGraphDates.Add(item.Key);
            }
        }
        protected override void Update(GameTime gameTime)
        {

            Shared.OnUpdate();
            ThreadManager.UpdateMain();

            currentTime0 += (float)gameTime.ElapsedGameTime.TotalSeconds;
            CurrentTime1 += (float)gameTime.ElapsedGameTime.TotalSeconds;
            CurrentTime2 += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (currentTime0 >= 1f)
            {
                currentTime0 -= 1f;
                Shared.EverySecond();
            }
            if (CurrentTime1 >= 5f)
            {
                CurrentTime1 -= 5f;
                Shared.EveryInGameMinute();
            }
            if (CurrentTime2 >= (float)DsSavePerioud)
            {
                CurrentTime2 -= (float)DsSavePerioud;
                MPSaveManager.SaveGlobalData();
            }

            CustomConsole.Updata(gameTime);

            if (Shared.DSQuit)
            {
                this.Exit();
            }


            base.Update(gameTime);
        }
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DarkGray);
            _spriteBatch.Begin();


            CustomConsole.Draw(_spriteBatch, gameTime);


            DrawServerInfo(_spriteBatch, gameTime);

            _spriteBatch.End();

            if (StatsGraph != null)
            {
                _spriteBatch.Begin();
                _spriteBatch.Draw(fontBg, GraphBgPossition, GraphBgRect, Color.White);
                _spriteBatch.Draw(fontBg, new Vector2(StatsGraph.Position.X, StatsGraph.Position.Y + 5), new Rectangle(0, 0, GraphBgRect.Width, 5), Color.Gray);
                _spriteBatch.End();
                StatsGraph.Draw(StatsGraphPoints, Color.Orange, StatsGraphDates);
            }

            base.Draw(gameTime);
        }
        protected override void OnExiting(object sender, EventArgs args)
        {
            MyMod.OnExiting();
            base.OnExiting(sender, args);
        }
        private void DrawServerInfo(SpriteBatch _spriteBatch, GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _frameCounter.Update(deltaTime);

            _spriteBatch.Draw(fontBg, new Vector2(0, 0), new Rectangle(0, 0, 100, 44), Color.White);

            _spriteBatch.DrawString(font, "Players: " + PlayersOnServer.ToString(), new Vector2(5, 5), Color.Coral);
            _spriteBatch.DrawString(font, $"FPS: {Math.Round(_frameCounter.AverageFramesPerSecond, 3)}", new Vector2(5, 25), Color.Coral);
        }
    }
}
