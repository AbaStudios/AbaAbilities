using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Luminance.Core.Graphics;
using Luminance.Assets;

namespace AbaAbilities.Content.Abilities.StarlitWhirlwind
{
    public class StarlitWhirlwindProjectile : ModProjectile, IPixelatedPrimitiveRenderer
    {
        // ai[0] = State
        // 0: Orbit
        // 1: Fire

        // ai[1] = Timer / Secondary Helper
        
        public PixelationPrimitiveLayer LayerToRenderTo => PixelationPrimitiveLayer.BeforeProjectiles;

        public override string Texture => "Terraria/Images/Projectile_12";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 15;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 24;
            Projectile.damage = 100;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 72000; // Lasts long while orbiting
            Projectile.penetrate = -1; // Infinite penetrate while orbiting, change on fire?
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.ai[0] == 0f) // Orbit State
            {
                OrbitBehavior(owner);
            }
            else // Fire State
            {
                FireBehavior();
            }

            UpdateVisuals();
        }

        private void OrbitBehavior(Player owner)
        {
            Projectile.timeLeft = 2; // Keep alive
            Projectile.tileCollide = false;
            
            // Circular orbit math
            float orbitRadius = 80f;
            float orbitSpeed = 0.05f;

            // Use ai[1] as the angle accumulator
            Projectile.ai[1] += orbitSpeed;
            float currentAngle = Projectile.ai[1];

            Vector2 offset = currentAngle.ToRotationVector2() * orbitRadius;
            Projectile.Center = owner.Center + offset;
            
            // Rotation spins
            Projectile.rotation += 0.2f;
        }

        private void FireBehavior()
        {
            Projectile.tileCollide = true;
            Projectile.timeLeft = 180; // Give it some time to live
            
            // "Fly real quick towards the cursor then home on enemies"
            // The cursor logic was handled in Ability when switching state (setting velocity)
            // Just add homing
            
            Projectile.ai[1]++; // Timer
            
            float homingDelay = 5f; 

            if (Projectile.ai[1] > homingDelay)
            {
                NPC target = Projectile.FindTargetWithinRange(600f);
                if (target != null)
                {
                    Vector2 dir = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                    float speed = 30f;
                    float inertia = 15f;
                    Projectile.velocity = (Projectile.velocity * (inertia - 1) + dir * speed) / inertia;
                }
            }

            Projectile.rotation += 0.4f;
        }

        public override bool? CanHitNPC(NPC target)
        {
            // Only hit in Fire state
            if (Projectile.ai[0] == 0f)
                return false;
            return null;
        }

        private void UpdateVisuals()
        {
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.YellowStarDust);
                d.noGravity = true;
                d.velocity *= 0.5f;
            }

            if (Projectile.ai[0] == 1f)
                Lighting.AddLight(Projectile.Center, Color.Yellow.ToVector3() * 0.8f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[ProjectileID.FallingStar].Value; // Use vanilla fallen star
            Rectangle frame = texture.Frame(1, 1, 0, 0);
            Vector2 origin = frame.Size() / 2f;
            
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                frame,
                Color.White,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0
            );
            
            return false;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
            if (Projectile.oldPos[0] == Vector2.Zero)
                return;

            ManagedShader shader = ShaderManager.GetShader("Luminance.StandardPrimitiveShader");
            
            PrimitiveSettings settings = new PrimitiveSettings(
                WidthFunction,
                ColorFunction,
                _ => Projectile.Size * 0.5f,
                Pixelate: true,
                Shader: shader
            );

            PrimitiveRenderer.RenderTrail(Projectile.oldPos, settings, 15);
        }

        private float WidthFunction(float completionRatio)
        {
            return MathHelper.Lerp(16f, 2f, completionRatio);
        }

        private Color ColorFunction(float completionRatio)
        {
            return Color.Lerp(Color.Gold, Color.Transparent, completionRatio) * 0.8f;   
        }
    }
}
