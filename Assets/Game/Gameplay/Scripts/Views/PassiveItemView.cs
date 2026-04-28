using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PassiveItemView : MonoBehaviour
{
    [SerializeField]
    private Image    _icon;
    [SerializeField]
    private TMP_Text _nameLabel;

    public void Render(IActivePassive passive)
    {
        if (_icon != null) _icon.sprite = passive.Icon;
        _nameLabel.text = passive.Name;
    }
}
