using UnityEngine;

namespace RiftArena.Input
{
    /// <summary>
    /// Player One's local control scheme (separate from Player Two so both can be
    /// tested on one keyboard):
    ///   W / S = move forward / back (Z axis)
    ///   A / D = move left / right (X axis)
    ///   Space = jump
    ///   V = Light Punch, B = Block
    /// </summary>
    public class PlayerOneInputReader : IInputReader
    {
        public float MoveAxis { get; private set; }
        public float MoveAxisForward { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool LightPunchPressed { get; private set; }
        public bool BlockPressed { get; private set; }

        public void Sample()
        {
            float axis = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.A)) axis -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.D)) axis += 1f;
            MoveAxis = axis;

            float axisForward = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.S)) axisForward -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.W)) axisForward += 1f;
            MoveAxisForward = axisForward;

            // OR-latch: a GetKeyDown edge on a render frame that falls between two
            // FixedUpdate steps must not be overwritten back to false by a later Sample()
            // call in the same window before FixedUpdate ever sees it.
            if (UnityEngine.Input.GetKeyDown(KeyCode.Space)) JumpPressed = true;
            if (UnityEngine.Input.GetKeyDown(KeyCode.V)) LightPunchPressed = true;
            if (UnityEngine.Input.GetKeyDown(KeyCode.B)) BlockPressed = true;
        }

        public void ConsumeFrame()
        {
            JumpPressed = false;
            LightPunchPressed = false;
            BlockPressed = false;
        }
    }
}
