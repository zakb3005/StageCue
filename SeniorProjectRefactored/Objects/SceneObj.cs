using Microsoft.Xna.Framework.Graphics;
using SeniorProjectRefactored.Objects;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SeniorProjectRefactored.Scenes
{
    public class SceneObj
    {
        public string Name { get; set; }
        public string StageRelativePath { get; set; }
        public List<Prop> Props { get; set; } = new List<Prop>();
        public List<Bubble> Bubbles { get; set; } = new List<Bubble>();
        
        [JsonIgnore]
        public Texture2D Stage { get; set; }

        public void addProp(Prop prop)
        {
            Props.Add(prop);
        }

        public void addBubble(Bubble bubble)
        {
            Bubbles.Add(bubble);
        }

        public void removeProp(Prop prop)
        {
            if (Props.Contains(prop))
                Props.Remove(prop);
        }

        public void removeBubble(Bubble bubble)
        {
            if (Bubbles.Contains(bubble))
                Bubbles.Remove(bubble);
        }

        public IReadOnlyList<Prop> GetProps() => Props.AsReadOnly();
        public IReadOnlyList<Bubble> GetBubbles() => Bubbles.AsReadOnly();
    }
}
