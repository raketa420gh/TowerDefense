using MagicStaff.Views;

public class GameplayHudView : DisplayableView
{
    public VirtualJoystickView Joystick => _joystick;

    [UnityEngine.SerializeField]
    VirtualJoystickView _joystick;
}
