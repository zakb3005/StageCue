using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SeniorProjectRefactored.Helpers;
using System;

namespace SeniorProjectRefactored.UI
{
    public class ConfirmationBox : UIElement
    {
        private Texture2D _dimTexture;
        private Texture2D _roundedCornerTexture;
        private SpriteFont _font;
        private string _message;

        private UIButton _yesButton;
        private UIButton _noButton;

        public Action OnYes { get; set; }

        public Action OnNo { get; set; }

        public bool IsModal { get; private set; } = true;

        public ConfirmationBox(GraphicsDevice device, SpriteFont font, string message, Texture2D roundedCornerTexture)
        {
            _font = font;
            _message = message;
            _dimTexture = CreateDimTexture(device);
            _roundedCornerTexture = roundedCornerTexture;

            _yesButton = new UIButton
            {
                Text = "Yes",
                Font = _font,
                ClickMode = ClickMode.OnRelease,
                OnClick = () => OnYes?.Invoke(),
                BackgroundColor = AppColors.PrimaryRed,
                Corners = RoundedCorners.All,
                RoundedCornerTexture = _roundedCornerTexture,
                TextColor = AppColors.White
            };
            _noButton = new UIButton
            {
                Text = "No",
                Font = _font,
                ClickMode = ClickMode.OnRelease,
                OnClick = () => OnNo?.Invoke(),
                BackgroundColor = AppColors.Gray,
                Corners = RoundedCorners.All,
                RoundedCornerTexture = _roundedCornerTexture,
                TextColor = AppColors.White
            };

            AddChild(_yesButton);
            AddChild(_noButton);
        }

        private Texture2D CreateDimTexture(GraphicsDevice device)
        {
            var tex = new Texture2D(device, 1, 1);
            tex.SetData(new[] { Color.Black });
            return tex;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
            if (!IsVisible) return;

            var device = spriteBatch.GraphicsDevice;
            Rectangle fullScreen = new Rectangle(0, 0, device.Viewport.Width, device.Viewport.Height);
            spriteBatch.Draw(_dimTexture, fullScreen, AppColors.Black * 0.5f);

            int boxWidth = 350;
            int boxHeight = 150;
            int centerX = (device.Viewport.Width - boxWidth) / 2;
            int centerY = (device.Viewport.Height - boxHeight) / 2;
            Rectangle boxRect = new Rectangle(centerX, centerY, boxWidth, boxHeight);
            UsefulMethods.DrawRounded(spriteBatch, boxRect, RoundedCorners.All, _roundedCornerTexture, AppColors.DarkGray);

            var msgSize = _font.MeasureString(_message);
            Vector2 msgPos = new Vector2(
                centerX + (boxWidth - msgSize.X) / 2,
                centerY + 20
            );
            spriteBatch.DrawString(_font, _message, msgPos, AppColors.White);

            int btnWidth = 120;
            int btnHeight = 40;

            _yesButton.Bounds = new Rectangle(
                centerX + 30,
                centerY + boxHeight - btnHeight - 20,
                btnWidth, btnHeight
            );

            _noButton.Bounds = new Rectangle(
                centerX + boxWidth - btnWidth - 30,
                centerY + boxHeight - btnHeight - 20,
                btnWidth, btnHeight
            );

            base.Draw(spriteBatch);
        }

        public override void HandleInput(MouseState mouseState, KeyboardState keyboardState)
        {
            base.HandleInput(mouseState, keyboardState);
        }

        private Texture2D _onePx;
        private Texture2D Get1x1(GraphicsDevice device)
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