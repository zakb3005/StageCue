using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SeniorProjectRefactored.Helpers;

namespace SeniorProjectRefactored.UI
{
    public class UITextLabel : UIElement
    {
        public SpriteFont Font { get; set; }
        public string Text { get; set; } = "";
        public Vector2 Size { get; set; } = new Vector2(1, 1);
        public bool BackgroundTransparent { get; set; } = true;
        public Color TextColor { get; set; } = AppColors.Black;
        public Color BackgroundColor { get; set; } = AppColors.White;
        public RoundedCorners Corners { get; set; } = RoundedCorners.All;
        public Texture2D RoundedCornerTexture { get; set; }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
            if (!IsVisible) return;

            if (!BackgroundTransparent)
            {
                if (RoundedCornerTexture != null)
                {
                    UsefulMethods.DrawRounded(spriteBatch, Bounds, Corners, RoundedCornerTexture, BackgroundColor);
                }
                else
                {
                    spriteBatch.Draw(Get1x1Texture(spriteBatch.GraphicsDevice), Bounds, BackgroundColor);
                }
            }

            if (Font != null && !string.IsNullOrEmpty(Text))
            {
                var textSize = Font.MeasureString(Text);
                var textPos = new Vector2(
                    (int)(Bounds.X + (Bounds.Width - textSize.X) / 2f),
                    (int)(Bounds.Y + (Bounds.Height - textSize.Y) / 2f)
                );

                spriteBatch.DrawString(Font, Text, textPos, TextColor);
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