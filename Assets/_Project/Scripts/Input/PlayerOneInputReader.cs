using UnityEngine;

namespace RiftArena.Input
{
    /// <summary>
    /// Production control scheme, shared by both players now that each will read
    /// input on their own local machine once network play (MVP1.5) is wired up:
    ///   W / S = move forward / back (Z axis)
    ///   A / D = move left / right (X axis)
    ///   Space = jump
    ///   J = Light Punch, K = Block
    /// </summary>
    public class PlayerOneInputReader : IInputReader
    {
        public float MoveAxis { get; private set; }
        public float MoveAxisForward { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool LightPunchPressed { get; private set; }
        public bool BlockPressed { get; private set; }

        public void Tick()
        {
            float axis = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.A)) axis -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.D)) axis += 1f;
            MoveAxis = axis;

            float axisForward = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.S)) axisForward -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.W)) axisForward += 1f;
            MoveAxisForward = axisForward;

            JumpPressed = UnityEngine.Input.GetKeyDown(KeyCode.Space);
            LightPunchPressed = UnityEngine.Input.GetKeyDown(KeyCode.J);
            BlockPressed = UnityEngine.Input.GetKeyDown(KeyCode.K);
        }
    }
}
