using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SeniorProjectRefactored.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SeniorProjectRefactored.UI
{
    public class UITextBox : UIElement
    {
        public SpriteFont Font { get; set; }
        public Texture2D RoundedCornerTexture { get; set; }
        public RoundedCorners Corners { get; set; } = RoundedCorners.All;
        public Vector2 Size { get; set; } = new Vector2(1, 1);

        public Color TextColor { get; set; } = AppColors.Black;
        public Color BackgroundColor { get; set; } = AppColors.White;
        public bool BackgroundTransparent { get; set; } = false;

        public int MaxLength { get; set; } = 32;
        public string Text { get; set; } = "";
        public string PlaceholderText { get; set; } = "";

        private Dictionary<Keys, float> keyHoldTimes = new Dictionary<Keys, float>();
        private Dictionary<Keys, float> lastRepeatTimes = new Dictionary<Keys, float>();

        private float keyDelay = 0.5f;
        private float keyInterval = 0.05f;

        private bool _isFocused = false;

        public UITextBox()
        {
            Size = new Vector2(200, 40);
        }

        public override void HandleInput(MouseState mouseState, KeyboardState keyboardState)
        {
            base.HandleInput(mouseState, keyboardState);
            if (!IsVisible) return;

            bool wasClicked = Bounds.Contains(mouseState.Position) && mouseState.LeftButton == ButtonState.Pressed && _previousLeftButton == ButtonState.Released;

            if (wasClicked)
            {
                _isFocused = true;
            }
            else
            {
                bool clickedOutside = mouseState.LeftButton == ButtonState.Pressed && _previousLeftButton == ButtonState.Released && !Bounds.Contains(mouseState.Position);
                if (clickedOutside)
                {
                    _isFocused = false;
                }
            }

            if (!_isFocused)
            {
                keyHoldTimes.Clear();
                lastRepeatTimes.Clear();
                _previousLeftButton = mouseState.LeftButton;
                return;
            }

            HandleTyping(keyboardState);

            _previousLeftButton = mouseState.LeftButton;
        }

        private void HandleTyping(KeyboardState keyboardState)
        {
            float dt = (float)_deltaTime;

            var pressedKeys = keyboardState.GetPressedKeys();

            foreach (var key in pressedKeys)
            {
                if (!keyHoldTimes.ContainsKey(key))
                {
                    keyHoldTimes[key] = 0f;
                    lastRepeatTimes[key] = 0f;

                    ApplyKeyPress(key, keyboardState);
                }
                else
                {
                    keyHoldTimes[key] += dt;

                    float holdTime = keyHoldTimes[key];
                    if (holdTime >= keyDelay)
                    {
                        float lastRepeat = lastRepeatTimes[key];
                        if (holdTime - lastRepeat >= keyInterval)
                        {
                            ApplyKeyPress(key, keyboardState);
                            lastRepeatTimes[key] = holdTime;
                        }
                    }
                }
            }

            var keysToRemove = new List<Keys>();
            foreach (var kvp in keyHoldTimes)
            {
                var key = kvp.Key;
                if (!pressedKeys.Contains(key))
                {
                    keysToRemove.Add(key);
                }
            }
            foreach (var key in keysToRemove)
            {
                keyHoldTimes.Remove(key);
                lastRepeatTimes.Remove(key);
            }
        }

        private void ApplyKeyPress(Keys key, KeyboardState keyboardState)
        {
            if (key == Keys.Back)
            {
                if (Text.Length > 0)
                {
                    Text = Text[..^1];
                }
            }
            else
            {
                char c = UsefulMethods.ConvertKeyToChar(key, keyboardState);
                if (c != '\0')
                {
                    if (c == '\n' || c == '\r') return;

                    if (Text.Length < MaxLength)
                    {
                        Text += c;
                    }
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
            if (!IsVisible) return;

            Color bgColor = _isFocused
                ? UsefulMethods.DarkenColor(BackgroundColor)
                : BackgroundColor;

            if (!BackgroundTransparent)
            {
                if (RoundedCornerTexture != null)
                {
                    UsefulMethods.DrawRounded(spriteBatch, Bounds, Corners, RoundedCornerTexture, bgColor);
                }
                else
                {
                    spriteBatch.Draw(Get1x1Texture(spriteBatch.GraphicsDevice), Bounds, bgColor);
                }
            }

            if (Font != null && !string.IsNullOrEmpty(Text))
            {
                var textSize = Font.MeasureString(Text);
                var textPos = new Vector2(
                    Bounds.X + 10,
                    Bounds.Y + (Bounds.Height - textSize.Y) / 2f
                );

                spriteBatch.DrawString(Font, Text, textPos, TextColor);
            }
            else if (Font != null && string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(PlaceholderText))
            {
                var textSize = Font.MeasureString(PlaceholderText);
                var textPos = new Vector2(
                    Bounds.X + 10,
                    Bounds.Y + (Bounds.Height - textSize.Y) / 2f
                );

                spriteBatch.DrawString(Font, PlaceholderText, textPos, AppColors.Gray);
            }
        }

        double _deltaTime;
        public override void Update(GameTime gameTime)
        {
            _deltaTime = gameTime.ElapsedGameTime.TotalSeconds;
        }

        private ButtonState _previousLeftButton;

        private Texture2D _onePx;
        private Texture2D Get1x1Texture(GraphicsDevice device)
        {
            if (_onePx == null)
            {
                _onePx = new Texture2D(device, 1, 1);
                _onePx.SetData(new[] { AppColors.White });
            }
            return _onePx;
        }
    }
}