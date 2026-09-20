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

        /// <summary>True on the physics frame the jump button was pressed down.</summary>
        bool JumpPressed { get; }

        /// <summary>True on the physics frame Light Punch was pressed down.</summary>
        bool LightPunchPressed { get; }

        /// <summary>True on the physics frame Heavy Punch was pressed down.</summary>
        bool HeavyPunchPressed { get; }

        /// <summary>
        /// Called once per FixedUpdate by the owning CharacterController, before any
        /// of the properties above are read, so "pressed this frame" edges are correct.
        /// </summary>
        void Tick();
    }
}
