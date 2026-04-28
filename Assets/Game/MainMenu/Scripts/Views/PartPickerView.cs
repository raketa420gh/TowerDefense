using System;
using UnityEngine;
using UnityEngine.UI;
using MagicStaff.Views;
using MagicStaff.Staff;

namespace MagicStaff.MainMenu
{
    public class PartPickerView : DisplayableView
    {
        public event Action<StaffPartConfig> OnPartSelected;
        public event Action OnClosed;

        [SerializeField]
        Button _closeButton;
        [SerializeField]
        Transform _listRoot;
        [SerializeField]
        PartPickerItemView _itemPrefab;

        void Awake()
        {
            _closeButton.onClick.AddListener(() => OnClosed?.Invoke());
        }

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
