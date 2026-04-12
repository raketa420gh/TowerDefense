using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public bool ReachedEnd => _reachedEnd;

    private Path _path;
    private float _speed;
    private int _nextIndex;
    private bool _reachedEnd;

    public void Init(Path path, float speed)
    {
        _path = path;
        _speed = speed;
        _nextIndex = 1;
        _reachedEnd = false;
        transform.position = path.SpawnPoint;
    }

    private void Update()
    {
        if (_reachedEnd || _path == null) return;

        var target = _path.GetPoint(_nextIndex);
        var pos = Vector3.MoveTowards(transform.position, target, _speed * Time.deltaTime);
        transform.position = pos;

        var flat = new Vector3(target.x - pos.x, 0, target.z - pos.z);
        if (flat.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(flat);

        if ((pos - target).sqrMagnitude < 0.0025f && ++_nextIndex >= _path.Count)
            _reachedEnd = true;
    }
}
