using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public abstract class DisplayableView : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup _canvasGroup;

    public bool IsVisible => _canvasGroup.alpha > 0.99f;

    protected virtual void Awake()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();
    }

    public virtual void Show()
    {
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
    }

    public virtual void Hide()
    {
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }
}
