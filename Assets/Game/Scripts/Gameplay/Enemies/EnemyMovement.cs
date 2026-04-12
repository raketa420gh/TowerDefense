using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public bool ReachedEnd => _reachedEnd;

    private Path _path;
    private float _speed;
    private float _multiplier = 1f;
    private int _nextIndex;
    private bool _reachedEnd;

    public void Init(Path path, float speed)
    {
        _path = path;
        _speed = speed;
        _multiplier = 1f;
        _nextIndex = 1;
        _reachedEnd = false;
        transform.position = path.SpawnPoint;
    }

    public void SetSpeedMultiplier(float multiplier) => _multiplier = Mathf.Clamp(multiplier, 0.1f, 1f);

    private void Update()
    {
        if (_reachedEnd || _path == null) return;

        var target = _path.GetPoint(_nextIndex);
        var step = _speed * _multiplier * Time.deltaTime;
        var pos = Vector3.MoveTowards(transform.position, target, step);
        transform.position = pos;

        var flat = new Vector3(target.x - pos.x, 0, target.z - pos.z);
        if (flat.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(flat);

        if ((pos - target).sqrMagnitude < 0.0025f && ++_nextIndex >= _path.Count)
            _reachedEnd = true;
    }
}
