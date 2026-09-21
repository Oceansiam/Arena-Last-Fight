namespace RiftArena.Input
{
    /// <summary>
    /// Abstraction over "where does this character's input come from".
    /// Exists so a future MVP1.5 pass can swap in network-driven input
    /// without touching CharacterController or the state machine.
    /// </summary>
    public interface IInputReader
    {
        /// <summary>-1 = walking left, 0 = idle, 1 = walking right.</summary>
        float MoveAxis { get; }

        /// <summary>-1 = moving backward (-Z), 0 = idle, 1 = moving forward (+Z).</summary>
        float MoveAxisForward { get; }

        /// <summary>
        /// True since the last ConsumeFrame() if the jump button was pressed down at any
        /// point during that window - latched rather than a raw single-frame read, so a
        /// press landing between two FixedUpdate steps is never missed (see Sample()).
        /// </summary>
        bool JumpPressed { get; }

        /// <summary>Latched Light Punch press - see JumpPressed for why this is latched.</summary>
        bool LightPunchPressed { get; }

        /// <summary>Latched Block press - see JumpPressed for why this is latched.</summary>
        bool BlockPressed { get; }

        /// <summary>
        /// Called once per rendered frame (Update, not FixedUpdate) by the owning
        /// Fighter. Movement axes are simply re-read every call; button presses are
        /// OR-latched into the *Pressed properties so a GetKeyDown edge that lands on a
        /// render frame with no matching FixedUpdate isn't lost before the state machine
        /// gets to see it.
        /// </summary>
        void Sample();

        /// <summary>
        /// Called once per FixedUpdate by the owning Fighter, after the state machine has
        /// read this frame's *Pressed flags, to clear the latches before the next
        /// Sample() window starts.
        /// </summary>
        void ConsumeFrame();
    }
}
