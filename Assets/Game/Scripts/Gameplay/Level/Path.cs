using UnityEngine;

public class Path : MonoBehaviour
{
    [SerializeField]
    private Transform[] _waypoints;

    public int Count => _waypoints.Length;
    public Vector3 SpawnPoint => _waypoints[0].position;

    public Vector3 GetPoint(int index) => _waypoints[index].position;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_waypoints == null || _waypoints.Length < 2) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < _waypoints.Length - 1; i++)
            if (_waypoints[i] && _waypoints[i + 1])
                Gizmos.DrawLine(_waypoints[i].position, _waypoints[i + 1].position);
    }
#endif
}
