using RiftArena.Character.FSM;

namespace RiftArena.Character.States
{
    /// <summary>
    /// Fixed-length defensive stance: press once, plays the full Block animation and
    /// commits for its duration (no early cancel), then returns to Idle. While active,
    /// HurtboxComponent.IsBlocking is set so incoming hits are scaled down instead of
    /// landing at full damage/knockback (see HurtboxComponent.ApplyHit).
    /// </summary>
    public class BlockState : IState
    {
        // Frame-based like AttackState's MoveData timing, but Block has no MoveData of
        // its own (it deals no damage) so the duration is just a fixed frame count here.
        private const int BlockDurationFrames = 30;

        private readonly Fighter fighter;
        private int frameCounter;

        public BlockState(Fighter fighter)
        {
            this.fighter = fighter;
        }

        public void Enter()
        {
            frameCounter = 0;

            // Blocking roots the character, same as attacking.
            var v = fighter.Body.linearVelocity;
            fighter.Body.linearVelocity = new UnityEngine.Vector3(0f, v.y, 0f);

            fighter.Hurtbox.SetBlocking(true);
            fighter.Animator.CrossFade("Block", 0.05f);
        }

        public void Tick()
        {
            frameCounter++;
            if (frameCounter >= BlockDurationFrames)
            {
                fighter.Machine.ChangeState(fighter.IdleState);
            }
        }

        public void Exit()
        {
            fighter.Hurtbox.SetBlocking(false);
        }
    }
}
