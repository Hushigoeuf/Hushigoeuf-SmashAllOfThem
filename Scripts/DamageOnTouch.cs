using Sirenix.OdinInspector;
using UnityEngine;

namespace Hushigoeuf
{
    [AddComponentMenu(HGEditor.PATH_MENU_CURRENT + nameof(DamageOnTouch))]
    [RequireComponent(typeof(Collider2D))]
    public class DamageOnTouch : HGMonoBehaviour
    {
        [HGShowInSettings] public LayerMask TargetLayerMask;
        [HGShowInSettings] [MinValue(0)] public int DamageCaused = 10;
        [HGShowInSettings] public bool MaximumDamageCaused;
        [HGShowInSettings] [MinValue(0)] public float InvincibilityDuration = 0.5f;

        protected Health _colliderHealth;

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
            if (Time.time == 0f) return;

            _colliderHealth = collider.gameObject.HGGetComponentNoAlloc<Health>();

            if (_colliderHealth == null) return;
            if (!(_colliderHealth.CurrentHealth > 0)) return;

            var damage = DamageCaused;
            if (MaximumDamageCaused)
                damage = _colliderHealth.MaximumHealth;
            _colliderHealth.Damage(damage, InvincibilityDuration);
        }
    }
}