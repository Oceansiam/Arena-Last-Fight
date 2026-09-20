using UnityEngine;

namespace RiftArena.Input
{
    /// <summary>
    /// Production control scheme for Player 1. This is the scheme that will later be
    /// replaced by network-driven input in MVP1.5 — keep it exactly as specified:
    ///   Arrow Left / Arrow Right = walk (X axis)
    ///   Q / E = move back / forward (Z axis)
    ///   Arrow Up = jump
    ///   W = Light Punch, A = Heavy Punch
    ///   (S / D reserved for Light Kick / Heavy Kick in MVP2 — intentionally unwired)
    /// </summary>
    public class PlayerOneInputReader : IInputReader
    {
        public float MoveAxis { get; private set; }
        public float MoveAxisForward { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool LightPunchPressed { get; private set; }
        public bool HeavyPunchPressed { get; private set; }

        public void Tick()
        {
            float axis = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.LeftArrow)) axis -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.RightArrow)) axis += 1f;
            MoveAxis = axis;

            float axisForward = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.Q)) axisForward -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.E)) axisForward += 1f;
            MoveAxisForward = axisForward;

            JumpPressed = UnityEngine.Input.GetKeyDown(KeyCode.UpArrow);
            LightPunchPressed = UnityEngine.Input.GetKeyDown(KeyCode.W);
            HeavyPunchPressed = UnityEngine.Input.GetKeyDown(KeyCode.A);

            // S (Light Kick) and D (Heavy Kick) are reserved for MVP2 and intentionally
            // left unread here.
        }
    }
}
