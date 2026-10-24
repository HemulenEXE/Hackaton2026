using UnityEngine;
using UnityEngine.InputSystem;

namespace GardenMerger.UI
{
    /// <summary>
    /// Программное управление движением и искажением фона в меню:
    /// - Бесшовный бесконечный диагональный дрейф тайловой сетки (с учетом точной обрезки спрайта в Unity)
    /// - Органическое волновое искажение пропорций (squash & stretch)
    /// - Гармонический наклон и покачивание (sway)
    /// - Плавающее перемещение по траектории Лиссажу (bobbing)
    /// - Дыхание размеров сетки (size breathing)
    /// - Интерактивный параллакс и динамический наклон от курсора мыши
    /// - Атмосферная пульсация цвета
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class MenuBackgroundAnimator : MonoBehaviour
    {
        [Header("Movement (Бесшовное движение)")]
        [Tooltip("Скорость непрерывного дрейфа (ед/сек)")]
        [SerializeField] private Vector2 _scrollSpeed = new Vector2(0.25f, 0.15f);

        [Header("Distortion / Искажение формы (Squash & Stretch)")]
        [Tooltip("Амплитуда искажения масштаба по осям X и Y")]
        [SerializeField] private Vector2 _distortIntensity = new Vector2(0.045f, 0.035f);
        [Tooltip("Частоты волн искажения")]
        [SerializeField] private Vector2 _distortFrequency = new Vector2(1.2f, 0.9f);

        [Header("Wave Sway (Волновой наклон)")]
        [Tooltip("Максимальный угол волнового наклона (в градусах)")]
        [SerializeField] private float _swayMaxAngle = 0.6f;
        [SerializeField] private float _swaySpeed = 0.75f;

        [Header("Lissajous Bobbing (Плавающее покачивание)")]
        [SerializeField] private bool _enableBobbing = true;
        [SerializeField] private Vector2 _bobbingAmount = new Vector2(0.2f, 0.15f);
        [SerializeField] private Vector2 _bobbingSpeed = new Vector2(0.8f, 1.3f);

        [Header("Size Breathing (Дыхание границ тайловой сетки)")]
        [SerializeField] private bool _enableSizeBreathing = true;
        [SerializeField] private Vector2 _sizeBreathingAmount = new Vector2(0.35f, 0.25f);
        [SerializeField] private Vector2 _sizeBreathingSpeed = new Vector2(0.6f, 0.45f);

        [Header("Interactive Mouse (Реакция на курсор мыши)")]
        [SerializeField] private bool _enableMouseParallax = true;
        [SerializeField] private Vector2 _mouseParallaxAmount = new Vector2(0.3f, 0.2f);
        [SerializeField] private float _mouseTiltStrength = 0.5f;
        [SerializeField] private float _mouseSmoothing = 5.0f;

        [Header("Atmospheric Color (Пульсация цвета)")]
        [SerializeField] private bool _enableColorPulse = true;
        [SerializeField] private Color _colorTintA = Color.white;
        [SerializeField] private Color _colorTintB = new Color(0.93f, 0.98f, 0.96f, 1.0f);
        [SerializeField] private float _colorPulseSpeed = 0.35f;

        private SpriteRenderer _spriteRenderer;
        private Vector3 _initialLocalPos;
        private Vector3 _initialLocalScale;
        private Vector2 _initialSpriteSize;
        private Vector2 _tileUnitSize = new Vector2(3f, 3f);
        private Vector2 _scrollAccumulator;
        private Vector2 _smoothedNormMouse;
        private float _time;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _initialLocalPos = transform.localPosition;
            _initialLocalScale = transform.localScale;
            _initialSpriteSize = _spriteRenderer.size;

            _tileUnitSize = CalculateTileUnitSize();
        }

