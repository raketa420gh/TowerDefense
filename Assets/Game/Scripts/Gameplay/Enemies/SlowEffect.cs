using UnityEngine;

public class SlowEffect : MonoBehaviour
{
    public float Multiplier => _active ? _multiplier : 1f;

    private EnemyMovement _movement;
    private float _multiplier = 1f;
    private float _timeLeft;
    private bool _active;

    public void Bind(EnemyMovement movement)
    {
        _movement = movement;
    }

    public void Apply(float multiplier, float duration)
    {
        if (!_active || multiplier < _multiplier || duration > _timeLeft)
        {
            _multiplier = Mathf.Min(_active ? _multiplier : 1f, multiplier);
            _timeLeft = Mathf.Max(_timeLeft, duration);
            _active = true;
            _movement.SetSpeedMultiplier(_multiplier);
        }
    }

    public void Reset()
    {
        _active = false;
        _multiplier = 1f;
        _timeLeft = 0f;
        if (_movement != null) _movement.SetSpeedMultiplier(1f);
    }

    private void Update()
    {
        if (!_active) return;
        _timeLeft -= Time.deltaTime;
        if (_timeLeft <= 0f) Reset();
    }
}
