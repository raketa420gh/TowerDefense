using System;
using UnityEngine;
using UnityEngine.UI;

public class HudView : DisplayableView
{
    public event Action EarlyStartClicked;
    public event Action PauseClicked;

    [SerializeField]
    private Text _goldLabel;

    [SerializeField]
    private Text _baseHpLabel;

    [SerializeField]
    private Text _waveLabel;

    [SerializeField]
    private Text _breakTimerLabel;

    [SerializeField]
    private Button _earlyStartButton;

    [SerializeField]
    private Button _pauseButton;

    protected override void Awake()
    {
        base.Awake();
        _earlyStartButton.onClick.AddListener(() => EarlyStartClicked?.Invoke());
        if (_pauseButton != null)
            _pauseButton.onClick.AddListener(() => PauseClicked?.Invoke());
        SetEarlyStartVisible(false);
    }

    public void SetGold(int value) => _goldLabel.text = value.ToString();

    public void SetBaseHp(int current, int max) =>
        _baseHpLabel.text = $"{current}/{max}";

    public void SetWave(int index, int total) =>
        _waveLabel.text = $"Wave {index + 1}/{total}";

    public void SetEarlyStartVisible(bool visible)
    {
        _earlyStartButton.gameObject.SetActive(visible);
        _breakTimerLabel.gameObject.SetActive(visible);
    }

    public void SetBreakTimer(float seconds) =>
        _breakTimerLabel.text = Mathf.CeilToInt(seconds).ToString();
}
