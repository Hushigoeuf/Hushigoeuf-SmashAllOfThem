using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Hushigoeuf
{
    [AddComponentMenu(HGEditor.PATH_MENU_CURRENT + nameof(Health))]
    public class Health : HGMonoBehaviour
    {
        [HGShowInSettings] [MinValue(0)] [MaxValue(nameof(MaximumHealth))]
        public int InitialHealth = 100;

        [HGShowInSettings] [MinValue(nameof(InitialHealth))]
        public int MaximumHealth = 100;

        [HGShowInSettings] public bool DisableOnDeath;
        [HGShowInSettings] public bool DestroyOnDeath;

        [HGShowInSettings] [MinValue(0)] public float DelayBeforeDestruction;

        [HGDebugField] [NonSerialized] public bool Invulnerable;
        [HGDebugField] [NonSerialized] public int CurrentHealth;
        [HGDebugField] [NonSerialized] public int LastDamage;

        protected virtual void OnEnable()
        {
            CurrentHealth = InitialHealth;
            DamageEnabled();
        }

        public virtual void Damage(int damage, float invincibilityDuration)
        {
            if (Invulnerable) return;
            if (CurrentHealth <= 0 && InitialHealth != 0) return;

            CurrentHealth -= damage;
            if (CurrentHealth < 0) CurrentHealth = 0;

            LastDamage = damage;

            if (CurrentHealth <= 0)
            {
                CurrentHealth = 0;

                Kill();
            }
            else if (invincibilityDuration > 0)
            {
                DamageDisabled();
                StartCoroutine(DamageEnabled(invincibilityDuration));
            }
        }

        public virtual void Kill()
        {
            CurrentHealth = 0;

            DamageDisabled();

            if (DelayBeforeDestruction > 0f)
                Invoke(nameof(DestroyObject), DelayBeforeDestruction);
            else
                DestroyObject();
        }

        protected virtual void DestroyObject()
        {
            if (DestroyOnDeath) Destroy(gameObject);
            else if (DisableOnDeath) gameObject.SetActive(false);
        }

        public virtual void DamageDisabled()
        {
            Invulnerable = true;
        }

        public virtual void DamageEnabled()
        {
            Invulnerable = false;
        }

        protected virtual IEnumerator DamageEnabled(float delay)
        {
            yield return new WaitForSeconds(delay);

            DamageEnabled();
        }
    }
}