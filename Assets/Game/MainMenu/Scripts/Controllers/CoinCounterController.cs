using MagicStaff;
using Zenject;

namespace MagicStaff.MainMenu
{
    public class CoinCounterController : IInitializable, System.IDisposable
    {
        private readonly ICoinService    _coinService;
        private readonly CoinCounterView _view;

        [Inject]
        public CoinCounterController(ICoinService coinService, CoinCounterView view)
        {
            _coinService = coinService;
            _view        = view;
        }

        public void Initialize()
        {
            _coinService.OnCoinsChanged += _view.SetCoins;
            _view.SetCoins(_coinService.Coins);
        }

        public void Dispose() => _coinService.OnCoinsChanged -= _view.SetCoins;
    }
}
