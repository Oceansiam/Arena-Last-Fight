using UnityEngine;

namespace RiftArena.Camera
{
    /// <summary>
    /// Fixed-angle isometric "god view" over both fighters: looks down at the midpoint
    /// between PlayerOne and PlayerTwo from a constant elevation/back angle, dollying
    /// in/out based on their horizontal separation so both stay in frame whether close
    /// together or far apart. Replaces the earlier single-player third-person follow -
    /// this is meant to show the whole fight, not one character's perspective.
    /// </summary>
    public class CombatCamera : MonoBehaviour
    {
        [SerializeField] private Transform playerOne;
        [SerializeField] private Transform playerTwo;

        [Header("Isometric Framing")]
        [SerializeField] private float heightOffset = 5f;
        [SerializeField] private float backOffset = 4f;
        [SerializeField] private float minDolly = 4f;
        [SerializeField] private float maxDolly = 9f;
        [SerializeField] private float separationToDollyFactor = 0.6f;
        [SerializeField] private float baseDolly = 4f;
        [SerializeField] private float lookHeightOffset = 1f;
        [SerializeField] private float positionSmoothTime = 0.15f;

        private Vector3 velocity;

        public void SetPlayers(Transform p1, Transform p2)
        {
            playerOne = p1;
            playerTwo = p2;
        }

        private void LateUpdate()
        {
            if (playerOne == null || playerTwo == null) return;

            Vector3 mid = (playerOne.position + playerTwo.position) * 0.5f;
            float separationX = Mathf.Abs(playerOne.position.x - playerTwo.position.x);
            float dolly = Mathf.Clamp(baseDolly + separationX * separationToDollyFactor, minDolly, maxDolly);

            Vector3 desiredPos = new Vector3(mid.x, mid.y + heightOffset, mid.z - (backOffset + dolly));
            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, positionSmoothTime);

            Vector3 lookAt = new Vector3(mid.x, mid.y + lookHeightOffset, mid.z);
            transform.LookAt(lookAt);
        }
    }
}
