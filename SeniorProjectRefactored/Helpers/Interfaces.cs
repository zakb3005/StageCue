using Microsoft.Xna.Framework.Graphics;
using SeniorProjectRefactored.Objects;

namespace SeniorProjectRefactored.UI
{
    public enum SpecialFolderType { Unknown, Objects, Stages, Scenes }
    public interface IBrowserItem
    {
        string Name { get; }
        string FullPath { get; }
        Texture2D IconTexture { get; }
        bool IsFolder { get; }
        public AssetType Type { get; set; }
    }

    public class FolderItem : IBrowserItem
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public Texture2D IconTexture { get; set; }
        public bool IsFolder => true;
        public AssetType Type { get; set; }
    }

    public class FileItem : IBrowserItem
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public Texture2D IconTexture { get; set; }
        public bool IsFolder => false;
        public AssetType Type { get; set; }
    }

    public enum RoundedCorners
    {
        None = 0,
        TopLeft = 1 << 0,
        TopRight = 1 << 1,
        BottomLeft = 1 << 2,
        BottomRight = 1 << 3,
        All = TopLeft | TopRight | BottomLeft | BottomRight
    }
}