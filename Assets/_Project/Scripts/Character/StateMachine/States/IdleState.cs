using RiftArena.Character.FSM;
using UnityEngine;

namespace RiftArena.Character.States
{
    public class IdleState : IState
    {
        private readonly Fighter fighter;

        public IdleState(Fighter fighter)
        {
            this.fighter = fighter;
        }

        public void Enter()
        {
            Vector3 v = fighter.Body.linearVelocity;
            fighter.Body.linearVelocity = new Vector3(0f, v.y, 0f);
            fighter.Animator.CrossFade("Idle", 0.1f);
        }

        public void Tick()
        {
            var input = fighter.InputReader;

            if (input.LightPunchPressed)
            {
                fighter.AttackState.SetMove(fighter.LightPunchMove);
                fighter.Machine.ChangeState(fighter.AttackState);
                return;
            }

            if (input.BlockPressed)
            {
                fighter.Machine.ChangeState(fighter.BlockState);
                return;
            }

            if (input.JumpPressed && fighter.IsGrounded)
            {
                fighter.Machine.ChangeState(fighter.JumpState);
                return;
            }

            if (!Mathf.Approximately(input.MoveAxis, 0f) || !Mathf.Approximately(input.MoveAxisForward, 0f))
            {
                fighter.Machine.ChangeState(fighter.WalkState);
                return;
            }
        }

        public void Exit()
        {
        }
    }
}
