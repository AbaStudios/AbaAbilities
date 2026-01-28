using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;
using AbaAbilities.Common.Graphics;

namespace AbaAbilities.Core.Graphics
{
    /// <summary>
    /// Manages HUD elements drawn above players, such as ability indicators and bars.
    /// </summary>
    public class PlayerHudSystem : ModSystem
    {
        private Dictionary<int, List<PlayerHudElement>> _hudElements = new Dictionary<int, List<PlayerHudElement>>();

        public override void Load()
        {
            _hudElements = new Dictionary<int, List<PlayerHudElement>>();
        }

        public override void Unload()
        {
            _hudElements = null;
        }

        public override void PreUpdatePlayers()
        {
            // Clear the list at the start of the frame so it can be repopulated
            foreach (var list in _hudElements.Values)
            {
                list.Clear();
            }
        }

        internal void AddHud(int playerIndex, PlayerHudElement element)
        {
            if (!_hudElements.TryGetValue(playerIndex, out var list))
            {
                list = new List<PlayerHudElement>();
                _hudElements[playerIndex] = list;
            }
            list.Add(element);
        }

        public IReadOnlyList<PlayerHudElement> GetHudElements(int playerIndex)
        {
            if (_hudElements.TryGetValue(playerIndex, out var list))
            {
                return list;
            }
            return null;
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int inventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
            if (inventoryIndex != -1)
            {
                layers.Insert(inventoryIndex, new LegacyGameInterfaceLayer(
                    "AbaAbilities: Player Hud",
                    delegate {
                        DrawPlayerHuds();
                        return true;
                    },
                    InterfaceScaleType.Game)
                );
            }
        }

        private void DrawPlayerHuds()
        {
            foreach (var player in Main.player)
            {
                if (player == null || !player.active || player.dead) continue;

                if (_hudElements.TryGetValue(player.whoAmI, out var elements) && elements.Count > 0)
                {
                    // Start drawing above the player
                    Vector2 drawPos = player.Top - Main.screenPosition;
                    drawPos.Y -= 20f; // Initial padding above head

                    // Draw elements (stacking upwards)
                    foreach (var element in elements)
                    {
                        drawPos.Y -= DrawHudElement(element, drawPos);
                        drawPos.Y -= 4f; // Padding between elements
                    }
                }
            }
        }

        // Returns height of the drawn element
        private float DrawHudElement(PlayerHudElement element, Vector2 bottomCenterPos)
        {
            if (!element.BarProgress.HasValue && string.IsNullOrEmpty(element.Text))
            {
                return 0f;
            }

            float totalHeight = 0f;
            Vector2 textScale = new Vector2(element.FontSize);
            Vector2 textSize = Vector2.Zero;

            if (!string.IsNullOrEmpty(element.Text))
            {
                textSize = ChatManager.GetStringSize(FontAssets.MouseText.Value, element.Text, textScale);
            }

            // Calculate total height
            float barTotalH = 0f;
            if (element.BarProgress.HasValue)
            {
                if (element.UseHealthBarStyle)
                {
                    barTotalH = element.BarThickness; // Health bar includes border in texture
                }
                else
                {
                    barTotalH = element.BarThickness + element.BorderThickness * 2f;
                }
            }

            float textTotalH = !string.IsNullOrEmpty(element.Text) ? textSize.Y : 0f;
            totalHeight = barTotalH + textTotalH;
            if (element.BarProgress.HasValue)
            {
                if (element.UseHealthBarStyle)
                {
                    DrawHealthBar(bottomCenterPos, element);
                }
                else
                {
                    DrawSimpleBar(bottomCenterPos, element, barTotalH);
                }
            }

            if (!string.IsNullOrEmpty(element.Text))
            {
                // Text sits above bar with configurable gap
                float barTop = bottomCenterPos.Y - (element.BarProgress.HasValue ? barTotalH / 2f : 0f);
                // "Hardcode it down" as requested - applying a positive offset to push text closer to/overlapping bar
                float hardcodedOffset = 12f; 
                float textY = barTop - textTotalH - element.TextGap + hardcodedOffset;
                
                Vector2 textPos = new Vector2(
                    bottomCenterPos.X - textSize.X / 2f,
                    textY
                );
                
                ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value, element.Text, textPos, element.TextColor, 0f, Vector2.Zero, textScale);
            }

            return totalHeight;
        }

