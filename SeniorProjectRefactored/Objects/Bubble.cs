using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Text.Json.Serialization;

public enum BubbleType { Speech, ActionPrompt }

namespace SeniorProjectRefactored.Objects
{
    public class Bubble: Prop
    {
        public string Text { get; set; }
        public Vector2 Size { get; set; }
        public int Padding { get; set; }
        public Vector2 TailPos { get; set; } = new Vector2(-1f, 0.99f);
        public Rectangle TailRect { get; set; }
        public float TailRotation { get; set; }
        public float TailScale { get; set; } = 3;
        public BubbleType Type { get; set; } = BubbleType.Speech;

        [JsonIgnore]
        public SpriteFont Font { get; set; }
        [JsonIgnore]
        public Texture2D TailImg { get; set; }

        public void UpdateSize()
        {
            if (string.IsNullOrEmpty(Text))
            {
                Size = new Vector2(7, Font.LineSpacing);
            }
            else
            {
                string[] lines = Text.Split('\n');
                float maxWidth = 0;
                float totalHeight = 0;

                foreach (var line in lines)
                {
                    Vector2 lineSize = Font.MeasureString(line);
                    maxWidth = Math.Max(maxWidth, lineSize.X);
                    totalHeight += Font.LineSpacing;
                }

                Size = new Vector2(maxWidth, totalHeight);
            }
        }
    }
}
