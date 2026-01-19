using Microsoft.Xna.Framework;
using Terraria;

namespace AbaAbilities.Common
{
    public readonly struct AnimationContext
    {
        public readonly float GlobalTime;
        public readonly int LifeTime;
        public readonly int PhaseTime;
        public readonly float DeltaTime;
        public readonly Vector2 VisualPosition;
        public readonly float VisualScale;
        public readonly float VisualOpacity;
        public readonly float VisualRotation;

        public AnimationContext(float globalTime, int lifeTime, int phaseTime, float deltaTime, Vector2 visualPos, float visualScale, float visualOpacity, float visualRotation) {
            GlobalTime = globalTime;
            LifeTime = lifeTime;
            PhaseTime = phaseTime;
            DeltaTime = deltaTime;
            VisualPosition = visualPos;
            VisualScale = visualScale;
            VisualOpacity = visualOpacity;
            VisualRotation = visualRotation;
        }
    }
}
