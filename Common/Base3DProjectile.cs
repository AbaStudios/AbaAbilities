using System;
using Luminance.Common.StateMachines;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using AbaAbilities.Core;

namespace AbaAbilities.Common
{
    public abstract class Base3DProjectile : ModProjectile
    {
        private PushdownAutomata<EntityAIState<int>, int> _stateMachine;
        private int _maxLifeTime;

        public float Z { get; set; }
        public float ZVelocity { get; set; }

        public Vector3 Center3D {
            get => new(Projectile.Center.X, Projectile.Center.Y, Z);
            set {
                Projectile.Center = new Vector2(value.X, value.Y);
                Z = value.Z;
            }
        }

        public PushdownAutomata<EntityAIState<int>, int> StateMachine {
            get {
                if (_stateMachine == null)
                    InitializeStateMachine();
                return _stateMachine;
            }
        }

        public int CurrentState => StateMachine.CurrentState?.Identifier ?? 0;
        public ref int StateTimer => ref StateMachine.CurrentState.Time;

        public AnimationContext Context {
            get {
                Vector2 visualPos = ProjectionHelpers.WorldToScreen(Center3D);
                float visualScale = ProjectionHelpers.GetScaleFactor(Z);
                float visualOpacity = ProjectionHelpers.GetOpacityFactor(Z);
                return new AnimationContext(
                    Main.GlobalTimeWrappedHourly,
                    _maxLifeTime - Projectile.timeLeft,
                    StateTimer,
                    1f / 60f,
                    visualPos,
                    visualScale,
                    visualOpacity,
                    Projectile.rotation
                );
            }
        }

        protected abstract void DefineStates();

        private void InitializeStateMachine() {
            _stateMachine = new PushdownAutomata<EntityAIState<int>, int>(new EntityAIState<int>(0));
            _stateMachine.OnStateTransition += OnStateTransition;
            DefineStates();
        }

        protected virtual void OnStateTransition(bool stateWasPopped, EntityAIState<int> oldState) { }

        protected void RegisterState(int stateId) {
            StateMachine.RegisterState(new EntityAIState<int>(stateId));
        }

        protected void RegisterTransition(int fromState, int toState, Func<bool> condition, Action callback = null) {
            StateMachine.RegisterTransition(fromState, toState, false, condition, callback);
        }

        protected void RegisterBehavior(int stateId, Action behavior) {
            StateMachine.RegisterStateBehavior(stateId, behavior);
        }

        public sealed override void SetDefaults() {
            _maxLifeTime = 600;
            SetDefaults3D();
            _maxLifeTime = Projectile.timeLeft;
        }

        public virtual void SetDefaults3D() { }

        public sealed override void AI() {
            StateMachine.PerformStateTransitionCheck();
            StateMachine.PerformBehaviors();
            Z += ZVelocity;
            AI3D();
            if (StateMachine.CurrentState != null)
                StateMachine.CurrentState.Time++;
        }

        public virtual void AI3D() { }

        public sealed override bool PreDraw(ref Color lightColor) {
            Draw3D(Main.spriteBatch);
            return false;
        }

        public virtual void Draw3D(SpriteBatch spriteBatch) { }

        public void MoveTo3D(Vector3 target, float speed) {
            Vector3 diff = target - Center3D;
            float length = diff.Length();
            if (length < speed) {
                Center3D = target;
                return;
            }
            Vector3 direction = diff / length;
            Center3D += direction * speed;
        }

        public override void SendExtraAI(System.IO.BinaryWriter writer) {
            writer.Write(Z);
            writer.Write(ZVelocity);
            writer.Write(CurrentState);
            writer.Write(StateTimer);
        }

        public override void ReceiveExtraAI(System.IO.BinaryReader reader) {
            Z = reader.ReadSingle();
            ZVelocity = reader.ReadSingle();
            int newState = reader.ReadInt32();
            int newTimer = reader.ReadInt32();
            if (StateMachine.CurrentState != null && 
                StateMachine.CurrentState.Identifier != newState && 
                StateMachine.StateRegistry.ContainsKey(newState)) {
                while (StateMachine.StateStack.Count > 0)
                    StateMachine.StateStack.Pop();
                StateMachine.StateStack.Push(StateMachine.StateRegistry[newState]);
            }
            if (StateMachine.CurrentState != null)
                StateMachine.CurrentState.Time = newTimer;
        }
    }
}
