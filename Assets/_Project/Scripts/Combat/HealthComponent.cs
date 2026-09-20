using System;
using UnityEngine;

namespace RiftArena.Combat
{
    /// <summary>
    /// Single source of truth for a character's HP. Nothing else is allowed to mutate
    /// health directly — always go through ApplyDamage / Heal / ResetHealth so
    /// OnDamaged / OnDeath fire reliably for listeners (UI, MatchManager, etc.).
    /// </summary>
    public class HealthComponent : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;

        // Bug fix (found in review): CurrentHealth used to be a plain auto-property only
        // ever initialized in Awake(). Anything that read it before this component's own
        // Awake() had run (e.g. HealthBarUI.OnEnable() wiring up a Slider, or an editor
        // script doing setup before the play-mode lifecycle settles) saw the C# default
        // of 0 instead of a full health bar - which is exactly what broke the health UI's
        // initial fill. CurrentHealth is now lazily defaulted to maxHealth on first read,
        // so it is never observably "0 before Awake" regardless of read order.
        private int currentHealth;
        private bool initialized;

        public int MaxHealth => maxHealth;

        public int CurrentHealth
        {
            get
            {
                EnsureInitialized();
                return currentHealth;
            }
            private set => currentHealth = value;
        }

        public bool IsDead { get; private set; }

        /// <summary>Fired with (currentHealth, maxHealth) whenever damage is applied.</summary>
        public event Action<int, int> OnDamaged;

        /// <summary>Fired once when health reaches zero.</summary>
        public event Action OnDeath;

        private void Awake()
        {
            ResetHealth();
        }

        private void EnsureInitialized()
        {
            if (initialized) return;
            currentHealth = maxHealth;
            initialized = true;
        }

        public void ResetHealth()
        {
            currentHealth = maxHealth;
            initialized = true;
            IsDead = false;
        }

        public void ApplyDamage(int amount)
        {
            if (IsDead || amount <= 0) return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            OnDamaged?.Invoke(CurrentHealth, maxHealth);

            if (CurrentHealth == 0)
            {
                IsDead = true;
                OnDeath?.Invoke();
            }
        }
    }
}
