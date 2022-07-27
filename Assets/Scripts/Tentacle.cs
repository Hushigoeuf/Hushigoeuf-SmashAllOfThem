using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Hushigoeuf
{
    /// <summary>
    /// Визуализирует поведение тентакля на основе LineRenderer.
    /// Источник: https://www.youtube.com/watch?v=9hTnlp9_wX8
    /// </summary>
    [AddComponentMenu(HGEditor.PATH_MENU_CURRENT + nameof(Tentacle))]
    [RequireComponent(typeof(LineRenderer))]
    public class Tentacle : HGMonoBehaviour, HGEventListener<HGUpdateEvent>
    {
        protected const string EDITOR_PATH_GROUP_MAIN =
            HGEditor.PATH_GROUP_SETTINGS + "/" + nameof(Length);

        protected const string EDITOR_PATH_GROUP_WIGGLE =
            HGEditor.PATH_GROUP_SETTINGS + "/" + nameof(WiggleIntensivity);

        /// Кол-во сегментов
        [HGShowInSettings] [BoxGroup(EDITOR_PATH_GROUP_MAIN, false)] [MinValue(2)]
        public int Length = 30;

        /// Сбрасывать LineRenderer на старте
        [HGShowInSettings] [BoxGroup(EDITOR_PATH_GROUP_MAIN, false)]
        public bool ResetOnEnable = true;

        /// Пауза перед началом работы
        [HGShowInSettings] [BoxGroup(EDITOR_PATH_GROUP_MAIN, false)] [MinValue(0)]
        public float DelayBeforeWorking;

        /// Дистанция между сегментами
        [FormerlySerializedAs("TargetDistance")]
        [HGShowInSettings]
        [BoxGroup(EDITOR_PATH_GROUP_MAIN, false)]
        [MinValue(0)]
        public float DistancePerSegment = .2f;

        /// Скорость сглаживания 
        [HGShowInSettings] [BoxGroup(EDITOR_PATH_GROUP_MAIN, false)] [MinValue(0)]
        public float SmoothSpeed = .02f;

        /// Основная скорость
        [HGShowInSettings] [BoxGroup(EDITOR_PATH_GROUP_MAIN, false)] [MinValue(0)]
        public float TrailSpeed = 350;

        /// Интенсивность покачивания
        [HGShowInSettings] [BoxGroup(EDITOR_PATH_GROUP_WIGGLE, false)] [Range(0, 1)]
        public float WiggleIntensivity = 1;

        /// Скорость покачивания
        [HGShowInSettings] [BoxGroup(EDITOR_PATH_GROUP_WIGGLE, false)] [MinValue(0)]
        public float WiggleSpeed = 10;

        /// Магнитуда покачивания
        [HGShowInSettings] [BoxGroup(EDITOR_PATH_GROUP_WIGGLE, false)] [MinValue(0)]
        public float WiggleMagnitude = 20;

        /// Transform на основе которого задается направление тентакля
        [HGShowInBindings] [HGBorders] public Transform TargetDirection;

        /// Transform который используется для покачивания тентакля
        [HGShowInBindings] [HGBorders] public Transform WiggleDirection;

        /// Дополнительный Transform который крепится к последнему сегменту
        [HGShowInBindings] [HGBorders] public Transform TargetEndOfTail;

        protected LineRenderer _target;
        protected Vector3[] _segmentPositions;
        protected Vector3[] _segmentVelocities;
        protected Quaternion _startWiggleRotation;
        protected bool _delayed;

        protected virtual LineRenderer Target
        {
            get
            {
                if (_target == null)
                    _target = GetComponent<LineRenderer>();
                return _target;
            }
        }

        protected virtual void Awake()
        {
            Initialization();

            if (!ResetOnEnable) ResetSegments();
        }

        protected virtual void Initialization()
        {
            Target.positionCount = Length;
            _segmentPositions = new Vector3[Length];
            _segmentVelocities = new Vector3[Length];
            if (TargetDirection == null)
                TargetDirection = transform;
            if (WiggleDirection != null)
                _startWiggleRotation = WiggleDirection.localRotation;
        }

        protected virtual void OnEnable()
        {
            this.HGEventStartListening();

            if (ResetOnEnable) ResetSegments();

            _delayed = false;

            if (DelayBeforeWorking > 0)
                StartCoroutine(DelayBeforeWorkingCoroutine());
        }

        protected virtual void OnDisable()
        {
            this.HGEventStopListening();

            StopCoroutine(DelayBeforeWorkingCoroutine());
        }

        protected virtual IEnumerator DelayBeforeWorkingCoroutine()
        {
            _delayed = true;

            yield return new WaitForSeconds(DelayBeforeWorking);

            _delayed = false;
        }

        /// <summary>
        /// Кастомный Update для обновления сегментов и покачивания тентакля.
        /// </summary>
        protected virtual void OnHGUpdate(float dt, float t)
        {
            if (WiggleDirection != null)
                WiggleDirection.localRotation = Quaternion.Euler(0, 0,
                    Mathf.Sin(t * (WiggleSpeed * WiggleIntensivity)) * (WiggleMagnitude * WiggleIntensivity));
            if (_delayed) ResetSegments();
            else UpdateSegments();
            if (TargetEndOfTail != null)
                TargetEndOfTail.position = _segmentPositions[_segmentPositions.Length - 1];
        }

        /// <summary>
        /// Обновляет позиции всех сегментов.
        /// </summary>
        protected virtual void UpdateSegments()
        {
            UpdateSegment(0);
            for (var i = 1; i < _segmentPositions.Length; i++)
                UpdateSegment(i);
            Target.SetPositions(_segmentPositions);
        }

        /// <summary>
        /// Обновляет позицию сегмента.
        /// </summary>
        protected virtual void UpdateSegment(int i)
        {
            if (i == 0)
                _segmentPositions[i] = Target.useWorldSpace ? TargetDirection.position : TargetDirection.localPosition;
            else if (i > 0)
                _segmentPositions[i] = Vector3.SmoothDamp(_segmentPositions[i],
                    _segmentPositions[i - 1] + TargetDirection.right * DistancePerSegment,
                    ref _segmentVelocities[i], SmoothSpeed + (TrailSpeed > 0 ? i / TrailSpeed : 0));
        }

        /// <summary>
        /// Сбрасывает параметры и позиции всех сегментов.
        /// </summary>
        protected virtual void ResetSegments()
        {
            if (WiggleDirection != null)
                WiggleDirection.localRotation = _startWiggleRotation;
            ResetSegment(0);
            for (var i = 1; i < _segmentPositions.Length; i++)
                ResetSegment(i);
            Target.SetPositions(_segmentPositions);
        }

        /// <summary>
        /// Сбрасывает позицию заданного сегмента.
        /// </summary>
        protected virtual void ResetSegment(int i)
        {
            if (i == 0)
                _segmentPositions[i] = TargetDirection.position;
            else if (i > 0)
                _segmentPositions[i] = _segmentPositions[i - 1] + TargetDirection.right * DistancePerSegment;
        }

        public virtual void OnHGEvent(HGUpdateEvent e)
        {
            if (e.EventType == HGUpdateEventTypes.Update)
                OnHGUpdate(e.DateTime, e.Time);
        }
    }
}