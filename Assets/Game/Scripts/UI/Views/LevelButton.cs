using System;
using UnityEngine;
using UnityEngine.UI;

public class LevelButton : MonoBehaviour
{
    public event Action<int> Clicked;

    [SerializeField]
    private int _levelId;

    [SerializeField]
    private Button _button;

    [SerializeField]
    private GameObject _lockIcon;

    [SerializeField]
    private GameObject[] _stars;

    [SerializeField]
    private Text _label;

    public int LevelId => _levelId;

    private void Awake()
    {
        _button.onClick.AddListener(() => Clicked?.Invoke(_levelId));
    }

    public void Bind(bool unlocked, int stars)
    {
        _button.interactable = unlocked;
        _lockIcon.SetActive(!unlocked);
        _label.text = _levelId.ToString();
        for (int i = 0; i < _stars.Length; i++)
            _stars[i].SetActive(i < stars);
    }
}
