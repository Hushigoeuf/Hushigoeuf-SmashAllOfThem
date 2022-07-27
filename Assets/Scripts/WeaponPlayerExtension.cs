using System;
using UnityEngine;

namespace Hushigoeuf
{
    /// <summary>
    /// Расширение для игрока, которое позволяет использовать оружие и вести прицеливание.
    /// </summary>
    [AddComponentMenu(HGEditor.PATH_MENU_CURRENT + nameof(WeaponPlayerExtension))]
    public class WeaponPlayerExtension : PlayerExtension
    {
        public enum WeaponStates
        {
            Disabled,
            Aiming,
            Shooting
        }

        /// Требовать ли нажатие кнопки прицеливания или автоматически быть в этом режиме
        [HGShowInSettings] public bool AimButtonRequired = true;

        /// Модель прицела, которые будет отображаться при прицеливании
        [HGShowInBindings] public Transform CursorModel;

        [NonSerialized] public HGStateMachine<WeaponStates> State;
        [NonSerialized] public Vector2 TargetDirection;

        public float TargetAngle => Mathf.Atan2(TargetDirection.y, TargetDirection.x) * Mathf.Rad2Deg;
        public Quaternion TargetRotation => Quaternion.AngleAxis(TargetAngle, Vector3.forward);

        protected override void OnInitialization()
        {
            base.OnInitialization();

            State = new HGStateMachine<WeaponStates>(gameObject, false);
            State.ChangeState(WeaponStates.Disabled);

            if (CursorModel != null)
                CursorModel.HGSetActive(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            State.OnStateChange += OnStateChanged;

            LinkedInputManager.AimButton.ButtonDownMethod += OnCursorButton;
            LinkedInputManager.AimButton.ButtonUpMethod += OnCursorButton;

            LinkedInputManager.ShootButton.ButtonDownMethod += OnShootButton;
            LinkedInputManager.ShootButton.ButtonUpMethod += OnShootButton;

            if (!AimButtonRequired) StartAiming();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            StopAiming();

            State.OnStateChange -= OnStateChanged;

            LinkedInputManager.AimButton.ButtonDownMethod -= OnCursorButton;
            LinkedInputManager.AimButton.ButtonUpMethod -= OnCursorButton;

            LinkedInputManager.ShootButton.ButtonDownMethod -= OnShootButton;
            LinkedInputManager.ShootButton.ButtonUpMethod -= OnShootButton;
        }

        public override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);

            HandleInput();
            HandleCursor();
        }

        /// <summary>
        /// Отслеживат позицию курсора или контроллера для прицеливания.
        /// </summary>
        protected virtual void HandleInput()
        {
            TargetDirection = LinkedInputManager.GetInputDirection(Transform);
        }

        /// <summary>
        /// Вращает прицел в сторону курсора.
        /// </summary>
        protected virtual void HandleCursor()
        {
            if (CursorModel == null || !CursorModel.gameObject.activeSelf) return;

            CursorModel.rotation = TargetRotation;
        }

        /// <summary>
        /// Начать прицеливание.
        /// </summary>
        public virtual void StartAiming()
        {
            if (State.CurrentState != WeaponStates.Disabled) return;

            State.ChangeState(WeaponStates.Aiming);

            if (CursorModel != null)
                CursorModel.HGSetActive(true);
        }

        /// <summary>
        /// Остановить прицеливание.
        /// </summary>
        public virtual void StopAiming()
        {
            switch (State.CurrentState)
            {
                case WeaponStates.Shooting:
                    StopShooting();
                    break;

                default:
                    State.ChangeState(WeaponStates.Disabled);
                    break;
            }

            if (CursorModel != null)
                CursorModel.HGSetActive(false);
        }

        /// <summary>
        /// Начать стрельбу (работает только в режиме прицеливания).
        /// </summary>
        public virtual void StartShooting()
        {
            if (State.CurrentState != WeaponStates.Aiming) return;

            State.ChangeState(WeaponStates.Shooting);
            Base.TriggerEvent(PlayerEventTypes.StartShooting, this);
        }

        /// <summary>
        /// Остановить стрельбу.
        /// </summary>
        public virtual void StopShooting()
        {
            if (State.CurrentState != WeaponStates.Shooting) return;

            State.ChangeState(WeaponStates.Aiming);
            Base.TriggerEvent(PlayerEventTypes.StopShooting, this);
        }

        protected virtual void OnStateChanged()
        {
        }

        /// <summary>
        /// Обрабатывает кнопку управления для прицеливания.
        /// </summary>
        protected virtual void OnCursorButton()
        {
            if (!AimButtonRequired) return;

            switch (LinkedInputManager.AimButton.State.CurrentState)
            {
                case InputButton.ButtonStates.ButtonDown:
                    StartAiming();
                    break;

                case InputButton.ButtonStates.ButtonUp:
                    StopAiming();
                    break;
            }
        }

        /// <summary>
        /// Обрабатывает кнопку управления для стрельбы.
        /// </summary>
        protected virtual void OnShootButton()
        {
            switch (LinkedInputManager.ShootButton.State.CurrentState)
            {
                case InputButton.ButtonStates.ButtonDown:
                    StartShooting();
                    break;

                case InputButton.ButtonStates.ButtonUp:
                    StopShooting();
                    break;
            }
        }

#if UNITY_EDITOR
        protected void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            Gizmos.color = Color.white;
            Gizmos.DrawSphere(transform.position + (Vector3) TargetDirection, 0.1f);
        }
#endif
    }
}