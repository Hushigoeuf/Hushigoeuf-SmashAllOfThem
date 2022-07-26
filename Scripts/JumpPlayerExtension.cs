﻿using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Hushigoeuf
{
    [AddComponentMenu(HGEditor.PATH_MENU_CURRENT + nameof(JumpPlayerExtension))]
    public class JumpPlayerExtension : PlayerExtension<MovementPlayerExtension>, HGEventListener<PlayerEvent>
    {
        [HGShowInSettings] [MinValue(0)] public float SpeedOnStart;
        [HGShowInSettings] [MinValue(0)] public float Duration;
        [HGShowInSettings] [MinValue(0)] public float Acceleration;
        [HGShowInSettings] [Range(0, 1)] public float StrengthOnStart;

        [HGShowInBindings] public ProgressBarUI StrengthProgressBar;

        [NonSerialized] public float Speed;
        [NonSerialized] public bool Started;
        [NonSerialized] public float CurrentStrength;
        [NonSerialized] public int CurrentDirection;
        [NonSerialized] public Vector2 LastJumpDirection;
        [NonSerialized] public Vector3 LastJumpPosition;
        [NonSerialized] public float LastJumpStrength01;
        [NonSerialized] public float LastJumpSpeed;
        [NonSerialized] public float LastJumpMaxMagnitude;
        [NonSerialized] public float LastJumpMagnitude01;

        protected override void OnInitialization()
        {
            base.OnInitialization();

            Speed = SpeedOnStart;

            if (StrengthProgressBar != null)
                StrengthProgressBar.HGSetActive(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Parent.State.OnStateChange += OnMovementStateChanged;

            this.HGEventStartListening();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            StopJumping();

            Parent.State.OnStateChange -= OnMovementStateChanged;

            this.HGEventStopListening();
        }

        public override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);

            HandleStrength(dt);
        }

        public override void OnFixedUpdate(float fdt)
        {
            base.OnFixedUpdate(fdt);

            var m = Parent.Magnitude;
            if (m > LastJumpMaxMagnitude)
                LastJumpMaxMagnitude = m;
            LastJumpMagnitude01 = 0;
            if (LastJumpMaxMagnitude > 0)
                LastJumpMagnitude01 = Mathf.Clamp01(m / LastJumpMaxMagnitude);
        }

        protected virtual void HandleStrength(float dt)
        {
            if (!Started) return;

            CurrentStrength += Acceleration * CurrentDirection * dt;
            if (CurrentDirection > 0 && CurrentStrength > 1)
                CurrentDirection *= -1;
            else if (CurrentDirection < 0 && CurrentStrength < 0)
                CurrentDirection *= -1;
            CurrentStrength = Mathf.Clamp01(CurrentStrength);

            if (StrengthProgressBar != null)
                StrengthProgressBar.SetValue(CurrentStrength);
        }

        public virtual void StartJumping()
        {
            if (Parent.State.CurrentState != MovementPlayerExtension.MovementStates.GrubMovement) return;
            if (Started) return;

            Started = true;
            CurrentStrength = 0;
            CurrentDirection = 1;
            if (StrengthProgressBar != null)
                StrengthProgressBar.HGSetActive(true);

            Parent.InputEnabled = false;

            Base.TriggerEvent(PlayerEventTypes.StartJumping, this);
        }

        public virtual void StopJumping()
        {
            if (!Started) return;
            Started = false;
            if (StrengthProgressBar != null)
                StrengthProgressBar.HGSetActive(false);

            Parent.InputEnabled = true;

            Base.TriggerEvent(PlayerEventTypes.StopJumping, this);
        }

        protected virtual void Jump(Vector2 direction, float strength01)
        {
            LastJumpDirection = direction;
            LastJumpPosition = Transform.position;
            LastJumpStrength01 = strength01;
            LastJumpSpeed = Speed * strength01;
            LastJumpMaxMagnitude = 0;

            Parent.AddForce(Speed * direction * strength01);

            StartCoroutine(JumpCoroutine(Duration));

            Base.TriggerEvent(PlayerEventTypes.Jumped, this);
        }

        protected IEnumerator JumpCoroutine(float duration)
        {
            if (duration > 0) yield return new WaitForSeconds(duration);
        }

        protected virtual void OnMovementStateChanged()
        {
            if (Parent.State.CurrentState == MovementPlayerExtension.MovementStates.GrubMovement) return;

            StopJumping();
        }

        public void OnHGEvent(PlayerEvent e)
        {
            if (e.PlayerID != Base.PlayerID) return;

            switch (e.EventType)
            {
                case PlayerEventTypes.StartShooting:
                    StartJumping();
                    break;

                case PlayerEventTypes.StopShooting:
                    if (Started)
                        Jump(((WeaponPlayerExtension) e.Target).TargetDirection, CurrentStrength);
                    StopJumping();
                    break;
            }
        }
    }
}