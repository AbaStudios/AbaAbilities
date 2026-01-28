using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameInput;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace AbaAbilities.Content.UI;

/// <summary>
/// UI state for the Runecrafter's dialogue interface, providing options to enchant or view unlocks.
/// </summary>
public class RunecrafterDialogueUI : UIState
{
    public event Action OnEnchantClicked;
    public event Action OnUnlockInfoClicked;
    public event Action OnCloseClicked;
    public event Action OnAskClicked;

    public bool IsFullyClosed { get; private set; } = true;

    private BackgroundElement _background;
    private DialogueTextElement _dialogueText;
    private DialogueButtonContainer _buttonContainer;
    private float _timeOpen;
    private bool _closing;

    private const float BG_FADE_DURATION = 0.3f;
    private const float BTN_DELAY = 0.05f;
    private const float BTN_ANIM_DURATION = 0.3f;
    private string _currentDialogue = "Hello! I'm the Runecrafter. I can help you with your abilities.";

    public void SetDialogueText(string text) {
        _currentDialogue = text;
        if (_dialogueText != null)
            _dialogueText.SetText(text);
    }

    public override void OnInitialize() {
        _background = new BackgroundElement();
        _background.Width.Set(0, 1f);
        _background.Height.Set(0, 1f);
        Append(_background);

        _dialogueText = new DialogueTextElement();
        _dialogueText.Width.Set(520, 0f);
        _dialogueText.Height.Set(80, 0f);
        _dialogueText.HAlign = 0.5f;
        _dialogueText.VAlign = 0.35f;
        Append(_dialogueText);

        _buttonContainer = new DialogueButtonContainer();
        _buttonContainer.Width.Set(600, 0f);
        _buttonContainer.Height.Set(60, 0f);
        _buttonContainer.HAlign = 0.5f;
        _buttonContainer.VAlign = 0.65f;
        _buttonContainer.OnCloseClicked += RequestClose;
        _buttonContainer.OnEnchantClicked += () => OnEnchantClicked?.Invoke();
        _buttonContainer.OnAskClicked += () => OnAskClicked?.Invoke();
        Append(_buttonContainer);
    }

    public override void OnActivate() {
        _timeOpen = 0f;
        _closing = false;
        IsFullyClosed = false;
        _background?.Reset();
        _dialogueText?.Reset();
        _buttonContainer?.Reset();
    }

    public void RequestClose() {
        _closing = true;
        IsFullyClosed = true;
        OnCloseClicked?.Invoke();
    }

    public override void Update(GameTime gameTime) {
        base.Update(gameTime);
        Recalculate();
        if (_closing) return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timeOpen += dt;

        float bgT = MathHelper.Clamp(_timeOpen / BG_FADE_DURATION, 0f, 1f);
        _background?.SetProgress(EaseOutQuad(bgT));

        _dialogueText?.SetEntryProgress(1f);

        float btnTime = _timeOpen - BTN_DELAY;
        float btnT = btnTime > 0 ? MathHelper.Clamp(btnTime / BTN_ANIM_DURATION, 0f, 1f) : 0f;
        _buttonContainer?.SetEntryProgress(EaseOutQuad(btnT));

        _dialogueText?.UpdateLetterAnimation(dt);
    }

