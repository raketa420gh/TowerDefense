using System;
using UnityEngine;
using UnityEngine.EventSystems;
using MagicStaff.Views;

public class VirtualJoystickView : DisplayableView,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public event Action<Vector2> OnInputChanged;

    [SerializeField]
    RectTransform _background;
    [SerializeField]
    RectTransform _handle;
    [SerializeField]
    CanvasGroup _canvasGroup;
    [SerializeField]
    float _idleAlpha = 0.25f;

    float _radius;

    void Awake()
    {
        _radius = _background.sizeDelta.x * 0.5f;
        _canvasGroup.alpha = _idleAlpha;
    }

    public void OnPointerDown(PointerEventData e)
    {
        _handle.anchoredPosition = Vector2.zero;
        _canvasGroup.alpha = 1f;
    }

    public void OnDrag(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _background, e.position, e.pressEventCamera, out var localPoint);
        var clamped = Vector2.ClampMagnitude(localPoint, _radius);
        _handle.anchoredPosition = clamped;
        OnInputChanged?.Invoke(clamped / _radius);
    }

    public void OnPointerUp(PointerEventData e)
    {
        _handle.anchoredPosition = Vector2.zero;
        _canvasGroup.alpha = _idleAlpha;
        OnInputChanged?.Invoke(Vector2.zero);
    }
}
