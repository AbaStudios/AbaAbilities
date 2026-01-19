using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using AbaAbilities.Common.Attachments;

namespace AbaAbilities.Core
{
    public class PlayerStore : ModPlayer
    {
        public AbilityDispatcher Dispatcher { get; private set; }
        public List<Attachment> PlayerAttachments { get; private set; } = new List<Attachment>();
        public uint LastDamageTime { get; private set; }
        private readonly Dictionary<int, List<Attachment>> _itemAttachmentsByUid = new Dictionary<int, List<Attachment>>();

        private int _lastSelectedItem = -1;
        private bool _prevControlUseItem = false;
        private bool _prevControlActivateTile = false;
        private int _attackKeyHeldTicks = 0;
        private int _activateKeyHeldTicks = 0;
        private bool _singletonsInitialized = false;

        public override void Initialize()
        {
            Dispatcher = new AbilityDispatcher(Player);
            _lastSelectedItem = -1;
            _singletonsInitialized = false;
        }

        private void EnsureSingletonsInitialized()
        {
            if (_singletonsInitialized)
                return;
            _singletonsInitialized = true;
            Dispatcher.InitializeSingletons();
        }

        public override void ResetEffects()
        {
            EnsureSingletonsInitialized();
            RefreshActiveAbilities();
            Dispatcher.Dispatch_ResetEffects();
        }

        public override void UpdateDead()
        {
            Dispatcher.Dispatch_UpdateDead();
        }

        public override void PreUpdate()
        {
            Dispatcher.Dispatch_PreUpdate();
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            Dispatcher.Dispatch_ProcessTriggers(triggersSet);
        }

        public override void SetControls()
        {
            Dispatcher.Dispatch_SetControls();
        }

        public override void PreUpdateBuffs()
        {
            Dispatcher.Dispatch_PreUpdateBuffs();
        }

        public override void PostUpdateBuffs()
        {
            Dispatcher.Dispatch_PostUpdateBuffs();
        }

        public override void UpdateEquips()
        {
            Dispatcher.Dispatch_UpdateEquips();
        }

        public override void PostUpdateEquips()
        {
            Dispatcher.Dispatch_PostUpdateEquips();
        }

        public override void PostUpdateMiscEffects()
        {
            Dispatcher.Dispatch_PostUpdateMiscEffects();
        }

        public override void PostUpdateRunSpeeds()
        {
            Dispatcher.Dispatch_PostUpdateRunSpeeds();
        }

        public override void PreUpdateMovement()
        {
            Dispatcher.Dispatch_PreUpdateMovement();
        }

        public override void PostUpdate()
        {
            bool attackKeyDown = Player.controlUseItem;
            bool useKeyDown = Player.controlUseTile;

            if (attackKeyDown)
            {
                if (_attackKeyHeldTicks == 0)
                    Dispatcher.Dispatch_OnAttackKeyDown();
                Dispatcher.Dispatch_WhileAttackKeyDown(_attackKeyHeldTicks);
                _attackKeyHeldTicks++;
            }
            else
            {
                if (_prevControlUseItem && _attackKeyHeldTicks > 0)
                    Dispatcher.Dispatch_OnAttackKeyUp(_attackKeyHeldTicks);
                _attackKeyHeldTicks = 0;
            }

            if (useKeyDown)
            {
                if (_activateKeyHeldTicks == 0)
                    Dispatcher.Dispatch_OnActivateKeyDown();
                Dispatcher.Dispatch_WhileActivateKeyDown(_activateKeyHeldTicks);
                _activateKeyHeldTicks++;
            }
            else
            {
                if (_prevControlActivateTile && _activateKeyHeldTicks > 0)
                    Dispatcher.Dispatch_OnActivateKeyUp(_activateKeyHeldTicks);
                _activateKeyHeldTicks = 0;
            }

            _prevControlUseItem = attackKeyDown;
            _prevControlActivateTile = useKeyDown;

            if (Player.selectedItem != _lastSelectedItem)
            {
                _lastSelectedItem = Player.selectedItem;
                RefreshActiveAbilities();
            }

            Dispatcher.Dispatch_PostUpdate();
            Dispatcher.CheckDeactivations();
        }

        public override void UpdateLifeRegen()
        {
            Dispatcher.Dispatch_UpdateLifeRegen();
        }

