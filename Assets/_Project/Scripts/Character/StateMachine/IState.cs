namespace RiftArena.Character.FSM
{
    /// <summary>
    /// One behavioral state of a character (Idle, Walk, Jump, Attack, Dead, ...).
    /// Ticked from FixedUpdate so combat timing stays frame-based rather than
    /// wall-clock based.
    /// </summary>
    public interface IState
    {
        void Enter();
        void Tick();
        void Exit();
    }
}
