using System;
using UnityEngine;

namespace MagicStaff
{
    public class CoinService : ICoinService
    {
        private const string Key = "PlayerCoins";

        public event Action<int> OnCoinsChanged;

        public int Coins => PlayerPrefs.GetInt(Key, 0);

        public void AddCoins(int amount)
        {
            PlayerPrefs.SetInt(Key, Coins + amount);
            PlayerPrefs.Save();
            OnCoinsChanged?.Invoke(Coins);
        }
    }
}
