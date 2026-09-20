using RiftArena.Character.FSM;
using RiftArena.Character.States;
using RiftArena.Combat;
using RiftArena.Input;
using UnityEngine;

namespace RiftArena.Character
{
    /// <summary>
    /// Thin coordinator ("CharacterController" in the design doc — named Fighter here to
    /// avoid colliding with UnityEngine.CharacterController). It owns references to the
    /// StateMachine, InputReader, Rigidbody, and combat components, and forwards input
    /// into the state machine each physics step. It intentionally contains NO
    /// attack/combo/damage rules — those live in the IState classes and MoveData/Hitbox.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Fighter : MonoBehaviour
    {
        [Header("Player Identity")]
        [Tooltip("If true, uses PlayerOneInputReader. If false, uses the DEBUG-ONLY PlayerTwoDebugInputReader local stand-in. Both currently read the same WASD/Space/J/K scheme.")]
        [SerializeField] private bool isPlayerOne = true;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpVelocity = 8f;

        [Header("Arena Bounds")]
        [Tooltip("Ground-plane movement is clamped to this rectangle so characters can't walk out of the ring. Tune these to match the ring model's actual footprint (see the gizmo in Scene view).")]
        [SerializeField] private float minX = -4f;
        [SerializeField] private float maxX = 4f;
        [SerializeField] private float minZ = -4f;
        [SerializeField] private float maxZ = 4f;

        [Header("Combat Wiring")]
        [SerializeField] private MoveData lightPunchMove;
        [SerializeField] private HitboxComponent hitbox;
        [SerializeField] private HurtboxComponent hurtbox;
        [SerializeField] private HealthComponent health;

        [Tooltip("The opposing character's Hurtbox. Re-applied to the Hitbox every Awake so wiring survives scene (de)serialization regardless of edit-time call order.")]
        [SerializeField] private HurtboxComponent targetHurtbox;

        [Tooltip("The opposing character. Used only to orient the hitbox toward them and for the debug facing flip - no gameplay rules here.")]
        [SerializeField] private Transform opponent;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        public Animator Animator => animator;

        private Rigidbody body;
        private StateMachine stateMachine;
        private IInputReader inputReader;
        private float groundY;
        private float hitboxBaseLocalX;
        private bool frozen;

        // States (one instance each, reused - avoids per-frame allocation).
        private IdleState idleState;
        private WalkState walkState;
        private JumpState jumpState;
        private AttackState attackState;
        private BlockState blockState;
        private DeadState deadState;

        public Rigidbody Body => body;
        public StateMachine Machine => stateMachine;
        public IInputReader InputReader => inputReader;
        public float MoveSpeed => moveSpeed;
        public float JumpVelocity => jumpVelocity;
        public float GroundY => groundY;
        public MoveData LightPunchMove => lightPunchMove;
        public HitboxComponent Hitbox => hitbox;
        public HurtboxComponent Hurtbox => hurtbox;
        public HealthComponent Health => health;

        public IdleState IdleState => idleState;
        public WalkState WalkState => walkState;
        public JumpState JumpState => jumpState;
        public AttackState AttackState => attackState;
        public BlockState BlockState => blockState;
        public DeadState DeadState => deadState;

        public bool IsGrounded => transform.position.y <= groundY + 0.01f && body.linearVelocity.y <= 0.01f;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (health == null) health = GetComponent<HealthComponent>();

            // Rotation stays locked (characters never tip over); X/Z are both free now
            // so the ring supports full ground-plane movement, clamped in FixedUpdate.
            body.constraints = RigidbodyConstraints.FreezeRotationX
                                | RigidbodyConstraints.FreezeRotationY
                                | RigidbodyConstraints.FreezeRotationZ;

            groundY = transform.position.y;

            if (hitbox != null)
            {
                hitboxBaseLocalX = Mathf.Abs(hitbox.transform.localPosition.x);

                // Re-apply regardless of whether this was wired at edit time via
                // SetTargetHurtbox (HitboxComponent's own target field is intentionally
                // not serialized, so this is the one place that's guaranteed to run).
                if (targetHurtbox != null) hitbox.SetTarget(targetHurtbox);
            }

            inputReader = isPlayerOne
                ? (IInputReader)new PlayerOneInputReader()
                : new PlayerTwoDebugInputReader();

            stateMachine = new StateMachine();
            idleState = new IdleState(this);
            walkState = new WalkState(this);
            jumpState = new JumpState(this);
            attackState = new AttackState(this);
            blockState = new BlockState(this);
            deadState = new DeadState(this);

            stateMachine.ChangeState(idleState);

            if (opponent != null)
                {
                    Collider myCollider = GetComponent<Collider>();
                    Collider opponentCollider = opponent.GetComponent<Collider>();
                    if (myCollider != null && opponentCollider != null)
                    {
                        Physics.IgnoreCollision(myCollider, opponentCollider, true);
                    }
                }

            if (health != null)
            {
                health.OnDeath += HandleOwnDeath;
            }
        }

        /// <summary>Wires this fighter's hitbox at the opposing fighter's hurtbox. Called by scene setup.</summary>
        public void SetTargetHurtbox(HurtboxComponent opponentHurtbox)
        {
            targetHurtbox = opponentHurtbox;
            if (hitbox != null) hitbox.SetTarget(opponentHurtbox);
        }

        public void SetOpponent(Transform opponentTransform)
        {
            opponent = opponentTransform;
        }

        /// <summary>True once MatchManager has frozen this character at the end of a match.</summary>
        public bool IsFrozen => frozen;

        /// <summary>Used by MatchManager to stop this character responding to input once the match ends.</summary>
        public void SetFrozen(bool value)
        {
            frozen = value;
            if (frozen && body != null)
            {
                Vector3 v = body.linearVelocity;
                body.linearVelocity = new Vector3(0f, v.y, 0f);
            }
        }

        private void HandleOwnDeath()
        {
            stateMachine.ChangeState(deadState);
        }

        private void FixedUpdate()
        {
            if (!frozen)
            {
                inputReader.Tick();
                stateMachine.Tick();
            }

            // Rotate the whole character to face the opponent every frame — full 360°
            // facing (not just left/right), so the hitbox naturally rotates along with
            // the model instead of needing a separate flip.
            if (opponent != null)
            {
                Vector3 toOpponent = opponent.position - transform.position;
                toOpponent.y = 0f;
                if (toOpponent.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(toOpponent);
                    Quaternion smoothed = Quaternion.RotateTowards(transform.rotation, targetRotation, 720f * Time.fixedDeltaTime);
                    body.MoveRotation(smoothed);
                }
            }

            // Keep the character inside the ring rectangle regardless of how it got
            // pushed there (walking, knockback, aerial drift).
            Vector3 clamped = transform.position;
            clamped.x = Mathf.Clamp(clamped.x, minX, maxX);
            clamped.z = Mathf.Clamp(clamped.z, minZ, maxZ);
            if (clamped != transform.position)
            {
                transform.position = clamped;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Visualizes the arena bounds rectangle at ground height so it can be
            // eyeballed against the ring model in Scene view while tuning min/max X/Z.
            Gizmos.color = Color.cyan;
            Vector3 center = new Vector3((minX + maxX) * 0.5f, transform.position.y, (minZ + maxZ) * 0.5f);
            Vector3 size = new Vector3(maxX - minX, 0.01f, maxZ - minZ);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
