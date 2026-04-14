using UnityEngine;

public class TowerMeshSwitcher : MonoBehaviour
{
    [SerializeField]
    private GameObject[] _levelVisuals;

    public void Apply(int level)
    {
        for (int i = 0; i < _levelVisuals.Length; i++)
            if (_levelVisuals[i]) _levelVisuals[i].SetActive(i == level - 1);
    }
}