        public override void UpdateBadLifeRegen()
        {
            Dispatcher.Dispatch_UpdateBadLifeRegen();
        }

        public override void NaturalLifeRegen(ref float regen)
        {
            Dispatcher.Dispatch_NaturalLifeRegen(ref regen);
        }

        public override void FrameEffects()
        {
            Dispatcher.Dispatch_FrameEffects();
        }

        public override bool FreeDodge(Player.HurtInfo info)
        {
            return Dispatcher.Dispatch_FreeDodge(info);
        }

        public override bool ConsumableDodge(Player.HurtInfo info)
        {
            return Dispatcher.Dispatch_ConsumableDodge(info);
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            Dispatcher.Dispatch_ModifyHurt(ref modifiers);
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            LastDamageTime = Main.GameUpdateCount;
            Dispatcher.Dispatch_OnHurt(info);
        }

        public override void PostHurt(Player.HurtInfo info)
        {
            Dispatcher.Dispatch_PostHurt(info);
        }

        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
        {
            return Dispatcher.Dispatch_PreKill(damage, hitDirection, pvp, ref playSound, ref genDust, ref damageSource);
        }

        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            Dispatcher.Dispatch_Kill(damage, hitDirection, pvp, damageSource);
        }

        public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
        {
            Dispatcher.Dispatch_ModifyHitByNPC(npc, ref modifiers);
        }

        public override void OnHitByNPC(NPC npc, Player.HurtInfo hurtInfo)
        {
            Dispatcher.Dispatch_OnHitByNPC(npc, hurtInfo);
        }

        public override void ModifyHitByProjectile(Projectile proj, ref Player.HurtModifiers modifiers)
        {
            Dispatcher.Dispatch_ModifyHitByProjectile(proj, ref modifiers);
        }

        public override void OnHitByProjectile(Projectile proj, Player.HurtInfo hurtInfo)
        {
            Dispatcher.Dispatch_OnHitByProjectile(proj, hurtInfo);
        }

        public override bool CanHitNPC(NPC target)
        {
            return Dispatcher.Dispatch_CanHitNPC(target);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            Dispatcher.Dispatch_ModifyHitNPC(target, ref modifiers);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Dispatcher.Dispatch_OnHitNPC(target, hit, damageDone);
        }

        public override bool? CanHitNPCWithItem(Item item, NPC target)
        {
            return Dispatcher.Dispatch_CanHitNPCWithItem(item, target);
        }

        public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
        {
            Dispatcher.Dispatch_ModifyHitNPCWithItem(item, target, ref modifiers);
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            Dispatcher.Dispatch_OnHitNPCWithItem(item, target, hit, damageDone);
        }