        private Vector2 CalculateTileUnitSize()
        {
            var sprite = _spriteRenderer.sprite;
            if (sprite == null) return new Vector2(3f, 3f);

            // В Unity при DrawMode = Tiled и наличии spriteBorder тайлится именно внутренний прямоугольник спрайта
            Vector4 border = sprite.border;
            float innerWidth = Mathf.Max(1f, sprite.rect.width - border.x - border.z);
            float innerHeight = Mathf.Max(1f, sprite.rect.height - border.y - border.w);
            float ppu = Mathf.Max(0.0001f, sprite.pixelsPerUnit);

            return new Vector2(innerWidth / ppu, innerHeight / ppu);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _time += dt;

            // 1. Бесшовное бесконечное смещение (дрейф тайлов)
            _scrollAccumulator += _scrollSpeed * dt;
            float wrappedX = Mathf.Repeat(_scrollAccumulator.x, _tileUnitSize.x);
            float wrappedY = Mathf.Repeat(_scrollAccumulator.y, _tileUnitSize.y);
            Vector3 scrollOffset = new Vector3(wrappedX, wrappedY, 0f);

            // 2. Многогармоническое волновое искажение формы (squash & stretch)
            float wave1 = Mathf.Sin(_time * _distortFrequency.x) * _distortIntensity.x;
            float wave2 = Mathf.Cos(_time * _distortFrequency.y) * _distortIntensity.y;
            float scaleX = _initialLocalScale.x * (1f + wave1 - wave2 * 0.4f);
            float scaleY = _initialLocalScale.y * (1f - wave1 * 0.5f + wave2);
            transform.localScale = new Vector3(scaleX, scaleY, 1f);

            // 3. Волновой наклон (sway)
            float swayAngle = Mathf.Sin(_time * _swaySpeed) * _swayMaxAngle
                            + Mathf.Cos(_time * (_swaySpeed * 1.63f)) * (_swayMaxAngle * 0.35f);

            // 4. Плавающее покачивание по кривой Лиссажу (bobbing)
            Vector3 bobOffset = Vector3.zero;
            if (_enableBobbing)
            {
                float bobX = Mathf.Sin(_time * _bobbingSpeed.x) * _bobbingAmount.x;
                float bobY = Mathf.Sin(_time * _bobbingSpeed.y + Mathf.PI * 0.5f) * _bobbingAmount.y;
                bobOffset = new Vector3(bobX, bobY, 0f);
            }

            // 5. Дыхание размеров сетки SpriteRenderer.size
            if (_enableSizeBreathing)
            {
                float sizeX = Mathf.Sin(_time * _sizeBreathingSpeed.x) * _sizeBreathingAmount.x;
                float sizeY = Mathf.Cos(_time * _sizeBreathingSpeed.y) * _sizeBreathingAmount.y;
                _spriteRenderer.size = _initialSpriteSize + new Vector2(sizeX, sizeY);
            }

            // 6. Интерактивный параллакс и наклон от курсора мыши
            Vector3 mouseOffset = Vector3.zero;
            float mouseTilt = 0f;
            GetMouseNormalized(out Vector2 normMouse, out bool hasMouse);

            if (_enableMouseParallax && hasMouse)
            {
                _smoothedNormMouse = Vector2.Lerp(_smoothedNormMouse, normMouse, dt * _mouseSmoothing);
            }
            else
            {
                _smoothedNormMouse = Vector2.Lerp(_smoothedNormMouse, Vector2.zero, dt * 2f);
            }

            if (_enableMouseParallax)
            {
                mouseOffset = new Vector3(
                    -_smoothedNormMouse.x * _mouseParallaxAmount.x,
                    -_smoothedNormMouse.y * _mouseParallaxAmount.y,
                    0f
                );
                mouseTilt = -_smoothedNormMouse.x * _mouseTiltStrength;
            }

            // 7. Итоговая позиция и поворот
            transform.localPosition = _initialLocalPos + scrollOffset + bobOffset + mouseOffset;
            transform.localRotation = Quaternion.Euler(0f, 0f, swayAngle + mouseTilt);

            // 8. Атмосферная пульсация цвета
            if (_enableColorPulse)
            {
                float colorT = (Mathf.Sin(_time * _colorPulseSpeed) + 1f) * 0.5f;
                _spriteRenderer.color = Color.Lerp(_colorTintA, _colorTintB, colorT);
            }
        }

        private void GetMouseNormalized(out Vector2 normMouse, out bool hasMouse)
        {
            normMouse = Vector2.zero;
            hasMouse = false;

            Vector2 screenPos = Vector2.zero;
            var mouse = Mouse.current;
            if (mouse != null)
            {
                screenPos = mouse.position.ReadValue();
                hasMouse = true;
            }
            else if (Input.mousePresent)
            {
                screenPos = Input.mousePosition;
                hasMouse = true;
            }

            if (!hasMouse) return;

            if (Screen.width > 0 && Screen.height > 0)
            {
                normMouse = new Vector2(
                    (screenPos.x / Screen.width) * 2f - 1f,
                    (screenPos.y / Screen.height) * 2f - 1f
                );
            }
        }
    }
}
