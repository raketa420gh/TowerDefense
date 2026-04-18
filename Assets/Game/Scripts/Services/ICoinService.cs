using System;

namespace MagicStaff
{
    public interface ICoinService
    {
        int Coins { get; }
        void AddCoins(int amount);
        event Action<int> OnCoinsChanged;
    }
}
