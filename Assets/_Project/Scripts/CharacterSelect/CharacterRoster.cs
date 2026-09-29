using UnityEngine;

namespace RiftArena.CharacterSelect
{
    /// <summary>
    /// Single source of truth for the playable roster, shared by CharacterSelectController
    /// (to cycle through on the select screen) and Fighter (to resolve
    /// CharacterSelection's chosen index back into an actual model/animator to swap in).
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterRoster", menuName = "Rift Arena/Character Roster")]
    public class CharacterRoster : ScriptableObject
    {
        public CharacterDefinition[] characters;
    }
}
