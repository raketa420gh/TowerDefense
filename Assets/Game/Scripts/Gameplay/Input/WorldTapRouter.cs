using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

public class WorldTapRouter : IInitializable, IDisposable, ITickable
{
    private readonly InputReader _input;
    private readonly LevelContext _levelContext;
    private bool _isPointerOverUI;

    public WorldTapRouter(InputReader input, LevelContext levelContext)
    {
        _input = input;
        _levelContext = levelContext;
    }

    public void Initialize() => _input.Tap += OnTap;
    public void Dispose() => _input.Tap -= OnTap;

    public void Tick()
    {
        _isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void OnTap(Vector2 screenPos)
    {
        if (_isPointerOverUI) return;

        var camera = _levelContext.LevelCamera != null
            ? _levelContext.LevelCamera
            : Camera.main;
        if (camera == null) return;

        var ray = camera.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out var hit, 500f)) return;

        var slot = hit.collider.GetComponentInParent<TowerSlot>();
        if (slot != null) { slot.OnTap(); return; }

        var tower = hit.collider.GetComponentInParent<Tower>();
        if (tower != null) tower.OnTap();
    }
}
