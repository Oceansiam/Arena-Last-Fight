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
        [SerializeField] private float knockbackVelocityPerUnit = 1f;

        private BoxCollider box;

        public BoxCollider Box => box;

        private void Awake()
        {
            box = GetComponent<BoxCollider>();
            box.isTrigger = true;

            if (health == null) health = GetComponentInParent<HealthComponent>();
            if (body == null) body = GetComponentInParent<Rigidbody>();
        }

        /// <summary>
        /// Applies a landed hit: damage through HealthComponent (the only legal way to
        /// mutate HP) and a knockback impulse along the fighting plane's X axis, pushing
        /// this character away from the attacker's position.
        /// </summary>
        public void ApplyHit(MoveData move, Vector3 attackerPosition)
        {
            if (health == null || move == null) return;

            health.ApplyDamage(move.damage);

            if (body != null)
            {
                float direction = Mathf.Sign(transform.position.x - attackerPosition.x);
                if (direction == 0f) direction = 1f;
                Vector3 velocity = body.linearVelocity;
                velocity.x = direction * move.knockback * knockbackVelocityPerUnit;
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
