using UnityEngine;

namespace RiftArena.Input
{
    // DEBUG ONLY — MVP1 local testing stand-in for Player 2.
    // Replaced by network input in MVP1.5. Do not ship.
    //
    // Lets one person test both sides of a match on a single keyboard while there is
    // no networking yet:
    //   J / L = walk left/right
    //   I = jump
    //   U = Light Punch, O = Heavy Punch
    public class PlayerTwoDebugInputReader : IInputReader
    {
        public float MoveAxis { get; private set; }

        // Not wired for the P2 debug stand-in - forward/back movement was only
        // requested for Player One. Always 0 so P2 stays on the old X-only behavior.
        public float MoveAxisForward => 0f;

        public bool JumpPressed { get; private set; }
        public bool LightPunchPressed { get; private set; }
        public bool HeavyPunchPressed { get; private set; }

        public void Tick()
        {
            float axis = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.J)) axis -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.L)) axis += 1f;
            MoveAxis = axis;

            JumpPressed = UnityEngine.Input.GetKeyDown(KeyCode.I);
            LightPunchPressed = UnityEngine.Input.GetKeyDown(KeyCode.U);
            HeavyPunchPressed = UnityEngine.Input.GetKeyDown(KeyCode.O);
        }
    }
}
