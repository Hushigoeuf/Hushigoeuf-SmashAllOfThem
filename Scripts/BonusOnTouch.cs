using Sirenix.OdinInspector;
using UnityEngine;

namespace Hushigoeuf
{
    [AddComponentMenu(HGEditor.PATH_MENU_CURRENT + nameof(BonusOnTouch))]
    public class BonusOnTouch : HGMonoBehaviour
    {
        [HGShowInSettings] public LayerMask TargetLayerMask;
        [HGShowInSettings] [MinValue(1)] public int BonusCaused;
        [HGShowInSettings] public bool DestroyOnTouch = true;

        protected virtual void OnTriggerStay2D(Collider2D collider)
        {
            Colliding(collider.gameObject);
        }

        protected virtual void OnTriggerEnter2D(Collider2D collider)
        {
            Colliding(collider.gameObject);
        }

        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            Colliding(collision.gameObject);
        }

        protected virtual void OnCollisionStay2D(Collision2D collision)
        {
            Colliding(collision.gameObject);
        }

        protected virtual void Colliding(GameObject collider)
        {
            if (!isActiveAndEnabled) return;
            if (!TargetLayerMask.HGLayerInLayerMask(collider.layer)) return;

            var bonus = collider.gameObject.HGGetComponentNoAlloc<BonusPlayerExtension>();
            if (bonus == null) return;

            bonus.BonusCount += BonusCaused;

            if (DestroyOnTouch) Destroy(gameObject);
        }
    }
}