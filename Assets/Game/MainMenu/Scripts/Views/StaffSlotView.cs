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
        [SerializeField]
        private GameObject _lockedOverlay;

        public StaffSlot Slot => _slot;

        void Awake() => _button.onClick.AddListener(() => OnSlotClicked?.Invoke(_slot));

        public void Render(StaffPartConfig part)
        {
            _lockedOverlay.SetActive(false);
            _button.interactable = true;
            _icon.sprite = part.icon;
            _label.text  = part.partName;
        }

        public void RenderLocked()
        {
            _lockedOverlay.SetActive(true);
            _button.interactable = false;
        }
    }
}
