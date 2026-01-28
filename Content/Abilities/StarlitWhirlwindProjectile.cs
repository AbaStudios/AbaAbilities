using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Luminance.Core.Graphics;
using Luminance.Assets;
using AbaAbilities.Common;

namespace AbaAbilities.Content.Abilities
{
    public class StarlitWhirlwindProjectile : BaseOrbitingProjectile, IPixelatedPrimitiveRenderer
    {
        private float _fireTimer;

        public float DamageAbsorption
        {
            get => Projectile.ai[2];
            set => Projectile.ai[2] = value;
        }

        public override float AbilityCapacityCost => 0.33f;
        public override float OrbitRadius => 50f;
        public override float OrbitSpeed => 0.05f;
        public override Color GlowColor => Color.Gold;
        public override float GlowIntensity => 0.6f;

        public PixelationPrimitiveLayer LayerToRenderTo => PixelationPrimitiveLayer.BeforeProjectiles;

        public override string Texture => "Terraria/Images/Projectile_12";

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 15;
        }

        public override void SetProjectileDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 24;
            Projectile.damage = 100;
            DamageAbsorption = 20f;
        }

        protected override void FiredAI(Player owner)
        {
            Projectile.tileCollide = true;
            _fireTimer++;

            if (_fireTimer <= 0f)
            {
                Projectile.rotation += 0.4f;
                return;
            }

            if (_fireTimer == 1f)
                Projectile.timeLeft = 90;

            NPC target = Projectile.FindTargetWithinRange(600f);
            if (target != null)
            {
                Vector2 dir = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                float speed = 30f;
                float inertia = 15f;
                Projectile.velocity = (Projectile.velocity * (inertia - 1) + dir * speed) / inertia;
            }

            Projectile.rotation += 0.4f;
        }

        protected override void OnFire(Vector2 target, float distance)
        {
            float speed = GetFireSpeed();
            _fireTimer = -(distance / speed);
        }

        protected override void DrawProjectile(Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[ProjectileID.FallingStar].Value;
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
            Color paleGold = Color.Lerp(Color.Gold, Color.White, 0.5f);
            return Color.Lerp(paleGold, Color.Transparent, completionRatio) * 0.6f;
        }
    }
}
