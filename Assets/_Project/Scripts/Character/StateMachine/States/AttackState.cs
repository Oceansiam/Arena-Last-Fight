using RiftArena.Character.FSM;
using RiftArena.Combat;
using UnityEngine;

namespace RiftArena.Character.States
{
    /// <summary>
    /// Generic attack state parametrized by a MoveData asset - one class handles every
    /// move (Light Punch, Heavy Punch, and any future move) rather than one state class
    /// per attack. Timing is driven purely by FixedUpdate frame counts read from the
    /// MoveData, per the project's frame-based (rollback-friendly) timing rule.
    /// </summary>
    public class AttackState : IState
    {
        private enum Phase { Startup, Active, Recovery }

        private readonly Fighter fighter;
        private MoveData move;
        private Phase phase;
        private int frameCounter;

        public AttackState(Fighter fighter)
        {
            this.fighter = fighter;
        }

        /// <summary>Must be called before ChangeState(AttackState) to pick which move plays.</summary>
        public void SetMove(MoveData moveToPlay)
        {
            move = moveToPlay;
        }

        public void Enter()
        {
            frameCounter = 0;
            phase = Phase.Startup;

            // Attacks root the character - no movement while attacking in MVP1.
            Vector3 v = fighter.Body.linearVelocity;
            fighter.Body.linearVelocity = new Vector3(0f, v.y, 0f);
            fighter.Animator.CrossFade("Attack", 0.05f);
        }

        public void Tick()
        {
            if (move == null)
            {
                fighter.Machine.ChangeState(fighter.IdleState);
                return;
            }

            frameCounter++;

            switch (phase)
            {
                case Phase.Startup:
                    if (frameCounter >= move.startupFrames)
                    {
                        phase = Phase.Active;
                        fighter.Hitbox.BeginActive(move);
                    }
                    break;

                case Phase.Active:
                    if (frameCounter >= move.startupFrames + move.activeFrames)
                    {
                        phase = Phase.Recovery;
                        fighter.Hitbox.EndActive();
                    }
                    break;

                case Phase.Recovery:
                    if (frameCounter >= move.TotalFrames)
                    {
                        fighter.Machine.ChangeState(fighter.IdleState);
                    }
                    break;
            }
        }

        public void Exit()
        {
            // Defensive cleanup in case the state is exited mid-active-window
            // (e.g. this character was defeated mid-swing).
            fighter.Hitbox.EndActive();
            move = null;
        }
    }
}
