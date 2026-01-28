using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace AbaAbilities.Content.Projectiles.Hyperion
{
    public class HyperionTrail : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_1"; // Using invisible texture would be better but this is fine since PreDraw returns false

        public Vector2 Start => Projectile.Center;
        public Vector2 End => new Vector2(Projectile.ai[0], Projectile.ai[1]);

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 30;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; 
        }

        public override void AI()
        {
            // Just exist for damage and visual reference
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.timeLeft < 25) return false; // Only damage in the first few frames
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Start, End, 40f, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}
