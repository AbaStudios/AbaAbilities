using System;
using System.Collections.Generic;
using Luminance.Assets;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AbaAbilities.Content.Projectiles
{
    public class CelestialCollapseChargeProj : ModProjectile
    {
        public override string Texture => MiscTexturesRegistry.InvisiblePixelPath;

        public Player Owner => Main.player[Projectile.owner];
        public ref float Time => ref Projectile.ai[0];

        private readonly List<Vector2>[] _spiralArms = new List<Vector2>[3];
        private float _pulsePhase;

        public override void SetDefaults()
        {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
        }

        public override void AI()
        {
            Projectile.Center = Owner.Center;
            Time++;
            _pulsePhase += 0.15f;

            float chargeProgress = Math.Min(Time / 90f, 1f);
            Vector2 toMouse = Main.MouseWorld - Owner.Center;
            float mouseAngle = toMouse.ToRotation();

            for (int i = 0; i < 3; i++)
            {
                _spiralArms[i] ??= new List<Vector2>();

                float armAngle = mouseAngle + i * MathHelper.TwoPi / 3f + Time * 0.08f;
                float spiralRadius = MathHelper.Lerp(80f, 30f, chargeProgress) * (1f + 0.1f * (float)Math.Sin(_pulsePhase + i));
                Vector2 armPos = Owner.Center + armAngle.ToRotationVector2() * spiralRadius;

                _spiralArms[i].Add(armPos);
                if (_spiralArms[i].Count > 15)
                    _spiralArms[i].RemoveAt(0);
            }

            if (Main.netMode != NetmodeID.Server && chargeProgress > 0.2f)
            {
                int dustType = Main.rand.NextBool() ? DustID.PurpleTorch : DustID.Shadowflame;
                Vector2 dustPos = Main.MouseWorld + Main.rand.NextVector2Circular(40f, 40f) * (1f - chargeProgress);
                Vector2 dustVel = (Main.MouseWorld - dustPos).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(2f, 5f);
                Dust d = Dust.NewDustPerfect(dustPos, dustType, dustVel, 100, default, 1.2f * chargeProgress);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server)
                return false;

            float chargeProgress = Math.Min(Time / 90f, 1f);
            ManagedShader shader = ShaderManager.GetShader("Luminance.StandardPrimitiveShader");

            for (int i = 0; i < 3; i++)
            {
                if (_spiralArms[i] == null || _spiralArms[i].Count < 2)
                    continue;

                Color armColor = Color.Lerp(new Color(138, 43, 226), new Color(75, 0, 130), i / 3f);
                PrimitiveSettings settings = new(
                    progress => MathHelper.Lerp(8f, 2f, progress) * chargeProgress,
                    progress => armColor * (1f - progress) * chargeProgress * 0.8f,
                    Shader: shader
                );
                PrimitiveRenderer.RenderTrail(_spiralArms[i], settings);
            }

            if (chargeProgress > 0.1f)
            {
                Texture2D bloom = MiscTexturesRegistry.BloomCircleSmall.Value;
                float pulseScale = 0.4f + 0.15f * (float)Math.Sin(_pulsePhase * 2f);
                float scale = pulseScale * chargeProgress;
                Color bloomColor = new Color(148, 0, 211, 0) * chargeProgress * 0.6f;

                Main.spriteBatch.Draw(bloom, Owner.Center - Main.screenPosition, null, bloomColor, 0f, bloom.Size() * 0.5f, scale, 0, 0f);
                Main.spriteBatch.Draw(bloom, Owner.Center - Main.screenPosition, null, Color.White * chargeProgress * 0.3f, 0f, bloom.Size() * 0.5f, scale * 0.5f, 0, 0f);

                if (chargeProgress > 0.3f)
                {
                    Vector2 cursorPos = Main.MouseWorld - Main.screenPosition;
                    float cursorScale = 0.3f * chargeProgress;
                    Color cursorBloom = new Color(75, 0, 130, 0) * chargeProgress * 0.5f;
                    Main.spriteBatch.Draw(bloom, cursorPos, null, cursorBloom, 0f, bloom.Size() * 0.5f, cursorScale, 0, 0f);
                }
            }

            return false;
        }
    }

    public class CelestialCollapseRiftProj : ModProjectile
    {
        public override string Texture => MiscTexturesRegistry.InvisiblePixelPath;

        public ref float Time => ref Projectile.ai[0];
        public float Lifetime => Projectile.ai[1] > 0 ? Projectile.ai[1] : 180f;
        public float RiftScale => Projectile.ai[2] > 0 ? Projectile.ai[2] : 1f;

        private const float BaseRadius = 80f;
        private const float SuctionRadius = 350f;

        private float _rotationOffset;
        private float _pulsePhase;
        private readonly List<RiftEnergyOrb> _orbs = new();

        private struct RiftEnergyOrb
        {
            public float Angle;
            public float Distance;
            public float Speed;
            public float Size;
            public Color OrbColor;
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 800;
        }

        public override void SetDefaults()
        {
            Projectile.width = 160;
            Projectile.height = 160;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 999;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }

        public override void AI()
        {
            if (Lifetime > 0 && Time >= Lifetime)
            {
                PerformImplosion();
                Projectile.Kill();
                return;
            }

            Time++;
            _rotationOffset += 0.03f;
            _pulsePhase += 0.1f;

            float lifeRatio = Time / Lifetime;
            float suctionStrength = lifeRatio < 0.1f ? lifeRatio / 0.1f : (lifeRatio > 0.85f ? (1f - lifeRatio) / 0.15f : 1f);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                foreach (NPC npc in Main.npc)
                {
                    if (!npc.active || npc.friendly || npc.dontTakeDamage)
                        continue;

                    float dist = Vector2.Distance(npc.Center, Projectile.Center);
                    if (dist < SuctionRadius * RiftScale && dist > 20f)
                    {
                        Vector2 pullDir = (Projectile.Center - npc.Center).SafeNormalize(Vector2.Zero);
                        float forceFactor = (1f - dist / (SuctionRadius * RiftScale)) * suctionStrength;
                        npc.velocity += pullDir * forceFactor * 2f;
                        npc.velocity *= 0.96f;
                    }
                }
            }

            Projectile.scale = RiftScale * GetScaleModifier(lifeRatio);

            if (Main.netMode != NetmodeID.Server)
            {
                SpawnAmbientEffects(lifeRatio, suctionStrength);
                UpdateOrbs();
            }
        }

        private float GetScaleModifier(float lifeRatio)
        {
            if (lifeRatio < 0.1f)
                return (float)Math.Pow(lifeRatio / 0.1f, 0.5f);
            if (lifeRatio > 0.9f)
                return 1f + (lifeRatio - 0.9f) / 0.1f * 0.3f;
            return 1f + 0.05f * (float)Math.Sin(_pulsePhase);
        }

        private void SpawnAmbientEffects(float lifeRatio, float suctionStrength)
        {
            if (Main.rand.NextBool(3) && suctionStrength > 0.3f && _orbs.Count < 50)
            {
                float spawnAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                float spawnDist = Main.rand.NextFloat(200f, 350f) * RiftScale;
                Vector2 spawnPos = Projectile.Center + spawnAngle.ToRotationVector2() * spawnDist;

                _orbs.Add(new RiftEnergyOrb
                {
                    Angle = spawnAngle,
                    Distance = spawnDist,
                    Speed = Main.rand.NextFloat(3f, 7f),
                    Size = Main.rand.NextFloat(0.3f, 0.8f),
                    OrbColor = Main.rand.NextBool() ? new Color(148, 0, 211) : new Color(75, 0, 130)
                });
            }

            if (Main.rand.NextBool(4))
            {
                float edgeAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 edgePos = Projectile.Center + edgeAngle.ToRotationVector2() * BaseRadius * Projectile.scale * 0.9f;
                int dustType = Main.rand.NextBool() ? DustID.Shadowflame : DustID.PurpleTorch;
                Dust d = Dust.NewDustPerfect(edgePos, dustType, edgeAngle.ToRotationVector2() * Main.rand.NextFloat(1f, 3f), 0, default, 1.5f);
                d.noGravity = true;
            }
        }

        private void UpdateOrbs()
        {
            for (int i = _orbs.Count - 1; i >= 0; i--)
            {
                var orb = _orbs[i];
                orb.Distance -= orb.Speed;
                orb.Angle += 0.02f + (1f - orb.Distance / 350f) * 0.08f;
                _orbs[i] = orb;

                if (orb.Distance < BaseRadius * Projectile.scale * 0.5f)
                    _orbs.RemoveAt(i);
            }
        }

        private void PerformImplosion()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            for (int i = 0; i < 40; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float speed = Main.rand.NextFloat(8f, 20f);
                Vector2 vel = angle.ToRotationVector2() * speed;
                int dustType = Main.rand.NextBool() ? DustID.Shadowflame : DustID.PurpleTorch;
                Dust d = Dust.NewDustPerfect(Projectile.Center, dustType, vel, 0, default, Main.rand.NextFloat(1.5f, 2.5f));
                d.noGravity = true;
            }

            for (int i = 0; i < 20; i++)
            {
                float angle = i * MathHelper.TwoPi / 20f;
                Vector2 vel = angle.ToRotationVector2() * 12f;
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Vortex, vel, 0, default, 2f);
                d.noGravity = true;
            }

            float flashIntensity = 15f * RiftScale;
            Lighting.AddLight(Projectile.Center, new Vector3(0.5f, 0.2f, 0.8f) * flashIntensity);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server)
                return false;

            DrawRiftCore();
            DrawOrbs();
            DrawOuterHalo();

            return false;
        }

        private void DrawRiftCore()
        {
            float radius = BaseRadius * Projectile.scale;
            float lifeRatio = Time / Lifetime;
            float coreOpacity = lifeRatio < 0.1f ? lifeRatio / 0.1f : (lifeRatio > 0.9f ? (1f - lifeRatio) / 0.1f : 1f);

            PrimitiveSettingsCircle voidSettings = new(
                _ => radius * 0.6f,
                _ => Color.Black * coreOpacity
            );
            PrimitiveRenderer.RenderCircle(Projectile.Center, voidSettings, 64);

            Texture2D bloom = MiscTexturesRegistry.BloomCircleSmall.Value;
            float bloomScale = radius * 2.5f / bloom.Width;
            Color innerGlow = new Color(30, 0, 50, 0) * coreOpacity * 0.8f;
            Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, innerGlow, 0f, bloom.Size() * 0.5f, bloomScale, 0, 0f);

            for (int layer = 0; layer < 3; layer++)
            {
                float layerRadius = radius * (0.7f + layer * 0.15f);
                float layerWidth = 6f - layer * 1.5f;
                Color layerColor = layer switch
                {
                    0 => new Color(148, 0, 211) * 0.9f,
                    1 => new Color(138, 43, 226) * 0.7f,
                    _ => new Color(75, 0, 130) * 0.5f
                };

                PrimitiveSettingsCircleEdge edgeSettings = new(
                    _ => layerWidth * (1f + 0.2f * (float)Math.Sin(_pulsePhase + layer)),
                    progress => layerColor * coreOpacity * (0.8f + 0.2f * (float)Math.Sin(progress * MathHelper.TwoPi * 3f + _rotationOffset * (layer + 1))),
                    _ => layerRadius
                );
                PrimitiveRenderer.RenderCircleEdge(Projectile.Center, edgeSettings, 100);
            }

            Color outerBloom = new Color(148, 0, 211, 0) * coreOpacity * 0.4f;
            Main.spriteBatch.Draw(bloom, Projectile.Center - Main.screenPosition, null, outerBloom, 0f, bloom.Size() * 0.5f, bloomScale * 1.5f, 0, 0f);
        }

        private void DrawOrbs()
        {
            Texture2D bloom = MiscTexturesRegistry.BloomCircleSmall.Value;

            foreach (var orb in _orbs)
            {
                Vector2 orbPos = Projectile.Center + orb.Angle.ToRotationVector2() * orb.Distance;
                float distFactor = 1f - orb.Distance / 350f;
                float orbOpacity = distFactor * 0.8f;
                float orbScale = orb.Size * (0.5f + distFactor * 0.5f) * 0.15f;

                Color orbColor = orb.OrbColor;
                orbColor.A = 0;

                Main.spriteBatch.Draw(bloom, orbPos - Main.screenPosition, null, orbColor * orbOpacity, 0f, bloom.Size() * 0.5f, orbScale, 0, 0f);
                Main.spriteBatch.Draw(bloom, orbPos - Main.screenPosition, null, Color.White * orbOpacity * 0.5f, 0f, bloom.Size() * 0.5f, orbScale * 0.4f, 0, 0f);
            }
        }

        private void DrawOuterHalo()
        {
            float radius = BaseRadius * Projectile.scale;
            float lifeRatio = Time / Lifetime;
            float haloOpacity = (lifeRatio < 0.1f ? lifeRatio / 0.1f : (lifeRatio > 0.9f ? (1f - lifeRatio) / 0.1f : 1f)) * 0.3f;

            int rayCount = 8;
            Texture2D bloom = MiscTexturesRegistry.BloomCircleSmall.Value;

            for (int i = 0; i < rayCount; i++)
            {
                float rayAngle = i * MathHelper.TwoPi / rayCount + _rotationOffset * 0.5f;
                float rayPulse = 1f + 0.3f * (float)Math.Sin(_pulsePhase + i * 0.5f);
                float rayLength = radius * 1.8f * rayPulse;
                Vector2 rayEnd = Projectile.Center + rayAngle.ToRotationVector2() * rayLength;

                Color rayColor = new Color(138, 43, 226, 0) * haloOpacity;
                float rayScale = 0.08f * rayPulse;

                Main.spriteBatch.Draw(bloom, rayEnd - Main.screenPosition, null, rayColor, 0f, bloom.Size() * 0.5f, rayScale, 0, 0f);
            }

            Texture2D chromatic = MiscTexturesRegistry.ChromaticBurst.Value;
            float chromaticScale = radius * 3f / chromatic.Width;
            float chromaticOpacity = haloOpacity * (0.3f + 0.1f * (float)Math.Sin(_pulsePhase * 0.5f));
            Color chromaticColor = new Color(148, 0, 211, 0) * chromaticOpacity;

            Main.spriteBatch.Draw(chromatic, Projectile.Center - Main.screenPosition, null, chromaticColor, _rotationOffset, chromatic.Size() * 0.5f, chromaticScale, 0, 0f);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            PerformImplosion();
        }
    }
}
