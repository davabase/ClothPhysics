using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace ClothPhysics
{
    public class Point
    {
        public Vector2 position, previousPosition;
        public bool locked;
    }

    public class Stick
    {
        public Point pointA, pointB;
        public float length;
    }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        int pointRadius = 4;
        float stickThickness = 2f;
        Texture2D pointTexture, lineTexture;

        Vector2 gravity = new Vector2(0f, 0.005f);
        bool simulate = false;
        List<Point> points = new List<Point>();
        List<Stick> sticks = new List<Stick>();

        bool leftClickPrevious, rightClickPrevious, spacePressedPrevious, fPressedPrevious;
        Point selectedPoint = null;
        Point dragPoint = new Point();
        Stick dragStick = new Stick();

        Texture2D CreateCircle(int radius, Color color)
        {
            int diameter = radius * 2;
            Texture2D texture = new Texture2D(_graphics.GraphicsDevice, diameter, diameter);
            Color[] colorData = new Color[diameter * diameter];

            for (int x = 0; x < diameter; x++)
            {
                for (int y = 0; y < diameter; y++)
                {
                    int index = x * diameter + y;
                    float distanceToCenter = (float)Math.Sqrt(Math.Pow(radius - x, 2) + Math.Pow(radius - y, 2));
                    if (distanceToCenter <= radius)
                    {
                        colorData[index] = color;
                    }
                    else
                    {
                        colorData[index] = Color.Transparent;
                    }
                }
            }

            texture.SetData(colorData);
            return texture;
        }

        void DrawLine(SpriteBatch spriteBatch, Vector2 pointA, Vector2 pointB, Color color, float thickness = 1f)
        {
            float distance = Vector2.Distance(pointA, pointB);
            float angle = (float)Math.Atan2(pointB.Y - pointA.Y, pointB.X - pointA.X);
            Vector2 origin = new Vector2(0f, 0.5f);
            Vector2 scale = new Vector2(distance, thickness);
            spriteBatch.Draw(lineTexture, pointA, null, color, angle, origin, scale, SpriteEffects.None, 0);
        }

        void Simulate(GameTime gameTime, int numIterations = 20)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            foreach(Point point in points)
            {
                if (!point.locked)
                {
                    Vector2 positionBeforeUpdate = point.position;
                    Vector2 velocity = point.position - point.previousPosition;
                    // Set a max speed, otherwise the cloth bounces too much.
                    if (velocity.Length() > 10f)
                    {
                        velocity = Vector2.Normalize(velocity) * 10f;
                    }
                    point.position += velocity;
                    Vector2 acceleration = gravity * deltaTime * deltaTime;
                    point.position += acceleration;
                    point.previousPosition = positionBeforeUpdate;
                }
            }

            for (int i = 0; i < numIterations; i++)
            {
                foreach(Stick stick in sticks)
                {
                    Vector2 center = (stick.pointA.position + stick.pointB.position) / 2f;
                    Vector2 direction = Vector2.Normalize(stick.pointA.position - stick.pointB.position);
                    if (!stick.pointA.locked)
                    {
                        stick.pointA.position = center + direction * stick.length / 2f;
                    }
                    if (!stick.pointB.locked)
                    {
                        stick.pointB.position = center - direction * stick.length / 2f;
                    }
                }
            }
        }

        void GenerateGrid(Vector2 position, float width, float height, int columns, int rows)
        {
            float xDistance = width / columns;
            float yDistance = height / rows;

            for (int x = 0; x < columns; x++)
            {
                Point pointPrevious = null;
                for (int y = 0; y < rows; y++)
                {
                    Point point = new Point();
                    point.position.X = x * xDistance;
                    point.position.Y = y * yDistance;
                    point.position += position;
                    points.Add(point);

                    if (pointPrevious != null)
                    {
                        Stick stick = new Stick();
                        stick.pointA = pointPrevious;
                        stick.pointB = point;
                        stick.length = Vector2.Distance(point.position, pointPrevious.position);
                        sticks.Add(stick);
                    }
                    pointPrevious = point;
                }
            }

            for (int i = 0; i < rows; i++)
            {
                for(int j = columns + i; j < points.Count; j += columns)
                {
                    Point point = points[j];
                    Point pointPrevious = points[j - columns];
                    Stick stick = new Stick();
                    stick.pointA = pointPrevious;
                    stick.pointB = point;
                    stick.length = Vector2.Distance(point.position, pointPrevious.position);
                    sticks.Add(stick);
                }
            }
        }

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.ToggleFullScreen();
            _graphics.ApplyChanges();

            pointTexture = CreateCircle(pointRadius, Color.White);
            lineTexture = new Texture2D(_graphics.GraphicsDevice, 1, 1);
            Color[] colors = new Color[1];
            Array.Fill<Color>(colors, Color.White);
            lineTexture.SetData<Color>(colors);

            Vector2 position = new Vector2(1280f / 2f - 720f / 2f, 25f);
            GenerateGrid(position, 720, 720 - 50, 30, 30);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            MouseState mouseState = Mouse.GetState();
            KeyboardState keyState = Keyboard.GetState();
            Vector2 mousePos = mouseState.Position.ToVector2();
            bool leftClick = mouseState.LeftButton == ButtonState.Pressed && !leftClickPrevious;
            bool leftReleased = mouseState.LeftButton == ButtonState.Released && leftClickPrevious;
            bool leftPressed = mouseState.LeftButton == ButtonState.Pressed;
            bool rightClick = mouseState.RightButton == ButtonState.Pressed && !rightClickPrevious;
            leftClickPrevious = mouseState.LeftButton == ButtonState.Pressed;
            rightClickPrevious = mouseState.RightButton == ButtonState.Pressed;
            bool spacePressed = keyState.IsKeyDown(Keys.Space) && !spacePressedPrevious;
            spacePressedPrevious = keyState.IsKeyDown(Keys.Space);
            bool controlDown = keyState.IsKeyDown(Keys.LeftControl) || keyState.IsKeyDown(Keys.RightControl);
            bool fPressed = keyState.IsKeyDown(Keys.F) && !fPressedPrevious;
            fPressedPrevious = keyState.IsKeyDown(Keys.F);

            if (!simulate)
            {
                if (leftClick)
                {
                    if (!controlDown)
                    {
                        bool selected = false;
                        foreach(Point point in points)
                        {
                            if (Vector2.Distance(mousePos, point.position) <= pointRadius)
                            {
                                selectedPoint = point;
                                selected = true;
                                dragStick.pointA = point;
                                dragStick.pointB = dragPoint;
                                sticks.Add(dragStick);
                                break;
                            }
                        }

                        if (!selected)
                        {
                            Point point = new Point();
                            point.position = mousePos;
                            points.Add(point);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < points.Count; i++)
                        {
                            Point point = points[i];
                            if (Vector2.Distance(mousePos, point.position) <= pointRadius)
                            {
                                points.Remove(point);
                                for (int j = 0; j < sticks.Count; j++)
                                {
                                    Stick stick = sticks[j];
                                    if (stick.pointA == point || stick.pointB == point)
                                    {
                                        sticks.Remove(stick);
                                        j--;
                                    }
                                }
                                break;
                            }
                        }
                    }
                }

                if (selectedPoint != null)
                {
                    dragPoint.position = mousePos;
                    if (leftReleased)
                    {
                        foreach(Point point in points)
                        {
                            if (Vector2.Distance(mousePos, point.position) <= pointRadius)
                            {
                                Stick stick = new Stick();
                                stick.pointA = selectedPoint;
                                stick.pointB = point;
                                stick.length = Vector2.Distance(selectedPoint.position, point.position);
                                sticks.Add(stick);
                                selectedPoint = null;
                                sticks.Remove(dragStick);
                                break;
                            }
                        }
                    }
                }

                if (rightClick)
                {
                    foreach(Point point in points)
                    {
                        if (Vector2.Distance(mousePos, point.position) <= pointRadius)
                        {
                            point.locked = !point.locked;
                        }
                    }
                }
            }

            if (spacePressed)
            {
                simulate = !simulate;
            }

            if (simulate)
            {
                Simulate(gameTime);

                 if (leftPressed)
                {
                    foreach(Stick stick in sticks)
                    {
                        float distance = MathF.Abs(Vector2.Distance(stick.pointA.position, stick.pointB.position) -
                                                   (Vector2.Distance(mousePos, stick.pointA.position) +
                                                    Vector2.Distance(mousePos, stick.pointB.position)));
                        // Distance the mouse has to be from the line.
                        if (distance < 2f)
                        {
                            sticks.Remove(stick);
                            Point pointA = stick.pointA;
                            Point pointB = stick.pointB;
                            bool aInStick = false;
                            bool bInStick = false;
                            foreach(Stick s in sticks)
                            {
                                if (s.pointA == pointA || s.pointB == pointA)
                                {
                                    aInStick = true;
                                }
                                if (s.pointA == pointB || s.pointB == pointB)
                                {
                                    bInStick = true;
                                }
                            }
                            if (!aInStick)
                            {
                                points.Remove(pointA);
                            }
                            if (!bInStick)
                            {
                                points.Remove(pointB);
                            }
                            break;
                        }
                    }
                }
            }

            if (fPressed)
            {
                _graphics.ToggleFullScreen();
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();
            foreach(Stick stick in sticks)
            {
                DrawLine(_spriteBatch, stick.pointA.position, stick.pointB.position, Color.White, stickThickness);
            }

            foreach(Point point in points)
            {
                Color color = point.locked ? Color.Red : Color.White;
                _spriteBatch.Draw(pointTexture, point.position - new Vector2(pointRadius), color);
            }
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
