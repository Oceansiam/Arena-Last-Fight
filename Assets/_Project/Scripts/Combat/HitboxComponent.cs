using UnityEngine;

namespace RiftArena.Combat
{
    /// <summary>
    /// The "can deal damage" half of the hit pair. AttackState enables this only during
    /// a move's Active frames (BeginActive/EndActive) and it overlap-checks against a
    /// single assigned opponent HurtboxComponent every FixedUpdate while active, applying
    /// damage at most once per activation so one active window can't multi-hit.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class HitboxComponent : MonoBehaviour
    {
        [SerializeField] private BoxCollider box;

        private HurtboxComponent target;
        private MoveData currentMove;
        private bool isActive;
        private bool hasHitThisActivation;

        private void Awake()
        {
            if (box == null) box = GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.enabled = false;
        }

        /// <summary>The single opponent hurtbox this hitbox is allowed to hit. Set once at setup — MVP1 is strictly 1v1.</summary>
        public void SetTarget(HurtboxComponent opponentHurtbox)
        {
            target = opponentHurtbox;
        }

        public void BeginActive(MoveData move)
        {
            currentMove = move;
            hasHitThisActivation = false;
            isActive = true;
            box.enabled = true;
        }

        public void EndActive()
        {
            isActive = false;
            box.enabled = false;
            currentMove = null;
        }

        private void FixedUpdate()
        {
            if (!isActive || hasHitThisActivation || target == null || currentMove == null) return;

            if (Overlaps(target.Box))
            {
                hasHitThisActivation = true;
                target.ApplyHit(currentMove, transform.position);
            }
        }

        private bool Overlaps(BoxCollider targetBox)
        {
            Bounds a = BoundsInWorld(box);
            Bounds b = BoundsInWorld(targetBox);
            return a.Intersects(b);
        }

        private static Bounds BoundsInWorld(BoxCollider col)
        {
            // World-space AABB approximation of the (possibly rotated) box collider.
            // Good enough for MVP1's axis-aligned, non-rotated fighter capsules.
            Vector3 worldCenter = col.transform.TransformPoint(col.center);
            Vector3 worldSize = Vector3.Scale(col.size, col.transform.lossyScale);
            return new Bounds(worldCenter, worldSize);
        }

        private void OnDrawGizmos()
        {
            var col = box != null ? box : GetComponent<BoxCollider>();
            if (col == null) return;
            Gizmos.color = isActive ? Color.red : new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(col.center, col.size);
        }
    }
}
