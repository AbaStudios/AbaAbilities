using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI;
using Terraria.UI.Chat;
using AbaAbilities.Core.UI;

namespace AbaAbilities.Common.UI;

public class PageButton : UIElement
{
    public event Action OnPressed;

    private readonly Asset<Texture2D> _iconTexture;
    private readonly string _text;
    private readonly bool _useReforgeIcon;

    private Asset<Texture2D> _panelBg;
    private Asset<Texture2D> _panelBorder;

    private float _entryProgress;
    private float _hoverProgress;
    private bool _wasHovering;
    private bool _visible = true;

    private static readonly Color PanelBgColor = new(63, 82, 151);
    private static readonly Color PanelBorderColor = new(89, 116, 213);

    public PageButton(Asset<Texture2D> iconTexture, string text = null, bool useReforgeIcon = false) {
        _iconTexture = iconTexture;
        _text = text ?? string.Empty;
        _useReforgeIcon = useReforgeIcon;
    }

    public override void OnInitialize() {
        _panelBg = Main.Assets.Request<Texture2D>("Images/UI/PanelBackground");
        _panelBorder = Main.Assets.Request<Texture2D>("Images/UI/PanelBorder");
    }

    public void Reset() {
        _entryProgress = 0f;
        _hoverProgress = 0f;
        _wasHovering = false;
    }

    public void SetEntryProgress(float progress) => _entryProgress = progress;

    public void SetVisible(bool visible) {
        _visible = visible;
        IgnoresMouseInteraction = !visible;
    }

    public override void Update(GameTime gameTime) {
        base.Update(gameTime);
        if (!_visible || _entryProgress <= 0f)
            return;

        bool hovering = IsMouseHovering && _entryProgress >= 1f && !PlayerInput.IgnoreMouseInterface;

        if (hovering && !_wasHovering) {
            SoundEngine.PlaySound(SoundID.MenuTick);
            Main.LocalPlayer.mouseInterface = true;
        }
        else if (!hovering && _wasHovering) {
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        _wasHovering = hovering;
        _hoverProgress = hovering ? 1f : 0f;

        if (hovering)
            Main.LocalPlayer.mouseInterface = true;
    }

    public override void LeftClick(UIMouseEvent evt) {
        base.LeftClick(evt);
        if (!_visible || _entryProgress < 1f)
            return;

        SoundEngine.PlaySound(SoundID.MenuTick);
        Main.mouseLeftRelease = false;
        Main.LocalPlayer.releaseUseItem = false;
        Main.LocalPlayer.mouseInterface = true;
        OnPressed?.Invoke();
    }

    protected override void DrawSelf(SpriteBatch spriteBatch) {
        if (!_visible || _entryProgress <= 0f)
            return;

        CalculatedStyle dims = GetDimensions();
        float opacity = _entryProgress;

        Vector2 center = new(dims.X + dims.Width / 2f, dims.Y + dims.Height / 2f);
        Vector2 size = new(dims.Width, dims.Height);
        Vector2 topLeft = center - size / 2f;

        float brightness = MathHelper.Lerp(0.7f, 0.9f, _hoverProgress);
        Color bgColor = PanelBgColor * brightness * opacity;
        Color borderColor = PanelBorderColor * brightness * opacity;

        PanelRenderer.Draw(spriteBatch, _panelBg.Value, topLeft, size, bgColor);
        PanelRenderer.Draw(spriteBatch, _panelBorder.Value, topLeft, size, borderColor);

        int num = Main.mouseTextColor;
        Color baseColor = new Color(num, (int)((double)num / 1.1), num / 2, num) * opacity;
        Color shadowColor = _hoverProgress > 0.01f ? Color.Brown : Color.Black;
        float scale = 0.9f;
        if (_hoverProgress > 0.01f)
            scale *= 1.2f;

        if (_useReforgeIcon) {
            DrawReforgeStyle(spriteBatch, center, opacity, baseColor, shadowColor, scale);
        }
        else if (_iconTexture != null && _iconTexture.IsLoaded) {
            if (string.IsNullOrEmpty(_text)) {
                DrawIconOnly(spriteBatch, center, opacity);
            }
            else {
                DrawIconWithText(spriteBatch, center, opacity, baseColor, shadowColor, scale);
            }
        }
        else if (!string.IsNullOrEmpty(_text)) {
            DrawTextOnly(spriteBatch, center, baseColor, shadowColor, scale);
        }
    }

    private void DrawIconOnly(SpriteBatch spriteBatch, Vector2 center, float opacity) {
        Texture2D icon = _iconTexture.Value;
        spriteBatch.Draw(icon, center, null, Color.White * opacity, 0f, icon.Size() * 0.5f, 0.8f, SpriteEffects.None, 0f);
    }

    private void DrawIconWithText(SpriteBatch spriteBatch, Vector2 center, float opacity, Color baseColor, Color shadowColor, float scale) {
        Texture2D icon = _iconTexture.Value;
        float iconScale = 0.8f;
        Color contentColor = Color.White * opacity;

        float iconW = icon.Width * iconScale;
        var font = FontAssets.MouseText.Value;
        Vector2 textSize = ChatManager.GetStringSize(font, _text, new Vector2(scale));

        float spacing = 4f;
        float totalW = iconW + spacing + textSize.X;
        float startX = center.X - totalW / 2f;

        spriteBatch.Draw(icon, new Vector2(startX + iconW / 2f, center.Y), null, contentColor, 0f, icon.Size() / 2f, iconScale, SpriteEffects.None, 0f);

        Vector2 textPos = new(startX + iconW + spacing, center.Y - textSize.Y / 2f);
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, _text, textPos, baseColor, shadowColor, 0f, Vector2.Zero, new Vector2(scale), -1f, 1f);
    }

    private void DrawTextOnly(SpriteBatch spriteBatch, Vector2 center, Color baseColor, Color shadowColor, float scale) {
        var font = FontAssets.MouseText.Value;
        Vector2 textSize = ChatManager.GetStringSize(font, _text, new Vector2(scale));
        Vector2 textPos = new(center.X - textSize.X / 2f, center.Y - textSize.Y / 2f);
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, _text, textPos, baseColor, shadowColor, 0f, Vector2.Zero, new Vector2(scale), -1f, 1f);
    }

    private void DrawReforgeStyle(SpriteBatch spriteBatch, Vector2 center, float opacity, Color baseColor, Color shadowColor, float scale) {
        Texture2D icon = TextureAssets.Reforge[_hoverProgress > 0.5f ? 1 : 0].Value;
        float iconScale = 0.8f;
        Color contentColor = Color.White * opacity;

        float iconW = icon.Width * iconScale;
        var font = FontAssets.MouseText.Value;
        Vector2 textSize = ChatManager.GetStringSize(font, _text, new Vector2(scale));

        float spacing = 4f;
        float totalW = iconW + spacing + textSize.X;
        float startX = center.X - totalW / 2f;

        spriteBatch.Draw(icon, new Vector2(startX + iconW / 2f, center.Y), null, contentColor, 0f, icon.Size() / 2f, iconScale, SpriteEffects.None, 0f);

        Vector2 textPos = new(startX + iconW + spacing, center.Y - textSize.Y / 2f);
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, _text, textPos, baseColor, shadowColor, 0f, Vector2.Zero, new Vector2(scale), -1f, 1f);
    }
}
