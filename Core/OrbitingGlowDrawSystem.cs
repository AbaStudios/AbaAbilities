using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Luminance.Assets;
using AbaAbilities.Common;

namespace AbaAbilities.Core
{
    public class OrbitingGlowDrawSystem : ModSystem
    {
        private struct GlowDrawData
        {
            public Vector2 Position;
            public Color Color;
            public float Scale;
        }

        private static OrbitingGlowDrawSystem _instance;
        private readonly List<GlowDrawData> _glowQueue = new(32);

        public override void Load()
        {
            _instance = this;
            On_Main.DrawProjectiles += DrawProjectilesHook;
        }

        public override void Unload()
        {
            On_Main.DrawProjectiles -= DrawProjectilesHook;
            _instance = null;
        }

        private void DrawProjectilesHook(On_Main.orig_DrawProjectiles orig, Main self)
        {
            _glowQueue.Clear();
            orig(self);
            FlushGlowQueue();
        }

        private void FlushGlowQueue()
        {
            if (_glowQueue.Count == 0)
                return;

            Texture2D bloomTex = MiscTexturesRegistry.BloomCircleSmall.Value;
            Vector2 origin = bloomTex.Size() / 2f;

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            for (int i = 0; i < _glowQueue.Count; i++)
            {
                ref GlowDrawData data = ref System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_glowQueue)[i];
                Main.spriteBatch.Draw(bloomTex, data.Position, null, data.Color, 0f, origin, data.Scale, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.End();
        }

        internal static void QueueGlow(Vector2 position, Color color, float scale)
        {
            _instance?._glowQueue.Add(new GlowDrawData
            {
                Position = position,
                Color = color,
                Scale = scale
            });
        }
    }
}
