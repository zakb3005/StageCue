using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SeniorProjectRefactored.Scenes;
using SeniorProjectRefactored.UI;
using System.IO;

namespace SeniorProjectRefactored.Helpers
{
    public static class UsefulMethods
    {
        private static readonly Rectangle SrcTopLeft = new Rectangle(0, 0, 2, 2);
        private static readonly Rectangle SrcTopRight = new Rectangle(3, 0, 2, 2);
        private static readonly Rectangle SrcBottomLeft = new Rectangle(0, 3, 2, 2);
        private static readonly Rectangle SrcBottomRight = new Rectangle(3, 3, 2, 2);

        private static readonly Rectangle SrcTopEdge = new Rectangle(2, 0, 1, 2);
        private static readonly Rectangle SrcLeftEdge = new Rectangle(0, 2, 2, 1);
        private static readonly Rectangle SrcCenter = new Rectangle(2, 2, 1, 1);
        private static readonly Rectangle SrcRightEdge = new Rectangle(3, 2, 2, 1);
        private static readonly Rectangle SrcBottomEdge = new Rectangle(2, 3, 1, 2);
        
        public static Color DarkenColor(Color baseColor)
        {
            return new Color(
                (int)(baseColor.R * 0.8f),
                (int)(baseColor.G * 0.8f),
                (int)(baseColor.B * 0.8f),
                baseColor.A
            );
        }

        public static char ConvertKeyToChar(Keys key, KeyboardState ks)
        {
            bool shift = ks.IsKeyDown(Keys.LeftShift) || ks.IsKeyDown(Keys.RightShift);

            if (key >= Keys.A && key <= Keys.Z)
            {
                char letter = (char)('a' + (key - Keys.A));
                return shift ? char.ToUpper(letter) : letter;
            }

            if (key >= Keys.D0 && key <= Keys.D9)
            {
                int digit = key - Keys.D0;
                if (!shift)
                {
                    return (char)('0' + digit);
                }
                else
                {
                    switch (digit)
                    {
                        case 0: return ')';
                        case 1: return '!';
                        case 2: return '@';
                        case 3: return '#';
                        case 4: return '$';
                        case 5: return '%';
                        case 6: return '^';
                        case 7: return '&';
                        case 8: return '*';
                        case 9: return '(';
                    }
                }
            }

            if (key == Keys.Space)
            {
                return ' ';
            }

            if (key == Keys.OemTilde)
            {
                return shift ? '~' : '`';
            }

            switch (key)
            {
                case Keys.OemComma: return shift ? '<' : ',';
                case Keys.OemPeriod: return shift ? '>' : '.';
                case Keys.OemMinus: return shift ? '_' : '-';
                case Keys.OemPlus: return shift ? '+' : '=';
                case Keys.OemSemicolon: return shift ? ':' : ';';
                case Keys.OemQuotes: return shift ? '"' : '\'';
                case Keys.OemOpenBrackets: return shift ? '{' : '[';
                case Keys.OemCloseBrackets: return shift ? '}' : ']';
                case Keys.OemBackslash: return shift ? '|' : '\\';
                case Keys.OemQuestion: return shift ? '?' : '/';
            }

            return '\0';
        }

