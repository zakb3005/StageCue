using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace SeniorProjectRefactored.UI
{
    public enum Corner { TopRight, TopLeft, BottomRight, BottomLeft, None }
    public abstract class UIElement
    {
        public Rectangle Bounds { get; set; }
        public bool IsVisible { get; set; } = true;
        public Corner Corner { get; set; } = Corner.None;

        public UIElement Parent { get; private set; }
        private List<UIElement> _children = new List<UIElement>();

        public virtual void AddChild(UIElement child)
        {
            _children.Add(child);
            child.Parent = this;
        }

        public virtual void RemoveChild(UIElement child)
        {
            if (_children.Contains(child))
            {
                _children.Remove(child);
                child.Parent = null;
            }
        }

        public IReadOnlyList<UIElement> GetChildren()
        {
            return _children.AsReadOnly();
        }

        public void RemoveAllChildren()
        {
            var snapshot = new List<UIElement>(_children);
            foreach (var child in snapshot)
            {
                RemoveChild(child);
            }
        }

        public virtual void Update(GameTime gameTime)
        {
            foreach (var child in _children)
            {
                child.Update(gameTime);
            }
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible) return;

            foreach (var child in _children)
            {
                child.Draw(spriteBatch);
            }
        }

        public virtual void HandleInput(MouseState mouseState, KeyboardState keyboardState)
        {
            foreach (var child in _children)
            {
                child.HandleInput(mouseState, keyboardState);
            }
        }
    }
}