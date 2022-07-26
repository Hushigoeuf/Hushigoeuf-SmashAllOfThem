using System;
using UnityEngine;

namespace Hushigoeuf
{
    [AddComponentMenu(HGEditor.PATH_MENU_CURRENT + nameof(MovementGrubHook))]
    [RequireComponent(typeof(LineRenderer))]
    public class MovementGrubHook : HGMonoBehaviour
    {
        [HGShowInBindings] public Transform LastSegment;

        [HGShowInDebug] [NonSerialized] public Vector2 GrubPoint;
        [HGShowInDebug] [NonSerialized] public int SegmentLength;
        [HGShowInDebug] [NonSerialized] public float SmoothSpeed;

        protected Transform _transform;
        protected LineRenderer _targetRenderer;
        protected Vector3[] _segmentPositions;
        protected Vector3[] _segmentVelocities;

        protected virtual Vector2 DirectionToGrubPoint => (GrubPoint - (Vector2) transform.position).normalized;
        protected virtual float DistanceToGrubPoint => Vector2.Distance(_transform.position, GrubPoint);
        protected virtual float DistancePerSegment => DistanceToGrubPoint / (SegmentLength - 1);

        protected virtual void Awake()
        {
            _transform = transform;
            _targetRenderer = GetComponent<LineRenderer>();
            _targetRenderer.positionCount = SegmentLength;
            _segmentPositions = new Vector3[SegmentLength];
            _segmentVelocities = new Vector3[SegmentLength];
        }

        protected virtual void OnEnable()
        {
            ResetSegments();
        }

        public virtual void OnUpdate(float dt)
        {
            if (!isActiveAndEnabled) return;

            UpdateSegments();

            if (LastSegment != null)
                LastSegment.position = _segmentPositions[_segmentPositions.Length - 1];
        }

        protected virtual void UpdateSegments()
        {
            if (_segmentPositions.Length > 0)
            {
                _segmentPositions[0].x = _transform.position.x;
                _segmentPositions[0].y = _transform.position.y;
            }

            if (_segmentPositions.Length > 1)
            {
                _segmentPositions[_segmentPositions.Length - 1].x = GrubPoint.x;
                _segmentPositions[_segmentPositions.Length - 1].y = GrubPoint.y;
            }

            if (_segmentPositions.Length > 2)
            {
                var direction = DirectionToGrubPoint;
                var distance = DistancePerSegment;
                for (var i = 1; i < _segmentPositions.Length - 1; i++)
                {
                    var pos = Vector3.SmoothDamp(_segmentPositions[i],
                        (Vector2) _segmentPositions[i - 1] + direction * distance,
                        ref _segmentVelocities[i], SmoothSpeed);
                    _segmentPositions[i].x = pos.x;
                    _segmentPositions[i].y = pos.y;
                }
            }

            _targetRenderer.SetPositions(_segmentPositions);
        }

        protected virtual void ResetSegments()
        {
            if (_segmentPositions.Length > 0)
            {
                _segmentPositions[0].x = _transform.position.x;
                _segmentPositions[0].y = _transform.position.y;
            }

            if (_segmentPositions.Length > 1)
            {
                _segmentPositions[_segmentPositions.Length - 1].x = GrubPoint.x;
                _segmentPositions[_segmentPositions.Length - 1].y = GrubPoint.y;
            }

            if (_segmentPositions.Length > 2)
            {
                var direction = DirectionToGrubPoint;
                var distance = DistancePerSegment;
                for (var i = 1; i < _segmentPositions.Length - 1; i++)
                {
                    var pos = (Vector2) _segmentPositions[i - 1] + direction * distance;
                    _segmentPositions[i].x = pos.x;
                    _segmentPositions[i].y = pos.y;
                }
            }

            _targetRenderer.SetPositions(_segmentPositions);
        }
    }
}