using MagicStaff.Views;
using TMPro;
using UnityEngine;

namespace MagicStaff.MainMenu
{
    public class CoinCounterView : DisplayableView
    {
        [SerializeField]
        private TMP_Text _label;

        public void SetCoins(int amount) => _label.text = amount.ToString();
    }
}
