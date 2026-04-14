using UnityEngine;

[RequireComponent(typeof(Path))]
public class SnowPathRenderer : MonoBehaviour
{
    [SerializeField]
    private GameObject _straightPrefab;

    [SerializeField]
    private GameObject _cornerPrefab;

    [SerializeField]
    private float _tileSize = 2f;

    [SerializeField]
    private Transform _tilesRoot;

    [ContextMenu("Regenerate")]
    public void Regenerate()
    {
        Clear();
        Build();
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        if (_tilesRoot == null) return;
        for (int i = _tilesRoot.childCount - 1; i >= 0; i--)
            DestroyImmediate(_tilesRoot.GetChild(i).gameObject);
    }

    private void Build()
    {
        if (_tilesRoot == null || _straightPrefab == null) return;

        var path = GetComponent<Path>();
        if (path == null || path.Count < 2) return;

        for (int i = 0; i < path.Count; i++)
        {
            var pos = path.GetPoint(i);

            // Place corner tile at intermediate waypoints where direction actually changes
            if (i > 0 && i < path.Count - 1 && _cornerPrefab != null)
            {
                var inDir = Flat(pos - path.GetPoint(i - 1));
                var outDir = Flat(path.GetPoint(i + 1) - pos);
                float cross = inDir.x * outDir.z - inDir.z * outDir.x;
                if (Mathf.Abs(cross) > 0.01f)
                {
                    float yRot = cross > 0f ? 0f : 90f;
                    float baseRot = Mathf.Atan2(inDir.x, inDir.z) * Mathf.Rad2Deg;
                    Place(_cornerPrefab, pos, baseRot + yRot);
                }
            }

            // Fill straight tiles along each segment
            if (i < path.Count - 1)
            {
                var from = path.GetPoint(i);
                var to = path.GetPoint(i + 1);
                var dir = Flat(to - from);
                float yRot = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                float segLen = Vector3.Distance(from, to);
                int count = Mathf.RoundToInt(segLen / _tileSize);
                for (int t = 0; t < count; t++)
                {
                    var tilePos = from + dir * (_tileSize * (t + 0.5f));
                    Place(_straightPrefab, tilePos, yRot);
                }
            }
        }
    }

    private void Place(GameObject prefab, Vector3 pos, float yRot)
    {
#if UNITY_EDITOR
        var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, _tilesRoot);
#else
        var go = Instantiate(prefab, _tilesRoot);
#endif
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yRot, 0f));
    }

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v.normalized;
    }
}
