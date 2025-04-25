using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SeniorProjectRefactored.Helpers;
using System;

namespace SeniorProjectRefactored.UI
{
    public class SceneSaveDialog: UIElement
    {
        private Texture2D _dimTexture;
        private Texture2D _roundedCornerTexture;
        private SpriteFont _font;
        private string _message;

        private UIButton createBtn;
        private UIButton cancelBtn;
        public UITextBox nameEntry;

        public Action OnCreate { get; set; }

        public Action OnCancel { get; set; }

        public bool IsModal { get; private set; } = true;

        public SceneSaveDialog(GraphicsDevice device, SpriteFont font, Texture2D roundedCornerTexture)
        {
            _font = font;
            _dimTexture = CreateDimTexture(device);
            _roundedCornerTexture = roundedCornerTexture;

            nameEntry = new UITextBox
            {
                PlaceholderText = "Name",
                Font = _font,
                MaxLength = 16,
                Corners = RoundedCorners.All,
                RoundedCornerTexture = _roundedCornerTexture,
                TextColor = AppColors.Black
            };
            createBtn = new UIButton
            {
                Text = "Create",
                Font = _font,
                ClickMode = ClickMode.OnRelease,
                OnClick = () => OnCreate?.Invoke(),
                BackgroundColor = AppColors.PrimaryRed,
                Corners = RoundedCorners.All,
                RoundedCornerTexture = _roundedCornerTexture,
                TextColor = AppColors.White
            };
            cancelBtn = new UIButton
            {
                Text = "Cancel",
                Font = _font,
                ClickMode = ClickMode.OnRelease,
                OnClick = () => OnCancel?.Invoke(),
                BackgroundColor = AppColors.Gray,
                Corners = RoundedCorners.All,
                RoundedCornerTexture = _roundedCornerTexture,
                TextColor = AppColors.White
            };

            AddChild(nameEntry);
            AddChild(createBtn);
            AddChild(cancelBtn);
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

            int spacing = 30;
            int btnWidth = 120;
            int btnHeight = 40;

            int nameEntryWidth = 290;
            int nameEntryHeight = 40;

            nameEntry.Bounds = new Rectangle(
                centerX + spacing,
                centerY + (int)(nameEntryHeight * 0.5f),
                nameEntryWidth,
                nameEntryHeight
            );

            createBtn.Bounds = new Rectangle(
                centerX + boxWidth - btnWidth - spacing,
                centerY + boxHeight - btnHeight - 20,
                btnWidth, btnHeight
            );

            cancelBtn.Bounds = new Rectangle(
                centerX + boxWidth - nameEntryWidth - spacing,
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
