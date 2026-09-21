using RiftArena.Character;
using UnityEngine;

namespace RiftArena.Combat
{
    /// <summary>
    /// Marks the region of a character that can receive hits. A HitboxComponent on the
    /// opposing character overlap-checks against this collider and, on a hit, calls
    /// ApplyHit here so damage and knockback always flow through one place per character.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class HurtboxComponent : MonoBehaviour
    {
        [SerializeField] private HealthComponent health;
        [SerializeField] private Rigidbody body;
        [SerializeField] private Fighter fighter;
        [SerializeField] private float knockbackVelocityPerUnit = 1f;

        [Tooltip("Damage and knockback are multiplied by this while blocking (BlockState toggles IsBlocking on/off). 0.2 = blocked hits still chip through 20% damage; 0 = full immunity.")]
        [SerializeField, Range(0f, 1f)] private float blockedDamageMultiplier = 0.2f;

        [Header("Audio")]
        [SerializeField] private AudioClip hitImpactClip;
        [SerializeField] private AudioClip blockedImpactClip;

        private BoxCollider box;

        public BoxCollider Box => box;

        /// <summary>Set by BlockState while the character is in its blocking window.</summary>
        public bool IsBlocking { get; private set; }

        private void Awake()
        {
            box = GetComponent<BoxCollider>();
            box.isTrigger = true;

            if (health == null) health = GetComponentInParent<HealthComponent>();
            if (body == null) body = GetComponentInParent<Rigidbody>();
            if (fighter == null) fighter = GetComponentInParent<Fighter>();
        }

        public void SetBlocking(bool value)
        {
            IsBlocking = value;
        }

        /// <summary>
        /// Applies a landed hit: damage through HealthComponent (the only legal way to
        /// mutate HP) and a knockback impulse along the fighting plane's X axis, pushing
        /// this character away from the attacker's position. Both are scaled down by
        /// blockedDamageMultiplier while IsBlocking is true.
        /// </summary>
        public void ApplyHit(MoveData move, Vector3 attackerPosition)
        {
            if (health == null || move == null) return;

            float multiplier = IsBlocking ? blockedDamageMultiplier : 1f;

            int damage = Mathf.RoundToInt(move.damage * multiplier);
            health.ApplyDamage(damage);

            if (fighter != null)
            {
                fighter.PlaySfx(IsBlocking ? blockedImpactClip : hitImpactClip);
                if (!IsBlocking) fighter.PlaySfx(fighter.DamageGruntClip);
            }

            if (body != null)
            {
                float direction = Mathf.Sign(transform.position.x - attackerPosition.x);
                if (direction == 0f) direction = 1f;
                Vector3 velocity = body.linearVelocity;
                velocity.x = direction * move.knockback * multiplier * knockbackVelocityPerUnit;
                body.linearVelocity = velocity;
            }
        }

        private void OnDrawGizmos()
        {
            var col = GetComponent<BoxCollider>();
            if (col == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(col.center, col.size);
        }
    }
}
