using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;
using Terraria.UI;

namespace AbaAbilities.Core.UI;

public class ShadedBackground : UIElement
{
    private readonly Asset<Texture2D> _texture;
    private float _progress;

    public ShadedBackground(string texturePath) {
        _texture = ModContent.Request<Texture2D>(texturePath);
    }

    public void Reset() => _progress = 0f;

    public void SetProgress(float progress) => _progress = progress;

    protected override void DrawSelf(SpriteBatch spriteBatch) {
        if (_texture == null || !_texture.IsLoaded || _progress <= 0f)
            return;

        Texture2D tex = _texture.Value;
        Rectangle dims = GetDimensions().ToRectangle();

        float scale = (float)dims.Height / tex.Height;
        float width = tex.Width * scale;
        float x = dims.X + (dims.Width - width) / 2f;

        spriteBatch.Draw(tex, new Vector2(x, dims.Y), null, Color.White * _progress, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
}
