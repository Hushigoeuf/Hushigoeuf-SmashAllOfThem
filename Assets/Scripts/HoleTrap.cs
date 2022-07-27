using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Hushigoeuf
{
    /// <summary>
    /// Управляет поведением ловушки, которая переодически появляется и исчезает.
    /// Реализовано с помощью DOTween: http://dotween.demigiant.com/
    /// </summary>
    [AddComponentMenu(HGEditor.PATH_MENU_CURRENT + nameof(HoleTrap))]
    public class HoleTrap : HGMonoBehaviour
    {
        [HGShowInSettings] [MinValue(0)] public float Delay;
        [HGShowInSettings] [MinValue(0)] public float Duration;
        [HGShowInSettings] public Vector2 TargetScale = new Vector2(0, 1);
        [HGShowInSettings] public bool StateOnStart;

        [HGShowInBindings] public Collider2D TargetCollider;

        protected Transform _transform;
        protected Sequence _sequence;

        protected virtual void Awake()
        {
            _transform = transform;
            if (TargetCollider == null)
                TargetCollider = GetComponent<Collider2D>();
        }

        protected virtual void OnEnable()
        {
            _transform.DOScale(!StateOnStart ? TargetScale.x : TargetScale.y, 0);
            TargetCollider.enabled = StateOnStart;

            _sequence = DOTween.Sequence();
            _sequence.AppendInterval(Delay);
            if (StateOnStart)
            {
                _sequence.Append(_transform.DOScale(TargetScale.x, Duration));
                _sequence.AppendCallback(() => { TargetCollider.enabled = false; });
                _sequence.AppendInterval(Delay);
                _sequence.AppendCallback(() => { TargetCollider.enabled = true; });
                _sequence.Append(_transform.DOScale(TargetScale.y, Duration));
            }
            else
            {
                _sequence.AppendCallback(() => { TargetCollider.enabled = true; });
                _sequence.Append(_transform.DOScale(TargetScale.y, Duration));
                _sequence.AppendInterval(Delay);
                _sequence.Append(_transform.DOScale(TargetScale.x, Duration));
                _sequence.AppendCallback(() => { TargetCollider.enabled = false; });
            }

            _sequence.SetLoops(-1, LoopType.Restart);
        }

        protected virtual void OnDisable()
        {
            _sequence.Kill();
            _sequence = null;
        }
    }
}