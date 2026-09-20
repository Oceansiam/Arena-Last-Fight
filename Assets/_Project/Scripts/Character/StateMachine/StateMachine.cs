namespace RiftArena.Character.FSM
{
    /// <summary>
    /// Holds the current IState and the transition mechanics (Enter/Exit ordering).
    /// Deliberately contains no gameplay rules of its own — those live in the
    /// individual IState implementations.
    /// </summary>
    public class StateMachine
    {
        public IState CurrentState { get; private set; }

        public void ChangeState(IState newState)
        {
            if (newState == null || newState == CurrentState) return;

            CurrentState?.Exit();
            CurrentState = newState;
            CurrentState.Enter();
        }

        public void Tick()
        {
            CurrentState?.Tick();
        }
    }
}
