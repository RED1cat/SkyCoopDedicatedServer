using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkyCoop;

namespace DedicatedServer
{
    public class Graph
    {
        public enum GraphType
        {
            Line,
            Fill
        }

        /// <summary>
        /// Determines whether the drawn graph will be line only, or filled
        /// </summary>
        public GraphType Type { get; set; }

        /// <summary>
        /// The bottom left position of the graph
        /// </summary>
        public Vector2 Position { get; set; }
        /// <summary>
        /// The size of the graph.
        /// The graph values will be scaled horizontally to fill width (Size.X)
        /// Vertically, the values will be scaled based on MaxValue property, where the position of the value that is equal to MaxValue will be Size.Y
        /// </summary>
        public Point Size { get; set; }

        /// <summary>
        /// Determines the vertical scaling of the graph.
        /// The value that is equal to MaxValue will be displayed at the top of the graph (at point Size.Y)
        /// </summary>
        public float MaxValue { get; set; }

        private Vector2 _scale = new Vector2(1.0f, 1.0f);

        BasicEffect _effect;
        SpriteBatch _spriteBatch;
        short[] lineListIndices;
        short[] triangleStripIndices;

        public Graph(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Point size)
        {
            _effect = new BasicEffect(graphicsDevice);
            _spriteBatch = spriteBatch;
            _effect.View = Matrix.CreateLookAt(Vector3.Backward, Vector3.Zero, Vector3.Up);
            _effect.Projection = Matrix.CreateOrthographicOffCenter(0, (float)graphicsDevice.Viewport.Width, (float)graphicsDevice.Viewport.Height, 0, 1.0f, 1000.0f);
            _effect.World = Matrix.Identity;

            _effect.VertexColorEnabled = true;

            this.MaxValue = 1;
            this.Size = size;
            if (size.Y <= 0)
                Size = new Point(size.X, 1);
            if (size.X <= 0)
                Size = new Point(1, size.Y);

            this.Type = Graph.GraphType.Line;
        }

        void UpdateWorld()
        {
            _effect.World = Matrix.CreateScale(_scale.X, _scale.Y, 1.0f)
                          * Matrix.CreateRotationX(MathHelper.Pi) //flips the graph so that the higher values are above. Makes bottom left the graph origin.
                          * Matrix.CreateTranslation(new Vector3(this.Position, 0));
        }

        /// <summary>
        /// Draws the values in given order, with specific color for each value
        /// </summary>
        /// <param name="values">Value/color pairs to draw, in order from left to right</param>
        public void Draw(List<Tuple<float, Color>> values, List<string> Names)
        {
            if (values.Count < 2)
                return;

            //creates scaling (for the transformation) based on the number of points to draw
            float xScale = this.Size.X / (float)values.Count;
            float yScale = this.Size.Y / MaxValue;

            _scale = new Vector2(xScale, yScale);
            UpdateWorld();

            //different point lists for different types of graphs
            if (this.Type == GraphType.Line)
            {
                VertexPositionColor[] pointList = new VertexPositionColor[values.Count];
                for (int i = 0; i < values.Count; i++)
                {
                    pointList[i] = new VertexPositionColor(new Vector3(i, values[i].Item1 < this.MaxValue ? values[i].Item1 : this.MaxValue, 0), values[i].Item2);
                }

                DrawLineList(pointList, Names);
            } else if (this.Type == GraphType.Fill)
            {
                VertexPositionColor[] pointList = new VertexPositionColor[values.Count * 2];
                for (int i = 0; i < values.Count; i++)
                {
                    //The vertices are created so that the triangles are inverted (back facing). When rotated they will become front facing.
                    //This is done to avoid changing rasterizer state to CullMode.CullClockwiseFace.
                    pointList[i * 2 + 1] = new VertexPositionColor(new Vector3(i, values[i].Item1 < this.MaxValue ? values[i].Item1 : this.MaxValue, 0), values[i].Item2);
                    pointList[i * 2] = new VertexPositionColor(new Vector3(i, 0, 0), values[i].Item2);
                }

                DrawTriangleStrip(pointList);
            }
        }

        /// <summary>
        /// Draws the values in given order, in specified color
        /// </summary>
        /// <param name="values">Values to draw, in order from left to right</param>
        /// <param name="color">Color of the entire graph</param>
        public void Draw(List<float> values, Color color, List<string> Names)
        {
            if (values.Count < 2)
                return;

            float xScale = this.Size.X / (float)values.Count;
            float yScale = this.Size.Y / MaxValue;

            _scale = new Vector2(xScale, yScale);
            UpdateWorld();

            if (this.Type == GraphType.Line)
            {
                VertexPositionColor[] pointList = new VertexPositionColor[values.Count];
                for (int i = 0; i < values.Count; i++)
                {
                    pointList[i] = new VertexPositionColor(new Vector3(i, values[i] < this.MaxValue ? values[i] : this.MaxValue, 0), color);
                }

                DrawLineList(pointList, Names);
            } else if (this.Type == GraphType.Fill)
            {
                VertexPositionColor[] pointList = new VertexPositionColor[values.Count * 2];
                for (int i = 0; i < values.Count; i++)
                {
                    pointList[i * 2 + 1] = new VertexPositionColor(new Vector3(i, values[i] < this.MaxValue ? values[i] : this.MaxValue, 0), color);
                    pointList[i * 2] = new VertexPositionColor(new Vector3(i, 0, 0), color);
                }

                DrawTriangleStrip(pointList);
            }
        }

