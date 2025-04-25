using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SeniorProject.Scenes;
using SeniorProjectRefactored.Objects;
using SeniorProjectRefactored.Systems;
using SeniorProjectRefactored.UI;
using System.Text.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using SeniorProjectRefactored.Networking;
using System.Collections.Generic;
using SeniorProjectRefactored.Helpers;
using System.Reflection;


namespace SeniorProjectRefactored.Scenes
{
    public enum Role { Director, Actor }

    internal class PlayScene : Scene
    {
        private Role role;
        private SpriteFont font;
        private SpriteFont fontMedium;
        private SpriteFont fontLarge;
        
        private AssetHandler assetHandler;
        private SceneHandler sceneHandler;
        private UIManager uiManager;
        private NetworkManager networkManager;
        private TurnManager turnManager;
        private Player myPlayer;

        private ObjectBrowserPanel objectBrowser;
        private UIButton deleteBtn;
        private UIButton mirrorXBtn;
        private UIButton mirrorYBtn;
        private UIButton endTurnBtn;
        private UIButton chatBubbleBtn;
        private UIButton actionPromptBtn;
        private UIButton setOwnerBtn;
        private UITextLabel playerCountLbl;

        private Dictionary<string, Player> playerLookup = new();

        public PlayScene(SceneManager manager, NetworkManager networkManage, Role playerRole) : base(manager)
        {
            networkManager = networkManage;
            role = playerRole;
        }

