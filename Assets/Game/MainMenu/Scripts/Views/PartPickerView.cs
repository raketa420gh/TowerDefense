using System;
using UnityEngine;
using MagicStaff.Views;
using MagicStaff.Staff;

namespace MagicStaff.MainMenu
{
    public class PartPickerView : DisplayableView
    {
        public event Action<StaffPartConfig> OnPartSelected;

        [SerializeField]
        Transform _listRoot;
        [SerializeField]
        PartPickerItemView _itemPrefab;

        public void Render(StaffPartConfig[] availableParts)
        {
            foreach (Transform child in _listRoot)
                Destroy(child.gameObject);

            foreach (var part in availableParts)
            {
                var item = Instantiate(_itemPrefab, _listRoot);
                item.Render(part);
                item.OnSelected += p => OnPartSelected?.Invoke(p);
            }
        }
    }
}