        // Draw Terraria-style health bar using Hb1/Hb2 textures with optional custom color
        private void DrawHealthBar(Vector2 bottomCenterPos, PlayerHudElement element)
        {
            float healthPercent = MathHelper.Clamp(element.BarProgress.Value, 0f, 1f);
            
            // Support custom bar width (default 36 for Terraria bar)
            float baseBarWidth = element.BarWidth;
            float barWidth = baseBarWidth * element.FontSize;
            float barThickness = element.BarThickness;
            // Center Render position
            float barX = bottomCenterPos.X - barWidth / 2f;
            float barY = bottomCenterPos.Y - barThickness / 2f;

            // Use custom bar color directly if BarColor was explicitly set
            // Otherwise, use Terraria's health-based color gradient
            Color barColor = element.BarColor;
            
            // Only apply gradient if using default green color (not custom)
            if (element.BarColor == Color.Green)
            {
                // Calculate color using Terraria's logic
                float colorHealth = healthPercent - 0.1f;
                float red = 0f, green = 0f;
                
                if (colorHealth > 0.5f)
                {
                    green = 255f;
                    red = 255f * (1f - colorHealth) * 2f;
                }
                else if (colorHealth > 0f)
                {
                    green = 255f * colorHealth * 2f;
                    red = 255f;
                }
                else
                {
                    red = 255f;
                }

                float alpha = 0.95f;
                red = MathHelper.Clamp(red * alpha, 0f, 255f);
                green = MathHelper.Clamp(green * alpha, 0f, 255f);

                barColor = new Color((byte)red, (byte)green, 0, 255);
            }

            // Draw using textures if available
            if (TextureAssets.Hb1.IsLoaded && TextureAssets.Hb2.IsLoaded)
            {
                // Draw Background (Hb2) - Stretched to full width
                // We use destination rectangles to handle the stretching of custom widths
                Rectangle bgDest = new Rectangle((int)barX, (int)barY, (int)barWidth, (int)barThickness);
                Main.spriteBatch.Draw(TextureAssets.Hb2.Value, bgDest, null, barColor, 0f, Vector2.Zero, SpriteEffects.None, 0f);

                // Draw Fill (Hb1) - Stretched proportionally
                // We calculate source width based on percentage to maintain texture consistency (distorted but consistent)
                if (healthPercent > 0f)
                {
                    int fillDestWidth = (int)(barWidth * healthPercent);
                    int fillSourceWidth = (int)(TextureAssets.Hb1.Width() * healthPercent);
                    
                    if (fillDestWidth > 0 && fillSourceWidth > 0)
                    {
                        Rectangle fillDest = new Rectangle((int)barX, (int)barY, fillDestWidth, (int)barThickness);
                        Rectangle fillSource = new Rectangle(0, 0, fillSourceWidth, TextureAssets.Hb1.Height());
                        
                        Main.spriteBatch.Draw(TextureAssets.Hb1.Value, fillDest, fillSource, barColor, 0f, Vector2.Zero, SpriteEffects.None, 0f);
                    }
                }
            }
            else
            {
                // Fallback: draw simple rectangles
                DrawSimpleBar(bottomCenterPos, element, element.BarThickness);
            }
        }

        // Draw simple rectangular bar
        private void DrawSimpleBar(Vector2 bottomCenterPos, PlayerHudElement element, float barTotalH)
        {
            float barWidth = element.BarWidth;
            float barThickness = element.BarThickness;

            Vector2 barCenter = bottomCenterPos - new Vector2(0, barTotalH / 2f);
            
            // Border Rect
            int borderW = (int)(barWidth + element.BorderThickness * 2f);
            int borderH = (int)(barThickness + element.BorderThickness * 2f);
            Rectangle borderRect = new Rectangle(
                (int)(barCenter.X - borderW / 2f),
                (int)(barCenter.Y - borderH / 2f),
                borderW,
                borderH
            );

            // Draw Border
            if (element.BorderThickness > 0)
            {
                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, borderRect, element.BorderColor);
            }

            // Inner Rect
            Rectangle innerRect = new Rectangle(
                (int)(barCenter.X - barWidth / 2f),
                (int)(barCenter.Y - barThickness / 2f),
                (int)barWidth,
                (int)barThickness
            );

            // Draw Background (Unfilled)
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, innerRect, element.UnfilledBarColor);

            // Draw Fill
            int fillWidth = (int)(barWidth * MathHelper.Clamp(element.BarProgress.Value, 0f, 1f));
            if (fillWidth > 0)
            {
                Rectangle fillRect = new Rectangle(innerRect.X, innerRect.Y, fillWidth, innerRect.Height);
                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, fillRect, element.BarColor);
            }
        }
    }
}
