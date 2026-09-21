using UnityEngine;

namespace RiftArena.Input
{
    // DEBUG ONLY — MVP1 local testing stand-in for Player 2.
    // Replaced by network input in MVP1.5. Do not ship.
    //
    // Separate scheme from Player One so both can be tested on one keyboard:
    //   Up / Down Arrow = move forward / back (Z axis)
    //   Left / Right Arrow = move left / right (X axis)
    //   Right Shift = jump
    //   J = Light Punch, K = Block
    public class PlayerTwoDebugInputReader : IInputReader
    {
        public float MoveAxis { get; private set; }
        public float MoveAxisForward { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool LightPunchPressed { get; private set; }
        public bool BlockPressed { get; private set; }

        public void Sample()
        {
            float axis = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.LeftArrow)) axis -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.RightArrow)) axis += 1f;
            MoveAxis = axis;

            float axisForward = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.DownArrow)) axisForward -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.UpArrow)) axisForward += 1f;
            MoveAxisForward = axisForward;

            // OR-latch: a GetKeyDown edge on a render frame that falls between two
            // FixedUpdate steps must not be overwritten back to false by a later Sample()
            // call in the same window before FixedUpdate ever sees it.
            if (UnityEngine.Input.GetKeyDown(KeyCode.RightShift)) JumpPressed = true;
            if (UnityEngine.Input.GetKeyDown(KeyCode.J)) LightPunchPressed = true;
            if (UnityEngine.Input.GetKeyDown(KeyCode.K)) BlockPressed = true;
        }

        public void ConsumeFrame()
        {
            JumpPressed = false;
            LightPunchPressed = false;
            BlockPressed = false;
        }
    }
}
