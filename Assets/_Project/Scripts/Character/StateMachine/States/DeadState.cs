using RiftArena.Character.FSM;

namespace RiftArena.Character.States
{
    /// <summary>
    /// Terminal state entered when this character's HealthComponent hits zero. Stops
    /// reacting to input; the character is free to keep sliding from residual knockback
    /// but attacks no longer connect for it (the shared Hitbox never activates again).
    /// </summary>
    public class DeadState : IState
    {
        private readonly Fighter fighter;

        public DeadState(Fighter fighter)
        {
            this.fighter = fighter;
        }

        public void Enter()
        {
            fighter.Hitbox.EndActive();
            fighter.Animator.CrossFade("Death", 0.1f);
        }

        public void Tick()
        {
            // Intentionally inert - dead characters do not act. MatchManager handles
            // freezing both fighters and the win/reset flow.
        }

        public void Exit()
        {
        }
    }
}
