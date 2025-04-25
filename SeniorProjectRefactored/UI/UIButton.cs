using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SeniorProjectRefactored.Helpers;
using System;

namespace SeniorProjectRefactored.UI
{
    public enum ClickMode
    {
        OnPress,
        OnRelease
    }

    public class UIButton : UIElement
    {
        public SpriteFont Font { get; set; }
        public Texture2D Icon { get; set; }
        public string Text { get; set; }
        public Action OnClick { get; set; }
        public float FontScale { get; set; } = 1f;
        public Vector2 Size { get; set; } = new Vector2(1, 1);
        public bool BackgroundTransparent { get; set; } = false;
        public Color TextColor { get; set; } = AppColors.Black;
        public Color BackgroundColor { get; set; } = AppColors.White;
        public Color IconColor { get; set; } = AppColors.White;
        public int IconPadding { get; set; } = 12;
        public bool IconGrowAnim { get; set; } = true;
        public bool TextGrowAnim { get; set; } = false;

        public ClickMode ClickMode { get; set; } = ClickMode.OnRelease;
        public RoundedCorners Corners { get; set; } = RoundedCorners.All;
        public Texture2D RoundedCornerTexture { get; set; }

        private bool _isHovering;
        private bool _pressedInside;
        private ButtonState _prevLeftState = ButtonState.Released;

        private float iconScale = 1f;
        private float textScale = 1f;

        public UIButton()
        {
            iconScale = IconGrowAnim ? 0.75f : 1f;
            textScale = TextGrowAnim ? 0.75f : 1f;
        }

        public override void HandleInput(MouseState mouseState, KeyboardState keyboardState)
        {
            base.HandleInput(mouseState, keyboardState);
            if (!IsVisible) return;

            _isHovering = Bounds.Contains(mouseState.Position);
            var leftNow = mouseState.LeftButton;

            if (ClickMode == ClickMode.OnPress)
            {
                HandleOnPressLogic(mouseState);
            }
            else
            {
                HandleOnReleaseLogic(mouseState);
            }

            _prevLeftState = leftNow;
        }

        private void HandleOnPressLogic(MouseState mouseState)
        {
            bool currentlyHovering = _isHovering;
            bool wasReleased = _prevLeftState == ButtonState.Released;
            bool isPressed = mouseState.LeftButton == ButtonState.Pressed;

            if (currentlyHovering && wasReleased && isPressed)
            {
                OnClick?.Invoke();
            }
        }

        private void HandleOnReleaseLogic(MouseState mouseState)
        {
            bool currentlyHovering = _isHovering;
            var leftNow = mouseState.LeftButton;

            if (_prevLeftState == ButtonState.Released &&
                leftNow == ButtonState.Pressed &&
                currentlyHovering)
            {
                _pressedInside = true;
            }

            if (_pressedInside && leftNow == ButtonState.Pressed && !currentlyHovering)
            {
                _pressedInside = false;
            }

            if (_prevLeftState == ButtonState.Pressed &&
                leftNow == ButtonState.Released &&
                _pressedInside && currentlyHovering)
            {
                OnClick?.Invoke();
            }

            if (_prevLeftState == ButtonState.Pressed && leftNow == ButtonState.Released)
            {
                _pressedInside = false;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
            if (!IsVisible) return;

            var backgroundHoverColor = UsefulMethods.DarkenColor(BackgroundColor);
            var backgroundColor = _isHovering ? backgroundHoverColor : BackgroundColor;

            var iconHoverColor = UsefulMethods.DarkenColor(IconColor);
            var iconColor = _isHovering ? iconHoverColor : IconColor;

            if (!BackgroundTransparent)
            {
                if (RoundedCornerTexture != null)
                {
                    UsefulMethods.DrawRounded(spriteBatch, Bounds, Corners, RoundedCornerTexture, backgroundColor);
                }
                else
                {
                    spriteBatch.Draw(Get1x1Texture(spriteBatch.GraphicsDevice), Bounds, backgroundColor);
                }
            }

            if (Icon != null)
            {
                if (_isHovering && IconGrowAnim)
                {
                    iconScale = MathHelper.Lerp(iconScale, _pressedInside ? 0.9f : 1.1f, 0.25f);
                }
                else
                {
                    iconScale = MathHelper.Lerp(iconScale, 1.0f, 0.25f);
                    if (iconScale - 1.0f <= 0.02)
                    {
                        iconScale = 1.0f;
                    }
                }

                float iconAspect = (float)Icon.Width / Icon.Height;
                float buttonAspect = (float)Bounds.Width / Bounds.Height;

                int adjustedPadding = (int)(IconPadding / iconScale);

                int iconDrawWidth, iconDrawHeight;

                if (iconAspect > buttonAspect)
                {
                    iconDrawWidth = (int)((Bounds.Width - adjustedPadding) * iconScale);
                    iconDrawHeight = (int)(iconDrawWidth / iconAspect);
                }
                else
                {
                    iconDrawHeight = (int)((Bounds.Height - adjustedPadding) * iconScale);
                    iconDrawWidth = (int)(iconDrawHeight * iconAspect);
                }

                int iconX = (int)(Bounds.X + (Bounds.Width - iconDrawWidth) / 2);
                int iconY = (int)(Bounds.Y + (Bounds.Height - iconDrawHeight) / 2);
                var iconBounds = new Rectangle(iconX, iconY, iconDrawWidth, iconDrawHeight);

                spriteBatch.Draw(Icon, iconBounds, iconColor);
            }

            if (Font != null && !string.IsNullOrEmpty(Text))
            {
                if (_isHovering && TextGrowAnim)
                {
                    textScale = MathHelper.Lerp(textScale, _pressedInside ? 0.9f : 1.1f, 0.25f);
                }
                else
                {
                    textScale = MathHelper.Lerp(textScale, 1.0f, 0.25f);
                    if (textScale - 1.0f <= 0.02)
                    {
                        textScale = 1.0f;
                    } 
                }

                var textSize = Font.MeasureString(Text) * (FontScale * textScale);

                var textPos = new Vector2(
                    (int)(Bounds.X + (Bounds.Width - textSize.X) / 2),
                    (int)(Bounds.Y + (Bounds.Height - textSize.Y) / 2)
                );

                spriteBatch.DrawString(
                    Font,
                    Text,
                    textPos,
                    TextColor,
                    0f,
                    Vector2.Zero,
                    FontScale * textScale,
                    SpriteEffects.None,
                    0f
                );
            }
        }

        private Texture2D _onePx;
        private Texture2D Get1x1Texture(GraphicsDevice device)
        {
            if (_onePx == null)
            {
                _onePx = new Texture2D(device, 1, 1);
                _onePx.SetData(new[] { Color.White });
            }
            return _onePx;
        }
    }
}