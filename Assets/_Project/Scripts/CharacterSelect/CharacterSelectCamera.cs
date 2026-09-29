using UnityEngine;

namespace RiftArena.CharacterSelect
{
    /// <summary>
    /// Slow cinematic sway around the stage point - not a full orbit, just enough idle
    /// motion so the select screen doesn't feel like a static screenshot.
    /// </summary>
    public class CharacterSelectCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float orbitDegreesPerSecond = 3f;
        [SerializeField] private float radius = 3.5f;
        [SerializeField] private float height = 1.6f;
        [SerializeField] private float lookHeight = 1f;

        private float angle;

        private void Update()
        {
            if (target == null) return;

            angle += orbitDegreesPerSecond * Time.deltaTime;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Sin(rad) * radius, height, -Mathf.Cos(rad) * radius);
            transform.position = target.position + offset;
            transform.LookAt(target.position + Vector3.up * lookHeight);
        }
    }
}
