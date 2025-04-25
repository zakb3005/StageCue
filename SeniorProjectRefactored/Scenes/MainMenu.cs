using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SeniorProject.Scenes;
using SeniorProjectRefactored.Networking;
using SeniorProjectRefactored.Systems;
using SeniorProjectRefactored.UI;
using System;
using System.Diagnostics;

namespace SeniorProjectRefactored.Scenes
{
    internal class MainMenu : Scene
    {
        private SpriteFont font;
        private SpriteFont fontLarge;
        private UIManager uiManager;
        private SceneManager sceneManager;
        private NetworkManager netManager;

        private UIButton hostBtn;
        private UITextBox hostPortEntry;
        private UIButton hostStartBtn;
        private UIButton hostCancelBtn;
        private UITextLabel hostingLabel;

        private UIButton joinBtn;
        private UITextBox joinIPEntry;
        private UITextBox joinPortEntry;
        private UIButton joinCancelBtn;
        private UIButton joinStartBtn;
        private UITextLabel joiningLabel;

        private UITextLabel errorLabel;

        public MainMenu(SceneManager _sceneManager, NetworkManager _networkManager) : base(_sceneManager)
        {
            sceneManager = _sceneManager;
            netManager = _networkManager;
        }

        public override void LoadContent()
        {
            var game = _sceneManager.GetGame();

            font = game.Content.Load<SpriteFont>("PixelFont");
            fontLarge = game.Content.Load<SpriteFont>("PixelFontLarge");

            var cornerTex = game.Content.Load<Texture2D>("RoundedCorners");

            uiManager = new UIManager();

            hostBtn = new UIButton
            {
                Font = fontLarge,
                Text = "Host Game",
                Size = new Vector2(350, 50),
                OnClick = () =>
                {
                    hostPortEntry.Text = "";
                    hostPortEntry.IsVisible = true;
                    hostStartBtn.IsVisible = true;
                    hostCancelBtn.IsVisible = true;

                    hostingLabel.IsVisible = false;
                    errorLabel.IsVisible = false;
                    hostBtn.IsVisible = false;
                    joinBtn.IsVisible = false;
                },
                BackgroundColor = AppColors.PrimaryRed,
                Corners = RoundedCorners.All,
                RoundedCornerTexture = cornerTex,
                TextColor = AppColors.White,
                IsVisible = true,
                TextGrowAnim = true
            };
            uiManager.AddElement(hostBtn);

            hostPortEntry = new UITextBox
            {
                Font = fontLarge,
                PlaceholderText = "Port #",
                Size = new Vector2(350, 50),
                Corners = RoundedCorners.All,
                MaxLength = 5,
                RoundedCornerTexture = cornerTex,
                IsVisible = false
            };
            uiManager.AddElement(hostPortEntry);

            hostingLabel = new UITextLabel
            {
                Font = fontLarge,
                Text = "Starting Server...",
                Size = new Vector2(350, 50),
                TextColor = AppColors.White,
                IsVisible = false
            };
            uiManager.AddElement(hostingLabel);

            hostStartBtn = new UIButton
            {
                Font = fontLarge,
                Text = "Start",
                Size = new Vector2(150, 50),
                OnClick = () =>
                {
                    if (int.TryParse(hostPortEntry.Text, out int portNumber) && portNumber >= 1024 && portNumber <= 65535)
                    {
                        Debug.WriteLine($"Trying to host on port: {portNumber}");

                        hostStartBtn.IsVisible = false;
                        hostPortEntry.IsVisible = false;
                        hostCancelBtn.IsVisible = false;
                        errorLabel.IsVisible = false;
                        hostingLabel.IsVisible = true;

                        netManager.OnServerStarted -= HandleServerStarted;
                        netManager.OnServerFailed -= HandleServerFailed;

                        netManager.OnServerStarted += HandleServerStarted;
                        netManager.OnServerFailed += HandleServerFailed;

                        netManager.StartHosting(portNumber);
                    }
                    else
                    {
                        errorLabel.Text = "Invalid port number. Enter a number between 1024 and 65535.";
                        errorLabel.IsVisible = true;
                    }
                },
                BackgroundColor = AppColors.PrimaryRed,
                Corners = RoundedCorners.All,
                RoundedCornerTexture = cornerTex,
                TextColor = AppColors.White,
                IsVisible = false,
                TextGrowAnim = true
            };
            uiManager.AddElement(hostStartBtn);

            hostCancelBtn = new UIButton
            {
                Font = fontLarge,
                Text = "Cancel",
                Size = new Vector2(150, 50),
                OnClick = () =>
                {
                    hostPortEntry.IsVisible = false;
                    hostStartBtn.IsVisible = false;
                    hostCancelBtn.IsVisible = false;
                    errorLabel.IsVisible = false;

                    hostBtn.IsVisible = true;
                    joinBtn.IsVisible = true;
                },
                BackgroundColor = AppColors.Gray,
                Corners = RoundedCorners.All,
                RoundedCornerTexture = cornerTex,
                TextColor = AppColors.White,
                IsVisible = false,
                TextGrowAnim = true
            };
            uiManager.AddElement(hostCancelBtn);

            joinBtn = new UIButton
            {
                Font = fontLarge,
                Text = "Join Game",
                Size = new Vector2(350, 50),
                OnClick = () =>
                {
                    joinIPEntry.Text = "";
                    joinPortEntry.Text = "";
                    joinIPEntry.IsVisible = true;
                    joinPortEntry.IsVisible = true;
                    joinCancelBtn.IsVisible = true;
                    joinStartBtn.IsVisible = true;

                    joiningLabel.IsVisible = false;
                    errorLabel.IsVisible = false;
                    hostBtn.IsVisible = false;
                    joinBtn.IsVisible = false;
                },
                BackgroundColor = AppColors.PrimaryRed,
                Corners = RoundedCorners.All,
                RoundedCornerTexture = cornerTex,
                TextColor = AppColors.White,
                IsVisible = true,
                TextGrowAnim = true
            };
            uiManager.AddElement(joinBtn);

            errorLabel = new UITextLabel
            {
                Font = fontLarge,
                Text = "Error",
                Size = new Vector2(350, 50),
                TextColor = AppColors.BrightRed,
                IsVisible = false
            };
            uiManager.AddElement(errorLabel);

            joinIPEntry = new UITextBox
            {
                Font = fontLarge,
                PlaceholderText = "IP Address",
                Size = new Vector2(240, 50),
                Corners = RoundedCorners.All,
                MaxLength = 15,
                RoundedCornerTexture = cornerTex,
                IsVisible = false
            };
            uiManager.AddElement(joinIPEntry);

            joinPortEntry = new UITextBox
            {
                Font = fontLarge,
                PlaceholderText = "Port",
                Size = new Vector2(80, 50),
                Corners = RoundedCorners.All,
                MaxLength = 4,
                RoundedCornerTexture = cornerTex,
                IsVisible = false
            };
            uiManager.AddElement(joinPortEntry);

            joinCancelBtn = new UIButton
            {
                Font = fontLarge,
                Text = "Cancel",
                Size = new Vector2(150, 50),
                OnClick = () =>
                {
                    joinIPEntry.IsVisible = false;
                    joinPortEntry.IsVisible = false;
                    joinCancelBtn.IsVisible = false;
                    joinStartBtn.IsVisible = false;
                    errorLabel.IsVisible = false;

                    hostBtn.IsVisible = true;
                    joinBtn.IsVisible = true;
                },
                BackgroundColor = AppColors.Gray,
                Corners = RoundedCorners.All,
                RoundedCornerTexture = cornerTex,
                TextColor = AppColors.White,
                IsVisible = false,
                TextGrowAnim = true
            };
            uiManager.AddElement(joinCancelBtn);

            joinStartBtn = new UIButton
            {
                Font = fontLarge,
                Text = "Join",
                Size = new Vector2(150, 50),
                OnClick = async () =>
                {
                    string ip = joinIPEntry.Text;
                    if (int.TryParse(joinPortEntry.Text, out int portNumber) && portNumber >= 1024 && portNumber <= 65535)
                    {
                        if (IsValidIPv4(ip))
                        {
                            Debug.WriteLine($"Trying to join {ip}:{portNumber}");

                            joinStartBtn.IsVisible = false;
                            joinIPEntry.IsVisible = false;
                            joinCancelBtn.IsVisible = false;
                            joinPortEntry.IsVisible = false;
                            errorLabel.IsVisible = false;
                            joiningLabel.IsVisible = true;

                            netManager.OnClientConnected -= HandleClientConnected;
                            netManager.OnClientFailed -= HandleClientFailed;

                            netManager.OnClientConnected += HandleClientConnected;
                            netManager.OnClientFailed += HandleClientFailed;

                            await netManager.ConnectToServerAsync(ip, portNumber);
                        }
                        else
                        {
                            errorLabel.Text = "Invalid IP address.";
                            errorLabel.IsVisible = true;
                        }
                    }
                    else
                    {
                        errorLabel.Text = "Invalid port number. Enter 1024-65535.";
                        errorLabel.IsVisible = true;
                    }
                },
                BackgroundColor = AppColors.PrimaryRed,
                Corners = RoundedCorners.All,
                RoundedCornerTexture = cornerTex,
                TextColor = AppColors.White,
                IsVisible = false,
                TextGrowAnim = true
            };
            uiManager.AddElement(joinStartBtn);

            joiningLabel = new UITextLabel
            {
                Font = fontLarge,
                Text = "Joining Server...",
                Size = new Vector2(350, 50),
                TextColor = AppColors.White,
                IsVisible = false
            };
            uiManager.AddElement(joiningLabel);
        }

