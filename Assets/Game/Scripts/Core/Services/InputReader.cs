using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

public class InputReader : IInitializable, IDisposable
{
    public event Action<Vector2> Tap;

    private InputAction _tapAction;
    private InputAction _pointAction;

    public void Initialize()
    {
        _tapAction = new InputAction("Tap", InputActionType.Button, "<Pointer>/press");
        _pointAction = new InputAction("Point", InputActionType.Value, "<Pointer>/position");
        _tapAction.performed += OnTapPerformed;
        _tapAction.Enable();
        _pointAction.Enable();
    }

    public void Dispose()
    {
        if (_tapAction != null)
        {
            _tapAction.performed -= OnTapPerformed;
            _tapAction.Disable();
            _tapAction.Dispose();
        }
        if (_pointAction != null)
        {
            _pointAction.Disable();
            _pointAction.Dispose();
        }
    }

    private void OnTapPerformed(InputAction.CallbackContext ctx)
    {
        var pos = _pointAction.ReadValue<Vector2>();
        Tap?.Invoke(pos);
    }
}
