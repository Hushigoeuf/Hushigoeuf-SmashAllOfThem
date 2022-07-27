using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Hushigoeuf
{
    /// <summary>
    /// Этот компонент определяет здоровье для целевого объекта.
    /// </summary>
    [AddComponentMenu(HGEditor.PATH_MENU_CURRENT + nameof(Health))]
    public class Health : HGMonoBehaviour
    {
        public delegate void OnDeathDelegate();

        /// Кол-во здоровья на самом старте
        [HGShowInSettings] [MinValue(0)] [MaxValue(nameof(MaximumHealth))]
        public int InitialHealth = 100;

        /// Максимальное кол-во здоровья
        [HGShowInSettings] [MinValue(nameof(InitialHealth))]
        public int MaximumHealth = 100;

        /// Деактивировать ли объект после смерти
        [HGShowInSettings] public bool DisableOnDeath;

        /// Уничтожить ли объект после смерти
        [HGShowInSettings] public bool DestroyOnDeath;

        /// Длительность задержки перед смертью
        [HGShowInSettings] [MinValue(0)] public float DelayBeforeDeath;

        /// Целевое префаб, которые будет создан после смерти
        [HGShowInBindings] [HGAssetsOnly] public GameObject DropPrefab;

        [NonSerialized] public bool Invulnerable;
        [NonSerialized] public int CurrentHealth;
        [NonSerialized] public int LastDamage;
        [NonSerialized] public OnDeathDelegate OnDeath;

        protected virtual void OnEnable()
        {
            CurrentHealth = InitialHealth;

            DamageEnabled();
        }

        protected virtual IEnumerator DamageEnabled(float delay)
        {
            yield return new WaitForSeconds(delay);

            DamageEnabled();
        }

        protected virtual void Death()
        {
            if (DropPrefab != null) Drop(DropPrefab);

            OnDeath.Invoke();

            if (DestroyOnDeath) Destroy(gameObject);
            else if (DisableOnDeath) gameObject.SetActive(false);
        }

        protected virtual void Drop(GameObject prefab)
        {
            var target = Instantiate(prefab);
            target.transform.position = (Vector2) transform.position;
            target.HGSetActive(true);
        }

        /// <summary>
        /// Сделать объект уязвимым к урону.
        /// </summary>
        public virtual void DamageEnabled()
        {
            Invulnerable = false;
        }

        /// <summary>
        /// Сделать объект неуязвимым к урону.
        /// </summary>
        public virtual void DamageDisabled()
        {
            Invulnerable = true;
        }

        /// <summary>
        /// Наносит урон объекту.
        /// </summary>
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

        /// <summary>
        /// Нанести фатальный урон объекту.
        /// </summary>
        public virtual void Kill()
        {
            CurrentHealth = 0;

            DamageDisabled();

            if (DelayBeforeDeath > 0f)
                Invoke(nameof(Death), DelayBeforeDeath);
            else
                Death();
        }
    }
}