using Android.Views;
using BinaryLand.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace BinaryLand
{
    public class BinaryLandGame : Game
    {
        // Resources for drawing.
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        Vector2 baseScreenSize = new (800, 480);
        private Matrix globalTransformation;
        int backbufferWidth, backbufferHeight;

        // Global content.
        private SpriteFont hudFont;

        private Texture2D winOverlay;
        private Texture2D loseOverlay;
        private Texture2D diedOverlay;

        private GamePadState gamePadState;
        private KeyboardState keyboardState;
        private TouchCollection touchState;

        private VirtualGamePad virtualGamePad;
        private Stick Stick;

        public BinaryLandGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            ScalePresentationArea();

            // Load fonts
            hudFont = Content.Load<SpriteFont>("Fonts/heartspritefont");

            // Load overlay textures
            //winOverlay = Content.Load<Texture2D>("Overlays/you_win");
            //loseOverlay = Content.Load<Texture2D>("Overlays/you_lose");
            //diedOverlay = Content.Load<Texture2D>("Overlays/you_died");


            //float aliveZoneFollowFactor = 1.3f, float aliveZoneFollowSpeed = 0.05f, float edgeSpacing = 25f, float aliveZoneSize = 65f, float deadZoneSize = 5f
            Stick = new (Content.Load<Texture2D>("Sprites/arrows"), 5f, new Rectangle(0, 100, (int)(TouchPanel.DisplayWidth * 0.3f), TouchPanel.DisplayHeight - 100), 65f, 1.3f, 0.05f, 25f);
            Stick.FixedLocation = new Vector2(65f * 1.3f, TouchPanel.DisplayHeight - 65f * 1.3f);
            Stick.SetAsFree();

            virtualGamePad = new VirtualGamePad(baseScreenSize, globalTransformation, Content.Load<Texture2D>("Sprites/arrows"));
        }

        public void ScalePresentationArea()
        {
            backbufferWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
            backbufferHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;
            float horScaling = backbufferWidth / baseScreenSize.X;
            float verScaling = backbufferHeight / baseScreenSize.Y;
            Vector3 screenScalingFactor = new Vector3(horScaling, verScaling, 1);
            globalTransformation = Matrix.CreateScale(screenScalingFactor);
            System.Diagnostics.Debug.WriteLine("Screen Size - Width[" + GraphicsDevice.PresentationParameters.BackBufferWidth + "] Height [" + GraphicsDevice.PresentationParameters.BackBufferHeight + "]");
        }

        protected override void Update(GameTime gameTime)
        {
            //if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            //    Exit();
            //// TODO: Add your update logic here

            //Confirm the screen has not been resized by the user
            if (backbufferHeight != GraphicsDevice.PresentationParameters.BackBufferHeight ||
                backbufferWidth != GraphicsDevice.PresentationParameters.BackBufferWidth)
            {
                ScalePresentationArea();
            }
            // Handle polling for our input and handling high-level input
            HandleInput(gameTime);
            Vector2 relativePostion = Stick.GetRelativeVector(65f);
            

            // update our level, passing down the GameTime along with all of our input states
            //level.Update(gameTime, keyboardState, gamePadState,
            //             accelerometerState, Window.CurrentOrientation);

            //if (level.Player.Velocity != Vector2.Zero)
            //    virtualGamePad.NotifyPlayerIsMoving();

            base.Update(gameTime);
        }


        private readonly TapStart[] tapStarts = new TapStart[4];
        private int tapStartCount = 0;
        private double totalTime;
        private void HandleInput(GameTime gameTime)
        {
            var dt = (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            totalTime += dt;

            var state = TouchPanel.GetState();
            TouchLocation? leftTouch = null;

            if (tapStartCount > state.Count)
                tapStartCount = state.Count;

            foreach (TouchLocation loc in state)
            {
                if (loc.State == TouchLocationState.Released)
                {
                    int tapStartId = -1;
                    for (int i = 0; i < tapStartCount; ++i)
                    {
                        if (tapStarts[i].Id == loc.Id)
                        {
                            tapStartId = i;
                            break;
                        }
                    }
                    if (tapStartId >= 0)
                    {
                        for (int i = tapStartId; i < tapStartCount - 1; ++i)
                            tapStarts[i] = tapStarts[i + 1];
                        tapStartCount--;
                    }
                    continue;
                }
                else if (loc.State == TouchLocationState.Pressed && tapStartCount < tapStarts.Length)
                {
                    tapStarts[tapStartCount] = new TapStart(loc.Id, totalTime, loc.Position);
                    tapStartCount++;
                }

                if (Stick.touchLocation.HasValue && loc.Id == Stick.touchLocation.Value.Id)
                {
                    leftTouch = loc;
                    continue;
                }

                if (!loc.TryGetPreviousLocation(out TouchLocation locPrev))
                    locPrev = loc;

                if (!Stick.touchLocation.HasValue)
                {
                    if (Stick.StartRegion.Contains((int)locPrev.Position.X, (int)locPrev.Position.Y))
                    {
                        if (Stick.Style == TouchStickStyle.Fixed)
                        {
                            if (Vector2.Distance(locPrev.Position, Stick.StartLocation) < 65f)
                            {
                                leftTouch = locPrev;
                            }
                        }
                        else
                        {
                            leftTouch = locPrev;
                            Stick.StartLocation = leftTouch.Value.Position;
                            if (Stick.StartLocation.X < Stick.StartRegion.Left + 25f)
                                Stick.StartLocation.X = Stick.StartRegion.Left + 25f;
                            if (Stick.StartLocation.Y > Stick.StartRegion.Bottom - 25f)
                                Stick.StartLocation.Y = Stick.StartRegion.Bottom - 25f;
                        }
                        continue;
                    }
                }
            }

            Stick.Update(state, leftTouch, dt);
        }

        protected override void Draw(GameTime gameTime)
        {
            Rectangle titleSafeArea = GraphicsDevice.Viewport.TitleSafeArea;
            Vector2 hudLocation = new Vector2(titleSafeArea.X, titleSafeArea.Y);

            GraphicsDevice.Clear(Color.CornflowerBlue);

            //_spriteBatch.Begin(SpriteSortMode.Immediate, null, null, null, null, null, globalTransformation);

            //level.Draw(gameTime, spriteBatch);

            //DrawHud();

            //_spriteBatch.End();

            spriteBatch.Begin();
            Stick.Draw(spriteBatch);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawHud()
        {
            Rectangle titleSafeArea = GraphicsDevice.Viewport.TitleSafeArea;
            Vector2 hudLocation = new Vector2(titleSafeArea.X, titleSafeArea.Y);
            //Vector2 center = new Vector2(titleSafeArea.X + titleSafeArea.Width / 2.0f,
            //                             titleSafeArea.Y + titleSafeArea.Height / 2.0f);

            Vector2 center = new Vector2(baseScreenSize.X / 2, baseScreenSize.Y / 2);

            // Draw time remaining. Uses modulo division to cause blinking when the
            // player is running out of time.
            //string timeString = "TIME: " + level.TimeRemaining.Minutes.ToString("00") + ":" + level.TimeRemaining.Seconds.ToString("00");
            //Color timeColor;
            //if (level.TimeRemaining > WarningTime ||
            //    level.ReachedExit ||
            //    (int)level.TimeRemaining.TotalSeconds % 2 == 0)
            //{
            //    timeColor = Color.Yellow;
            //}
            //else
            //{
            //    timeColor = Color.Red;
            //}
            //DrawShadowedString(hudFont, timeString, hudLocation, timeColor);

            // Draw score
            //float timeHeight = hudFont.MeasureString(timeString).Y;
            //DrawShadowedString(hudFont, "SCORE: " + level.Score.ToString(), hudLocation + new Vector2(0.0f, timeHeight * 1.2f), Color.Yellow);

            // Determine the status overlay message to show.
            //Texture2D status = null;
            //if (level.TimeRemaining == TimeSpan.Zero)
            //{
            //    if (level.ReachedExit)
            //    {
            //        status = winOverlay;
            //    }
            //    else
            //    {
            //        status = loseOverlay;
            //    }
            //}
            //else if (!level.Player.IsAlive)
            //{
            //    status = diedOverlay;
            //}

            //if (status != null)
            //{
            //    // Draw status message.
            //    Vector2 statusSize = new Vector2(status.Width, status.Height);
            //    spriteBatch.Draw(status, center - statusSize / 2, Color.White);
            //}

            if (touchState.IsConnected)
                virtualGamePad.Draw(spriteBatch);
        }
    }
}