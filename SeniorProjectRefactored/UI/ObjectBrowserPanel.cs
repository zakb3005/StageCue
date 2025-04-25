using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SeniorProjectRefactored.Helpers;
using SeniorProjectRefactored.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SeniorProjectRefactored.UI
{
    public class BreadcrumbSegment
    {
        public string Name;
        public string FullPath;
        public Rectangle Bounds;
    }

    public class ObjectBrowserPanel : UIElement
    {
        public bool IsOpen { get; set; }

        public Texture2D folderIcon;
        public Texture2D backArrowTexture;
        public Texture2D cornerTexture;
        public Rectangle backArrowBounds;
        
        public string rootDirectory;
        public string currentDirectory;
        public SpriteFont font;
        public AssetHandler assetHandler;
        public Action<IBrowserItem> onItemSelected;

        public Texture2D plusIcon;
        private Rectangle plusBounds = Rectangle.Empty;
        public Action<string> onPlusClicked;

        private List<BreadcrumbSegment> _breadcrumbSegments = new List<BreadcrumbSegment>();
        private List<IBrowserItem> _items = new List<IBrowserItem>();

        private float _scrollOffsetY;
        private float _scrollOffsetTargetY;
        private float _contentHeight;

        private int _previousScrollWheel;
        private bool _isDraggingScrollbar;
        private int _dragStartMouseY;
        private float _dragStartScrollOffsetY;
        private bool _mouseInsidePanel;

        private const int TopBarHeight = 40;
        private const int ColumnCount = 2;
        private const int ScrollBarWidth = 11;
        private const int ScrollSpeedWheel = 50;
        private const int IconSize = 100;
        private const int Padding = 9;
        private const float SmoothFactor = 0.25f;
        private bool showPlus = false;

        private List<UIButton> _pooledButtons = new List<UIButton>();

        private List<UIElement> _childrenToAdd = new List<UIElement>();
        private List<UIElement> _childrenToRemove = new List<UIElement>();
        private Texture2D _onePixel;
        private ButtonState _previousLeftButton = ButtonState.Released;

        private int _prevFirstVisibleRow = -1;
        private int _prevLastVisibleRow = -1;

        private readonly string[] _recognizedRootFolders = { "Objects", "Scenes", "Stages" };

        public void NavigateTo(string folderPath)
        {
            _items.Clear();
            foreach (var child in GetChildren())
            {
                ScheduleRemoveChild(child);
            }
            _pooledButtons.Clear();

            currentDirectory = folderPath;
            BuildBreadcrumbs();

            if (Directory.Exists(folderPath))
            {
                bool isRoot =
                    Path.GetFullPath(folderPath).Equals(Path.GetFullPath(rootDirectory), StringComparison.OrdinalIgnoreCase);

                foreach (var directory in Directory.GetDirectories(folderPath))
                {
                    if (isRoot)
                    {
                        string dirName = Path.GetFileName(directory);
                        if (!_recognizedRootFolders.Contains(dirName, StringComparer.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                    }

                    string name = Path.GetFileName(directory);
                    _items.Add(new FolderItem
                    {
                        Name = name,
                        FullPath = directory,
                        IconTexture = folderIcon
                    });
                }
            }

            string relative = GetRelativeFromRoot(folderPath);
            if (assetHandler.AssetsByFolder.TryGetValue(relative, out var assets))
            {
                var grouped = assets.GroupBy(a => (a.Name, a.Type)).ToDictionary(g => g.Key, g => g.ToList());

                foreach (var group in grouped)
                {
                    var key = group.Key;
                    var list = group.Value;

                    var jsonAsset = list.FirstOrDefault(a => a.IsJson);
                    var nonJsonAsset = list.FirstOrDefault(a => !a.IsJson);

                    if (jsonAsset != null)
                    {
                        _items.Add(new FileItem
                        {
                            Name = jsonAsset.Name,
                            FullPath = GetRelativeFromRoot(jsonAsset.Path),
                            IconTexture = nonJsonAsset?.Texture,
                            Type = jsonAsset.Type
                        });
                    }
                    else if (nonJsonAsset != null)
                    {
                        _items.Add(new FileItem
                        {
                            Name = nonJsonAsset.Name,
                            FullPath = GetRelativeFromRoot(nonJsonAsset.Path),
                            IconTexture = nonJsonAsset.Texture,
                            Type = nonJsonAsset.Type
                        });
                    }
                }
            }

            ComputeContentHeight();
            ClampTargetScroll();

            _prevFirstVisibleRow = -1;
            _prevLastVisibleRow = -1;
        }

        private void BuildBreadcrumbs()
        {
            _breadcrumbSegments.Clear();
            string rootName = Path.GetFileName(Path.GetFullPath(rootDirectory));
            string relative = GetRelativeFromRoot(currentDirectory);

            if (string.IsNullOrEmpty(relative))
            {
                _breadcrumbSegments.Add(new BreadcrumbSegment
                {
                    Name = rootName,
                    FullPath = rootDirectory
                });
                return;
            }

            var parts = relative.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            _breadcrumbSegments.Add(new BreadcrumbSegment
            {
                Name = rootName,
                FullPath = rootDirectory
            });

            string cumulative = rootDirectory;
            foreach (var p in parts)
            {
                cumulative = Path.Combine(cumulative, p);
                _breadcrumbSegments.Add(new BreadcrumbSegment
                {
                    Name = p,
                    FullPath = cumulative
                });
            }
        }

        public void RepositionSelf(Point position, int width, int height)
        {
            Bounds = new Rectangle(position.X, position.Y, width, height);
            ComputeContentHeight();
            ClampTargetScroll();

            _prevFirstVisibleRow = -1;
            _prevLastVisibleRow = -1;
        }

        public override void Update(GameTime gameTime)
        {
            ApplyChildChanges();

            _scrollOffsetY = MathHelper.Lerp(_scrollOffsetY, _scrollOffsetTargetY, SmoothFactor);
            ClampScroll(ref _scrollOffsetY);

            showPlus = currentDirectory.StartsWith(Path.Combine(rootDirectory, "Scenes"), StringComparison.OrdinalIgnoreCase);

            RebuildItems();

            base.Update(gameTime);
        }

        public override void HandleInput(MouseState mouse, KeyboardState keyboard)
        {
            if (!IsOpen)
            {
                _previousScrollWheel = mouse.ScrollWheelValue;
                _previousLeftButton = mouse.LeftButton;
                return;
            }

            if (!IsAtRoot() && backArrowBounds.Contains(mouse.Position))
            {
                if (mouse.LeftButton == ButtonState.Pressed && _previousLeftButton == ButtonState.Released)
                {
                    string parent = Path.GetDirectoryName(currentDirectory) ?? currentDirectory;
                    NavigateTo(parent);
                }
            }

            if (!IsAtRoot() && showPlus && plusBounds.Contains(mouse.Position) && mouse.LeftButton == ButtonState.Pressed && _previousLeftButton == ButtonState.Released)
            {
                onPlusClicked?.Invoke(currentDirectory);
            }

            BreadcrumbSegment crumbClicked = null;
            foreach (var crumb in _breadcrumbSegments)
            {
                if (crumb.Bounds.Contains(mouse.Position))
                {
                    if (mouse.LeftButton == ButtonState.Pressed && _previousLeftButton == ButtonState.Released)
                    {
                        crumbClicked = crumb;
                        break;
                    }
                }
            }
            if (crumbClicked != null)
            {
                NavigateTo(crumbClicked.FullPath);
            }

            _mouseInsidePanel = Bounds.Contains(mouse.Position);
            if (_mouseInsidePanel && HasOverflow)
            {
                int scrollDelta = mouse.ScrollWheelValue - _previousScrollWheel;
                if (scrollDelta != 0)
                {
                    _scrollOffsetTargetY -= (scrollDelta / 120f) * ScrollSpeedWheel;
                    ClampTargetScroll();
                }
                HandleScrollBarDragging(mouse);
            }

            _previousLeftButton = mouse.LeftButton;
            _previousScrollWheel = mouse.ScrollWheelValue;

            base.HandleInput(mouse, keyboard);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsOpen)
                return;

            GraphicsDevice device = spriteBatch.GraphicsDevice;
            var oldScissor = device.ScissorRectangle;
            var oldRaster = device.RasterizerState;
            var raster = new RasterizerState { ScissorTestEnable = true };

            UsefulMethods.DrawRounded(spriteBatch, Bounds, RoundedCorners.All, cornerTexture, AppColors.DarkGray);

            var topBarRect = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, TopBarHeight);
            UsefulMethods.DrawRounded(spriteBatch, topBarRect, RoundedCorners.TopLeft | RoundedCorners.TopRight, cornerTexture, AppColors.Gray);

            backArrowBounds = Rectangle.Empty;
            if (!IsAtRoot() && backArrowTexture != null)
            {
                int arrowSize = TopBarHeight - 8;
                backArrowBounds = new Rectangle(
                    topBarRect.X + 4,
                    topBarRect.Y + (TopBarHeight - arrowSize) / 2,
                    arrowSize,
                    arrowSize
                );
                spriteBatch.Draw(backArrowTexture, backArrowBounds, Color.White);
            }

            int plusSize = TopBarHeight - 10;
            if (showPlus && plusIcon != null)
            {
                plusBounds = new Rectangle(topBarRect.Right + 8, topBarRect.Y + (TopBarHeight - plusSize) / 2, plusSize, plusSize);
                spriteBatch.Draw(plusIcon, plusBounds, Color.White);
            }
            else plusBounds = Rectangle.Empty;

            DrawBreadcrumbsRightAligned(spriteBatch, topBarRect);

            spriteBatch.End();

            var contentRect = new Rectangle(
                Bounds.X,
                Bounds.Y + TopBarHeight,
                Bounds.Width,
                Bounds.Height - TopBarHeight
            );
            device.ScissorRectangle = contentRect;
            device.RasterizerState = raster;

            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                raster
            );

            base.Draw(spriteBatch);

            DrawBreadcrumbsRightAligned(spriteBatch, topBarRect);

            spriteBatch.End();

            device.ScissorRectangle = oldScissor;
            device.RasterizerState = oldRaster;

            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullCounterClockwise
            );

            if (HasOverflow)
            {
                var trackRect = GetScrollBarTrackRect();
                spriteBatch.Draw(Get1x1Texture(device), trackRect, Color.Black * 0.3f);

                var thumbRect = ComputeScrollBarThumbRect();
                spriteBatch.Draw(
                    Get1x1Texture(device),
                    thumbRect,
                    _isDraggingScrollbar ? Color.White * 0.8f : Color.White
                );
            }
        }

        private void DrawBreadcrumbsRightAligned(SpriteBatch spriteBatch, Rectangle barRect)
        {
            var truncated = _breadcrumbSegments.Select(seg => new {
                Segment = seg,
                DisplayName = seg.Name.Length > 10 ? seg.Name[..10] + "..." : seg.Name
            }).ToList();

            float arrowWidth = font.MeasureString(" > ").X;
            float maxWidth = barRect.Width - backArrowBounds.Width;
            float usedWidth = 0f;

            var finalReversed = new List<(BreadcrumbSegment seg, string text)>();

            for (int i = truncated.Count - 1; i >= 0; i--)
            {
                var item = truncated[i];
                float txtW = font.MeasureString(item.DisplayName).X;
                float needed = txtW;

                if (finalReversed.Count > 0) needed += arrowWidth;

                if (usedWidth + needed > maxWidth)
                {
                    if (i > 0)
                    {
                        if (finalReversed.Count == 0)
                        {
                            finalReversed.Add((item.Segment, "..."));
                        }
                        else
                        {
                            var first = truncated[0];
                            finalReversed.Add((first.Segment, "..."));
                        }
                    }
                    break;
                }
                else
                {
                    finalReversed.Add((item.Segment, item.DisplayName));
                    usedWidth += needed;
                }
            }

            finalReversed.Reverse();

            float totalUsed = 0f;
            for (int i = 0; i < finalReversed.Count; i++)
            {
                float w = font.MeasureString(finalReversed[i].text).X;
                totalUsed += w;
                if (i < finalReversed.Count - 1) totalUsed += arrowWidth;
            }

            float xStart = barRect.Right - 5f - totalUsed;
            float y = barRect.Y + (TopBarHeight - font.LineSpacing) / 2f;
            float xCursor = xStart;

            for (int i = 0; i < finalReversed.Count; i++)
            {
                var (segment, txt) = finalReversed[i];
                float w = font.MeasureString(txt).X;

                if (txt == "...")
                {
                    segment.Bounds = Rectangle.Empty;
                }
                else
                {
                    segment.Bounds = new Rectangle((int)xCursor, (int)y, (int)w, (int)font.LineSpacing);
                }

                spriteBatch.DrawString(font, txt, new Vector2(xCursor, y), AppColors.White);
                xCursor += w;

                if (i < finalReversed.Count - 1)
                {
                    spriteBatch.DrawString(font, " > ", new Vector2(xCursor, y), AppColors.White);
                    xCursor += arrowWidth;
                }
            }
        }

        bool HasOverflow => _contentHeight > (Bounds.Height - TopBarHeight);

        private void RebuildItems()
        {
            if (_items.Count == 0) return;

            float listHeight = Bounds.Height + IconSize + Padding;
            if (listHeight <= 0) return;

            float rowHeight = IconSize + Padding;
            int firstVisibleRow = (int)Math.Floor(_scrollOffsetY / rowHeight);
            if (firstVisibleRow < 0) firstVisibleRow = 0;

            int visibleRows = (int)Math.Ceiling(listHeight / rowHeight);

            int lastVisibleRow = firstVisibleRow + visibleRows;
            int totalRows = (int)Math.Ceiling(_items.Count / (float)ColumnCount);
            if (lastVisibleRow > totalRows) lastVisibleRow = totalRows;

            if (firstVisibleRow == _prevFirstVisibleRow && lastVisibleRow == _prevLastVisibleRow)
            {
                return;
            }

            _prevFirstVisibleRow = firstVisibleRow;
            _prevLastVisibleRow = lastVisibleRow;

            int startIndex = firstVisibleRow * ColumnCount;
            int endIndex = lastVisibleRow * ColumnCount;
            if (endIndex > _items.Count) endIndex = _items.Count;

            int neededCount = endIndex - startIndex;
            if (neededCount < 0) neededCount = 0;

            while (_pooledButtons.Count < neededCount)
            {
                var btn = new UIButton
                {
                    Font = font,
                    Bounds = new Rectangle(0, 0, IconSize, IconSize),
                    TextColor = AppColors.Black,
                    ClickMode = ClickMode.OnRelease
                };
                ScheduleAddChild(btn);
                _pooledButtons.Add(btn);
            }
            while (_pooledButtons.Count > neededCount)
            {
                var last = _pooledButtons[^1];
                ScheduleRemoveChild(last);
                _pooledButtons.RemoveAt(_pooledButtons.Count - 1);
            }

            float topOffset = Bounds.Y + TopBarHeight + Padding;

            for (int i = startIndex; i < endIndex; i++)
            {
                int poolIndex = i - startIndex;
                var item = _items[i];
                var btn = _pooledButtons[poolIndex];

                int globalRow = i / ColumnCount;
                int globalCol = i % ColumnCount;

                var scrollWidth = HasOverflow ? ScrollBarWidth : 0;

                int totalWidthForCols = Bounds.Width - (Padding * (ColumnCount + 1)) - scrollWidth;
                int colWidth = totalWidthForCols / ColumnCount;

                float yPos = topOffset + (globalRow * rowHeight) - _scrollOffsetY;
                int xPos = Bounds.X + Padding + (globalCol * (colWidth + Padding));

                btn.Bounds = new Rectangle(xPos, (int)yPos, colWidth, IconSize);
                btn.Icon = item.IconTexture;

                if (item.IsFolder)
                {
                    btn.Text = item.Name;
                    btn.BackgroundTransparent = true;
                    btn.IconColor = AppColors.PrimaryRed;
                    btn.TextColor = AppColors.White;
                }
                else
                {
                    btn.Text = "";
                    btn.BackgroundTransparent = false;
                    btn.BackgroundColor = AppColors.Gray;
                    btn.Corners = RoundedCorners.All;
                    btn.RoundedCornerTexture = cornerTexture;
                }

                btn.OnClick = () =>
                {
                    if (item.IsFolder)
                        NavigateTo(item.FullPath);
                    else
                        onItemSelected?.Invoke(item);
                };
            }
        }

        bool IsAtRoot()
        {
            return Path.GetFullPath(currentDirectory) == Path.GetFullPath(rootDirectory);
        }

        string GetRelativeFromRoot(string folderPath)
        {
            var full = Path.GetFullPath(folderPath);
            var root = Path.GetFullPath(rootDirectory);
            var rel = Path.GetRelativePath(root, full);
            if (rel == "." || rel == "") rel = "";
            return rel.Replace('\\', '/').Trim('/');
        }

        void ComputeContentHeight()
        {
            if (_items.Count == 0)
            {
                _contentHeight = 0;
                return;
            }
            int rowCount = (int)Math.Ceiling(_items.Count / (float)ColumnCount);
            if (rowCount < 1) rowCount = 1;

            _contentHeight = rowCount * IconSize + (rowCount - 1) * Padding + (Padding * 2);
        }

        void ClampTargetScroll()
        {
            float maxOffset = _contentHeight - (Bounds.Height - TopBarHeight);
            if (maxOffset < 0) maxOffset = 0;
            if (_scrollOffsetTargetY < 0) _scrollOffsetTargetY = 0;
            if (_scrollOffsetTargetY > maxOffset) _scrollOffsetTargetY = maxOffset;
        }

        void ClampScroll(ref float offset)
        {
            float maxOffset = _contentHeight - (Bounds.Height - TopBarHeight);
            if (maxOffset < 0) maxOffset = 0;
            if (offset < 0) offset = 0;
            if (offset > maxOffset) offset = maxOffset;
        }

        void HandleScrollBarDragging(MouseState mouse)
        {
            var point = new Point(mouse.X, mouse.Y);
            bool leftPressed = (mouse.LeftButton == ButtonState.Pressed);

            if (_isDraggingScrollbar)
            {
                if (leftPressed)
                {
                    int delta = point.Y - _dragStartMouseY;
                    var track = GetScrollBarTrackRect();
                    float thumbH = ComputeScrollBarThumbHeight();
                    float range = track.Height - thumbH;
                    if (range > 0.1f)
                    {
                        float ratio = delta / range;
                        float totalOver = _contentHeight - (Bounds.Height - TopBarHeight);
                        float newTarget = _dragStartScrollOffsetY + (ratio * totalOver);

                        _scrollOffsetTargetY = newTarget;
                        ClampTargetScroll();
                    }
                }
                else
                {
                    _isDraggingScrollbar = false;
                }
            }
            else
            {
                if (leftPressed && HasOverflow)
                {
                    var thumb = ComputeScrollBarThumbRect();
                    if (thumb.Contains(point))
                    {
                        _isDraggingScrollbar = true;
                        _dragStartMouseY = point.Y;
                        _dragStartScrollOffsetY = _scrollOffsetTargetY;
                    }
                }
            }
        }

        Rectangle GetScrollBarTrackRect()
        {
            return new Rectangle(
                Bounds.Right - ScrollBarWidth,
                Bounds.Y + TopBarHeight,
                ScrollBarWidth,
                Bounds.Height - TopBarHeight
            );
        }

        float ComputeScrollBarThumbHeight()
        {
            float viewHeight = Bounds.Height - TopBarHeight;
            if (_contentHeight <= viewHeight)
                return viewHeight;

            float ratio = viewHeight / _contentHeight;
            return viewHeight * ratio;
        }

        Rectangle ComputeScrollBarThumbRect()
        {
            var trackRect = GetScrollBarTrackRect();
            float thumbH = ComputeScrollBarThumbHeight();
            float range = trackRect.Height - thumbH;
            float over = _contentHeight - (Bounds.Height - TopBarHeight);
            float rel = (over > 0.1f) ? (_scrollOffsetY / over) : 0f;
            float top = trackRect.Top + rel * range;

            return new Rectangle(trackRect.Left, (int)top, trackRect.Width, (int)thumbH);
        }

        void ScheduleAddChild(UIElement child)
        {
            if (!_childrenToAdd.Contains(child))
            {
                _childrenToAdd.Add(child);
            }
        }

        void ScheduleRemoveChild(UIElement child)
        {
            if (!_childrenToRemove.Contains(child))
            {
                _childrenToRemove.Add(child);
            }
        }

        void ApplyChildChanges()
        {
            if (_childrenToAdd.Count > 0)
            {
                foreach (var element in _childrenToAdd)
                {
                    base.AddChild(element);
                }
                _childrenToAdd.Clear();
            }
            if (_childrenToRemove.Count > 0)
            {
                foreach (var element in _childrenToRemove)
                {
                    base.RemoveChild(element);
                }
                _childrenToRemove.Clear();
            }
        }

        Texture2D Get1x1Texture(GraphicsDevice device)
        {
            if (_onePixel == null)
            {
                _onePixel = new Texture2D(device, 1, 1);
                _onePixel.SetData(new[] { Color.White });
            }
            return _onePixel;
        }
    }
}