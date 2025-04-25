using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;

namespace SeniorProjectRefactored.UI
{
    public class UIManager
    {
        private List<UIElement> elements = new List<UIElement>();
        private List<UIElement> toAdd = new List<UIElement>();
        private List<UIElement> toRemove = new List<UIElement>();

        public void AddElement(UIElement element)
        {
            if (!toAdd.Contains(element))
                toAdd.Add(element);
        }

        public void RemoveElement(UIElement element)
        {
            if (!toRemove.Contains(element))
                toRemove.Add(element);
        }

        public IReadOnlyList<UIElement> GetElements() => elements.AsReadOnly();

        public void PositionObjectBrowser(ObjectBrowserPanel browser, Rectangle stageRect)
        {
            int panelWidth = 239;
            int panelHeight = (int)(stageRect.Height);

            int x = stageRect.Right + 15;
            int y = stageRect.Top;

            browser.RepositionSelf(new Point(x, y), panelWidth, panelHeight);
        }

        public void PositionCorners(Rectangle stageRect)
        {
            int buttonSpacingTR = 54;
            int btnIndexTR = 0;
            foreach (var elem in elements)
            {
                if (elem is UIButton btn && btn.Corner == Corner.TopRight && elem.IsVisible)
                {
                    btn.Bounds = new Rectangle(
                        stageRect.Right + 15,
                        stageRect.Top + (btnIndexTR * buttonSpacingTR),
                        (int)btn.Size.X, 
                        (int)btn.Size.Y
                    );
                    btnIndexTR++;
                }
            }

            int btnIndexBL = 0;
            foreach (var elem in elements)
            {
                if (elem is UIButton btn && btn.Corner == Corner.BottomLeft && elem.IsVisible)
                {
                    int spacing = (int)btn.Size.Y + 14;
                    btn.Bounds = new Rectangle(
                        stageRect.Left - (int)btn.Size.X - 15,
                        stageRect.Bottom - (btnIndexBL * spacing) - (int)btn.Size.Y,
                        (int)btn.Size.X,
                        (int)btn.Size.Y
                    );
                    btnIndexBL++;
                } else if (elem is UITextLabel lbl && lbl.Corner == Corner.BottomLeft && elem.IsVisible)
                {
                    var textSize = lbl.Font.MeasureString(lbl.Text);

                    elem.Bounds = new Rectangle(
                        (int)(stageRect.Left + (int)(textSize.X * 0.5f)),
                        (int)(stageRect.Bottom + 16),
                        (int)lbl.Size.X,
                        (int)lbl.Size.Y
                    );
                }
            }

            int btnIndexTL = 0;
            foreach (var elem in elements)
            {
                if (elem is UIButton btn && btn.Corner == Corner.TopLeft && elem.IsVisible)
                {
                    int spacing = (int)btn.Size.Y + 14;
                    btn.Bounds = new Rectangle(
                        stageRect.Left - (int)btn.Size.X - 15,
                        stageRect.Top + (btnIndexTL * spacing),
                        (int)btn.Size.X,
                        (int)btn.Size.Y
                    );
                    btnIndexTL++;
                }
            }
        }

        public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyboardState)
        {
            if (toAdd.Count > 0)
            {
                foreach (var elem in toAdd)
                {
                    if (!elements.Contains(elem))
                        elements.Add(elem);
                }
                toAdd.Clear();
            }

            var snapshot = new List<UIElement>(elements);

            UIElement modalElement = null;
            foreach (var elem in snapshot)
            {
                if (elem.IsVisible && elem is ConfirmationBox cb && cb.IsVisible)
                {
                    modalElement = cb;
                }
            }

            if (modalElement != null)
            {
                if (elements.Contains(modalElement))
                {
                    modalElement.HandleInput(mouseState, keyboardState);
                    modalElement.Update(gameTime);
                }
            }
            else
            {
                foreach (var elem in snapshot)
                {
                    if (elements.Contains(elem))
                    {
                        elem.HandleInput(mouseState, keyboardState);
                        elem.Update(gameTime);
                    }
                }
            }

            if (toRemove.Count > 0)
            {
                foreach (var elem in toRemove)
                {
                    elements.Remove(elem);
                }
                toRemove.Clear();
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            var snapshot = new List<UIElement>(elements);
            foreach (var elem in snapshot)
            {
                if (elements.Contains(elem))
                    elem.Draw(spriteBatch);
            }
        }

        public bool IsAnyModalOpen()
        {
            foreach (var elem in elements)
            {
                if (elem.IsVisible && elem is ConfirmationBox cb)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsHoveringAnyElement(MouseState mouseState)
        {
            foreach (var elem in elements)
            {
                if (elem.IsVisible && elem.Bounds.Contains(mouseState.Position))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
