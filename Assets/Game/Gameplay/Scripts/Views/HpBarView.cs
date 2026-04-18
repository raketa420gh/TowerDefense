using MagicStaff.Views;
using UnityEngine;
using UnityEngine.UI;

public class HpBarView : DisplayableView
{
    [SerializeField]
    private Slider _slider;

    public void SetProgress(float normalized) => _slider.value = normalized;
}
