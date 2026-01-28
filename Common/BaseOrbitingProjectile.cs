using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Luminance.Common.StateMachines;
using AbaAbilities.Core;

namespace AbaAbilities.Common
{
    public abstract class BaseOrbitingProjectile : ModProjectile
    {
        public enum OrbitingState { Orbiting, Fired }

        private bool _registered;
        private int _maxLifeTime;
        private int _timer;
        private PushdownAutomata<EntityAIState<int>, int> _stateMachine;
        private AnimationContext _cachedContext;
        private uint _contextCacheFrame;

        #region Virtual Configuration
        public virtual float AbilityCapacityCost => 0.33f;
        public virtual float OrbitRadius => 50f;
        public virtual float OrbitSpeed => 0.05f;
        public virtual Color GlowColor => Color.White;
        public virtual float GlowIntensity => 0.5f;
        public virtual bool Use3D => false;
        public virtual bool UseStateMachine => false;
        #endregion

        #region Orbiting State
        public OrbitingState State
        {
            get => (OrbitingState)(int)Projectile.ai[0];
            set => Projectile.ai[0] = (float)value;
        }

        public float OrbitAngle
        {
            get => Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        public bool IsOrbiting => State == OrbitingState.Orbiting;
        #endregion

        #region 3D Properties
        public float Z { get; set; }
        public float ZVelocity { get; set; }

        public Vector3 Center3D
        {
            get => new(Projectile.Center.X, Projectile.Center.Y, Z);
            set
            {
                Projectile.Center = new Vector2(value.X, value.Y);
                Z = value.Z;
            }
        }
        #endregion

        #region State Machine
        public PushdownAutomata<EntityAIState<int>, int> StateMachine
        {
            get
            {
                if (_stateMachine == null && UseStateMachine)
                    InitializeStateMachine();
                return _stateMachine;
            }
        }

        public int CurrentState => StateMachine?.CurrentState?.Identifier ?? 0;

        public int StateTimer
        {
            get => StateMachine?.CurrentState?.Time ?? _timer;
            set
            {
                if (StateMachine?.CurrentState != null)
                    StateMachine.CurrentState.Time = value;
                else
                    _timer = value;
            }
        }

        public int Timer => _timer;
        #endregion

        #region Animation Context
        public AnimationContext Context
        {
            get
            {
                uint frame = Main.GameUpdateCount;
                if (_contextCacheFrame != frame)
                {
                    Vector2 visualPos = Use3D ? ProjectionHelpers.WorldToScreen(Center3D) : Projectile.Center - Main.screenPosition;
                    float visualScale = Use3D ? ProjectionHelpers.GetScaleFactor(Z) : 1f;
                    float visualOpacity = Use3D ? ProjectionHelpers.GetOpacityFactor(Z) : 1f;
                    _cachedContext = new AnimationContext(
                        Main.GlobalTimeWrappedHourly,
                        _maxLifeTime - Projectile.timeLeft,
                        Timer,
                        1f / 60f,
                        visualPos,
                        visualScale,
                        visualOpacity,
                        Projectile.rotation
                    );
                    _contextCacheFrame = frame;
                }
                return _cachedContext;
            }
        }
        #endregion

        #region State Machine Helpers
        private void InitializeStateMachine()
        {
            _stateMachine = new PushdownAutomata<EntityAIState<int>, int>(new EntityAIState<int>(0));
            _stateMachine.OnStateTransition += OnStateTransition;
            DefineStates();
        }

        protected virtual void OnStateTransition(bool stateWasPopped, EntityAIState<int> oldState) { }
        protected virtual void DefineStates() { }

        protected void RegisterState(int stateId)
        {
            StateMachine?.RegisterState(new EntityAIState<int>(stateId));
        }

        protected void RegisterTransition(int fromState, int toState, Func<bool> condition, Action callback = null)
        {
            StateMachine?.RegisterTransition(fromState, toState, false, condition, callback);
        }

        protected void RegisterBehavior(int stateId, Action behavior)
        {
            StateMachine?.RegisterStateBehavior(stateId, behavior);
        }
        #endregion

        #region 3D Helpers
        public void MoveTo3D(Vector3 target, float speed)
        {
            Vector3 diff = target - Center3D;
            float length = diff.Length();
            if (length < speed)
            {
                Center3D = target;
                return;
            }
            Vector3 direction = diff / length;
            Center3D += direction * speed;
        }
        #endregion

        #region Core Lifecycle
        public override void SetStaticDefaults()
        {
            AbilityCapacityApi.CacheProjectileCost(Type, AbilityCapacityCost);
        }

        public sealed override void SetDefaults()
        {
            _maxLifeTime = 300;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 300;
            Projectile.penetrate = -1;
            SetProjectileDefaults();
            _maxLifeTime = Projectile.timeLeft;
        }

        public virtual void SetProjectileDefaults() { }

        public sealed override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (UseStateMachine && StateMachine != null)
            {
                StateMachine.PerformStateTransitionCheck();
                StateMachine.PerformBehaviors();
            }

            if (State == OrbitingState.Orbiting)
            {
                if (!_registered)
                {
                    OrbitingProjectileSystem.Register(Projectile.owner, Projectile.whoAmI);
                    _registered = true;
                }
                OrbitingAI(owner);
            }
            else
            {
                if (_registered)
                {
                    OrbitingProjectileSystem.Unregister(Projectile.owner, Projectile.whoAmI);
                    _registered = false;
                }
                FiredAI(owner);
            }

            if (Use3D)
                Z += ZVelocity;

            UpdateVisuals(owner);
            CustomAI(owner);

            if (UseStateMachine && StateMachine?.CurrentState != null)
                StateMachine.CurrentState.Time++;
            else
                _timer++;
        }

