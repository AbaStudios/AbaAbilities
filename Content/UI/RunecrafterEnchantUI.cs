using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;
using AbaAbilities.Common.Attachments;

namespace AbaAbilities.Content.UI;

public class RunecrafterEnchantUI : UIState
{
    public event Action OnCloseClicked;

    public Item[] SlotItems { get; } = new Item[1];

    private BackgroundElement _background;
    private MagicCircleElement _magicCircle;
    private ScalableUIItemSlot _itemSlot;
    private ActionButton _closeButton;
    private ActionButton _enchantButton;
    private CoinCostDisplay _costDisplay;

    private static readonly string[] AbilityIds = new[]
    {
        "AbaAbilities:MagicalDash",
        "AbaAbilities:CelestialCollapse",
        "AbaAbilities:StarlitWhirlwind"
    };

    public override void OnInitialize()
    {
        _background = new BackgroundElement();
        _background.Width.Set(0, 1f);
        _background.Height.Set(0, 1f);
        Append(_background);

        UIElement centerContainer = new UIElement();
        centerContainer.Width.Set(460f, 0f);
        centerContainer.Height.Set(460f, 0f);
        centerContainer.HAlign = 0.5f;
        centerContainer.VAlign = 0.45f;
        Append(centerContainer);

        _magicCircle = new MagicCircleElement(() => IsSlotEligibleForEnchant());
        _magicCircle.Width.Set(460f, 0f);
        _magicCircle.Height.Set(460f, 0f);
        _magicCircle.HAlign = 0.5f;
        _magicCircle.VAlign = 0.5f;
        _magicCircle.IgnoresMouseInteraction = true;
        centerContainer.Append(_magicCircle);

        SlotItems[0] = new Item();
        SlotItems[0].TurnToAir();

        // Change context to PrefixItem (Reforge) to reuse vanilla logic and visuals (no stack number)
        _itemSlot = new ScalableUIItemSlot(SlotItems, 0, Terraria.UI.ItemSlot.Context.PrefixItem);
        float slotSize = 52f * 2f;
        _itemSlot.Left.Set((460f - slotSize) * 0.5f, 0f);
        _itemSlot.Top.Set((460f - slotSize) * 0.5f, 0f);
        _itemSlot.Width.Set(slotSize, 0f);
        _itemSlot.Height.Set(slotSize, 0f);
        _itemSlot.Scale = 0.85f * 2f;
        centerContainer.Append(_itemSlot);

        UIElement bottomButtons = new UIElement();
        bottomButtons.Width.Set(600f, 0f);
        bottomButtons.Height.Set(60f, 0f);
        bottomButtons.HAlign = 0.5f;
        bottomButtons.VAlign = 0.75f;
        Append(bottomButtons);

        _closeButton = new ActionButton(ActionButtonKind.Close, "");
        _closeButton.Width.Set(50f, 0f);
        _closeButton.Height.Set(50f, 0f);
        _closeButton.Left.Set(20f, 0f);
        _closeButton.VAlign = 0.5f;
        _closeButton.OnPressed += HandleCloseClicked;
        bottomButtons.Append(_closeButton);

        _enchantButton = new ActionButton(ActionButtonKind.Enchant, "Enchant");
        _enchantButton.Width.Set(200f, 0f);
        _enchantButton.Height.Set(50f, 0f);
        _enchantButton.HAlign = 0.5f;
        _enchantButton.VAlign = 0.5f;
        _enchantButton.OnPressed += TryEnchant;
        bottomButtons.Append(_enchantButton);

        _costDisplay = new CoinCostDisplay(() => CalculateCost(), () => IsSlotEligibleForEnchant());
        _costDisplay.Width.Set(300f, 0f);
        _costDisplay.Height.Set(30f, 0f);
        _costDisplay.HAlign = 0.5f;
        _costDisplay.VAlign = 0.85f;
        Append(_costDisplay);
    }

