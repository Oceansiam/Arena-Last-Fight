using RiftArena.Character.FSM;
using RiftArena.Character.States;
using RiftArena.CharacterSelect;
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
        [Tooltip("If true, uses PlayerOneInputReader (WASD/Space/V/B). If false, uses the DEBUG-ONLY PlayerTwoDebugInputReader local stand-in (Arrows/RightShift/J/K).")]
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

        [Header("Character Select")]
        [Tooltip("Same roster asset the CharacterSelect scene cycles through. If CharacterSelection has a pick for this player, the pre-placed Model child below is swapped out for it in Awake. Leave empty (or skip select) to just use whatever model is already on the Fighter.")]
        [SerializeField] private CharacterRoster characterRoster;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip attackEffortClip;
        [SerializeField] private AudioClip damageGruntClip;
        public AudioClip AttackEffortClip => attackEffortClip;
        public AudioClip DamageGruntClip => damageGruntClip;

        private Rigidbody body;
        private StateMachine stateMachine;
        private IInputReader inputReader;
        private float groundY;
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

        /// <summary>Plays a one-shot SFX on this character's own AudioSource (attack effort, damage grunt, hit/block impact, ...).</summary>
        public void PlaySfx(AudioClip clip)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
        }

        /// <summary>
        /// If the player went through CharacterSelect, swaps the pre-placed "Model" child
        /// for whichever character they picked. No-ops (keeping the scene's own model)
        /// when there's no selection - e.g. opening MVP1_Arena directly to test.
        /// </summary>
        private void ApplySelectedCharacterModel()
        {
            if (characterRoster == null || !CharacterSelection.HasSelection) return;

            int index = isPlayerOne ? CharacterSelection.PlayerOneCharacterIndex : CharacterSelection.PlayerTwoCharacterIndex;
            if (index < 0 || characterRoster.characters == null || index >= characterRoster.characters.Length) return;

            CharacterDefinition def = characterRoster.characters[index];
            if (def == null || def.modelPrefab == null) return;

            Transform oldModel = transform.Find("Model");
            Vector3 localPos = oldModel != null ? oldModel.localPosition : Vector3.zero;
            if (oldModel != null) Destroy(oldModel.gameObject);

            GameObject newModel = Instantiate(def.modelPrefab, transform);
            newModel.name = "Model";
            newModel.transform.localPosition = localPos;
            newModel.transform.localRotation = Quaternion.identity;
            newModel.transform.localScale = Vector3.one;

            Animator newAnimator = newModel.GetComponent<Animator>();
            if (newAnimator != null)
            {
                newAnimator.applyRootMotion = false;
                if (def.animatorController != null) newAnimator.runtimeAnimatorController = def.animatorController;
                animator = newAnimator;
            }

            // Roster model prefabs (Maria/Paladin) don't ship with their own HitFlash -
            // the scene's pre-placed "Model" did, and it just got destroyed above. Without
            // this, going through Character Select silently loses the hit/block flash for
            // every match. HitFlash.Awake() resolves health/hurtbox via
            // GetComponentInParent on its own, so no manual wiring is needed here.
            if (newModel.GetComponentInChildren<HitFlash>() == null)
            {
                newModel.AddComponent<HitFlash>();
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (health == null) health = GetComponent<HealthComponent>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();

            ApplySelectedCharacterModel();

            // Rotation stays locked (characters never tip over); X/Z are both free now
            // so the ring supports full ground-plane movement, clamped in FixedUpdate.
            body.constraints = RigidbodyConstraints.FreezeRotationX
                                | RigidbodyConstraints.FreezeRotationY
                                | RigidbodyConstraints.FreezeRotationZ;

            groundY = transform.position.y;

            if (hitbox != null)
            {
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

            if (health != null)
            {
                health.OnDeath += HandleOwnDeath;
            }

            // Without this, the two fighters' own body colliders physically push each
            // other around (jitter/knockback fighting the intended combat knockback)
            // whenever they walk into one another. Hit detection doesn't rely on physics
            // trigger callbacks (HitboxComponent polls bounds overlap manually), so
            // ignoring collision here doesn't affect combat.
            if (opponent != null)
            {
                Collider[] myColliders = GetComponents<Collider>();
                Collider[] opponentColliders = opponent.GetComponents<Collider>();
                foreach (Collider mine in myColliders)
                {
                    foreach (Collider theirs in opponentColliders)
                    {
                        Physics.IgnoreCollision(mine, theirs, true);
                    }
                }
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

        private void Update()
        {
            // Sampled every rendered frame (not FixedUpdate) so a GetKeyDown edge never
            // lands in a render frame with no corresponding physics step and gets missed
            // - see IInputReader.Sample(). FixedUpdate below consumes and clears it.
            if (!frozen)
            {
                inputReader.Sample();
            }
        }

        private void FixedUpdate()
        {
            if (!frozen)
            {
                stateMachine.Tick();
                inputReader.ConsumeFrame();
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