        protected virtual void CustomAI(Player owner) { }
        #endregion

        #region Orbiting Behavior
        protected virtual void OrbitingAI(Player owner)
        {
            Projectile.timeLeft = 2;
            Projectile.tileCollide = false;

            OrbitAngle += OrbitSpeed;
            Vector2 offset = OrbitAngle.ToRotationVector2() * OrbitRadius;
            Projectile.Center = owner.Center + offset;
            Projectile.rotation += 0.2f;
        }

        protected virtual void FiredAI(Player owner)
        {
            Projectile.tileCollide = true;
            Projectile.rotation += 0.4f;
        }

        protected virtual void UpdateVisuals(Player owner)
        {
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.YellowStarDust);
                d.noGravity = true;
                d.velocity *= 0.5f;
            }

            if (State == OrbitingState.Fired)
                Lighting.AddLight(Projectile.Center, GlowColor.ToVector3() * 0.8f);
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (State == OrbitingState.Orbiting)
                return false;
            return null;
        }

        public override void OnKill(int timeLeft)
        {
            if (_registered)
                OrbitingProjectileSystem.Unregister(Projectile.owner, Projectile.whoAmI);
            OnProjectileKill(timeLeft);
        }

        protected virtual void OnProjectileKill(int timeLeft) { }

        public void Fire(Vector2 target)
        {
            State = OrbitingState.Fired;

            Vector2 dir = (target - Projectile.Center);
            float distance = dir.Length();
            dir = dir.SafeNormalize(Vector2.UnitX);

            float speed = GetFireSpeed();
            Projectile.velocity = dir * speed;
            Projectile.timeLeft = 300;
            Projectile.netUpdate = true;

            OnFire(target, distance);
        }

        protected virtual float GetFireSpeed() => 24f;
        protected virtual void OnFire(Vector2 target, float distance) { }
        #endregion

        #region Drawing
        public override bool PreDraw(ref Color lightColor)
        {
            DrawProjectile(lightColor);
            if (State == OrbitingState.Orbiting)
                DrawGlow();
            return false;
        }

        protected virtual void DrawProjectile(Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame(1, 1, 0, 0);
            Vector2 origin = frame.Size() / 2f;

            Vector2 drawPos = Use3D ? Context.VisualPosition : Projectile.Center - Main.screenPosition;
            float drawScale = Use3D ? Projectile.scale * Context.VisualScale : Projectile.scale;
            Color drawColor = Use3D ? Color.White * Context.VisualOpacity : Color.White;

            Main.EntitySpriteDraw(
                texture,
                drawPos,
                frame,
                drawColor,
                Projectile.rotation,
                origin,
                drawScale,
                SpriteEffects.None,
                0
            );
        }

        protected virtual void DrawGlow()
        {
            Texture2D bloomTex = Luminance.Assets.MiscTexturesRegistry.BloomCircleSmall.Value;
            Vector2 drawPos = Use3D ? Context.VisualPosition : Projectile.Center - Main.screenPosition;
            float scale = (Projectile.width / (float)bloomTex.Width) * 2.5f;
            if (Use3D)
                scale *= Context.VisualScale;

            float opacity = Use3D ? GlowIntensity * Context.VisualOpacity : GlowIntensity;
            OrbitingGlowDrawSystem.QueueGlow(drawPos, GlowColor * opacity, scale);
        }
        #endregion

        #region Networking
        public override void SendExtraAI(System.IO.BinaryWriter writer)
        {
            writer.Write(OrbitAngle);
            writer.Write(_timer);
            if (Use3D)
            {
                writer.Write(Z);
                writer.Write(ZVelocity);
            }
            if (UseStateMachine && StateMachine?.CurrentState != null)
            {
                writer.Write(CurrentState);
                writer.Write(StateTimer);
            }
            SendCustomAI(writer);
        }

        public override void ReceiveExtraAI(System.IO.BinaryReader reader)
        {
            OrbitAngle = reader.ReadSingle();
            _timer = reader.ReadInt32();
            if (Use3D)
            {
                Z = reader.ReadSingle();
                ZVelocity = reader.ReadSingle();
            }
            if (UseStateMachine && StateMachine != null)
            {
                int newState = reader.ReadInt32();
                int newTimer = reader.ReadInt32();
                if (StateMachine.CurrentState != null &&
                    StateMachine.CurrentState.Identifier != newState &&
                    StateMachine.StateRegistry.ContainsKey(newState))
                {
                    while (StateMachine.StateStack.Count > 0)
                        StateMachine.StateStack.Pop();
                    StateMachine.StateStack.Push(StateMachine.StateRegistry[newState]);
                }
                if (StateMachine.CurrentState != null)
                    StateMachine.CurrentState.Time = newTimer;
            }
            ReceiveCustomAI(reader);
        }

        protected virtual void SendCustomAI(System.IO.BinaryWriter writer) { }
        protected virtual void ReceiveCustomAI(System.IO.BinaryReader reader) { }
        #endregion
    }
}
