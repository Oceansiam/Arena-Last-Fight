using RiftArena.Character.FSM;
using RiftArena.Combat;
using RiftArena.Input;
using UnityEngine;

namespace RiftArena.Character.States
{
    /// <summary>
    /// Generic attack state parametrized by a MoveData asset - one class handles every
    /// move (currently just Light Punch, but any future move) rather than one state
    /// class per attack. Timing is driven purely by FixedUpdate frame counts read from
    /// the MoveData, per the project's frame-based (rollback-friendly) timing rule.
    ///
    /// Combo chaining: while a move is in its Active or Recovery frames, the next
    /// Light Punch press is buffered into <see cref="queuedMove"/> (first press wins,
    /// no overwriting). Once the current move's TotalFrames elapse, a buffered move
    /// plays immediately by re-running Enter() directly - never through
    /// StateMachine.ChangeState, which no-ops on a same-state transition.
    /// </summary>
    public class AttackState : IState
    {
        private enum Phase { Startup, Active, Recovery }

        private readonly Fighter fighter;
        private MoveData move;
        private MoveData queuedMove;
        private Phase phase;
        private int frameCounter;
        private int comboCount;

        public AttackState(Fighter fighter)
        {
            this.fighter = fighter;
        }

        /// <summary>How many hits deep the current combo chain is (1 = first hit, 2 = first chained hit, ...).</summary>
        public int ComboCount => comboCount;

        /// <summary>Must be called before ChangeState(AttackState) to pick which move plays.</summary>
        public void SetMove(MoveData moveToPlay)
        {
            move = moveToPlay;
        }

        public void Enter()
        {
            frameCounter = 0;
            phase = Phase.Startup;
            queuedMove = null;
            comboCount++;

            Debug.Log($"[Combo] hit {comboCount}: {move.id}");

            fighter.Animator.CrossFade("Attack", 0.05f);

            // Attacks root the character - no movement while attacking in MVP1.
            Vector3 v = fighter.Body.linearVelocity;
            fighter.Body.linearVelocity = new Vector3(0f, v.y, 0f);
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
                    BufferComboInput();

                    if (frameCounter >= move.startupFrames + move.activeFrames)
                    {
                        phase = Phase.Recovery;
                        fighter.Hitbox.EndActive();
                    }
                    break;

                case Phase.Recovery:
                    BufferComboInput();

                    if (frameCounter >= move.TotalFrames)
                    {
                        if (queuedMove != null)
                        {
                            // Combo continues: replay Enter() directly on this same
                            // state instance instead of going through
                            // StateMachine.ChangeState (which would no-op since we're
                            // already the current state).
                            MoveData next = queuedMove;
                            SetMove(next);
                            Enter();
                        }
                        else
                        {
                            fighter.Machine.ChangeState(fighter.IdleState);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Records the next move to chain into on the first Light Punch press seen
        /// during this move's Active or Recovery frames. Later presses in the same
        /// window are ignored - only one buffered input per activation.
        /// </summary>
        private void BufferComboInput()
        {
            if (queuedMove != null) return;

            IInputReader input = fighter.InputReader;
            if (input.LightPunchPressed) queuedMove = fighter.LightPunchMove;
        }

        public void Exit()
        {
            // Defensive cleanup in case the state is exited mid-active-window
            // (e.g. this character was defeated mid-swing).
            fighter.Hitbox.EndActive();
            move = null;
            queuedMove = null;
            comboCount = 0;
        }
    }
}
