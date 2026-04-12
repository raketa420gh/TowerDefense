using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelCompleteView : DisplayableView
{
    public event Action Continue;

    [SerializeField]
    private TextMeshProUGUI _titleLabel;

    [SerializeField]
    private GameObject[] _starIcons;

    [SerializeField]
    private Button _continueButton;

    protected override void Awake()
    {
        base.Awake();
        _continueButton.onClick.AddListener(() => Continue?.Invoke());
        Hide();
    }

    public void Populate(string levelName, int stars)
    {
        _titleLabel.text = $"{levelName} — Победа";
        for (int i = 0; i < _starIcons.Length; i++)
            _starIcons[i].SetActive(i < stars);
    }
}
