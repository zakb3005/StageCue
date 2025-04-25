using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SeniorProject.Scenes;
using SeniorProjectRefactored.Helpers;
using SeniorProjectRefactored.Networking;
using SeniorProjectRefactored.Objects;
using SeniorProjectRefactored.Scenes;
using SeniorProjectRefactored.UI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace SeniorProjectRefactored.Systems
{
    public class SceneHandler
    {
        public SceneObj scene;
        public Rectangle stageRect;

        public float scale = 1;
        public int highlightThickness = 2;
        public int highlightPadding = 0;

        public Prop selectedProp = null;
        public Bubble selectedBubbleTail = null;
        public Rectangle propRect;

        private KeyboardState previousKeyboardState;
        private ButtonState previousM1;
        private bool isDragging = false;
        private Vector2 dragOffset;

        private Dictionary<Keys, float> keyHoldTimes = new Dictionary<Keys, float>();
        private Dictionary<Keys, float> lastRepeatTimes = new Dictionary<Keys, float>();

        private const float keyDelay = 0.5f;
        private const float keyInterval = 0.025f;

        public bool IsMyTurn = false;
        public Role role;
        public Player myPlayer;

        public void SwitchScene(SceneObj sceneObj)
        {
            selectedProp = null;
            scene = sceneObj;
        }

        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState, bool isHoveringUI, bool modalOpen)
        {
            if (!IsMyTurn || modalOpen)
                return;

            Vector2 mousePosition = mouseState.Position.ToVector2();
            bool clicked = mouseState.LeftButton == ButtonState.Pressed && previousM1 == ButtonState.Released && !isHoveringUI;

            if (clicked)
            {
                Prop newSelectedProp = null;
                Bubble newSelectedBubbleTail = null;

                foreach (var prop in scene.GetProps())
                {
                    if (role == Role.Actor && (prop.OwnerId == null || prop.OwnerId != myPlayer?.ID))
                        continue;

                    int propX = (int)(stageRect.X + (prop.RelativePos.X * stageRect.Width));
                    int propY = (int)(stageRect.Y + (prop.RelativePos.Y * stageRect.Height));

                    var propBounds = new Rectangle(
                        propX,
                        propY,
                        (int)(prop.Image.Width * scale),
                        (int)(prop.Image.Height * scale)
                    );

                    if (propBounds.Contains(mouseState.Position))
                    {
                        newSelectedProp = prop;
                    }
                }

                foreach (var prop in scene.GetBubbles())
                {
                    if (prop is Bubble bubble)
                    {
                        if (role == Role.Actor && (bubble.OwnerId == null || bubble.OwnerId != myPlayer?.ID))
                            continue;

                        int propX = (int)(stageRect.X + (bubble.RelativePos.X * stageRect.Width));
                        int propY = (int)(stageRect.Y + (bubble.RelativePos.Y * stageRect.Height));

                        var bubbleBounds = new Rectangle(
                            propX - bubble.Padding,
                            propY - bubble.Padding,
                            (int)(bubble.Size.X + (bubble.Padding * 2)),
                            (int)(bubble.Size.Y + (bubble.Padding * 2))
                        );

                        if (bubble.TailRect.Contains(mouseState.Position))
                        {
                            newSelectedBubbleTail = bubble;
                        }
                        else if (bubbleBounds.Contains(mouseState.Position))
                        {
                            newSelectedProp = bubble;
                        }
                    }
                }

                if (newSelectedBubbleTail != null)
                {
                    selectedBubbleTail = newSelectedBubbleTail;
                    isDragging = true;
                } 
                else if (newSelectedProp != null)
                {
                    if (selectedProp != newSelectedProp || !isDragging)
                    {
                        selectedProp = newSelectedProp;
                        isDragging = true;

                        float propX = stageRect.X + (selectedProp.RelativePos.X * stageRect.Width);
                        float propY = stageRect.Y + (selectedProp.RelativePos.Y * stageRect.Height);

                        dragOffset = mousePosition - new Vector2(propX, propY);

                        if (selectedProp.GetType() == typeof(Bubble))
                        {
                            scene.Bubbles.Remove((Bubble)selectedProp);
                            scene.Bubbles.Add((Bubble)selectedProp);
                        } else
                        {
                            scene.Props.Remove(selectedProp);
                            scene.Props.Add(selectedProp);
                        }
                    }
                }
                else
                {
                    selectedProp = null;
                    selectedBubbleTail = null;
                    isDragging = false;
                }
            }

            if (selectedBubbleTail != null && isDragging)
            {
                if (mouseState.LeftButton == ButtonState.Released)
                {
                    isDragging = false;
                    selectedBubbleTail = null;
                } else
                {
                    int propX = (int)(stageRect.X + (selectedBubbleTail.RelativePos.X * stageRect.Width));
                    int propY = (int)(stageRect.Y + (selectedBubbleTail.RelativePos.Y * stageRect.Height));

                    var bubbleBounds = new Rectangle(
                        propX - selectedBubbleTail.Padding,
                        propY - selectedBubbleTail.Padding,
                        (int)(selectedBubbleTail.Size.X + (selectedBubbleTail.Padding * 2)),
                        (int)(selectedBubbleTail.Size.Y + (selectedBubbleTail.Padding * 2))
                    );

                    float centerX = bubbleBounds.X + bubbleBounds.Width / 2f;
                    float centerY = bubbleBounds.Y + bubbleBounds.Height / 2f;
                    Vector2 bubbleCenter = new Vector2(centerX, centerY);

                    Vector2 mousePos = mouseState.Position.ToVector2();

                    Vector2 offset = mousePos - bubbleCenter;

                    float halfW = bubbleBounds.Width / 2f;
                    float halfH = bubbleBounds.Height / 2f;

                    float tailX = offset.X / halfW;
                    float tailY = -offset.Y / halfH;

                    tailX = Math.Clamp(tailX, -1f, 1f);
                    tailY = Math.Clamp(tailY, -1f, 1f);

                    selectedBubbleTail.TailPos = new System.Numerics.Vector2(tailX, tailY);
                }
            }
            else if (selectedProp != null)
            {
                if (selectedProp is Bubble bubble)
                {
                    HandleBubbleTyping(bubble, keyboardState, gameTime);
                }

                if (isDragging)
                {
                    if (mouseState.LeftButton == ButtonState.Released)
                    {
                        isDragging = false;
                    }
                    else if (stageRect.Contains(mouseState.Position))
                    {
                        Vector2 newPropScreenPos = mousePosition - dragOffset;

                        float relativeX = (newPropScreenPos.X - stageRect.X) / stageRect.Width;
                        float relativeY = (newPropScreenPos.Y - stageRect.Y) / stageRect.Height;

                        selectedProp.RelativePos = new System.Numerics.Vector2(relativeX, relativeY);
                    }
                }
            }

            previousM1 = mouseState.LeftButton;
            previousKeyboardState = keyboardState;
        }

        private void HandleBubbleTyping(Bubble bubble, KeyboardState keyboardState, GameTime gameTime)
        {
            double dt = gameTime.ElapsedGameTime.TotalSeconds;
            var pressedKeys = keyboardState.GetPressedKeys();

            foreach (var key in pressedKeys)
            {
                if (!keyHoldTimes.ContainsKey(key))
                {
                    keyHoldTimes[key] = 0f;
                    lastRepeatTimes[key] = 0f;

                    ApplyKeyPress(bubble, key, keyboardState);
                }
                else
                {
                    keyHoldTimes[key] += (float)dt;

                    float holdTime = keyHoldTimes[key];
                    if (holdTime >= keyDelay)
                    {
                        float lastRepeat = lastRepeatTimes[key];
                        if (holdTime - lastRepeat >= keyInterval)
                        {
                            ApplyKeyPress(bubble, key, keyboardState);
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

        private void ApplyKeyPress(Bubble bubble, Keys key, KeyboardState keyboardState)
        {
            if (key == Keys.Enter)
            {
                bubble.Text += "\n";
            }
            else if (key == Keys.Back)
            {
                if (bubble.Text.Length > 0)
                {
                    bubble.Text = bubble.Text[..^1];
                }
            }
            else
            {
                char c = UsefulMethods.ConvertKeyToChar(key, keyboardState);
                if (c != '\0')
                {
                    bubble.Text += c;
                }
            }
            bubble.UpdateSize();
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (scene == null || scene.Stage == null || scene.Props == null)
                return;

            var stage = scene.Stage;

            scale = Math.Min((float)this.stageRect.Width / stage.Width, (float)this.stageRect.Height / stage.Height);

            int stageWidth = (int)(stage.Width * scale);
            int stageHeight = (int)(stage.Height * scale);

            int posX = this.stageRect.X + (this.stageRect.Width - stageWidth) / 2;
            int posY = this.stageRect.Y + (this.stageRect.Height - stageHeight) / 2;

            Rectangle stageRect = new Rectangle(posX, posY, stageWidth, stageHeight);

            spriteBatch.Draw(stage, stageRect, AppColors.White);

            foreach (var prop in scene.Props)
            {
                int propX = (int)(stageRect.X + (prop.RelativePos.X * stageRect.Width));
                int propY = (int)(stageRect.Y + (prop.RelativePos.Y * stageRect.Height));

                propRect = new Rectangle(
                    propX,
                    propY,
                    (int)(prop.Image.Width * scale),
                    (int)(prop.Image.Height * scale)
                );

                SpriteEffects spriteEffect = SpriteEffects.None;
                if (prop.MirrorX) spriteEffect |= SpriteEffects.FlipHorizontally;
                if (prop.MirrorY) spriteEffect |= SpriteEffects.FlipVertically;

                float opacity = 1.0f;
                if ( IsMyTurn && role == Role.Actor && (prop.OwnerId == null || prop.OwnerId != myPlayer?.ID))
                    opacity = 0.7f;

                spriteBatch.Draw(prop.Image, propRect, null, AppColors.White * opacity, 0f, Vector2.Zero, spriteEffect, 0f);

                if (selectedProp != null && selectedProp == prop)
                {
                    Texture2D boxTex = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                    boxTex.SetData(new[] { AppColors.White });

                    Rectangle topRect = new Rectangle(propRect.Left, propRect.Top - highlightThickness, propRect.Width, highlightThickness);
                    spriteBatch.Draw(boxTex, topRect, AppColors.LightBlue);

                    Rectangle bottomRect = new Rectangle(propRect.Left, propRect.Bottom, propRect.Width, highlightThickness);
                    spriteBatch.Draw(boxTex, bottomRect, AppColors.LightBlue);

                    Rectangle leftRect = new Rectangle(propRect.Left - highlightThickness, propRect.Top, highlightThickness, propRect.Height);
                    spriteBatch.Draw(boxTex, leftRect, AppColors.LightBlue);

                    Rectangle rightRect = new Rectangle(propRect.Right, propRect.Top, highlightThickness, propRect.Height);
                    spriteBatch.Draw(boxTex, rightRect, AppColors.LightBlue);
                }
            }

            foreach (var prop in scene.Bubbles)
            {
                int propX = (int)(stageRect.X + (prop.RelativePos.X * stageRect.Width));
                int propY = (int)(stageRect.Y + (prop.RelativePos.Y * stageRect.Height));

                propRect = new Rectangle(
                    propX,
                    propY,
                    (int)(prop.Image.Width * scale),
                    (int)(prop.Image.Height * scale)
                );

                if (prop is Bubble bubble)
                {
                    int bubbleWidth = (int)(bubble.Size.X);
                    int bubbleHeight = (int)(bubble.Size.Y);

                    propRect = new Rectangle(propX - bubble.Padding, propY - bubble.Padding, bubbleWidth + (bubble.Padding * 2), bubbleHeight + (bubble.Padding * 2));

                    float opacity = 1.0f;
                    if (IsMyTurn && role == Role.Actor && (bubble.OwnerId == null || bubble.OwnerId != myPlayer?.ID))
                        opacity = 0.7f;

                    UsefulMethods.DrawRounded(spriteBatch, propRect, RoundedCorners.All, bubble.Image, AppColors.White * opacity);

                    float absX = Math.Abs(bubble.TailPos.X);
                    float absY = Math.Abs(bubble.TailPos.Y);

                    int cornerMargin = 18;

                    Vector2 anchor;
                    float rotation;

                    if (absX >= absY)
                    {
                        bool isRight = (bubble.TailPos.X > 0);

                        float verticalOffset = bubble.TailPos.Y * (propRect.Height / 2f);
                        float maxOffset = (propRect.Height / 2f) - cornerMargin;
                        verticalOffset = maxOffset < 0 ? 0f : Math.Clamp(verticalOffset, -maxOffset, maxOffset);

                        float tailWidth = bubble.TailImg.Width * bubble.TailScale;

                        float anchorX = isRight ? (int)(propRect.Right + tailWidth / 2f) - 2 : (int)(propRect.Left - tailWidth / 2f) + 4;

                        float anchorY = (propRect.Y + propRect.Height / 2f) - verticalOffset;

                        anchor = new Vector2(anchorX, anchorY);
                        rotation = isRight ? MathF.PI : 0f;
                    }
                    else
                    {
                        bool isBottom = (bubble.TailPos.Y < 0);

                        float horizontalOffset = bubble.TailPos.X * (propRect.Width / 2f);
                        float maxOffset = (propRect.Width / 2f) - cornerMargin;
                        horizontalOffset = maxOffset < 0 ? 0f : Math.Clamp(horizontalOffset, -maxOffset, maxOffset);

                        float tailHeight = bubble.TailImg.Height * bubble.TailScale;

                        float anchorX = (propRect.X + propRect.Width / 2f) + horizontalOffset;
                        float anchorY = isBottom? (int)(propRect.Bottom + tailHeight / 2f) - 2 : (int)(propRect.Top - tailHeight / 2f) + 4;

                        rotation = isBottom ? (-MathF.PI / 2f) : (MathF.PI / 2f);
                        anchor = new Vector2(anchorX, anchorY);
                    }

                    bubble.TailRotation = rotation;

                    float w = bubble.TailImg.Width * bubble.TailScale;
                    float h = bubble.TailImg.Height * bubble.TailScale;

                    Vector2 tailTopLeft = anchor - new Vector2(w / 2f, h / 2f);

                    bubble.TailRect = new Rectangle(
                        (int)tailTopLeft.X,
                        (int)tailTopLeft.Y,
                        (int)w,
                        (int)h
                    );

                    Vector2 tailCenterPos = bubble.TailRect.Location.ToVector2() + new Vector2(bubble.TailRect.Width / 2f, bubble.TailRect.Height / 2f);

                    Vector2 tailOrigin = new Vector2(
                        bubble.TailImg.Width / 2f,
                        bubble.TailImg.Height / 2f
                    );

                    spriteBatch.Draw(
                        bubble.TailImg,
                        tailCenterPos,
                        null,
                        AppColors.White * opacity,
                        bubble.TailRotation,
                        tailOrigin,
                        bubble.TailScale,
                        SpriteEffects.None,
                        0f
                    );

                    Vector2 textPosition = new Vector2(
                        propX,
                        propY
                    );

                    spriteBatch.DrawString(bubble.Font, bubble.Text, textPosition, AppColors.Black);

                    if (selectedProp != null && selectedProp == prop)
                    {
                        Texture2D boxTex = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                        boxTex.SetData(new[] { AppColors.White });

                        Rectangle topRect = new Rectangle(propRect.Left, propRect.Top - highlightThickness, propRect.Width, highlightThickness);
                        spriteBatch.Draw(boxTex, topRect, AppColors.LightBlue);

                        Rectangle bottomRect = new Rectangle(propRect.Left, propRect.Bottom, propRect.Width, highlightThickness);
                        spriteBatch.Draw(boxTex, bottomRect, AppColors.LightBlue);

                        Rectangle leftRect = new Rectangle(propRect.Left - highlightThickness, propRect.Top, highlightThickness, propRect.Height);
                        spriteBatch.Draw(boxTex, leftRect, AppColors.LightBlue);

                        Rectangle rightRect = new Rectangle(propRect.Right, propRect.Top, highlightThickness, propRect.Height);
                        spriteBatch.Draw(boxTex, rightRect, AppColors.LightBlue);
                    }
                }
            }
        }

        public void UpdateStageRect(SpriteBatch spriteBatch)
        {
            var graphics = spriteBatch.GraphicsDevice;
            int screenWidth = graphics.Viewport.Width;
            int screenHeight = graphics.Viewport.Height;

            int sideGoal = (int)(screenHeight * 0.9f);
            if (sideGoal > screenWidth) sideGoal = screenWidth;

            int stageSize = sideGoal;

            int gridX = (screenWidth - stageSize) / 2;
            int gridY = (screenHeight - stageSize) / 2;

            stageRect = new Rectangle(gridX, gridY, stageSize, stageSize);
        }
    }
}