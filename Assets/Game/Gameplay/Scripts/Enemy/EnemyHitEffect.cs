using System.Collections;
using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class EnemyHitEffect : MonoBehaviour
{
    [SerializeField] private float _flashDuration = 0.12f;
    [SerializeField] private float _scalePeak     = 1.25f;
    [SerializeField] private float _scaleDuration = 0.15f;

    private HealthComponent _health;
    private Renderer        _renderer;
    private Coroutine       _flashRoutine;
    private Coroutine       _scaleRoutine;
    private Color           _baseColor;
    private Vector3         _baseScale;

    private void Awake()
    {
        _health    = GetComponent<HealthComponent>();
        _renderer  = GetComponentInChildren<Renderer>();
        _baseScale = transform.localScale;
    }

    private void OnEnable()  => _health.OnDamaged += HandleDamaged;
    private void OnDisable() => _health.OnDamaged -= HandleDamaged;

    private void HandleDamaged()
    {
        if (_renderer == null) return;

        if (_flashRoutine == null)
            _baseColor = _renderer.material.color;

        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);

        _flashRoutine = StartCoroutine(FlashRed());
        _scaleRoutine = StartCoroutine(BounceScale());
    }

    private IEnumerator FlashRed()
    {
        float half = _flashDuration * 0.5f;

        for (float t = 0f; t < half; t += Time.deltaTime)
        {
            _renderer.material.color = Color.Lerp(_baseColor, Color.red, t / half);
            yield return null;
        }

        _renderer.material.color = Color.red;

        for (float t = 0f; t < half; t += Time.deltaTime)
        {
            _renderer.material.color = Color.Lerp(Color.red, _baseColor, t / half);
            yield return null;
        }

        _renderer.material.color = _baseColor;
        _flashRoutine = null;
    }

    private IEnumerator BounceScale()
    {
        float   half      = _scaleDuration * 0.5f;
        Vector3 peakScale = _baseScale * _scalePeak;

        for (float t = 0f; t < half; t += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(_baseScale, peakScale, t / half);
            yield return null;
        }

        for (float t = 0f; t < half; t += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(peakScale, _baseScale, t / half);
            yield return null;
        }

        transform.localScale = _baseScale;
        _scaleRoutine = null;
    }
}