        private void HandleServerStarted()
        {
            Debug.WriteLine("Server started successfully!");
            sceneManager.ChangeScene(new PlayScene(sceneManager, netManager, Role.Director));
        }

        private void HandleServerFailed(string error)
        {
            Debug.WriteLine("Server failed to start: " + error);
            errorLabel.Text = "Server failed to start: " + error;
            errorLabel.IsVisible = true;

            hostPortEntry.IsVisible = true;
            hostStartBtn.IsVisible = true;
            hostCancelBtn.IsVisible = true;
            hostingLabel.IsVisible = false;
        }

        private void HandleClientConnected()
        {
            Debug.WriteLine("Client connected successfully to the server!");
            sceneManager.ChangeScene(new PlayScene(sceneManager, netManager, Role.Actor));
        }

        private void HandleClientFailed(string error)
        {
            Debug.WriteLine("Client failed to connect: " + error);
            errorLabel.Text = "Client failed to connect: " + error;
            errorLabel.IsVisible = true;

            joinStartBtn.IsVisible = true;
            joinIPEntry.IsVisible = true;
            joinCancelBtn.IsVisible = true;
            joinPortEntry.IsVisible = true;
            joiningLabel.IsVisible = false;
        }

        private bool IsValidIPv4(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip) || ip.Length > 15) return false;

