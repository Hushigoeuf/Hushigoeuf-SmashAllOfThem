using UnityEngine;

namespace Hushigoeuf
{
    [AddComponentMenu(HGEditor.PATH_MENU_CURRENT + nameof(GlassBoxCrashOnTouch))]
    public class GlassBoxCrashOnTouch : HGMonoBehaviour
    {
        [HGShowInSettings] public LayerMask TargetLayerMask;

        [HGShowInBindings] public GlassBox TargetGlassBox;

        protected void OnTriggerEnter2D(Collider2D target)
        {
            OnEnter(target.gameObject);
        }

        protected void OnCollisionEnter2D(Collision2D target)
        {
            OnEnter(target.gameObject);
        }

        protected void OnEnter(GameObject target)
        {
            if (!isActiveAndEnabled) return;
            if (!TargetLayerMask.HGLayerInLayerMask(target.layer)) return;

            var parent = TargetGlassBox;
            if (parent == null) parent = GetComponentInParent<GlassBox>();
            if (parent == null) return;

            parent.Crash(target);
        }
    }
}