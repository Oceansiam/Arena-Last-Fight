namespace RiftArena.CharacterSelect
{
    /// <summary>
    /// Plain static carrier for the chosen roster indices between the CharacterSelect
    /// scene and MVP1_Arena - static fields survive a scene load within the same Play
    /// session (they reset on domain reload / stopping Play, which is fine: MVP1_Arena
    /// falls back to whatever model is already pre-placed in the scene if nobody went
    /// through character select, e.g. testing the arena scene directly).
    /// </summary>
    public static class CharacterSelection
    {
        public const int NoSelection = -1;

        public static int PlayerOneCharacterIndex = NoSelection;
        public static int PlayerTwoCharacterIndex = NoSelection;

        public static bool HasSelection => PlayerOneCharacterIndex != NoSelection;

        public static void Clear()
        {
            PlayerOneCharacterIndex = NoSelection;
            PlayerTwoCharacterIndex = NoSelection;
        }
    }
}
