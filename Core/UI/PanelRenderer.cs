using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AbaAbilities.Core.UI;

internal static class PanelRenderer
{
    private const int Corner = 12;
    private const int Bar = 4;

    public static void Draw(SpriteBatch sb, Texture2D tex, Vector2 pos, Vector2 size, Color color) {
        int x = (int)pos.X;
        int y = (int)pos.Y;
        int w = (int)size.X;
        int h = (int)size.Y;
        int x2 = x + w - Corner;
        int y2 = y + h - Corner;
        int midW = w - Corner * 2;
        int midH = h - Corner * 2;

        sb.Draw(tex, new Rectangle(x, y, Corner, Corner), new Rectangle(0, 0, Corner, Corner), color);
        sb.Draw(tex, new Rectangle(x2, y, Corner, Corner), new Rectangle(Corner + Bar, 0, Corner, Corner), color);
        sb.Draw(tex, new Rectangle(x, y2, Corner, Corner), new Rectangle(0, Corner + Bar, Corner, Corner), color);
        sb.Draw(tex, new Rectangle(x2, y2, Corner, Corner), new Rectangle(Corner + Bar, Corner + Bar, Corner, Corner), color);
        sb.Draw(tex, new Rectangle(x + Corner, y, midW, Corner), new Rectangle(Corner, 0, Bar, Corner), color);
        sb.Draw(tex, new Rectangle(x + Corner, y2, midW, Corner), new Rectangle(Corner, Corner + Bar, Bar, Corner), color);
        sb.Draw(tex, new Rectangle(x, y + Corner, Corner, midH), new Rectangle(0, Corner, Corner, Bar), color);
        sb.Draw(tex, new Rectangle(x2, y + Corner, Corner, midH), new Rectangle(Corner + Bar, Corner, Corner, Bar), color);
        sb.Draw(tex, new Rectangle(x + Corner, y + Corner, midW, midH), new Rectangle(Corner, Corner, Bar, Bar), color);
    }
}
