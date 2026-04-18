using MagicStaff.Views;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExperienceBarView : DisplayableView
{
    [SerializeField]
    private Slider _slider;
    [SerializeField]
    private TMP_Text _levelLabel;

    public void SetProgress(float normalized) => _slider.value = normalized;
    public void SetLevel(int level)           => _levelLabel.text = $"Lv {level}";
}
