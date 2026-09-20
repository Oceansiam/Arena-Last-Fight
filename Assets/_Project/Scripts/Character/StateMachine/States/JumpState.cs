using RiftArena.Character.FSM;
using UnityEngine;

namespace RiftArena.Character.States
{
    /// <summary>
    /// Covers both rise and fall of a simple no-double-jump parabola (gravity via the
    /// Rigidbody does the arc; this state just launches upward velocity and waits for
    /// landing). Folding "Fall" into "Jump" is an intentional MVP1 simplification.
    /// </summary>
    public class JumpState : IState
    {
        private readonly Fighter fighter;

        public JumpState(Fighter fighter)
        {
            this.fighter = fighter;
        }

        public void Enter()
        {
            Vector3 v = fighter.Body.linearVelocity;
            v.y = fighter.JumpVelocity;
            fighter.Body.linearVelocity = v;
        }

        public void Tick()
        {
            var input = fighter.InputReader;

            // Simple aerial drift control; no air attacks in MVP1.
            Vector3 v = fighter.Body.linearVelocity;
            v.x = input.MoveAxis * fighter.MoveSpeed;
            v.z = input.MoveAxisForward * fighter.MoveSpeed;
            fighter.Body.linearVelocity = v;

            if (fighter.IsGrounded)
            {
                bool stillMoving = !Mathf.Approximately(input.MoveAxis, 0f) || !Mathf.Approximately(input.MoveAxisForward, 0f);
                fighter.Machine.ChangeState(
                    stillMoving ? fighter.WalkState : (RiftArena.Character.FSM.IState)fighter.IdleState);
            }
        }

        public void Exit()
        {
            // Snap to ground level to avoid slow sub-physics-step sinking/penetration.
            Vector3 pos = fighter.Body.position;
            pos.y = fighter.GroundY;
            fighter.Body.position = pos;
        }
    }
}
