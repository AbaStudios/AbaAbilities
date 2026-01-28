using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AbaAbilities.Content.Projectiles.Hyperion
{
    public class HyperionExplosion : ModProjectile
    {
        public override string Texture => "AbaAbilities/Assets/Projectiles/Hyperion/ExplosionBloom";

        public ref float Radius => ref Projectile.ai[0];
        public ref float Timer => ref Projectile.ai[1];

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 40;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Timer == 0)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    ScreenShakeSystem.StartShakeAtPoint(Projectile.Center, 15f);
                    // Use a heavy explosion sound
                    Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.Item14, Projectile.Center); 
                }
                Radius = 10f;
            }

            Timer++;
            Radius = MathHelper.Lerp(10f, 300f, Utils.GetLerpValue(0f, 40f, Timer, true));
            
            // Resize hitbox for damage
            Projectile.Resize((int)Radius * 2, (int)Radius * 2);

            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 5; i++)
                {
                    Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(Radius, Radius);
                    Dust d = Dust.NewDustPerfect(pos, 27, Vector2.Zero, 0, default, 2f);
                    d.noGravity = true;
                    d.velocity = (pos - Projectile.Center).SafeNormalize(Vector2.Zero) * 4f;
                }
            }
        }
        
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Vector2.Distance(Projectile.Center, targetHitbox.ClosestPointInRect(Projectile.Center)) < Radius;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            float opacity = 1f - (Timer / 40f);
            float scale = Radius / (tex.Width / 2f);
            
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, new Color(255, 100, 255) * opacity, 0f, tex.Size() / 2f, scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
