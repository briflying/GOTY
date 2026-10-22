using UnityEngine;

namespace Darkwood
{
    /// <summary>
    /// Камера плавно догоняет игрока. Z держим -10 — стандарт для 2D.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;      // за кем следить (игрок)
        public float smooth = 5f;     // больше = быстрее догоняет

        void LateUpdate()
        {
            if (target == null) return;

            var destination = new Vector3(target.position.x, target.position.y, -10f);
            transform.position = Vector3.Lerp(transform.position, destination, smooth * Time.deltaTime);
        }
    }
}
