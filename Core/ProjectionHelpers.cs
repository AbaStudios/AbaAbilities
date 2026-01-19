using Microsoft.Xna.Framework;
using Terraria;

namespace AbaAbilities.Core
{
    public static class ProjectionHelpers
    {
        public const float DefaultDepthFactor = 0.002f;
        public const float MinScale = 0.4f;
        public const float MaxScale = 1.6f;

        public static Vector2 WorldToScreen(Vector3 worldPos, float depthFactor = DefaultDepthFactor) {
            float scaleFactor = GetScaleFactor(worldPos.Z, depthFactor);
            Vector2 center = new(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);
            Vector2 worldPos2D = new(worldPos.X, worldPos.Y);
            Vector2 screenPos = worldPos2D - Main.screenPosition;
            Vector2 offset = screenPos - center;
            return center + offset * scaleFactor;
        }

        public static float GetScaleFactor(float z, float depthFactor = DefaultDepthFactor) {
            float rawScale = 1f / (z * depthFactor + 1f);
            return MathHelper.Clamp(rawScale, MinScale, MaxScale);
        }

        public static float GetOpacityFactor(float z) {
            if (z > 0)
                return MathHelper.Lerp(1f, 0.5f, MathHelper.Clamp(z / 200f, 0f, 1f));
            return 1f;
        }
    }
}
