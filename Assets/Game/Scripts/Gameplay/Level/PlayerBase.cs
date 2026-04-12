using UnityEngine;
using Zenject;

public class PlayerBase : MonoBehaviour
{
    [SerializeField]
    private float _reachRadius = 0.8f;

    public float ReachRadiusSqr => _reachRadius * _reachRadius;

    private SignalBus _signalBus;
    private int _maxHp;
    private int _currentHp;

    [Inject]
    public void Construct(SignalBus signalBus)
    {
        _signalBus = signalBus;
    }

    public void Init(int maxHp)
    {
        _maxHp = maxHp;
        _currentHp = maxHp;
        _signalBus.Fire(new BaseHealthChangedSignal { Current = _currentHp, Max = _maxHp });
    }

    public void ApplyDamage(int damage)
    {
        if (_currentHp <= 0) return;
        _currentHp = Mathf.Max(0, _currentHp - damage);
        _signalBus.Fire(new BaseHealthChangedSignal { Current = _currentHp, Max = _maxHp });
        if (_currentHp == 0)
            _signalBus.Fire(new BaseDestroyedSignal());
    }
}
