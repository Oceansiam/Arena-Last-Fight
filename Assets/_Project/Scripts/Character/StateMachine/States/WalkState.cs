using RiftArena.Character.FSM;
using UnityEngine;

namespace RiftArena.Character.States
{
    public class WalkState : IState
    {
        private readonly Fighter fighter;

        public WalkState(Fighter fighter)
        {
            this.fighter = fighter;
        }

        public void Enter()
        {
            fighter.Animator.CrossFade("Walk", 0.1f); 
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

            if (input.HeavyPunchPressed)
            {
                fighter.AttackState.SetMove(fighter.HeavyPunchMove);
                fighter.Machine.ChangeState(fighter.AttackState);
                return;
            }

            if (input.JumpPressed && fighter.IsGrounded)
            {
                fighter.Machine.ChangeState(fighter.JumpState);
                return;
            }

            if (Mathf.Approximately(input.MoveAxis, 0f) && Mathf.Approximately(input.MoveAxisForward, 0f))
            {
                fighter.Machine.ChangeState(fighter.IdleState);
                return;
            }

            Vector3 v = fighter.Body.linearVelocity;
            v.x = input.MoveAxis * fighter.MoveSpeed;
            v.z = input.MoveAxisForward * fighter.MoveSpeed;
            fighter.Body.linearVelocity = v;
        }

        public void Exit()
        {
        }
    }
}