            try
            {
                var addr = System.Net.IPAddress.Parse(ip);
                return addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
            }
            catch
            {
                return false;
            }
        }

        public override void Update(GameTime gameTime)
        {
            var mouse = Microsoft.Xna.Framework.Input.Mouse.GetState();
            var keyboard = Microsoft.Xna.Framework.Input.Keyboard.GetState();

            uiManager.Update(gameTime, mouse, keyboard);

            if (hostBtn.IsVisible)
            {
                hostBtn.Bounds = new Rectangle(
                    (_sceneManager.GetGame().GraphicsDevice.Viewport.Width / 2) - ((int)hostBtn.Size.X / 2),
                    (_sceneManager.GetGame().GraphicsDevice.Viewport.Height / 2) - 65,
                    (int)hostBtn.Size.X,
                    (int)hostBtn.Size.Y
                );
            }

            if (joinBtn.IsVisible)
            {
                joinBtn.Bounds = new Rectangle(
                    (_sceneManager.GetGame().GraphicsDevice.Viewport.Width / 2) - ((int)joinBtn.Size.X / 2),
                    (_sceneManager.GetGame().GraphicsDevice.Viewport.Height / 2) + 25,
                    (int)joinBtn.Size.X,
                    (int)joinBtn.Size.Y
                );
            }

            if (errorLabel.IsVisible)
            {
                errorLabel.Bounds = new Rectangle(
                    (_sceneManager.GetGame().GraphicsDevice.Viewport.Width / 2) - ((int)errorLabel.Size.X / 2),
                    (_sceneManager.GetGame().GraphicsDevice.Viewport.Height - 100),
                    (int)errorLabel.Size.X,
                    (int)errorLabel.Size.Y
                );
            }

            if (hostPortEntry.IsVisible)
            {
                hostPortEntry.Bounds = new Rectangle(
                    (_sceneManager.GetGame().GraphicsDevice.Viewport.Width / 2) - ((int)hostPortEntry.Size.X / 2),
                    (_sceneManager.GetGame().GraphicsDevice.Viewport.Height / 2) - 65,
                    (int)hostPortEntry.Size.X,
                    (int)hostPortEntry.Size.Y
                );
            }

            if (hostStartBtn.IsVisible)
            {
                hostStartBtn.Bounds = new Rectangle(
                    hostPortEntry.Bounds.Right - (int)hostStartBtn.Size.X,
                    (_sceneManager.GetGame().GraphicsDevice.Viewport.Height / 2) + 25,
                    (int)hostStartBtn.Size.X,
                    (int)hostStartBtn.Size.Y
                );
            }

            if (hostCancelBtn.IsVisible)
            {
                hostCancelBtn.Bounds = new Rectangle(
                    hostPortEntry.Bounds.Left,
                    (_sceneManager.GetGame().GraphicsDevice.Viewport.Height / 2) + 25,
                    (int)hostCancelBtn.Size.X,
                    (int)hostCancelBtn.Size.Y
                );
            }

            if (hostingLabel.IsVisible)
            {
                hostingLabel.Bounds = new Rectangle(
                    (_sceneManager.GetGame().GraphicsDevice.Viewport.Width / 2) - ((int)hostingLabel.Size.X / 2),
                    (_sceneManager.GetGame().GraphicsDevice.Viewport.Height / 2) - ((int)hostingLabel.Size.Y / 2),
                    (int)hostingLabel.Size.X,
                    (int)hostingLabel.Size.Y
                );
            }

            if (joinIPEntry.IsVisible || joinPortEntry.IsVisible)
            {
                joinIPEntry.Bounds = new Rectangle(
                    (_sceneManager.GetGame().GraphicsDevice.Viewport.Width / 2) - 175,
                    (_sceneManager.GetGame().GraphicsDevice.Viewport.Height / 2) - 65,
                    (int)joinIPEntry.Size.X,
                    (int)joinIPEntry.Size.Y
                );

                joinPortEntry.Bounds = new Rectangle(
                    joinIPEntry.Bounds.Right + 30,
                    (_sceneManager.GetGame().GraphicsDevice.Viewport.Height / 2) - 65,
                    (int)joinPortEntry.Size.X,
                    (int)joinPortEntry.Size.Y
                );
            }

            if (joinStartBtn.IsVisible)
            {
                joinStartBtn.Bounds = new Rectangle(
                    joinPortEntry.Bounds.Right - (int)joinStartBtn.Size.X,
                    (_sceneManager.GetGame().GraphicsDevice.Viewport.Height / 2) + 25,
                    (int)joinStartBtn.Size.X,
                    (int)joinStartBtn.Size.Y
                );
            }

            if (joinCancelBtn.IsVisible)
            {
                joinCancelBtn.Bounds = new Rectangle(
                    joinIPEntry.Bounds.Left,
                    (_sceneManager.GetGame().GraphicsDevice.Viewport.Height / 2) + 25,
                    (int)joinCancelBtn.Size.X,
                    (int)joinCancelBtn.Size.Y
                );
            }

            if (joiningLabel.IsVisible)
            {
                joiningLabel.Bounds = new Rectangle(
                    (_sceneManager.GetGame().GraphicsDevice.Viewport.Width / 2) - ((int)joiningLabel.Size.X / 2),
                    (_sceneManager.GetGame().GraphicsDevice.Viewport.Height / 2) - ((int)joiningLabel.Size.Y / 2),
                    (int)joiningLabel.Size.X,
                    (int)joiningLabel.Size.Y
                );
            }

            base.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            uiManager.Draw(spriteBatch);
        }
    }
}
