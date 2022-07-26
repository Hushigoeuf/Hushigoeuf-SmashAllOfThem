using System;
using UnityEngine;

namespace Hushigoeuf
{
    [AddComponentMenu(HGEditor.PATH_MENU_CURRENT + nameof(Trail))]
    [RequireComponent(typeof(LineRenderer))]
    public class Trail : MonoBehaviour, HGEventListener<HGUpdateEvent>
    {
        [NonSerialized] public Transform Current;
        [NonSerialized] public Transform Target;

        protected LineRenderer _renderer;
        protected Vector3[] _segments;

        public LineRenderer Renderer
        {
            get
            {
                if (_renderer == null)
                    _renderer = GetComponent<LineRenderer>();
                return _renderer;
            }
        }

        protected virtual void Awake()
        {
            _segments = new Vector3[2];
        }

        protected virtual void OnEnable()
        {
            this.HGEventStartListening();
        }

        protected virtual void OnDisable()
        {
            this.HGEventStopListening();
        }

        protected virtual void HGOnUpdate(float dt)
        {
            if (Current == null) return;
            if (Target == null) return;

            _segments[0] = (Vector2) Current.position;
            _segments[1] = (Vector2) Target.position;

            Renderer.SetPositions(_segments);
        }

        public virtual void OnHGEvent(HGUpdateEvent e)
        {
            if (e.EventType == HGUpdateEventTypes.Update)
                HGOnUpdate(e.DateTime);
        }
    }
}