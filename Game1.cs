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

        float lastWindChange = 0;

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

            // Solve simulation for stick length.
            // The less iterations, the more stretchy the cloth behaves because we are not trying to hard to set constraints.
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
            // Calculate the separation distance between each point.
            float xDistance = width / columns;
            float yDistance = height / rows;

            // Loop through all points.
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

                    // Connect the columns together with sticks.
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

            // Connect the rows together with sticks.
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
            // Update the size of the window and toggle full screen.
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.ToggleFullScreen();
            _graphics.ApplyChanges();

            // Create the circle texture and line texture.
            pointTexture = CreateCircle(pointRadius, Color.White);
            lineTexture = new Texture2D(_graphics.GraphicsDevice, 1, 1);
            Color[] colors = new Color[1];
            Array.Fill<Color>(colors, Color.White);
            lineTexture.SetData<Color>(colors);

            // Generate a grid of points and sticks in the center of the screen.
            Vector2 position = new Vector2(1280f / 2f - 720f / 2f, 25f);
            GenerateGrid(position, 720, 720 - 50, 25, 25);

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

            // Capture mouse state and keyboard state.
            // Since we can only check if a button or key is down, save the previous state of the button to find the rising edge.
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

            // Simple random wind.
            bool doWind = false;
            if (doWind)
            {
                if (gameTime.TotalGameTime.TotalSeconds - lastWindChange > 5f)
                {
                    Random rand = new Random();
                    float wind = (float)rand.NextDouble() * 0.0025f;
                    wind *= rand.Next(2) == 1 ? -1 : 1;
                    gravity.X = wind;
                    lastWindChange = (float)gameTime.TotalGameTime.TotalSeconds;
                }
            }

            // Allow modification of the cloth when the simulation is not running.
            if (!simulate)
            {
                if (leftClick)
                {
                    if (!controlDown)
                    {
                        // Check if we clicked on a point.
                        bool selected = false;
                        foreach(Point point in points)
                        {
                            if (Vector2.Distance(mousePos, point.position) <= pointRadius)
                            {
                                // If we did, set selectedPoint to it and setup dragStick to use it.
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
                            // If we didn't click on a point, create a new one instead.
                            Point point = new Point();
                            point.position = mousePos;
                            points.Add(point);
                        }
                    }
                    else
                    {
                        // Delete points if we are holding the control key while clicking.
                        for (int i = 0; i < points.Count; i++)
                        {
                            Point point = points[i];
                            if (Vector2.Distance(mousePos, point.position) <= pointRadius)
                            {
                                points.Remove(point);

                                // Loop through all sticks and delete the ones that used this point.
                                for (int j = 0; j < sticks.Count; j++)
                                {
                                    Stick stick = sticks[j];
                                    if (stick.pointA == point || stick.pointB == point)
                                    {
                                        sticks.Remove(stick);

                                        // Since we modified the list in-place by removing the item at j, 
                                        // go back one to catch the next item.
                                        j--;
                                    }
                                }
                                // Only handle one point at a time so we don't accidentally delete two.
                                break;
                            }
                        }
                    }
                }

                // Check if we are currently dragging from one point to another.
                if (selectedPoint != null)
                {
                    // Move dragPoint to the mouse position so we can draw a stick from selectedPoint to the mouse.
                    dragPoint.position = mousePos;

                    // Check ff we released the mouse button on a point.
                    if (leftReleased)
                    {
                        foreach(Point point in points)
                        {
                            if (Vector2.Distance(mousePos, point.position) <= pointRadius)
                            {
                                // We've successfully dragged a stick from one point to another, create a stick.
                                Stick stick = new Stick();
                                stick.pointA = selectedPoint;
                                stick.pointB = point;
                                stick.length = Vector2.Distance(selectedPoint.position, point.position);
                                sticks.Add(stick);

                                // Remove dragStick from sticks so it is no longer drawn.
                                // Set selectedPoint to null to reset the dragging state.
                                sticks.Remove(dragStick);
                                selectedPoint = null;
                                break;
                            }
                        }
                    }
                }

                // Lock points if we right click on them.
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

            // Toggle the simulation if the spacebar is pressed.
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
                        // The distance the mouse is from the stick.
                        // https://stackoverflow.com/a/17693146/13900323
                        float distance = MathF.Abs(Vector2.Distance(stick.pointA.position, stick.pointB.position) -
                                                   (Vector2.Distance(mousePos, stick.pointA.position) +
                                                    Vector2.Distance(mousePos, stick.pointB.position)));

                        // Distance the mouse has to be from the line. 2 is the "radius" of the mouse.
                        if (distance < 2f)
                        {
                            sticks.Remove(stick);

                            // Once we remove the stick, check if the points are connected to any other sticks.
                            // Remove them if they are not.
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

            // Toggle fullscreen if F is pressed.
            // Also, pay respects.
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
                // Draw locked points as red.
                Color color = point.locked ? Color.Red : Color.White;
                _spriteBatch.Draw(pointTexture, point.position - new Vector2(pointRadius), color);
            }
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
