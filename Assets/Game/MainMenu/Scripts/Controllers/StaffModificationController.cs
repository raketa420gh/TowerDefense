using System;
using System.Linq;
using UnityEngine;
using Zenject;
using MagicStaff.Staff;

namespace MagicStaff.MainMenu
{
    public class StaffModificationController : IInitializable, IDisposable
    {
        readonly StaffModificationView _modView;
        readonly PartPickerView        _pickerView;
        readonly StaffLoadoutService   _loadoutService;

        StaffSlot _pendingSlot;

        public StaffModificationController(StaffModificationView modView,
                                           PartPickerView pickerView,
                                           StaffLoadoutService loadoutService)
        {
            _modView        = modView;
            _pickerView     = pickerView;
            _loadoutService = loadoutService;
        }

        public void Initialize()
        {
            _modView.OnSlotClicked     += OpenPicker;
            _modView.OnClosed          += () => _modView.Hide();
            _pickerView.OnPartSelected += SelectPart;
            _pickerView.OnClosed       += ClosePicker;

            _modView.RenderLoadout(_loadoutService.ActiveLoadout);
        }

        public void Dispose()
        {
            _modView.OnSlotClicked     -= OpenPicker;
            _pickerView.OnPartSelected -= SelectPart;
            _pickerView.OnClosed       -= ClosePicker;
        }

        void OpenPicker(StaffSlot slot)
        {
            _pendingSlot = slot;
            _pickerView.Render(LoadPartsForSlot(slot));
            _pickerView.Show();
        }

        void ClosePicker() => _pickerView.Hide();

        void SelectPart(StaffPartConfig part)
        {
            _loadoutService.SetPart(_pendingSlot, part);
            _modView.RenderLoadout(_loadoutService.ActiveLoadout);
            _pickerView.Hide();
        }

        static StaffPartConfig[] LoadPartsForSlot(StaffSlot slot) =>
            UnityEngine.Resources.LoadAll<StaffPartConfig>("StaffParts")
                       .Where(p => p.slot == slot)
                       .ToArray();
    }
}