    private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);

    private class BackgroundElement : UIElement
    {
        private Asset<Texture2D> _texture;
        private float _progress;

        public override void OnInitialize() {
            _texture = ModContent.Request<Texture2D>("AbaAbilities/Assets/Textures/UI/Background_Shaded_Center");
        }

        public void Reset() => _progress = 0f;
        public void SetProgress(float p) => _progress = p;

        protected override void DrawSelf(SpriteBatch spriteBatch) {
            if (_texture == null || !_texture.IsLoaded || _progress <= 0f) return;

            Texture2D tex = _texture.Value;
            Rectangle dims = GetDimensions().ToRectangle();

            float scale = (float)dims.Height / tex.Height;
            float width = tex.Width * scale;
            float x = dims.X + (dims.Width - width) / 2f;

            spriteBatch.Draw(tex, new Vector2(x, dims.Y), null, Color.White * _progress, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }

    private class DialogueTextElement : UIElement
    {
        private float _entryProgress;
        private float _letterProgress;
        private string _text = "";
        private int _totalLetters;

        public void Reset() {
            _entryProgress = 0f;
            _letterProgress = 0f;
        }

        public void SetEntryProgress(float p) => _entryProgress = p;

        public void SetText(string text) {
            _text = text;
            _totalLetters = text.Length;
            _letterProgress = 0f;
        }

        public void UpdateLetterAnimation(float dt) {
            if (_entryProgress < 1f) return;
            _letterProgress = Math.Min(_letterProgress + dt * 1000f / 5f, _totalLetters);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch) {
            if (_entryProgress <= 0f || string.IsNullOrEmpty(_text)) return;

            CalculatedStyle dims = GetDimensions();
            var font = FontAssets.MouseText.Value;
            Color textColor = Color.White * _entryProgress;

            int visibleLetters = (int)_letterProgress;
            string displayText = visibleLetters >= _text.Length ? _text : _text.Substring(0, visibleLetters);

            Vector2 textSize = font.MeasureString(displayText);
            float x = dims.X + (dims.Width - textSize.X) / 2f;
            float y = dims.Y + (dims.Height - textSize.Y) / 2f;
            Vector2 textPos = new(x, y);
            Utils.DrawBorderString(spriteBatch, displayText, textPos, textColor);
        }
    }

    private class DialogueButtonContainer : UIElement
    {
        public event Action OnCloseClicked;
        public event Action OnEnchantClicked;
        public event Action OnAskClicked;

        private DialogueButton _closeButton;
        private DialogueButton _enchantButton;
        private DialogueButton _askButton;
        private float _entryProgress;

        public override void OnInitialize() {
            const float CLOSE_BTN_SIZE = 50f;
            const float BTN_HEIGHT = 50f;
            const float SPACING = 20f;
            const float CONTAINER_WIDTH = 600f;
            
            float flexButtonWidth = (CONTAINER_WIDTH - CLOSE_BTN_SIZE - SPACING * 3) / 2f;
            float startX = SPACING;

            _closeButton = new DialogueButton(DialogueButtonType.Close, null);
            _closeButton.Width.Set(CLOSE_BTN_SIZE, 0f);
            _closeButton.Height.Set(BTN_HEIGHT, 0f);
            _closeButton.Left.Set(startX, 0f);
            _closeButton.VAlign = 0.5f;
            _closeButton.OnClicked += () => OnCloseClicked?.Invoke();
            Append(_closeButton);

            _enchantButton = new DialogueButton(DialogueButtonType.Enchant, null);
            _enchantButton.Width.Set(flexButtonWidth, 0f);
            _enchantButton.Height.Set(BTN_HEIGHT, 0f);
            _enchantButton.Left.Set(startX + CLOSE_BTN_SIZE + SPACING, 0f);
            _enchantButton.VAlign = 0.5f;
            _enchantButton.OnClicked += () => OnEnchantClicked?.Invoke();
            Append(_enchantButton);

            _askButton = new DialogueButton(DialogueButtonType.Ask, null);
            _askButton.Width.Set(flexButtonWidth, 0f);
            _askButton.Height.Set(BTN_HEIGHT, 0f);
            _askButton.Left.Set(startX + CLOSE_BTN_SIZE + SPACING + flexButtonWidth + SPACING, 0f);
            _askButton.VAlign = 0.5f;
            _askButton.OnClicked += () => OnAskClicked?.Invoke();
            Append(_askButton);
        }

        public void Reset() {
            _entryProgress = 0f;
            _closeButton?.Reset();
            _enchantButton?.Reset();
            _askButton?.Reset();
        }

        public void SetEntryProgress(float p) {
            _entryProgress = p;
            _closeButton?.SetEntryProgress(p);
            _enchantButton?.SetEntryProgress(p);
            _askButton?.SetEntryProgress(p);
        }
    }

    private enum DialogueButtonType
    {
        Close,
        Enchant,
        Ask
    }

    private class DialogueButton : UIElement
    {
        public event Action OnClicked;

        private Asset<Texture2D> _iconTexture;
        private Asset<Texture2D> _panelBg;
        private Asset<Texture2D> _panelBorder;

        private float _entryProgress;
        private float _hoverProgress;
        private bool _wasHovering;

        private static readonly Color PanelBgColor = new(63, 82, 151);
        private static readonly Color PanelBorderColor = new(89, 116, 213);

        private DialogueButtonType _buttonType;
        private string _buttonText;

        public DialogueButton(DialogueButtonType type, string text) {
            _buttonType = type;
            _buttonText = text ?? "";
        }

        public override void OnInitialize() {
            switch (_buttonType) {
                case DialogueButtonType.Close:
                    _iconTexture = ModContent.Request<Texture2D>("AbaAbilities/Assets/Textures/UI/Icon_X");
                    break;
                case DialogueButtonType.Enchant:
                    _buttonText = "Enchant";
                    // Reforge icon is handled specially in DrawSelf using TextureAssets.Reforge
                    break;
                case DialogueButtonType.Ask:
                    _buttonText = "Ask...";
                    _iconTexture = ModContent.Request<Texture2D>("AbaAbilities/Assets/Textures/UI/Icon_Help");
                    break;
            }
            _panelBg = Main.Assets.Request<Texture2D>("Images/UI/PanelBackground");
            _panelBorder = Main.Assets.Request<Texture2D>("Images/UI/PanelBorder");
        }

        public void Reset() {
            _entryProgress = 0f;
            _hoverProgress = 0f;
            _wasHovering = false;
        }

        public void SetEntryProgress(float p) => _entryProgress = p;

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);
            if (_entryProgress <= 0f) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            bool hovering = IsMouseHovering && _entryProgress >= 1f && !PlayerInput.IgnoreMouseInterface;

            if (hovering && !_wasHovering) {
                SoundEngine.PlaySound(SoundID.MenuTick);
                Main.LocalPlayer.mouseInterface = true;
            } else if (!hovering && _wasHovering) {
                SoundEngine.PlaySound(SoundID.MenuTick);
            }
            _wasHovering = hovering;

            _hoverProgress = hovering ? 1f : 0f;

            if (hovering)
                Main.LocalPlayer.mouseInterface = true;
        }

        public override void LeftClick(UIMouseEvent evt) {
            base.LeftClick(evt);
            if (_entryProgress >= 1f) {
                SoundEngine.PlaySound(SoundID.MenuTick);
                Main.mouseLeftRelease = false;
                Main.LocalPlayer.releaseUseItem = false;
                Main.LocalPlayer.mouseInterface = true;
                OnClicked?.Invoke();
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch) {
            if (_entryProgress <= 0f) return;

            CalculatedStyle dims = GetDimensions();
            float opacity = _entryProgress;

            Vector2 center = new(dims.X + dims.Width / 2f, dims.Y + dims.Height / 2f);
            Vector2 size = new(dims.Width, dims.Height);
            Vector2 topLeft = center - size / 2f;

            float brightness = MathHelper.Lerp(0.7f, 0.9f, _hoverProgress);
            Color bgColor = PanelBgColor * brightness * opacity;
            Color borderColor = PanelBorderColor * brightness * opacity;

            DrawPanel(spriteBatch, _panelBg.Value, topLeft, size, bgColor);
            DrawPanel(spriteBatch, _panelBorder.Value, topLeft, size, borderColor);

            int num = Main.mouseTextColor;
            Color baseColor = new Color(num, (int)((double)num / 1.1), num / 2, num) * opacity;
            Color shadowColor = _hoverProgress > 0.01f ? Color.Brown : Color.Black;
            float scale = 0.9f;
            if (_hoverProgress > 0.01f) scale *= 1.2f;

            if (_buttonType == DialogueButtonType.Enchant) {
                Texture2D icon = TextureAssets.Reforge[_hoverProgress > 0.5f ? 1 : 0].Value;
                float iconScale = 0.8f;
                Color contentColor = Color.White * opacity;

                float iconW = icon.Width * iconScale;
                var font = FontAssets.MouseText.Value;
                Vector2 textSize = ChatManager.GetStringSize(font, _buttonText, new Vector2(scale));

                float spacing = 4f;
                float totalW = iconW + spacing + textSize.X;
                float startX = center.X - totalW / 2f;

                spriteBatch.Draw(icon, new Vector2(startX + iconW / 2f, center.Y), null, contentColor, 0f, icon.Size() / 2f, iconScale, SpriteEffects.None, 0f);

                Vector2 textPos = new(startX + iconW + spacing, center.Y - textSize.Y / 2f);
                // Use ChatManager.DrawColorCodedStringWithShadow exactly like vanilla
                ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, _buttonText, textPos, baseColor, shadowColor, 0f, Vector2.Zero, new Vector2(scale), -1f, 1f);
            } else if (_iconTexture != null && _iconTexture.IsLoaded) {
                Texture2D icon = _iconTexture.Value;
                float iconScale = 0.8f;
                Color contentColor = Color.White * opacity;

                if (_buttonType == DialogueButtonType.Close) {
                    spriteBatch.Draw(icon, center, null, contentColor, 0f, icon.Size() / 2f, iconScale, SpriteEffects.None, 0f);
                } else if (_buttonType == DialogueButtonType.Ask) {
                    float iconW = icon.Width * iconScale;
                    var font = FontAssets.MouseText.Value;
                    Vector2 textSize = ChatManager.GetStringSize(font, _buttonText, new Vector2(scale));

                    float spacing = 4f;
                    float totalW = iconW + spacing + textSize.X;
                    float startX = center.X - totalW / 2f;

                    spriteBatch.Draw(icon, new Vector2(startX + iconW / 2f, center.Y), null, contentColor, 0f, icon.Size() / 2f, iconScale, SpriteEffects.None, 0f);

                    Vector2 textPos = new(startX + iconW + spacing, center.Y - textSize.Y / 2f);
                    ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, _buttonText, textPos, baseColor, shadowColor, 0f, Vector2.Zero, new Vector2(scale), -1f, 1f);
                }
            }
        }

        private static void DrawPanel(SpriteBatch sb, Texture2D tex, Vector2 pos, Vector2 size, Color color) {
            const int corner = 12;
            const int bar = 4;

            int x = (int)pos.X;
            int y = (int)pos.Y;
            int w = (int)size.X;
            int h = (int)size.Y;
            int x2 = x + w - corner;
            int y2 = y + h - corner;
            int midW = w - corner * 2;
            int midH = h - corner * 2;

            sb.Draw(tex, new Rectangle(x, y, corner, corner), new Rectangle(0, 0, corner, corner), color);
            sb.Draw(tex, new Rectangle(x2, y, corner, corner), new Rectangle(corner + bar, 0, corner, corner), color);
            sb.Draw(tex, new Rectangle(x, y2, corner, corner), new Rectangle(0, corner + bar, corner, corner), color);
            sb.Draw(tex, new Rectangle(x2, y2, corner, corner), new Rectangle(corner + bar, corner + bar, corner, corner), color);
            sb.Draw(tex, new Rectangle(x + corner, y, midW, corner), new Rectangle(corner, 0, bar, corner), color);
            sb.Draw(tex, new Rectangle(x + corner, y2, midW, corner), new Rectangle(corner, corner + bar, bar, corner), color);
            sb.Draw(tex, new Rectangle(x, y + corner, corner, midH), new Rectangle(0, corner, corner, bar), color);
            sb.Draw(tex, new Rectangle(x2, y + corner, corner, midH), new Rectangle(corner + bar, corner, corner, bar), color);
            sb.Draw(tex, new Rectangle(x + corner, y + corner, midW, midH), new Rectangle(corner, corner, bar, bar), color);
        }
    }
}
