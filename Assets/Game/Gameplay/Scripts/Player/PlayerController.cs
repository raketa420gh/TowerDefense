using System;
using Zenject;

public class PlayerController : IInitializable, IDisposable
{
    readonly MovementComponent _movement;
    readonly GameplayHudView   _hud;

    [Inject]
    public PlayerController(MovementComponent movement, GameplayHudView hud)
    {
        _movement = movement;
        _hud      = hud;
    }

    public void Initialize()
        => _hud.Joystick.OnInputChanged += _movement.SetInput;

    public void Dispose()
        => _hud.Joystick.OnInputChanged -= _movement.SetInput;
}
