using UnityEngine;

public class TowerMeshSwitcher : MonoBehaviour
{
    [SerializeField]
    private MeshFilter _target;

    public void Apply(Mesh[] meshes, int level)
    {
        if (_target == null || meshes == null || meshes.Length == 0) return;
        var idx = Mathf.Clamp(level - 1, 0, meshes.Length - 1);
        _target.sharedMesh = meshes[idx];
    }
}
