using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace AbaAbilities.Content.Projectiles.Hyperion
{
    public class HyperionWarpSystem : ModSystem
    {
        public static ManagedRenderTarget DistortionTarget { get; private set; }

        public override void Load()
        {
            if (Main.dedServ) return;
            DistortionTarget = new ManagedRenderTarget(true, ManagedRenderTarget.CreateScreenSizedTarget);
            ScreenModifierManager.RegisterScreenModifier(DrawWarpEffects, ScreenModifierManager.FilterLayer - 10);
        }

        private void DrawWarpEffects(RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
        {
            if (DistortionTarget == null || DistortionTarget.IsUninitialized) return;

            // 1. Draw Trails to DistortionTarget
            // This defines the "Map" of where distortion occurs.
            
            var device = Main.graphics.GraphicsDevice;
            
            // Ideally we check if there ARE any trails before switching targets to save perf
            bool hasWarpEffects = false;
            foreach (var proj in Main.ActiveProjectiles)
            {
                if (proj.active && (proj.type == ModContent.ProjectileType<HyperionTrail>() || proj.type == ModContent.ProjectileType<HyperionExplosion>()))
                {
                    hasWarpEffects = true;
                    break;
                }
            }
            if (!hasWarpEffects) return;

            device.SetRenderTarget(DistortionTarget.Target);
            device.Clear(Color.Transparent);
            
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
            
            foreach (var proj in Main.ActiveProjectiles)
            {
                if (proj.active)
                {
                    if (proj.type == ModContent.ProjectileType<HyperionTrail>())
                    {
                        HyperionTrail trail = proj.ModProjectile as HyperionTrail;
                        if (trail != null && proj.timeLeft > 0)
                        {
                            // Draw line from Start to End
                            // Color acts as data for the shader. Red/Alpha = Intensity.
                            float intensity = proj.timeLeft / 30f;
                            Color dataColor = new Color(intensity, 0f, 0f, 1f); 
                            Utils.DrawLine(Main.spriteBatch, trail.Start - Main.screenPosition, trail.End - Main.screenPosition, dataColor, dataColor, 60f * intensity);
                        }
                    }
                    else if (proj.type == ModContent.ProjectileType<HyperionExplosion>())
                    {
                        HyperionExplosion explosion = proj.ModProjectile as HyperionExplosion;
                        if (explosion != null)
                        {
                            Texture2D tex = ModContent.Request<Texture2D>(explosion.Texture).Value;
                            float scale = explosion.Radius / (tex.Width / 2f);
                            float opacity = 1f - (explosion.Timer / 40f);
                            Main.EntitySpriteDraw(tex, explosion.Projectile.Center - Main.screenPosition, null, new Color(opacity, 0f, 0f, 1f), 0f, tex.Size() / 2f, scale, SpriteEffects.None, 0);
                        }
                    }
                }
            }
            
            Main.spriteBatch.End();
            
            // 2. Apply Shader to Screen
            // Source: screenTarget1 (Game World)
            // Dest: screenTarget2 (Temp)
            
            RenderTarget2D source = screenTarget1;
            RenderTarget2D destination = screenTarget2; 
            
            device.SetRenderTarget(destination);
            device.Clear(clearColor);
            
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            
            // Setup Shader
            if (ShaderManager.TryGetShader("AbaAbilities.HyperionWarpShader", out ManagedShader shader))
            {
                shader.TrySetParameter("intensity", 5f);
                shader.TrySetParameter("uScreenSize", new Vector2(Main.screenWidth, Main.screenHeight));
                shader.SetTexture(DistortionTarget.Target, 1); // uImage1
                shader.Apply();
            }
            
            Main.spriteBatch.Draw(source, Vector2.Zero, Color.White);
            
            Main.spriteBatch.End();
            
            // 3. Copy Result back to screenTarget1
            
            device.SetRenderTarget(source);
            device.Clear(Color.Transparent); // Should allow alpha if any? Usually world is opaque.
            
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);
            Main.spriteBatch.Draw(destination, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
        }
    }
}
