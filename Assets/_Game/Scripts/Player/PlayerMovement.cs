using UnityEngine;
using UnityEngine.InputSystem;

namespace Darkwood
{
    /// <summary>
    /// Движение игрока: WASD, скорость в юнитах/сек.
    /// Работает в мировых координатах (вверх по экрану = +Y), поэтому в
    /// изометрии персонаж просто ходит по экрану в четырёх направлениях.
    /// При добавлении компонента сам ставит Rigidbody 2D (RequireComponent).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [Tooltip("Скорость в юнитах в секунду")]
        public float speed = 4f;

        Rigidbody2D _rb;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return; // клавиатура не подключена — стоим

            // собираем направление: каждая клавиша даёт -1 или +1 по своей оси
            float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            float y = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);

            // диагональ не должна быть быстрее: режем длину вектора до 1
            Vector2 dir = Vector2.ClampMagnitude(new Vector2(x, y), 1f);

            _rb.linearVelocity = dir * speed;
        }
    }
}
