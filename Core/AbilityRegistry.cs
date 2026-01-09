using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Linq.Expressions;
using System;
using Terraria.ModLoader.IO;
using AbaAbilities.Common.Attachments;

namespace AbaAbilities.Core
{
    public static class AbilityRegistry
    {
        private static readonly Dictionary<string, AbilityTypeData> _registry = new Dictionary<string, AbilityTypeData>(StringComparer.OrdinalIgnoreCase);
        private static bool _baked = false;

        public static void Register(Type abilityType, string abilityId)
        {
            if (_baked)
                throw new InvalidOperationException("Cannot register abilities after baking.");

            if (!typeof(Ability).IsAssignableFrom(abilityType))
                throw new ArgumentException($"{abilityType} must derive from Ability.");

            if (_registry.ContainsKey(abilityId))
                throw new InvalidOperationException($"Ability ID '{abilityId}' is already registered.");

            _registry[abilityId] = new AbilityTypeData { Type = abilityType, Id = abilityId };
        }

        public static void Bake()
        {
            if (_baked)
                return;

            foreach (var kvp in _registry)
            {
                var data = kvp.Value;
                var type = data.Type;

                data.Factory = CompileFactory(type);
                data.HookMask = ComputeHookMask(type);
                data.AllowMultipleInstances = type.GetCustomAttribute<AllowMultipleInstancesAttribute>() != null;
            }

            _baked = true;
        }

        public static AbilityTypeData GetTypeData(string abilityId)
        {
            if (!_baked)
                throw new InvalidOperationException("Registry not baked.");

            return _registry.TryGetValue(abilityId, out var data) ? data : null;
        }

        public static IEnumerable<AbilityTypeData> GetAllTypeData()
        {
            if (!_baked)
                throw new InvalidOperationException("Registry not baked.");

            return _registry.Values;
        }

        private static Func<Ability> CompileFactory(Type type)
        {
            var ctor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
            if (ctor == null)
                throw new InvalidOperationException($"{type} must have a parameterless constructor.");

            var newExpr = Expression.New(ctor);
            var lambda = Expression.Lambda<Func<Ability>>(newExpr);
            return lambda.Compile();
        }

        private static HookMask ComputeHookMask(Type type)
        {
            ulong mask = 0;
            ulong noAutoActivateMask = 0;

            var baseType = typeof(Ability);
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var method in methods)
            {
                var baseMethod = baseType.GetMethod(method.Name, BindingFlags.Public | BindingFlags.Instance);
                if (baseMethod == null || baseMethod.DeclaringType != baseType)
                    continue;

                if (method.GetBaseDefinition() != baseMethod)
                    continue;

                if (!TryGetHookType(method.Name, out var hookType))
                    continue;

                mask |= (ulong)hookType;

                if (method.GetCustomAttribute<NoAutoActivateAttribute>() != null)
                    noAutoActivateMask |= (ulong)hookType;
            }

            return new HookMask(mask, noAutoActivateMask);
        }


        private static bool TryGetHookType(string methodName, out HookType hookType)
        {
            return Enum.TryParse(methodName, out hookType);
        }
    }

    public class AbilityTypeData
    {
        public Type Type;
        public string Id;
        public Func<Ability> Factory;
        public HookMask HookMask;
        public bool AllowMultipleInstances;

    }
}
