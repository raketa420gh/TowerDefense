using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MagicStaff.Staff;

namespace MagicStaff.MainMenu
{
    public class StaffSlotView : MonoBehaviour
    {
        public event Action<StaffSlot> OnSlotClicked;

        [SerializeField]
        StaffSlot _slot;
        [SerializeField]
        Image _icon;
        [SerializeField]
        TMP_Text _label;
        [SerializeField]
        Button _button;

        public StaffSlot Slot => _slot;

        void Awake() => _button.onClick.AddListener(() => OnSlotClicked?.Invoke(_slot));

        public void Render(StaffPartConfig part)
        {
            _icon.sprite = part.icon;
            _label.text  = part.partName;
        }
    }
}