        public override void LoadContent()
        {
            var game = _sceneManager.GetGame();

            font = game.Content.Load<SpriteFont>("PixelFont");
            fontMedium = game.Content.Load<SpriteFont>("PixelFontMedium");
            fontLarge = game.Content.Load<SpriteFont>("PixelFontLarge");

            var arrowTex = game.Content.Load<Texture2D>("LeftArrow");
            var folderIcon = game.Content.Load<Texture2D>("Folder");
            var plusIcon = game.Content.Load<Texture2D>("Plus");

            var cornerTex = game.Content.Load<Texture2D>("RoundedCorners");
            var bubbleTex = game.Content.Load<Texture2D>("SpeechCorners");
            var actionBubbleTex = game.Content.Load<Texture2D>("ActionBubbleCorners");

            var tailTex = game.Content.Load<Texture2D>("SpeechTail");
            var speechBubbleIcon = game.Content.Load<Texture2D>("SpeechBubble");

            var actionTailTex = game.Content.Load<Texture2D>("ActionTail");
            var actionPromptIcon = game.Content.Load<Texture2D>("ActionArrow");

            var turnArrow = game.Content.Load<Texture2D>("TurnArrow");
            var passTurnIcon = game.Content.Load<Texture2D>("PassTurn");
            var trashIcon = game.Content.Load<Texture2D>("TrashIcon");
            var horizontalMirror = game.Content.Load<Texture2D>("HorizontalMirror");
            var verticalMirror = game.Content.Load<Texture2D>("VerticalMirror");

            assetHandler = new AssetHandler(game.GraphicsDevice);
            sceneHandler = new SceneHandler();
            uiManager = new UIManager();
            turnManager = new TurnManager();

            if (!networkManager.IsHosting)
            {
                myPlayer = new Player { ID = networkManager.MyId, Name = $"Player-{networkManager.MyId[..4]}" };
            }

            networkManager.OnClientMessage += json =>
            {
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    if (!doc.RootElement.TryGetProperty("type", out var typeProp))
                    {
                        Debug.WriteLine($"Msg without \"type\" ignored → {json}");
                        return;
                    }
                    string type = typeProp.GetString()!;

                    switch (type)
                    {
                        case "SceneUpdate":
                            var upd = JsonSerializer.Deserialize<SceneUpdateMsg>(json, JsonOpts);

                            if (upd?.scene != null)
                            {
                                upd.scene.Stage = assetHandler.LoadTextureFromRelativePath(upd.scene.StageRelativePath);

                                foreach (var p in upd.scene.Props)
                                    p.Image = assetHandler.LoadTextureFromRelativePath(p.AssetRelativePath);

                                foreach (var b in upd.scene.Bubbles)
                                {
                                    if (b.Type == BubbleType.Speech)
                                    {
                                        b.Image = bubbleTex;
                                        b.TailImg = tailTex;
                                    } else if (b.Type == BubbleType.ActionPrompt)
                                    {
                                        b.Image = actionBubbleTex;
                                        b.TailImg = actionTailTex;
                                    }
                                    b.Font = font;
                                    b.UpdateSize();
                                }

                                sceneHandler.SwitchScene(upd.scene);
                            }

                            if (networkManager.IsHosting)
                                networkManager.Broadcast(json);

                            break;

                        case "TurnUpdate":
                            var tu = JsonSerializer.Deserialize<TurnUpdateMsg>(json);
                            turnManager.RemoteAdvance(tu);
                            break;

                        case "TurnFinished":
                            turnManager.NextTurn();
                            var next = new TurnUpdateMsg(turnManager.IsHostTurn, turnManager.CurrentPlayerId);
                            networkManager.Broadcast(JsonSerializer.Serialize(next, JsonOpts));
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Failed to parse msg: " + ex.Message);
                }
            };

            networkManager.OnClientConnectedWithId += id =>
            {
                var player = new Player { ID = id, Name = $"Player-{id[..4]}" };
                playerLookup[id] = player;
                turnManager.AddPlayer(player);
            };

            networkManager.OnClientDisconnectedWithId += id =>
            {
                if (playerLookup.TryGetValue(id, out var player))
                {
                    turnManager.RemovePlayer(id);
                    playerLookup.Remove(id);
                }
            };

            turnManager.OnHostTurnStarted += () =>
            {
                sceneHandler.selectedProp = null;
                sceneHandler.selectedBubbleTail = null;
            };

            turnManager.OnClientTurnStarted += p =>
            {
                sceneHandler.selectedProp = null;
                sceneHandler.selectedBubbleTail = null;
            };

            networkManager.OnClientMessage += json =>
            {
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    if (!doc.RootElement.TryGetProperty("type", out var typeProp))
                        return;

                    string type = typeProp.GetString();

                    switch(type)
                    {
                        case "TurnUpdate":
                            var msg = JsonSerializer.Deserialize<TurnUpdateMsg>(json);
                            if (msg != null)
                                turnManager.RemoteAdvance(msg);
                            break;
                        
                        case "ServerPing":
                            if (doc.RootElement.TryGetProperty("count", out var countProp))
                            {
                                int count = countProp.GetInt32();
                                playerCountLbl.Text = $"Users Connected: {count}";
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Failed to parse server message: " + ex.Message);
                }
            };

            var myStage = assetHandler.GetStageByName("BackgroundTest");
            sceneHandler.SwitchScene(new SceneObj
            {
                Name = "TestScene",
                Stage = myStage.Texture,
                StageRelativePath = myStage.RelativeFilePath
            });

            string docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string baseFolder = Path.Combine(docsPath, "StageCue");

            objectBrowser = new ObjectBrowserPanel
            {
                font = font,
                folderIcon = folderIcon,
                backArrowTexture = arrowTex,
                cornerTexture = cornerTex,
                rootDirectory = baseFolder,
                assetHandler = assetHandler,
                plusIcon = plusIcon,
                onItemSelected = (item) =>
                {
                    if (!item.IsFolder)
                    {
                        Debug.WriteLine("Clicked file: " + item.Type);
                        if (item.Type == AssetType.Stage)
                        {
                            sceneHandler.scene.Stage = item.IconTexture;
                            sceneHandler.scene.StageRelativePath = item.FullPath;
                        }
                        else if (item.Type == AssetType.Object)
                        {
                            var stageRect = sceneHandler.stageRect;

                            var buffer = 8;

                            float relativeX = (float)(stageRect.Right - item.IconTexture.Width * sceneHandler.scale - stageRect.X - buffer) / stageRect.Width;
                            float relativeY = (float)(stageRect.Top + (stageRect.Height * 0.5) - stageRect.Y - (item.IconTexture.Height * 0.5) * sceneHandler.scale) / stageRect.Height;

                            var prop = new Prop
                            {
                                Image = item.IconTexture,
                                RelativePos = new System.Numerics.Vector2(relativeX, relativeY),
                                AssetRelativePath = item.FullPath
                            };

                            sceneHandler.scene.addProp(prop);
                            sceneHandler.selectedProp = prop;
                        }
                        else if (item.Type == AssetType.Scene)
                        {
                            var scene = assetHandler.LoadSceneFromFile(item.FullPath, bubbleTex, tailTex, font, actionBubbleTex, actionTailTex);
                            sceneHandler.SwitchScene(scene);
                        }
                    }
                },
                onPlusClicked = (folderPath) =>
                {
                    if (role != Role.Director)
                        return;

                    var saveDialog = new SceneSaveDialog(game.GraphicsDevice, fontMedium, cornerTex);

                    saveDialog.OnCancel = () => uiManager.RemoveElement(saveDialog);

                    saveDialog.OnCreate = () =>
                    {
                        string rawName = saveDialog.nameEntry.Text.Trim();
                        if (string.IsNullOrEmpty(rawName))
                            return;

                        string jsonPath = Path.Combine(folderPath, rawName + ".json");
                        string thumbPath = Path.Combine(folderPath, rawName + ".png");

                        if (File.Exists(jsonPath))
                            return;

                        var json = JsonSerializer.Serialize(new SceneUpdateMsg(sceneHandler.scene).scene, JsonOpts);
                        File.WriteAllText(jsonPath, json);

                        var graphics = game.GraphicsDevice;
                        var target = new RenderTarget2D(graphics, 144, 144);
                        var oldTargets = graphics.GetRenderTargets();

                        graphics.SetRenderTarget(target);
                        graphics.Clear(Color.Transparent);

                        var tempSpriteBatch = new SpriteBatch(graphics);
                        tempSpriteBatch.Begin(
                            SpriteSortMode.Deferred,
                            BlendState.AlphaBlend,
                            SamplerState.PointClamp,
                            DepthStencilState.None,
                            RasterizerState.CullCounterClockwise
                        );

                        sceneHandler.stageRect = new Rectangle(0, 0, 144, 144);
                        sceneHandler.scale = 1f;
                        sceneHandler.Draw(tempSpriteBatch);

                        tempSpriteBatch.End();

                        graphics.SetRenderTargets(oldTargets);

                        using (var stream = File.OpenWrite(thumbPath))
                        {
                            target.SaveAsPng(stream, 144, 144);
                        }

                        target.Dispose();

                        assetHandler.RefreshFolder(folderPath);
                        objectBrowser.NavigateTo(folderPath);
                        uiManager.RemoveElement(saveDialog);
                    };

                    uiManager.AddElement(saveDialog);
                }
            };

            objectBrowser.NavigateTo(baseFolder);
            uiManager.AddElement(objectBrowser);

            deleteBtn = new UIButton
            {
                Font = font,
                Corner = Corner.TopLeft,
                Size = new Vector2(68, 68),
                OnClick = () =>
                {
                    if (sceneHandler.selectedProp == null)
                        return;

                    var confirm = new ConfirmationBox(game.GraphicsDevice, fontLarge, "Delete this selection?", cornerTex);
                    confirm.OnYes = () =>
                    {
                        if (sceneHandler.selectedProp is Bubble selectedBubble)
                        {
                            sceneHandler.scene.removeBubble(selectedBubble);
                        }
                        else
                        {
                            sceneHandler.scene.removeProp(sceneHandler.selectedProp);
                        }
                        sceneHandler.selectedProp = null;
                        uiManager.RemoveElement(confirm);
                    };
                    confirm.OnNo = () => uiManager.RemoveElement(confirm);

                    uiManager.AddElement(confirm);
                },
                BackgroundColor = AppColors.PrimaryRed,
                Corners = RoundedCorners.All,
                RoundedCornerTexture = cornerTex,
                TextColor = AppColors.White,
                IsVisible = false,
                Icon = trashIcon
            };
            uiManager.AddElement(deleteBtn);

            mirrorXBtn = new UIButton
            {
                Font = font,
                Corner = Corner.TopLeft,
                Size = new Vector2(68, 68),
                OnClick = () =>
                {
                    if (sceneHandler.selectedProp != null)
                    {
                        sceneHandler.selectedProp.MirrorX = !sceneHandler.selectedProp.MirrorX;
                    }
                },
                BackgroundColor = AppColors.PrimaryRed,
                Corners = RoundedCorners.All,
                RoundedCornerTexture = cornerTex,
                TextColor = AppColors.White,
                IsVisible = false,
                Icon = horizontalMirror
            };
            uiManager.AddElement(mirrorXBtn);

            mirrorYBtn = new UIButton
            {
                Font = font,
                Corner = Corner.TopLeft,
                Size = new Vector2(68, 68),
                OnClick = () =>
                {
                    if (sceneHandler.selectedProp != null)
                    {
                        sceneHandler.selectedProp.MirrorY = !sceneHandler.selectedProp.MirrorY;
                    }
                },
                BackgroundColor = AppColors.PrimaryRed,
                Corners = RoundedCorners.All,
                RoundedCornerTexture = cornerTex,
                TextColor = AppColors.White,
                IsVisible = false,
                Icon = verticalMirror
            };
            uiManager.AddElement(mirrorYBtn);

            endTurnBtn = new UIButton
            {
                Font = fontMedium,
                Text = "End\nTurn",
                Corner = Corner.BottomLeft,
                Size = new Vector2(68, 68),
                OnClick = () =>
                {
                    if ((turnManager.IsHostTurn && networkManager.IsHosting) ||
                        (!turnManager.IsHostTurn && turnManager.CurrentPlayerId == networkManager.MyId))
                    {
                        var sceneUpdate = new SceneUpdateMsg(sceneHandler.scene);
                        string scenePayload = JsonSerializer.Serialize(sceneUpdate, JsonOpts);

                        if (networkManager.IsHosting)
                        {
                            networkManager.Broadcast(scenePayload);
                        }
                        else
                        {
                            _ = networkManager.ClientSendAsync(scenePayload);
                        }

                        if (networkManager.IsHosting)
                        {
                            turnManager.NextTurn();

                            var tu = new TurnUpdateMsg(turnManager.IsHostTurn, turnManager.CurrentPlayerId);
                            string turnPayload = JsonSerializer.Serialize(tu);

                            networkManager.Broadcast(turnPayload);
                        }
                        else
                        {
                            _ = networkManager.ClientSendAsync("{\"type\":\"TurnFinished\"}");
                        }
                    }
                },
                BackgroundColor = AppColors.PrimaryRed,
                Corners = RoundedCorners.All,
                RoundedCornerTexture = cornerTex,
                TextColor = AppColors.White,
                TextGrowAnim = true
            };
            uiManager.AddElement(endTurnBtn);

            chatBubbleBtn = new UIButton
            {
                Font = font,
                Corner = Corner.BottomLeft,
                Size = new Vector2(68, 68),
                OnClick = () =>
                {
                    var stageRect = sceneHandler.stageRect;
                    var buffer = 20;

                    var bubble = new Bubble
                    {
                        Image = bubbleTex,
                        Font = font,
                        Padding = 10,
                        TailImg = tailTex,
                        Text = ""
                    };

                    if (role == Role.Actor)
                    {
                        bubble.OwnerId = myPlayer?.ID;
                    }

                    float relativeX = 0;
                    float relativeY = 0;

                    if (sceneHandler.selectedProp != null)
                    {
                        float scale = sceneHandler.scale;

                        int propX = (int)(stageRect.X + (sceneHandler.selectedProp.RelativePos.X * stageRect.Width));
                        int propY = (int)(stageRect.Y + (sceneHandler.selectedProp.RelativePos.Y * stageRect.Height));

                        int propWidth = (int)(sceneHandler.selectedProp.Image.Width * scale);
                        int propHeight = (int)(sceneHandler.selectedProp.Image.Height * scale);

                        Rectangle selectedBounds = new Rectangle(propX, propY, propWidth, propHeight);

                        int topRightX = selectedBounds.Right + 13;
                        int topRightY = selectedBounds.Top + (int)(selectedBounds.Height * 0.15f);

                        int finalX = topRightX + buffer;
                        int finalY = topRightY;

                        relativeX = (float)(finalX - stageRect.X) / stageRect.Width;
                        relativeY = (float)(finalY - stageRect.Y) / stageRect.Height;
                    }
                    else
                    {
                        relativeX = (float)(stageRect.X + buffer - stageRect.X) / stageRect.Width;
                        relativeY = (float)(stageRect.Top + (stageRect.Height * 0.5) - stageRect.Y - (bubble.Size.Y * 0.5) * sceneHandler.scale) / stageRect.Height;
                    }

                    bubble.RelativePos = new System.Numerics.Vector2(relativeX, relativeY);
                    bubble.UpdateSize();

                    sceneHandler.scene.addBubble(bubble);
                    sceneHandler.selectedProp = bubble;
                },
                BackgroundColor = AppColors.PrimaryRed,
                Corners = RoundedCorners.All,
                RoundedCornerTexture = cornerTex,
                TextColor = AppColors.White,
                Icon = speechBubbleIcon
            };
            uiManager.AddElement(chatBubbleBtn);

            actionPromptBtn = new UIButton
            {
                Font = font,
                Corner = Corner.BottomLeft,
                Size = new Vector2(68, 68),
                OnClick = () =>
                {
                    if (role != Role.Actor)
                        return;

                    var stageRect = sceneHandler.stageRect;
                    var buffer = 20;

                    var bubble = new Bubble
                    {
                        OwnerId = myPlayer?.ID,
                        Type = BubbleType.ActionPrompt,
                        Image = actionBubbleTex,
                        TailImg = actionTailTex,
                        Font = font,
                        Padding = 10,
                        Text = ""
                    };

                    float relativeX = 0;
                    float relativeY = 0;

                    if (sceneHandler.selectedProp != null)
                    {
                        float scale = sceneHandler.scale;

                        int propX = (int)(stageRect.X + (sceneHandler.selectedProp.RelativePos.X * stageRect.Width));
                        int propY = (int)(stageRect.Y + (sceneHandler.selectedProp.RelativePos.Y * stageRect.Height));

                        int propWidth = (int)(sceneHandler.selectedProp.Image.Width * scale);
                        int propHeight = (int)(sceneHandler.selectedProp.Image.Height * scale);

                        Rectangle selectedBounds = new Rectangle(propX, propY, propWidth, propHeight);

                        int topRightX = selectedBounds.Right + 13;
                        int topRightY = selectedBounds.Top + (int)(selectedBounds.Height * 0.5f);

                        int finalX = topRightX + buffer;
                        int finalY = topRightY;

                        relativeX = (float)(finalX - stageRect.X) / stageRect.Width;
                        relativeY = (float)(finalY - stageRect.Y) / stageRect.Height;
                    }
                    else
                    {
                        relativeX = (float)(stageRect.X + buffer - stageRect.X) / stageRect.Width;
                        relativeY = (float)(stageRect.Top + (stageRect.Height * 0.5) - stageRect.Y - (bubble.Size.Y * 0.5) * sceneHandler.scale) / stageRect.Height;
                    }

                    bubble.RelativePos = new System.Numerics.Vector2(relativeX, relativeY);
                    bubble.UpdateSize();

                    sceneHandler.scene.addBubble(bubble);
                    sceneHandler.selectedProp = bubble;
                },
                BackgroundColor = AppColors.PrimaryRed,
                Corners = RoundedCorners.All,
                RoundedCornerTexture = cornerTex,
                TextColor = AppColors.White,
                Icon = actionPromptIcon
            };
            uiManager.AddElement(actionPromptBtn);

            setOwnerBtn = new UIButton
            {
                Font = font,
                Text = "Owner:\nNone",
                Corner = Corner.TopLeft,
                Size = new Vector2(68, 68),
                OnClick = () =>
                {
                    if (sceneHandler.selectedProp == null) return;

                    var ids = new List<string>(playerLookup.Keys) { null };
                    var current = sceneHandler.selectedProp.OwnerId;
                    var idx = ids.IndexOf(current);

                    var newId = ids[(idx + 1) % ids.Count];
                    sceneHandler.selectedProp.OwnerId = newId;
                },
                BackgroundColor = AppColors.PrimaryRed,
                Corners = RoundedCorners.All,
                RoundedCornerTexture = cornerTex,
                TextColor = AppColors.White,
                TextGrowAnim = true
            };
            uiManager.AddElement(setOwnerBtn);

            playerCountLbl = new UITextLabel
            {
                Font = font,
                Corner = Corner.BottomLeft,
                Text = $"Users Connected: {playerLookup.Count}",
                Size = new Vector2(1, 1),
                TextColor = AppColors.White,
                IsVisible = true
            };
            uiManager.AddElement(playerCountLbl);
        }

        public override void Update(GameTime gameTime)
        {
            if (uiManager == null || sceneHandler == null || turnManager == null || networkManager == null)
                return;

            var mouse = Microsoft.Xna.Framework.Input.Mouse.GetState();
            var keyboard = Microsoft.Xna.Framework.Input.Keyboard.GetState();

            updateVisibleWidgets();

            uiManager.Update(gameTime, mouse, keyboard);

            sceneHandler.IsMyTurn = (turnManager.IsHostTurn && networkManager.IsHosting) || (!turnManager.IsHostTurn && turnManager.CurrentPlayerId == networkManager.MyId);
            sceneHandler.role = role;
            sceneHandler.myPlayer = myPlayer;

            sceneHandler.Update(gameTime, mouse, keyboard, uiManager.IsHoveringAnyElement(mouse), uiManager.IsAnyModalOpen());
            base.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (uiManager == null || sceneHandler == null || turnManager == null || networkManager == null)
                return;

            sceneHandler.UpdateStageRect(spriteBatch);
            uiManager.PositionCorners(sceneHandler.stageRect);

            if (objectBrowser != null)
            {
                uiManager.PositionObjectBrowser(objectBrowser, sceneHandler.stageRect);
            }

            sceneHandler.Draw(spriteBatch);
            uiManager.Draw(spriteBatch);
        }

        private void updateVisibleWidgets()
        {
            if (turnManager == null || networkManager == null)
                return;

            bool isMyTurn = (turnManager.IsHostTurn && networkManager.IsHosting) || (!turnManager.IsHostTurn && turnManager.CurrentPlayerId == networkManager.MyId);

            deleteBtn.IsVisible = false;
            mirrorXBtn.IsVisible = false;
            mirrorYBtn.IsVisible = false;
            objectBrowser.IsOpen = false;
            chatBubbleBtn.IsVisible = false;
            endTurnBtn.IsVisible = false;
            setOwnerBtn.IsVisible = false;
            actionPromptBtn.IsVisible = false;

            if (role == Role.Director)
                playerCountLbl.Text = $"Users Connected: {playerLookup.Count}";

            if (!isMyTurn) return;

            if (role == Role.Director)
            {
                objectBrowser.IsOpen = true;

                if (sceneHandler.selectedProp != null && playerLookup.Count > 0)
                {
                    setOwnerBtn.IsVisible = true;
                    
                    var ownerId = sceneHandler.selectedProp.OwnerId;
                    if (ownerId == null)
                    {
                        setOwnerBtn.Text = "Owner:\nNone";
                    }
                    else
                    {
                        var players = turnManager.GetPlayers();
                        int index = players.FindIndex(p => p.ID == ownerId);
                        if (index != -1)
                            setOwnerBtn.Text = $"Owner:\nActor {index + 1}";
                        else
                            sceneHandler.selectedProp.OwnerId = null;
                    }
                }
                else
                {
                    setOwnerBtn.Text = "Owner:\nNone";
                }
            } else if (role == Role.Actor)
            {
                actionPromptBtn.IsVisible = true;
            }

            if (sceneHandler.selectedProp != null)
            {
                if (sceneHandler.selectedProp is not Bubble)
                {
                    mirrorXBtn.IsVisible = true;
                    mirrorYBtn.IsVisible = true;
                }

                if (sceneHandler.selectedProp is Bubble || role == Role.Director)
                {
                    deleteBtn.IsVisible = true;
                }
            }

            chatBubbleBtn.IsVisible = true;
            endTurnBtn.IsVisible = true;
        }

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            IncludeFields = true,
            WriteIndented = true
        };
    }
}