        public override bool? CanHitNPCWithProj(Projectile proj, NPC target)
        {
            return Dispatcher.Dispatch_CanHitNPCWithProj(proj, target);
        }

        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            Dispatcher.Dispatch_ModifyHitNPCWithProj(proj, target, ref modifiers);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            Dispatcher.Dispatch_OnHitNPCWithProj(proj, target, hit, damageDone);
        }

        public override bool CanHitPvp(Item item, Player target)
        {
            return Dispatcher.Dispatch_CanHitPvp(item, target);
        }

        public override bool CanHitPvpWithProj(Projectile proj, Player target)
        {
            return Dispatcher.Dispatch_CanHitPvpWithProj(proj, target);
        }

        public override void OnHitAnything(float x, float y, Entity victim)
        {
            Dispatcher.Dispatch_OnHitAnything(x, y, victim);
        }

        public override bool PreItemCheck()
        {
            return Dispatcher.Dispatch_PreItemCheck();
        }

        public override void PostItemCheck()
        {
            Dispatcher.Dispatch_PostItemCheck();
        }

        public override float UseTimeMultiplier(Item item)
        {
            return Dispatcher.Dispatch_UseTimeMultiplier(item);
        }

        public override float UseAnimationMultiplier(Item item)
        {
            return Dispatcher.Dispatch_UseAnimationMultiplier(item);
        }

        public override float UseSpeedMultiplier(Item item)
        {
            return Dispatcher.Dispatch_UseSpeedMultiplier(item);
        }

        public override bool CanConsumeAmmo(Item weapon, Item ammo)
        {
            return Dispatcher.Dispatch_CanConsumeAmmo(weapon, ammo);
        }

        public override void OnConsumeAmmo(Item weapon, Item ammo)
        {
            Dispatcher.Dispatch_OnConsumeAmmo(weapon, ammo);
        }

        public override bool CanShoot(Item item)
        {
            return Dispatcher.Dispatch_CanShoot(item);
        }

        public override void ModifyShootStats(Item item, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            Dispatcher.Dispatch_ModifyShootStats(item, ref position, ref velocity, ref type, ref damage, ref knockback);
        }

        public override bool Shoot(Item item, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            return Dispatcher.Dispatch_Shoot(item, source, position, velocity, type, damage, knockback);
        }

        public override void MeleeEffects(Item item, Rectangle hitbox)
        {
            Dispatcher.Dispatch_MeleeEffects(item, hitbox);
        }

        public override void ModifyWeaponDamage(Item item, ref StatModifier damage)
        {
            Dispatcher.Dispatch_ModifyWeaponDamage(item, ref damage);
        }

        public override void ModifyWeaponKnockback(Item item, ref StatModifier knockback)
        {
            Dispatcher.Dispatch_ModifyWeaponKnockback(item, ref knockback);
        }

        public override void ModifyWeaponCrit(Item item, ref float crit)
        {
            Dispatcher.Dispatch_ModifyWeaponCrit(item, ref crit);
        }

        public override void GetHealLife(Item item, bool quickHeal, ref int healValue)
        {
            Dispatcher.Dispatch_GetHealLife(item, quickHeal, ref healValue);
        }

        public override void GetHealMana(Item item, bool quickHeal, ref int healValue)
        {
            Dispatcher.Dispatch_GetHealMana(item, quickHeal, ref healValue);
        }

        public override void ModifyManaCost(Item item, ref float reduce, ref float mult)
        {
            Dispatcher.Dispatch_ModifyManaCost(item, ref reduce, ref mult);
        }

        public override void OnMissingMana(Item item, int neededMana)
        {
            Dispatcher.Dispatch_OnMissingMana(item, neededMana);
        }

        public override void OnConsumeMana(Item item, int manaConsumed)
        {
            Dispatcher.Dispatch_OnConsumeMana(item, manaConsumed);
        }

        public override void ModifyMaxStats(out StatModifier health, out StatModifier mana)
        {
            Dispatcher.Dispatch_ModifyMaxStats(out health, out mana);
        }

        public void RefreshActiveAbilities()
        {
            _itemAttachmentsByUid.Clear();

            ProcessItem(Player.inventory[Player.selectedItem]);

            for (int i = 0; i < 10; i++)
            {
                ProcessItem(Player.armor[i]);
            }

            for (int i = 0; i < 5; i++)
            {
                ProcessItem(Player.miscEquips[i]);
            }

            Dispatcher.RefreshContexts(PlayerAttachments, _itemAttachmentsByUid);
        }

        private void ProcessItem(Item item)
        {
            if (item != null && !item.IsAir)
            {
                var identity = item.GetGlobalItem<ItemIdentity>();
                if (identity.Attachments.Count > 0)
                {
                    identity.EnsureItemUid(item);
                    _itemAttachmentsByUid[identity.ItemUid] = identity.Attachments;
                }
            }
        }

        public override void SaveData(TagCompound tag)
        {
            if (PlayerAttachments.Count > 0)
            {
                var list = new List<TagCompound>();
                foreach (var attachment in PlayerAttachments)
                {
                    var attTag = new TagCompound
                    {
                        ["Id"] = attachment.Id,
                        ["Data"] = attachment.Data
                    };
                    list.Add(attTag);
                }
                tag["PlayerAttachments"] = list;
            }
        }

        public override void LoadData(TagCompound tag)
        {
            if (tag.ContainsKey("PlayerAttachments"))
            {
                var list = tag.GetList<TagCompound>("PlayerAttachments");
                PlayerAttachments.Clear();
                foreach (var attTag in list)
                {
                    var id = attTag.GetString("Id");
                    var data = attTag.Get<TagCompound>("Data");
                    PlayerAttachments.Add(new Attachment(id, data));
                }
            }
        }
    }
}
