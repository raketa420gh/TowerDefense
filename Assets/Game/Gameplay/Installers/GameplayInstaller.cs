using UnityEngine;
using Zenject;

public class GameplayInstaller : MonoInstaller
{
    [SerializeField]
    MovementComponent _movement;

    [SerializeField]
    GameplayHudView _hudView;

    [SerializeField]
    PlayerConfig _playerConfig;

    public override void InstallBindings()
    {
        Container.BindInstance(_playerConfig);
        Container.BindInstance(_movement);
        Container.BindInstance(_hudView);

        Container.BindInterfacesAndSelfTo<PlayerController>().AsSingle().NonLazy();
    }
}
