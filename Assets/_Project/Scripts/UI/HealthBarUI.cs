using RiftArena.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace RiftArena.UI
{
    /// <summary>
    /// Binds a Slider to a HealthComponent's OnDamaged event. Function over form for
    /// MVP1 - no polish, just a bar that moves.
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private HealthComponent target;

        private Slider slider;

        // Only updates the target reference and the immediate visual value here.
        // Subscription itself happens in OnEnable/OnDisable so it can never double-subscribe
        // regardless of whether SetTarget is called before or during play.
        public void SetTarget(HealthComponent health)
        {
            bool wasSubscribed = enabled && gameObject.activeInHierarchy && target != null;
            if (wasSubscribed) target.OnDamaged -= HandleDamaged;

            target = health;
            if (slider == null) slider = GetComponent<Slider>();

            if (target != null)
            {
                slider.minValue = 0f;
                slider.maxValue = target.MaxHealth;
                slider.value = target.CurrentHealth;
            }

            if (wasSubscribed && target != null) target.OnDamaged += HandleDamaged;
        }

        private void Awake()
        {
            if (slider == null) slider = GetComponent<Slider>();
        }

        private void OnEnable()
        {
            if (target != null)
            {
                slider.maxValue = target.MaxHealth;
                slider.value = target.CurrentHealth;
                target.OnDamaged += HandleDamaged;
            }
        }

        private void OnDisable()
        {
            if (target != null) target.OnDamaged -= HandleDamaged;
        }

        private void HandleDamaged(int current, int max)
        {
            slider.maxValue = max;
            slider.value = current;
        }
    }
}
