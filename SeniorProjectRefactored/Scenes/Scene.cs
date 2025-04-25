using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace SeniorProject.Scenes
{
    public abstract class Scene
    {
        protected SceneManager _sceneManager;

        public Scene(SceneManager sceneManager)
        {
            _sceneManager = sceneManager;
        }

        public virtual void LoadContent() { }
        public virtual void Update(GameTime gameTime) { }
        public virtual void Draw(SpriteBatch spriteBatch) { }
        public virtual void Unload() { }
    }
}