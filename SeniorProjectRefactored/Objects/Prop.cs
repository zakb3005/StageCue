using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SeniorProjectRefactored.Objects
{
    public class Prop
    {
        public string AssetRelativePath { get; set; }
        public bool MirrorX { get; set; } = false;
        public bool MirrorY { get; set; } = false;
        public Vector2 RelativePos { get; set; }
        public string OwnerId { get; set; } = null;

        [JsonIgnore]
        public Texture2D Image { get; set; }
    }
}
