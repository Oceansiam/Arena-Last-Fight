using UnityEngine;

namespace RiftArena.Camera
{
    /// <summary>
    /// Dynamically dollies the camera based on horizontal separation between the two
    /// fighters, so they both stay in frame whether they're close together or far apart
    /// along the fighting plane's X axis.
    /// </summary>
    public class CombatCamera : MonoBehaviour
    {
        [SerializeField] private Transform playerOne;
        [SerializeField] private Transform playerTwo;

        [SerializeField] private float heightOffset = 1.6f;
        [SerializeField] private float minDollyZ = 6f;
        [SerializeField] private float maxDollyZ = 14f;
        [SerializeField] private float separationToDollyFactor = 0.6f;
        [SerializeField] private float baseDollyZ = 6f;

        public void SetPlayers(Transform p1, Transform p2)
        {
            playerOne = p1;
            playerTwo = p2;
        }

        private void LateUpdate()
        {
            if (playerOne == null || playerTwo == null) return;

            float midX = (playerOne.position.x + playerTwo.position.x) * 0.5f;
            float midY = (playerOne.position.y + playerTwo.position.y) * 0.5f;
            float separationX = Mathf.Abs(playerOne.position.x - playerTwo.position.x);

            float dollyZ = Mathf.Clamp(baseDollyZ + separationX * separationToDollyFactor, minDollyZ, maxDollyZ);

            Vector3 targetPos = new Vector3(midX, midY + heightOffset, -dollyZ);
            Vector3 lookAt = new Vector3(midX, midY + heightOffset, 0f);

            transform.position = targetPos;
            transform.LookAt(lookAt);
        }
    }
}
