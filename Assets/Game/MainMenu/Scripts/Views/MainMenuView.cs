using System;
using UnityEngine;
using UnityEngine.UI;
using MagicStaff.Views;

namespace MagicStaff.MainMenu
{
    public class MainMenuView : DisplayableView
    {
        public event Action OnPlayClicked;
        public event Action OnStaffClicked;

        [SerializeField]
        Button _playButton;
        [SerializeField]
        Button _staffButton;

        void Awake()
        {
            _playButton.onClick.AddListener(() => OnPlayClicked?.Invoke());
            _staffButton.onClick.AddListener(() => OnStaffClicked?.Invoke());
        }
    }
}
