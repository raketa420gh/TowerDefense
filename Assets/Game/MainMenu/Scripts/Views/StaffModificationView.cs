using System;
using UnityEngine;
using UnityEngine.UI;
using MagicStaff.Views;
using MagicStaff.Staff;

namespace MagicStaff.MainMenu
{
    public class StaffModificationView : DisplayableView
    {
        public event Action<StaffSlot> OnSlotClicked;
        public event Action OnClosed;

        [SerializeField]
        StaffSlotView[] _slots;
        [SerializeField]
        Button _closeButton;

        void Awake()
        {
            foreach (var slot in _slots)
                slot.OnSlotClicked += s => OnSlotClicked?.Invoke(s);
            _closeButton.onClick.AddListener(() => OnClosed?.Invoke());
        }

        public void RenderLoadout(StaffLoadoutConfig loadout)
        {
            foreach (var slotView in _slots)
            {
                if (slotView.Slot == StaffSlot.Shaft)
                    slotView.Render(loadout.GetPart(StaffSlot.Shaft));
                else
                    slotView.RenderLocked();
            }
        }
    }
}