        public static void DrawRounded(SpriteBatch spriteBatch, Rectangle bounds, RoundedCorners Corners, Texture2D roundedCornerTexture, Color backgroundColor)
        {
            int sourceCornerSize = 2;
            int scaleFactor = 3;

            int scaledCorner = sourceCornerSize * scaleFactor;

            int innerWidth = bounds.Width - (scaledCorner * 2);
            int innerHeight = bounds.Height - (scaledCorner * 2);

            if (innerWidth < 0) innerWidth = 0;
            if (innerHeight < 0) innerHeight = 0;

            if (innerWidth > 0 && innerHeight > 0)
            {
                var destCenter = new Rectangle(
                    bounds.X + scaledCorner,
                    bounds.Y + scaledCorner,
                innerWidth,
                    innerHeight
                );

                spriteBatch.Draw(roundedCornerTexture, destCenter, SrcCenter, backgroundColor);
            }

            if (innerWidth > 0)
            {
                var destTopEdge = new Rectangle(
                    bounds.X + scaledCorner,
                    bounds.Y,
                innerWidth,
                    scaledCorner
                );
                spriteBatch.Draw(roundedCornerTexture, destTopEdge, SrcTopEdge, backgroundColor);
            }

            if (innerWidth > 0)
            {
                var destBottomEdge = new Rectangle(
                    bounds.X + scaledCorner,
                    bounds.Bottom - scaledCorner,
                innerWidth,
                    scaledCorner
                );
                spriteBatch.Draw(roundedCornerTexture, destBottomEdge, SrcBottomEdge, backgroundColor);
            }

            if (innerHeight > 0)
            {
                var destLeftEdge = new Rectangle(
                    bounds.X,
                    bounds.Y + scaledCorner,
                scaledCorner,
                    innerHeight
                );
                spriteBatch.Draw(roundedCornerTexture, destLeftEdge, SrcLeftEdge, backgroundColor);
            }

            if (innerHeight > 0)
            {
                var destRightEdge = new Rectangle(
                    bounds.Right - scaledCorner,
                    bounds.Y + scaledCorner,
                scaledCorner,
                    innerHeight
                );
                spriteBatch.Draw(roundedCornerTexture, destRightEdge, SrcRightEdge, backgroundColor);
            }

            if (Corners.HasFlag(RoundedCorners.TopLeft))
            {
                var destTL = new Rectangle(
                    bounds.X,
                    bounds.Y,
                scaledCorner,
                    scaledCorner
                );
                spriteBatch.Draw(roundedCornerTexture, destTL, SrcTopLeft, backgroundColor);
            }
            else
            {
                var destTL = new Rectangle(
                    bounds.X,
                    bounds.Y,
                    scaledCorner,
                    scaledCorner
                );
                spriteBatch.Draw(roundedCornerTexture, destTL, SrcCenter, backgroundColor);
            }

            if (Corners.HasFlag(RoundedCorners.TopRight))
            {
                var destTR = new Rectangle(
                    bounds.Right - scaledCorner,
                    bounds.Y,
                scaledCorner,
                    scaledCorner
                );
                spriteBatch.Draw(roundedCornerTexture, destTR, SrcTopRight, backgroundColor);
            }
            else
            {
                var destTR = new Rectangle(
                    bounds.Right - scaledCorner,
                    bounds.Y,
                scaledCorner,
                    scaledCorner
                );
                spriteBatch.Draw(roundedCornerTexture, destTR, SrcCenter, backgroundColor);
            }

            if (Corners.HasFlag(RoundedCorners.BottomLeft))
            {
                var destBL = new Rectangle(
                    bounds.X,
                    bounds.Bottom - scaledCorner,
                    scaledCorner,
                    scaledCorner
                );
                spriteBatch.Draw(roundedCornerTexture, destBL, SrcBottomLeft, backgroundColor);
            }
            else
            {
                var destBL = new Rectangle(
                    bounds.X,
                    bounds.Bottom - scaledCorner,
                    scaledCorner,
                    scaledCorner
                );
                spriteBatch.Draw(roundedCornerTexture, destBL, SrcCenter, backgroundColor);
            }

            if (Corners.HasFlag(RoundedCorners.BottomRight))
            {
                var destBR = new Rectangle(
                    bounds.Right - scaledCorner,
                    bounds.Bottom - scaledCorner,
                    scaledCorner,
                    scaledCorner
                );
                spriteBatch.Draw(roundedCornerTexture, destBR, SrcBottomRight, backgroundColor);
            }
            else
            {
                var destBR = new Rectangle(
                    bounds.Right - scaledCorner,
                    bounds.Bottom - scaledCorner,
                    scaledCorner,
                    scaledCorner
                );
                spriteBatch.Draw(roundedCornerTexture, destBR, SrcCenter, backgroundColor);
            }
        }
    }
    public class SceneUpdateMsg
    {
        public string type { get; set; } = "SceneUpdate";
        public SceneObj scene { get; set; }
        public SceneUpdateMsg() { }

        public SceneUpdateMsg(SceneObj scn)
        {
            type = "SceneUpdate";
            scene = scn;
        }
    }
    public class TurnUpdateMsg
    {
        public string type { get; set; } = "TurnUpdate";
        public bool IsHostTurn { get; set; }
        public string CurrentPlayerId { get; set; }

        public TurnUpdateMsg() { }

        public TurnUpdateMsg(bool isHostTurn, string currentPlayerId)
        {
            type = "TurnUpdate";
            IsHostTurn = isHostTurn;
            CurrentPlayerId = currentPlayerId;
        }
    }
}
