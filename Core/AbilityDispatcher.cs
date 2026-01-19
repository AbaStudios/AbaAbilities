using System;
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
    public class AbilityDispatcher
    {
        private readonly Player _player;
        private readonly List<Ability>[] _buckets;

        private readonly Dictionary<string, Ability> _singletons = new Dictionary<string, Ability>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<ActiveAttachment>> _singletonAttachments = new Dictionary<string, List<ActiveAttachment>>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, Dictionary<int, Ability>> _multiInstances = new Dictionary<string, Dictionary<int, Ability>>(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<(string, int)> _seenMultiKeys = new HashSet<(string, int)>();
        private readonly List<(string, int)> _toRemoveMulti = new List<(string, int)>();
        private readonly HashSet<string> _seenSingletonIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<Ability> _pendingBuffer = new List<Ability>();

        public AbilityDispatcher(Player player)
        {
            _player = player;
            _buckets = new List<Ability>[(int)HookType.Count];
            for (int i = 0; i < (int)HookType.Count; i++)
                _buckets[i] = new List<Ability>();
        }

        public void InitializeSingletons()
        {
            foreach (var typeData in AbilityRegistry.GetAllTypeData())
            {
                if (typeData.AllowMultipleInstances)
                    continue;

                var instance = typeData.Factory();
                instance.Player = _player;
                instance.Attachments = System.Array.Empty<ActiveAttachment>();
                instance.TypeHookMask = typeData.HookMask;
                instance.Activated = false;
                _singletons[typeData.Id] = instance;
                _singletonAttachments[typeData.Id] = new List<ActiveAttachment>();
            }
        }

        public Ability GetSingleton(string abilityId)
        {
            return _singletons.TryGetValue(abilityId, out var instance) ? instance : null;
        }

        public void RefreshContexts(List<Attachment> playerAttachments, Dictionary<int, List<Attachment>> itemAttachmentsByUid)
        {
            _seenSingletonIds.Clear();
            _seenMultiKeys.Clear();

            foreach (var list in _singletonAttachments.Values)
                list.Clear();

            foreach (var attachment in playerAttachments)
            {
                var typeData = AbilityRegistry.GetTypeData(attachment.Id);
                if (typeData == null)
                    continue;

                var context = AttachmentContext.ForPlayer(_player.whoAmI);

                if (typeData.AllowMultipleInstances)
                {
                    _seenMultiKeys.Add((attachment.Id, -1));
                    EnsurePendingMulti(attachment, context, typeData);
                }
                else
                {
                    _seenSingletonIds.Add(attachment.Id);
                    AddSingletonAttachment(attachment, context);
                }
            }

            foreach (var kvp in itemAttachmentsByUid)
            {
                int itemUid = kvp.Key;
                foreach (var attachment in kvp.Value)
                {
                    var typeData = AbilityRegistry.GetTypeData(attachment.Id);
                    if (typeData == null)
                        continue;

                    var context = AttachmentContext.ForItem(itemUid, _player.whoAmI);

                    if (typeData.AllowMultipleInstances)
                    {
                        _seenMultiKeys.Add((attachment.Id, itemUid));
                        EnsurePendingMulti(attachment, context, typeData);
                    }
                    else
                    {
                        _seenSingletonIds.Add(attachment.Id);
                        AddSingletonAttachment(attachment, context);
                    }
                }
            }

            FinalizeSingletonAttachments();
            RemoveStaleMultiInstances();
        }

        private void AddSingletonAttachment(Attachment attachment, AttachmentContext context)
        {
            if (_singletonAttachments.TryGetValue(attachment.Id, out var list))
                list.Add(new ActiveAttachment(context, attachment.Data));
        }

        private void FinalizeSingletonAttachments()
        {
            foreach (var kvp in _singletons)
            {
                var abilityId = kvp.Key;
                var instance = kvp.Value;
                var attachments = _singletonAttachments[abilityId];

                bool hadAttachments = instance.Attachments.Count > 0;
                bool hasAttachments = attachments.Count > 0;

                instance.Attachments = hasAttachments ? attachments.ToArray() : System.Array.Empty<ActiveAttachment>();

                if (hadAttachments && !hasAttachments)
                {
                    if (instance.Activated)
                    {
                        RemoveFromBuckets(instance);
                        instance.OnDeactivate();
                        instance.Activated = false;
                    }
                }
            }
        }

        private void EnsurePendingMulti(Attachment attachment, AttachmentContext context, AbilityTypeData typeData)
        {
            int uid = context.OwnerKind == OwnerKind.Item ? context.ItemUid : -1;

            if (!_multiInstances.TryGetValue(attachment.Id, out var uidMap))
            {
                uidMap = new Dictionary<int, Ability>();
                _multiInstances[attachment.Id] = uidMap;
            }

            if (!uidMap.ContainsKey(uid))
            {
                var instance = AbilityPool.Rent(attachment.Id, typeData);
                var activeAttachment = new ActiveAttachment(context, attachment.Data);
                instance.Player = _player;
                instance.Attachments = new[] { activeAttachment };
                instance.TypeHookMask = typeData.HookMask;
                instance.Activated = false;
                uidMap[uid] = instance;
            }
        }

        private void RemoveStaleMultiInstances()
        {
            _toRemoveMulti.Clear();
            foreach (var kvp in _multiInstances)
            {
                var abilityId = kvp.Key;
                foreach (var uidKvp in kvp.Value)
                {
                    var uid = uidKvp.Key;
                    if (!_seenMultiKeys.Contains((abilityId, uid)))
                        _toRemoveMulti.Add((abilityId, uid));
                }
            }

            foreach (var key in _toRemoveMulti)
                DeactivateMulti(key.Item1, key.Item2);
        }

        private void DeactivateMulti(string abilityId, int uid)
        {
            if (!_multiInstances.TryGetValue(abilityId, out var uidMap))
                return;

            if (!uidMap.TryGetValue(uid, out var instance))
                return;

            uidMap.Remove(uid);
            if (uidMap.Count == 0)
                _multiInstances.Remove(abilityId);

            if (instance.Activated)
            {
                RemoveFromBuckets(instance);
                instance.OnDeactivate();
                instance.Activated = false;
            }
            AbilityPool.Return(abilityId, instance);
        }

        private bool TryLazyActivate(Ability ability, HookType hook)
        {
            if (ability.Activated)
                return true;

            if (ability.Attachments.Count == 0)
                return false;

            if (ability.TypeHookMask.IsNoAutoActivate(hook))
                return false;

            var data = ability.Attachments[0].Data;
            if (!ability.CanActivate(data))
                return false;

            ability.Activated = true;
            AddToBuckets(ability, ability.TypeHookMask);
            ability.OnActivate();
            return true;
        }

        private void AddToBuckets(Ability instance, HookMask hookMask)
        {
            for (int i = 0; i < (int)HookType.Count; i++)
            {
                if (hookMask.HasHook((HookType)i))
                    _buckets[i].Add(instance);
            }
        }

        private void RemoveFromBuckets(Ability instance)
        {
            for (int i = 0; i < (int)HookType.Count; i++)
                _buckets[i].Remove(instance);
        }

        public void CheckDeactivations()
        {
            _toRemoveMulti.Clear();
            foreach (var kvp in _multiInstances)
            {
                var abilityId = kvp.Key;
                foreach (var uidKvp in kvp.Value)
                {
                    var instance = uidKvp.Value;
                    if (instance.Activated && instance.CanDeactivate())
                        _toRemoveMulti.Add((abilityId, uidKvp.Key));
                }
            }

            foreach (var key in _toRemoveMulti)
                DeactivateMulti(key.Item1, key.Item2);
        }

        private List<Ability> GetBucket(HookType type)
        {
            return _buckets[(int)type];
        }

        private void ActivatePending(HookType hook)
        {
            _pendingBuffer.Clear();
            foreach (var kvp in _singletons)
            {
                var instance = kvp.Value;
                if (!instance.Activated && instance.Attachments.Count > 0 && instance.TypeHookMask.HasHook(hook))
                    _pendingBuffer.Add(instance);
            }
            foreach (var kvp in _multiInstances)
            {
                foreach (var uidKvp in kvp.Value)
                {
                    var instance = uidKvp.Value;
                    if (!instance.Activated && instance.TypeHookMask.HasHook(hook))
                        _pendingBuffer.Add(instance);
                }
            }
            for (int i = 0; i < _pendingBuffer.Count; i++)
                TryLazyActivate(_pendingBuffer[i], hook);
        }

        public void Dispatch_ResetEffects() { ActivatePending(HookType.ResetEffects); foreach (var ability in GetBucket(HookType.ResetEffects)) ability.ResetEffects(); }
        public void Dispatch_UpdateDead() { ActivatePending(HookType.UpdateDead); foreach (var ability in GetBucket(HookType.UpdateDead)) ability.UpdateDead(); }
        public void Dispatch_PreUpdate() { ActivatePending(HookType.PreUpdate); foreach (var ability in GetBucket(HookType.PreUpdate)) ability.PreUpdate(); }
        public void Dispatch_ProcessTriggers(TriggersSet triggersSet) { ActivatePending(HookType.ProcessTriggers); foreach (var ability in GetBucket(HookType.ProcessTriggers)) ability.ProcessTriggers(triggersSet); }
        public void Dispatch_SetControls() { ActivatePending(HookType.SetControls); foreach (var ability in GetBucket(HookType.SetControls)) ability.SetControls(); }
        public void Dispatch_PreUpdateBuffs() { ActivatePending(HookType.PreUpdateBuffs); foreach (var ability in GetBucket(HookType.PreUpdateBuffs)) ability.PreUpdateBuffs(); }
        public void Dispatch_PostUpdateBuffs() { ActivatePending(HookType.PostUpdateBuffs); foreach (var ability in GetBucket(HookType.PostUpdateBuffs)) ability.PostUpdateBuffs(); }
        public void Dispatch_UpdateEquips() { ActivatePending(HookType.UpdateEquips); foreach (var ability in GetBucket(HookType.UpdateEquips)) ability.UpdateEquips(); }
        public void Dispatch_PostUpdateEquips() { ActivatePending(HookType.PostUpdateEquips); foreach (var ability in GetBucket(HookType.PostUpdateEquips)) ability.PostUpdateEquips(); }
        public void Dispatch_PostUpdateMiscEffects() { ActivatePending(HookType.PostUpdateMiscEffects); foreach (var ability in GetBucket(HookType.PostUpdateMiscEffects)) ability.PostUpdateMiscEffects(); }
        public void Dispatch_PostUpdateRunSpeeds() { ActivatePending(HookType.PostUpdateRunSpeeds); foreach (var ability in GetBucket(HookType.PostUpdateRunSpeeds)) ability.PostUpdateRunSpeeds(); }
        public void Dispatch_PreUpdateMovement() { ActivatePending(HookType.PreUpdateMovement); foreach (var ability in GetBucket(HookType.PreUpdateMovement)) ability.PreUpdateMovement(); }
        public void Dispatch_PostUpdate() { ActivatePending(HookType.PostUpdate); foreach (var ability in GetBucket(HookType.PostUpdate)) ability.PostUpdate(); }
        public void Dispatch_UpdateLifeRegen() { ActivatePending(HookType.UpdateLifeRegen); foreach (var ability in GetBucket(HookType.UpdateLifeRegen)) ability.UpdateLifeRegen(); }
        public void Dispatch_UpdateBadLifeRegen() { ActivatePending(HookType.UpdateBadLifeRegen); foreach (var ability in GetBucket(HookType.UpdateBadLifeRegen)) ability.UpdateBadLifeRegen(); }
        public void Dispatch_NaturalLifeRegen(ref float regen) { ActivatePending(HookType.NaturalLifeRegen); foreach (var ability in GetBucket(HookType.NaturalLifeRegen)) ability.NaturalLifeRegen(ref regen); }
        public void Dispatch_FrameEffects() { ActivatePending(HookType.FrameEffects); foreach (var ability in GetBucket(HookType.FrameEffects)) ability.FrameEffects(); }
        public bool Dispatch_FreeDodge(Player.HurtInfo info) { ActivatePending(HookType.FreeDodge); foreach (var ability in GetBucket(HookType.FreeDodge)) { if (ability.FreeDodge(info)) return true; } return false; }
        public bool Dispatch_ConsumableDodge(Player.HurtInfo info) { ActivatePending(HookType.ConsumableDodge); foreach (var ability in GetBucket(HookType.ConsumableDodge)) { if (ability.ConsumableDodge(info)) return true; } return false; }
        public void Dispatch_ModifyHurt(ref Player.HurtModifiers modifiers) { ActivatePending(HookType.ModifyHurt); foreach (var ability in GetBucket(HookType.ModifyHurt)) ability.ModifyHurt(ref modifiers); }
        public void Dispatch_OnHurt(Player.HurtInfo info) { ActivatePending(HookType.OnHurt); foreach (var ability in GetBucket(HookType.OnHurt)) ability.OnHurt(info); }
        public void Dispatch_PostHurt(Player.HurtInfo info) { ActivatePending(HookType.PostHurt); foreach (var ability in GetBucket(HookType.PostHurt)) ability.PostHurt(info); }
        public bool Dispatch_PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource) { ActivatePending(HookType.PreKill); bool allow = true; foreach (var ability in GetBucket(HookType.PreKill)) { if (!ability.PreKill(damage, hitDirection, pvp, ref playSound, ref genDust, ref damageSource)) allow = false; } return allow; }
        public void Dispatch_Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource) { ActivatePending(HookType.Kill); foreach (var ability in GetBucket(HookType.Kill)) ability.Kill(damage, hitDirection, pvp, damageSource); }
        public void Dispatch_ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers) { ActivatePending(HookType.ModifyHitByNPC); foreach (var ability in GetBucket(HookType.ModifyHitByNPC)) ability.ModifyHitByNPC(npc, ref modifiers); }
        public void Dispatch_OnHitByNPC(NPC npc, Player.HurtInfo hurtInfo) { ActivatePending(HookType.OnHitByNPC); foreach (var ability in GetBucket(HookType.OnHitByNPC)) ability.OnHitByNPC(npc, hurtInfo); }
        public void Dispatch_ModifyHitByProjectile(Projectile proj, ref Player.HurtModifiers modifiers) { ActivatePending(HookType.ModifyHitByProjectile); foreach (var ability in GetBucket(HookType.ModifyHitByProjectile)) ability.ModifyHitByProjectile(proj, ref modifiers); }
        public void Dispatch_OnHitByProjectile(Projectile proj, Player.HurtInfo hurtInfo) { ActivatePending(HookType.OnHitByProjectile); foreach (var ability in GetBucket(HookType.OnHitByProjectile)) ability.OnHitByProjectile(proj, hurtInfo); }
        public bool Dispatch_CanHitNPC(NPC target) { ActivatePending(HookType.CanHitNPC); foreach (var ability in GetBucket(HookType.CanHitNPC)) { if (!ability.CanHitNPC(target)) return false; } return true; }
        public void Dispatch_ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) { ActivatePending(HookType.ModifyHitNPC); foreach (var ability in GetBucket(HookType.ModifyHitNPC)) ability.ModifyHitNPC(target, ref modifiers); }
        public void Dispatch_OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) { ActivatePending(HookType.OnHitNPC); foreach (var ability in GetBucket(HookType.OnHitNPC)) ability.OnHitNPC(target, hit, damageDone); }
        public bool? Dispatch_CanHitNPCWithItem(Item item, NPC target) { ActivatePending(HookType.CanHitNPCWithItem); bool? result = null; foreach (var ability in GetBucket(HookType.CanHitNPCWithItem)) { var abilityResult = ability.CanHitNPCWithItem(item, target); if (abilityResult.HasValue) { if (!abilityResult.Value) return false; result = true; } } return result; }
        public void Dispatch_ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers) { ActivatePending(HookType.ModifyHitNPCWithItem); foreach (var ability in GetBucket(HookType.ModifyHitNPCWithItem)) ability.ModifyHitNPCWithItem(item, target, ref modifiers); }
        public void Dispatch_OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone) { ActivatePending(HookType.OnHitNPCWithItem); foreach (var ability in GetBucket(HookType.OnHitNPCWithItem)) ability.OnHitNPCWithItem(item, target, hit, damageDone); }
        public bool? Dispatch_CanHitNPCWithProj(Projectile proj, NPC target) { ActivatePending(HookType.CanHitNPCWithProj); bool? result = null; foreach (var ability in GetBucket(HookType.CanHitNPCWithProj)) { var abilityResult = ability.CanHitNPCWithProj(proj, target); if (abilityResult.HasValue) { if (!abilityResult.Value) return false; result = true; } } return result; }
        public void Dispatch_ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers) { ActivatePending(HookType.ModifyHitNPCWithProj); foreach (var ability in GetBucket(HookType.ModifyHitNPCWithProj)) ability.ModifyHitNPCWithProj(proj, target, ref modifiers); }
        public void Dispatch_OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone) { ActivatePending(HookType.OnHitNPCWithProj); foreach (var ability in GetBucket(HookType.OnHitNPCWithProj)) ability.OnHitNPCWithProj(proj, target, hit, damageDone); }
        public bool Dispatch_CanHitPvp(Item item, Player target) { ActivatePending(HookType.CanHitPvp); foreach (var ability in GetBucket(HookType.CanHitPvp)) { if (!ability.CanHitPvp(item, target)) return false; } return true; }
        public bool Dispatch_CanHitPvpWithProj(Projectile proj, Player target) { ActivatePending(HookType.CanHitPvpWithProj); foreach (var ability in GetBucket(HookType.CanHitPvpWithProj)) { if (!ability.CanHitPvpWithProj(proj, target)) return false; } return true; }
        public void Dispatch_OnHitAnything(float x, float y, Entity victim) { ActivatePending(HookType.OnHitAnything); foreach (var ability in GetBucket(HookType.OnHitAnything)) ability.OnHitAnything(x, y, victim); }
        public bool Dispatch_PreItemCheck() { ActivatePending(HookType.PreItemCheck); foreach (var ability in GetBucket(HookType.PreItemCheck)) { if (!ability.PreItemCheck()) return false; } return true; }
        public void Dispatch_PostItemCheck() { ActivatePending(HookType.PostItemCheck); foreach (var ability in GetBucket(HookType.PostItemCheck)) ability.PostItemCheck(); }
        public float Dispatch_UseTimeMultiplier(Item item) { ActivatePending(HookType.UseTimeMultiplier); float multiplier = 1f; foreach (var ability in GetBucket(HookType.UseTimeMultiplier)) multiplier *= ability.UseTimeMultiplier(item); return multiplier; }
        public float Dispatch_UseAnimationMultiplier(Item item) { ActivatePending(HookType.UseAnimationMultiplier); float multiplier = 1f; foreach (var ability in GetBucket(HookType.UseAnimationMultiplier)) multiplier *= ability.UseAnimationMultiplier(item); return multiplier; }
        public float Dispatch_UseSpeedMultiplier(Item item) { ActivatePending(HookType.UseSpeedMultiplier); float multiplier = 1f; foreach (var ability in GetBucket(HookType.UseSpeedMultiplier)) multiplier *= ability.UseSpeedMultiplier(item); return multiplier; }
        public bool Dispatch_CanConsumeAmmo(Item weapon, Item ammo) { ActivatePending(HookType.CanConsumeAmmo); foreach (var ability in GetBucket(HookType.CanConsumeAmmo)) { if (!ability.CanConsumeAmmo(weapon, ammo)) return false; } return true; }
        public void Dispatch_OnConsumeAmmo(Item weapon, Item ammo) { ActivatePending(HookType.OnConsumeAmmo); foreach (var ability in GetBucket(HookType.OnConsumeAmmo)) ability.OnConsumeAmmo(weapon, ammo); }
        public bool Dispatch_CanShoot(Item item) { ActivatePending(HookType.CanShoot); foreach (var ability in GetBucket(HookType.CanShoot)) { if (!ability.CanShoot(item)) return false; } return true; }
        public void Dispatch_ModifyShootStats(Item item, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback) { ActivatePending(HookType.ModifyShootStats); foreach (var ability in GetBucket(HookType.ModifyShootStats)) ability.ModifyShootStats(item, ref position, ref velocity, ref type, ref damage, ref knockback); }
        public bool Dispatch_Shoot(Item item, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) { ActivatePending(HookType.Shoot); foreach (var ability in GetBucket(HookType.Shoot)) { if (!ability.Shoot(item, source, position, velocity, type, damage, knockback)) return false; } return true; }
        public void Dispatch_MeleeEffects(Item item, Rectangle hitbox) { ActivatePending(HookType.MeleeEffects); foreach (var ability in GetBucket(HookType.MeleeEffects)) ability.MeleeEffects(item, hitbox); }
        public void Dispatch_ModifyWeaponDamage(Item item, ref StatModifier damage) { ActivatePending(HookType.ModifyWeaponDamage); foreach (var ability in GetBucket(HookType.ModifyWeaponDamage)) ability.ModifyWeaponDamage(item, ref damage); }
        public void Dispatch_ModifyWeaponKnockback(Item item, ref StatModifier knockback) { ActivatePending(HookType.ModifyWeaponKnockback); foreach (var ability in GetBucket(HookType.ModifyWeaponKnockback)) ability.ModifyWeaponKnockback(item, ref knockback); }
        public void Dispatch_ModifyWeaponCrit(Item item, ref float crit) { ActivatePending(HookType.ModifyWeaponCrit); foreach (var ability in GetBucket(HookType.ModifyWeaponCrit)) ability.ModifyWeaponCrit(item, ref crit); }
        public void Dispatch_GetHealLife(Item item, bool quickHeal, ref int healValue) { ActivatePending(HookType.GetHealLife); foreach (var ability in GetBucket(HookType.GetHealLife)) ability.GetHealLife(item, quickHeal, ref healValue); }
        public void Dispatch_GetHealMana(Item item, bool quickHeal, ref int healValue) { ActivatePending(HookType.GetHealMana); foreach (var ability in GetBucket(HookType.GetHealMana)) ability.GetHealMana(item, quickHeal, ref healValue); }
        public void Dispatch_ModifyManaCost(Item item, ref float reduce, ref float mult) { ActivatePending(HookType.ModifyManaCost); foreach (var ability in GetBucket(HookType.ModifyManaCost)) ability.ModifyManaCost(item, ref reduce, ref mult); }
        public void Dispatch_OnMissingMana(Item item, int neededMana) { ActivatePending(HookType.OnMissingMana); foreach (var ability in GetBucket(HookType.OnMissingMana)) ability.OnMissingMana(item, neededMana); }
        public void Dispatch_OnConsumeMana(Item item, int manaConsumed) { ActivatePending(HookType.OnConsumeMana); foreach (var ability in GetBucket(HookType.OnConsumeMana)) ability.OnConsumeMana(item, manaConsumed); }
        public void Dispatch_ModifyMaxStats(out StatModifier health, out StatModifier mana) { ActivatePending(HookType.ModifyMaxStats); health = StatModifier.Default; mana = StatModifier.Default; foreach (var ability in GetBucket(HookType.ModifyMaxStats)) { ability.ModifyMaxStats(out var h, out var m); health = health.CombineWith(h); mana = mana.CombineWith(m); } }
        public void Dispatch_OnAttackKeyDown() { ActivatePending(HookType.OnAttackKeyDown); foreach (var ability in GetBucket(HookType.OnAttackKeyDown)) ability.OnAttackKeyDown(); }
        public void Dispatch_WhileAttackKeyDown(int ticksHeld) { ActivatePending(HookType.WhileAttackKeyDown); foreach (var ability in GetBucket(HookType.WhileAttackKeyDown)) ability.WhileAttackKeyDown(ticksHeld); }
        public void Dispatch_OnAttackKeyUp(int ticksHeld) { ActivatePending(HookType.OnAttackKeyUp); foreach (var ability in GetBucket(HookType.OnAttackKeyUp)) ability.OnAttackKeyUp(ticksHeld); }
        public void Dispatch_OnActivateKeyDown() { ActivatePending(HookType.OnActivateKeyDown); foreach (var ability in GetBucket(HookType.OnActivateKeyDown)) ability.OnActivateKeyDown(); }
        public void Dispatch_WhileActivateKeyDown(int ticksHeld) { ActivatePending(HookType.WhileActivateKeyDown); foreach (var ability in GetBucket(HookType.WhileActivateKeyDown)) ability.WhileActivateKeyDown(ticksHeld); }
        public void Dispatch_OnActivateKeyUp(int ticksHeld) { ActivatePending(HookType.OnActivateKeyUp); foreach (var ability in GetBucket(HookType.OnActivateKeyUp)) ability.OnActivateKeyUp(ticksHeld); }
    }
}