    public override void OnActivate()
    {
        _background?.Reset();
        _closeButton?.Reset();
        _enchantButton?.Reset();

        Main.playerInventory = true;
        Main.npcChatText = "";

        if (SlotItems[0] == null)
        {
            SlotItems[0] = new Item();
        }

        if (SlotItems[0].IsAir)
        {
            SlotItems[0].TurnToAir();
        }
    }

    public override void OnDeactivate()
    {
        ReturnSlotItemToPlayer();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        Recalculate();

        bool eligible = IsSlotEligibleForEnchant();
        _enchantButton?.SetVisible(eligible);
    }

    private void HandleCloseClicked()
    {
        ReturnSlotItemToPlayer();
        OnCloseClicked?.Invoke();
    }

    private bool IsSlotEligibleForEnchant()
    {
        Item item = SlotItems[0];
        if (item == null || item.IsAir)
        {
            return false;
        }

        if (item.damage <= 0)
        {
            return false;
        }

        // Items with maxStack > 1 are not enchantable in this system
        if (item.maxStack > 1)
        {
            return false;
        }

        var attachments = AttachmentApi.GetItemAttachments(item);
        foreach (var abilityId in AbilityIds)
        {
            foreach (var existing in attachments)
            {
                if (existing.Id != null && existing.Id == abilityId)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private int CalculateCost()
    {
        Item item = SlotItems[0];
        if (item == null || item.IsAir)
            return 0;

        long rawCost = (long)item.value * 5L;
        if (rawCost < 1L)
            return 1;
        if (rawCost > int.MaxValue)
            return int.MaxValue;
        return (int)rawCost;
    }

    private void TryEnchant()
    {
        Item item = SlotItems[0];
        int cost = CalculateCost();

        Player player = Main.LocalPlayer;
        if (!player.CanAfford(cost))
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            Main.NewText("You cannot afford the enchantment cost.");
            return;
        }

        player.BuyItem(cost);
        
        string selectedAbility = AbilityIds[Main.rand.Next(AbilityIds.Length)];
        AttachmentApi.Attach(item, selectedAbility);
        
        SoundEngine.PlaySound(SoundID.Item37);
        PopupText.NewText(PopupTextContext.ItemReforge, item, item.stack, noStack: true);
    }

    private void ReturnSlotItemToPlayer()
    {
        if (Main.dedServ)
        {
            return;
        }

        Item item = SlotItems[0];
        if (item == null || item.IsAir)
        {
            return;
        }

        Player player = Main.LocalPlayer;

        Item itemToReturn = item.Clone();
        itemToReturn.position = player.Center;

        Item returned = player.GetItem(Main.myPlayer, itemToReturn, GetItemSettings.GetItemInDropItemCheck);
        if (returned.stack > 0)
        {
            int idx = Item.NewItem(new EntitySource_OverfullInventory(player), player.position, player.width, player.height, returned, noBroadcast: false, noGrabDelay: true);
            Main.item[idx].newAndShiny = false;
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(MessageID.SyncItem, -1, -1, null, idx, 1f);
            }
        }

        SlotItems[0].TurnToAir();
    }

    private class BackgroundElement : UIElement
    {
        private Asset<Texture2D> _texture;
        private float _progress;

        public override void OnInitialize()
        {
            _texture = ModContent.Request<Texture2D>("AbaAbilities/Assets/Textures/UI/Background_Shaded_Center");
        }

        public void Reset()
        {
            _progress = 1f;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (_texture == null || !_texture.IsLoaded || _progress <= 0f)
            {
                return;
            }

            Texture2D tex = _texture.Value;
            Rectangle dims = GetDimensions().ToRectangle();

            float scale = (float)dims.Height / tex.Height;
            float width = tex.Width * scale;
            float x = dims.X + (dims.Width - width) / 2f;

            spriteBatch.Draw(tex, new Vector2(x, dims.Y), null, Color.White * _progress, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }

    private class MagicCircleElement : UIElement
    {
        private readonly Func<bool> _isActive;
        private Asset<Texture2D> _outline;
        private Asset<Texture2D> _glow;

        public MagicCircleElement(Func<bool> isActive)
        {
            _isActive = isActive;
        }

        public override void OnInitialize()
        {
            _outline = ModContent.Request<Texture2D>("AbaAbilities/Assets/Textures/Effects/Magic_Circle_7_Outline");
            _glow = ModContent.Request<Texture2D>("AbaAbilities/Assets/Textures/Effects/Magic_Circle_7_Glow");
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (_outline == null || !_outline.IsLoaded)
            {
                return;
            }

            CalculatedStyle dims = GetDimensions();
            Vector2 center = dims.Center();

            Texture2D outlineTex = _outline.Value;
            float scale = Math.Min(dims.Width / outlineTex.Width, dims.Height / outlineTex.Height);

            spriteBatch.Draw(outlineTex, center, null, Color.White, 0f, outlineTex.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            bool active = _isActive != null && _isActive();
            if (active)
            {
                Texture2D glowTex = _glow != null && _glow.IsLoaded ? _glow.Value : Terraria.GameContent.TextureAssets.MagicPixel.Value;
                Color tint = Color.Red * 0.5f;
                if (_glow != null && _glow.IsLoaded)
                {
                    spriteBatch.Draw(glowTex, center, null, tint, 0f, glowTex.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                }
            }
        }
    }

    private enum ActionButtonKind
    {
        Close,
        Enchant
    }

    private class ActionButton : UIElement
    {
        public event Action OnPressed;

        private readonly ActionButtonKind _kind;
        private string _text;

        private Asset<Texture2D> _iconTexture;
        private Asset<Texture2D> _panelBg;
        private Asset<Texture2D> _panelBorder;

        private float _entryProgress;
        private float _hoverProgress;
        private bool _wasHovering;
        private bool _visible = true;

        private static readonly Color PanelBgColor = new(63, 82, 151);
        private static readonly Color PanelBorderColor = new(89, 116, 213);

        public ActionButton(ActionButtonKind kind, string text)
        {
            _kind = kind;
            _text = text ?? string.Empty;
        }

        public override void OnInitialize()
        {
            if (_kind == ActionButtonKind.Close)
            {
                _iconTexture = ModContent.Request<Texture2D>("AbaAbilities/Assets/Textures/UI/Icon_X");
            }

            _panelBg = Main.Assets.Request<Texture2D>("Images/UI/PanelBackground");
            _panelBorder = Main.Assets.Request<Texture2D>("Images/UI/PanelBorder");
        }

        public void Reset()
        {
            _entryProgress = 1f;
            _hoverProgress = 0f;
            _wasHovering = false;
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;
            IgnoresMouseInteraction = !visible;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (!_visible || _entryProgress <= 0f)
            {
                return;
            }

            bool hovering = IsMouseHovering && _entryProgress >= 1f && !Terraria.GameInput.PlayerInput.IgnoreMouseInterface;
            if (hovering && !_wasHovering)
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                Main.LocalPlayer.mouseInterface = true;
            }
            else if (!hovering && _wasHovering)
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
            }

            _wasHovering = hovering;
            _hoverProgress = hovering ? 1f : 0f;

            if (hovering)
            {
                Main.LocalPlayer.mouseInterface = true;
            }
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);

            if (!_visible || _entryProgress < 1f)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.MenuTick);
            Main.mouseLeftRelease = false;
            Main.LocalPlayer.releaseUseItem = false;
            Main.LocalPlayer.mouseInterface = true;
            OnPressed?.Invoke();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (!_visible || _entryProgress <= 0f)
            {
                return;
            }

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
            if (_hoverProgress > 0.01f)
            {
                scale *= 1.2f;
            }

            if (_kind == ActionButtonKind.Close)
            {
                if (_iconTexture != null && _iconTexture.IsLoaded)
                {
                    Texture2D icon = _iconTexture.Value;
                    spriteBatch.Draw(icon, center, null, Color.White * opacity, 0f, icon.Size() * 0.5f, 0.8f, SpriteEffects.None, 0f);
                }
            }
            else
            {
                var font = FontAssets.MouseText.Value;
                Vector2 textSize = ChatManager.GetStringSize(font, _text, new Vector2(scale));
                Vector2 textPos = new(center.X - textSize.X / 2f, center.Y - textSize.Y / 2f);
                ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, _text, textPos, baseColor, shadowColor, 0f, Vector2.Zero, new Vector2(scale), -1f, 1f);
            }
        }

        private static void DrawPanel(SpriteBatch sb, Texture2D tex, Vector2 pos, Vector2 size, Color color)
        {
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

    private class CoinCostDisplay : UIElement
    {
        private readonly Func<int> _costCalculator;
        private readonly Func<bool> _isEligible;

        public CoinCostDisplay(Func<int> costCalculator, Func<bool> isEligible)
        {
            _costCalculator = costCalculator;
            _isEligible = isEligible;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            int cost = _costCalculator?.Invoke() ?? 0;
            if (cost <= 0)
                return;

            CalculatedStyle dims = GetDimensions();
            string costText = "Cost: ";
            
            // Format coins similar to vanilla reforge
            int coins = cost;
            int platinum = coins / 1000000;
            coins -= platinum * 1000000;
            int gold = coins / 10000;
            coins -= gold * 10000;
            int silver = coins / 100;
            coins -= silver * 100;
            int copper = coins;

            string coinStr = "";
            if (platinum > 0)
                coinStr += $"[c/{Colors.AlphaDarken(Colors.CoinPlatinum).Hex3()}:{platinum} Platinum] ";
            if (gold > 0)
                coinStr += $"[c/{Colors.AlphaDarken(Colors.CoinGold).Hex3()}:{gold} Gold] ";
            if (silver > 0)
                coinStr += $"[c/{Colors.AlphaDarken(Colors.CoinSilver).Hex3()}:{silver} Silver] ";
            if (copper > 0 || coinStr == "")
                coinStr += $"[c/{Colors.AlphaDarken(Colors.CoinCopper).Hex3()}:{copper} Copper]";

            var font = FontAssets.MouseText.Value;
            Color textColor = _isEligible?.Invoke() ?? false ? Color.White : Color.Gray;
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, costText + coinStr, new Vector2(dims.X, dims.Y), textColor, Color.Black, 0f, Vector2.Zero, Vector2.One);
        }
    }

    private class ScalableUIItemSlot : UIItemSlot
    {
        public float Scale { get; set; } = 1f;
        public Item[] MyItemArray { get; private set; }
        public int MyItemIndex { get; private set; }

        public ScalableUIItemSlot(Item[] itemArray, int itemIndex, int itemSlotContext)
            : base(itemArray, itemIndex, itemSlotContext)
        {
            MyItemArray = itemArray;
            MyItemIndex = itemIndex;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            float checkScale = Main.inventoryScale;
            Main.inventoryScale = Scale;
            base.DrawSelf(spriteBatch);
            Main.inventoryScale = checkScale;

            if (MyItemArray != null && MyItemIndex >= 0 && MyItemIndex < MyItemArray.Length)
            {
                Item item = MyItemArray[MyItemIndex];
                if (item != null && !item.IsAir && item.damage > 0 && item.maxStack == 1)
                {
                    bool hasEnchant = false;
                    foreach (var existing in AttachmentApi.GetItemAttachments(item))
                    {
                        if (existing.Id != null && existing.Id.StartsWith("AbaAbilities")) { hasEnchant = true; break; }
                    }

                    if (!hasEnchant)
                    {
                        Rectangle dims = GetDimensions().ToRectangle();
                        spriteBatch.Draw(TextureAssets.MagicPixel.Value, dims, Color.Red * 0.3f);
                    }
                }
            }
        }
    }
}
