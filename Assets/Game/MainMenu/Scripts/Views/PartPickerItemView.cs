using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MagicStaff.Staff;

namespace MagicStaff.MainMenu
{
    public class PartPickerItemView : MonoBehaviour
    {
        public event Action<StaffPartConfig> OnSelected;

        [SerializeField]
        Image _icon;
        [SerializeField]
        TMP_Text _label;
        [SerializeField]
        Button _button;

        StaffPartConfig _part;

        void Awake() => _button.onClick.AddListener(() => OnSelected?.Invoke(_part));

        public void Render(StaffPartConfig part)
        {
            _part        = part;
            _icon.sprite = part.icon;
            _label.text  = part.partName;
        }
    }
}
