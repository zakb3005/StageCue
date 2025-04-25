using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using SeniorProjectRefactored;

namespace SeniorProject.Scenes
{
    public class SceneManager
    {
        private Game1 _game;
        private Scene _currentScene;

        public SceneManager(Game1 game)
        {
            _game = game;
        }

        public void ChangeScene(Scene newScene)
        {
            _currentScene?.Unload();
            _currentScene = newScene;
            _currentScene.LoadContent();
        }

        public void Update(GameTime gameTime)
        {
            _currentScene?.Update(gameTime);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            _currentScene?.Draw(spriteBatch);
        }

        public Game1 GetGame() => _game;
    }
}