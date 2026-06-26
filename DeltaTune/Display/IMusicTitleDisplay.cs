using DeltaTune.Media;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DeltaTune.Display
{
    public interface IMusicTitleDisplay
    {
        MediaInfo Content { get; set; }
        MusicTitleDisplayState State { get; set; }
        void Update(GameTime gameTime);
        void Draw(SpriteBatch spriteBatch, GameTime gameTime);
    }
}