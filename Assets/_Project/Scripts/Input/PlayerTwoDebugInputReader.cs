using UnityEngine;

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
    //   Comma = Light Punch, Period = Block
    public class PlayerTwoDebugInputReader : IInputReader
    {
        public float MoveAxis { get; private set; }
        public float MoveAxisForward { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool LightPunchPressed { get; private set; }
        public bool BlockPressed { get; private set; }

       public void Tick()
        {
            float axis = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.LeftArrow)) axis -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.RightArrow)) axis += 1f;
            MoveAxis = axis;

            float axisForward = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.DownArrow)) axisForward -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.UpArrow)) axisForward += 1f;
            MoveAxisForward = axisForward;

            JumpPressed = UnityEngine.Input.GetKeyDown(KeyCode.RightShift);
            LightPunchPressed = UnityEngine.Input.GetKeyDown(KeyCode.Comma);
            BlockPressed = UnityEngine.Input.GetKeyDown(KeyCode.Period);
        }
    }
}
