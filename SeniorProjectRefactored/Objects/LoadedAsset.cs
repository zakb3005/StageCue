using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeniorProjectRefactored.Objects
{
    public enum AssetType
    {
        Unknown,
        Object,
        Scene,
        Stage
    }
    public class LoadedAsset
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string RelativeFilePath { get; set; }
        public bool IsJson { get; set; }
        public Texture2D Texture { get; set; }
        public AssetType Type { get; set; }
    }
}
