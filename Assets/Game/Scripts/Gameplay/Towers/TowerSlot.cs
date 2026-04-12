using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TowerSlot : MonoBehaviour
{
    public event Action<TowerSlot> Clicked;

    public bool IsOccupied => _tower != null;
    public Tower Tower => _tower;
    public Vector3 Position => transform.position;

    [SerializeField]
    private GameObject _highlight;

    private Tower _tower;

    public void Attach(Tower tower)
    {
        _tower = tower;
        if (_highlight != null) _highlight.SetActive(false);
    }

    public void Detach()
    {
        _tower = null;
        if (_highlight != null) _highlight.SetActive(true);
    }

    private void OnMouseDown()
    {
        if (IsOccupied) return;
        Clicked?.Invoke(this);
    }
}
