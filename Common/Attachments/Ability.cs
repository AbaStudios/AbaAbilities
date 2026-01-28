using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using AbaAbilities.Core;

namespace AbaAbilities.Common.Attachments
{
    /// <summary>
    /// Base class for abilities that can be attached to players or items.
    /// </summary>
    public abstract class Ability
    {
        public Player Player { get; internal set; }
        public ActiveAttachment? CurrentActiveAttachment { get; internal set; }
        public IReadOnlyList<ActiveAttachment> AllAttachments { get; internal set; } = System.Array.Empty<ActiveAttachment>();
        internal HookMask TypeHookMask { get; set; }
        internal bool Activated { get; set; }

        public virtual string Id => $"{GetType().Assembly.GetName().Name}:{GetType().Name}";

        public bool HasActiveAttachment => CurrentActiveAttachment.HasValue;
        public bool IsActive => Activated;

        /// <summary>
        /// These are client-only. The rest run on local, server, and remote clients.
        /// </summary>
        public virtual IEnumerable<string> DefineTooltips(bool isShiftHeld) => null;
        public virtual void ProcessTriggers(TriggersSet triggersSet) { }
        public virtual void SetControls() { }
        public virtual void FrameEffects() { }
        public virtual void MeleeEffects(Item item, Rectangle hitbox) { }

        public virtual void OnActivate() { }
        public virtual void OnDeactivate() { }
        public virtual bool CanActivate(TagCompound data) => true;
        public virtual bool CanDeactivate() => true;

        public virtual void OnAttackKeyDown() { }
        public virtual void WhileAttackKeyDown(int ticksHeld) { }
        public virtual void OnAttackKeyUp(int ticksHeld) { }
        public virtual void OnActivateKeyDown() { }
        public virtual void WhileActivateKeyDown(int ticksHeld) { }
        public virtual void OnActivateKeyUp(int ticksHeld) { }
        public virtual void ResetEffects() { }
        public virtual void UpdateDead() { }
        public virtual void PreUpdate() { }

        public virtual void PreUpdateBuffs() { }
        public virtual void PostUpdateBuffs() { }
        public virtual void UpdateEquips() { }
        public virtual void PostUpdateEquips() { }
        public virtual void PostUpdateMiscEffects() { }
        public virtual void PostUpdateRunSpeeds() { }
        public virtual void PreUpdateMovement() { }
        public virtual void PostUpdate() { }
        public virtual void UpdateLifeRegen() { }
        public virtual void UpdateBadLifeRegen() { }
        public virtual void NaturalLifeRegen(ref float regen) { }

        public virtual bool FreeDodge(Player.HurtInfo info) => false;
        public virtual bool ConsumableDodge(Player.HurtInfo info) => false;
        public virtual void ModifyHurt(ref Player.HurtModifiers modifiers) { }
        public virtual void OnHurt(Player.HurtInfo info) { }
        public virtual void PostHurt(Player.HurtInfo info) { }
        public virtual bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource) => true;
        public virtual void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource) { }

        public virtual void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers) { }
        public virtual void OnHitByNPC(NPC npc, Player.HurtInfo hurtInfo) { }
        public virtual void ModifyHitByProjectile(Projectile proj, ref Player.HurtModifiers modifiers) { }
        public virtual void OnHitByProjectile(Projectile proj, Player.HurtInfo hurtInfo) { }

        public virtual bool CanHitNPC(NPC target) => true;
        public virtual void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) { }
        public virtual void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) { }

        public virtual bool? CanHitNPCWithItem(Item item, NPC target) => null;
        public virtual void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers) { }
        public virtual void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone) { }

        public virtual bool? CanHitNPCWithProj(Projectile proj, NPC target) => null;
        public virtual void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers) { }
        public virtual void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone) { }

        public virtual bool CanHitPvp(Item item, Player target) => true;
        public virtual bool CanHitPvpWithProj(Projectile proj, Player target) => true;

        public virtual void OnHitAnything(float x, float y, Entity victim) { }

        public virtual bool PreItemCheck() => true;
        public virtual void PostItemCheck() { }
        public virtual float UseTimeMultiplier(Item item) => 1f;
        public virtual float UseAnimationMultiplier(Item item) => 1f;
        public virtual float UseSpeedMultiplier(Item item) => 1f;

        public virtual bool CanConsumeAmmo(Item weapon, Item ammo) => true;
        public virtual void OnConsumeAmmo(Item weapon, Item ammo) { }

        public virtual bool CanShoot(Item item) => true;
        public virtual void ModifyShootStats(Item item, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback) { }
        public virtual bool Shoot(Item item, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) => true;

        public virtual void ModifyWeaponDamage(Item item, ref StatModifier damage) { }
        public virtual void ModifyWeaponKnockback(Item item, ref StatModifier knockback) { }
        public virtual void ModifyWeaponCrit(Item item, ref float crit) { }

        public virtual void GetHealLife(Item item, bool quickHeal, ref int healValue) { }
        public virtual void GetHealMana(Item item, bool quickHeal, ref int healValue) { }
        public virtual void ModifyManaCost(Item item, ref float reduce, ref float mult) { }
        public virtual void OnMissingMana(Item item, int neededMana) { }
        public virtual void OnConsumeMana(Item item, int manaConsumed) { }

        public virtual void ModifyMaxStats(out StatModifier health, out StatModifier mana)
        {
            health = StatModifier.Default;
            mana = StatModifier.Default;
        }
    }
}