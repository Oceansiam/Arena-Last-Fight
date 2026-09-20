using UnityEngine;

namespace RiftArena.Combat
{
    /// <summary>
    /// Data-driven description of a single attack. AttackState is generic and reads one
    /// of these rather than each move being its own hardcoded C# class, so adding a new
    /// move later is "create an asset", not "write a new state".
    ///
    /// Timing is expressed in FixedUpdate frames (not seconds) on purpose: frame counting
    /// is deterministic and keeps the door open for future rollback netcode, whereas
    /// Time.deltaTime / coroutines with real-time delays are not deterministic across
    /// machines.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMoveData", menuName = "Rift Arena/Move Data")]
    public class MoveData : ScriptableObject
    {
        [Tooltip("Unique identifier for this move, e.g. \"light_punch\".")]
        public string id;

        [TextArea]
        public string description;

        [Header("Damage & Knockback")]
        public int damage;
        public float knockback;

        [Header("Timing (FixedUpdate frames)")]
        [Tooltip("Frames before the hitbox becomes active. No hitbox, no movement lock beyond being committed to the move.")]
        public int startupFrames;
        [Tooltip("Frames the hitbox is active and can land a hit.")]
        public int activeFrames;
        [Tooltip("Frames after the active window during which the attacker cannot act.")]
        public int recoveryFrames;

        public int TotalFrames => startupFrames + activeFrames + recoveryFrames;
    }
}
