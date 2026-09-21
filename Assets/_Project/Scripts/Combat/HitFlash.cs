using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RiftArena.Combat
{
    /// <summary>
    /// Briefly tints every renderer under this character red when HealthComponent reports
    /// damage, then fades back. Uses Renderer.material (an auto-created per-instance
    /// copy) rather than sharedMaterial, so this never bleeds the flash color into other
    /// objects using the same material asset.
    /// </summary>
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] private HealthComponent health;
        [SerializeField] private HurtboxComponent hurtbox;
        [SerializeField] private Color flashColor = Color.red;
        [SerializeField] private Color blockFlashColor = Color.cyan;
        [SerializeField] private float flashDuration = 0.35f;

        private Renderer[] renderers;
        private Color[] originalColors;
        private Coroutine flashRoutine;

        private void Awake()
        {
            if (health == null) health = GetComponentInParent<HealthComponent>();
            if (hurtbox == null) hurtbox = GetComponentInParent<HurtboxComponent>();

            renderers = GetComponentsInChildren<Renderer>(true);
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalColors[i] = renderers[i].material.color;
            }
        }

        private void OnEnable()
        {
            if (health != null) health.OnDamaged += HandleDamaged;
        }

        private void OnDisable()
        {
            if (health != null) health.OnDamaged -= HandleDamaged;
        }

        private void HandleDamaged(int currentHealth, int maxHealth)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            // Blocked hits flash a distinct color from unblocked ones, so successfully
            // blocking is visually obvious instead of looking identical to getting hit.
            Color color = (hurtbox != null && hurtbox.IsBlocking) ? blockFlashColor : flashColor;
            flashRoutine = StartCoroutine(FlashRoutine(color));
        }

        private IEnumerator FlashRoutine(Color color)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].material.color = color;
            }

            yield return new WaitForSeconds(flashDuration);

            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].material.color = originalColors[i];
            }

            flashRoutine = null;
        }
    }
}
