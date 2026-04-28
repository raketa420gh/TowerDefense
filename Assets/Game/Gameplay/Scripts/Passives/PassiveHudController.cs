using System;
using Zenject;

public class PassiveHudController : IInitializable, IDisposable
{
    private ActivePassivesView    _view;
    private IPassiveEffectService _service;

    [Inject]
    public void Construct(ActivePassivesView    view,
                          IPassiveEffectService service)
    {
        _view    = view;
        _service = service;
    }

    public void Initialize()
    {
        _service.OnPassivesChanged += Refresh;
        Refresh();
    }

    public void Dispose() => _service.OnPassivesChanged -= Refresh;

    private void Refresh() => _view.Render(_service.ActivePassives);
}
