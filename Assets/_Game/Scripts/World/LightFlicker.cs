using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Darkwood
{
    /// <summary>
    /// Живой свет для костров и ламп: два слоя мерцания.
    /// 1) Медленная волна — костёр разгорается и затухает.
    /// 2) Быстрая дрожь — мелкое трепетание, как от языков пламени.
    /// Базовая яркость берётся из самого Light 2D при старте — настрой её там.
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public class LightFlicker : MonoBehaviour
    {
        [Tooltip("Насколько сильно огонь разгорается и затухает (0.3 = ±30%)")]
        public float flareAmplitude = 0.3f;

        [Tooltip("Сила быстрой дрожи")]
        public float jitterAmplitude = 0.1f;

        [Tooltip("Скорость быстрой дрожи")]
        public float jitterSpeed = 11f;

        [Tooltip("Слегка «дышит» ли радиус света (для Point-света)")]
        public bool wobbleRadius = true;

        Light2D _light;
        float _base;      // базовая яркость из инспектора Light 2D
        float _baseRadius;
        float _seed;      // свой сдвиг шума, чтобы два костра мерцали по-разному

        void Awake()
        {
            _light = GetComponent<Light2D>();
            _base = _light.intensity;
            _baseRadius = _light.pointLightOuterRadius;
            _seed = Random.value * 100f;
        }

        void Update()
        {
            float t = Time.time + _seed;

            // медленная волна 0..1
            float flare = Mathf.PerlinNoise(_seed, t * 0.35f);

            // провалы: когда волна уходит вниз, костёр почти гаснет
            float dip = flare < 0.3f ? Mathf.Lerp(0.35f, 1f, flare / 0.3f) : 1f;

            // быстрая дрожь -1..1
            float jitter = (Mathf.PerlinNoise(_seed + 50f, t * jitterSpeed) - 0.5f) * 2f;

            float k = (1f + flareAmplitude * (flare - 0.5f) * 2f * dip)
                    * (1f + jitterAmplitude * jitter);

            _light.intensity = _base * k;

            if (wobbleRadius && _baseRadius > 0f)
                _light.pointLightOuterRadius = _baseRadius * (1f + 0.08f * jitter);
        }
    }
}