        void DrawLineList(VertexPositionColor[] pointList, List<string> Names)
        {
            //indices updated only need to be updated when the number of points has changed
            if (lineListIndices == null || lineListIndices.Length != ((pointList.Length * 2) - 2))
            {
                lineListIndices = new short[(pointList.Length * 2) - 2];
                for (int i = 0; i < pointList.Length - 1; i++)
                {
                    lineListIndices[i * 2] = (short)(i);
                    lineListIndices[(i * 2) + 1] = (short)(i + 1);
                }
            }
            foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _effect.GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionColor>(
                    PrimitiveType.LineList,
                    pointList,
                    0,
                    pointList.Length,
                    lineListIndices,
                    0,
                    pointList.Length - 1
                );
            }
            int index = 0;
            foreach (string item in Names)
            {
                _spriteBatch.Begin();
                _spriteBatch.DrawString(MyMod.font, item, new Vector2(Position.X+ index*Size.X/pointList.Length, Position.Y-pointList[index].Position.Y *(Size.Y/MaxValue)-15), Color.Chocolate, 0, new Vector2(0,0), 0.7f, SpriteEffects.None, 0);
                //_spriteBatch.DrawString(MyMod.font, pointList[index].Position.Y.ToString(), new Vector2(Position.X + index * Size.X / pointList.Length, Position.Y - pointList[index].Position.Y * (Size.Y / MaxValue) + 7), Color.White, 0, new Vector2(0, 0), 0.7f, SpriteEffects.None, 0);
                _spriteBatch.End();
                index++;
            }
            int Row = 0;
            int IndexInRow = 1;
            int RowLimit = 6;
            _spriteBatch.Begin();

            _spriteBatch.DrawString(MyMod.font, "Time", new Vector2(Position.X + 20, Position.Y + 14), Color.Chocolate, 0, new Vector2(0, 0), 1f, SpriteEffects.None, 0);
            _spriteBatch.DrawString(MyMod.font, "-", new Vector2(Position.X + 67, Position.Y + 14), Color.White, 0, new Vector2(0, 0), 1f, SpriteEffects.None, 0);
            _spriteBatch.DrawString(MyMod.font, "Players On Server", new Vector2(Position.X + 80, Position.Y + 14), Color.Orange, 0, new Vector2(0, 0), 1f, SpriteEffects.None, 0);
            for (int i = 0; i < pointList.Length; i++)
            {
                Vector2 V2 = new Vector2(Position.X + 20 + Row * 80, Position.Y + 20 + IndexInRow*26);
                IndexInRow++;
                if(IndexInRow > RowLimit)
                {
                    IndexInRow = 1;
                    Row++;
                }
                
                //_spriteBatch.DrawString(MyMod.font, Names[i]+" - " +pointList[i].Position.Y, V2, Color.White, 0, new Vector2(0, 0), 0.7f, SpriteEffects.None, 0);

                _spriteBatch.DrawString(MyMod.font, Names[i], V2, Color.Chocolate, 0, new Vector2(0, 0), 0.7f, SpriteEffects.None, 0);
                _spriteBatch.DrawString(MyMod.font, "-", new Vector2(V2.X+37, V2.Y), Color.White, 0, new Vector2(0, 0), 0.7f, SpriteEffects.None, 0);
                _spriteBatch.DrawString(MyMod.font, pointList[i].Position.Y.ToString(), new Vector2(V2.X + 45, V2.Y), Color.Orange, 0, new Vector2(0, 0), 0.7f, SpriteEffects.None, 0);
                
            }
            _spriteBatch.End();
        }

        void DrawTriangleStrip(VertexPositionColor[] pointList)
        {
            if (triangleStripIndices == null || triangleStripIndices.Length != pointList.Length)
            {
                triangleStripIndices = new short[pointList.Length];
                for (int i = 0; i < pointList.Length; i++)
                    triangleStripIndices[i] = (short)i;
            }

            foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _effect.GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionColor>(
                    PrimitiveType.TriangleStrip,
                    pointList,
                    0,
                    pointList.Length,
                    triangleStripIndices,
                    0,
                    pointList.Length - 2
                );
            }
        }
    }
}
