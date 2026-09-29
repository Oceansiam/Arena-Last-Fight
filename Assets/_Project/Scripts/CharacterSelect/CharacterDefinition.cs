using UnityEngine;

namespace RiftArena.CharacterSelect
{
    /// <summary>
    /// One playable fighter: display name plus the model/animator used both on the
    /// select screen and (via CharacterRoster) as the swapped-in model on Fighter.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacterDefinition", menuName = "Rift Arena/Character Definition")]
    public class CharacterDefinition : ScriptableObject
    {
        public string displayName;
        public GameObject modelPrefab;
        public RuntimeAnimatorController animatorController;
    }
}
