using MagicStaff.Views;
using UnityEngine;
using UnityEngine.UI;

public class HpBarView : DisplayableView
{
    [SerializeField]
    private Image _fill;

    public void SetProgress(float normalized) => _fill.fillAmount = normalized;
}
