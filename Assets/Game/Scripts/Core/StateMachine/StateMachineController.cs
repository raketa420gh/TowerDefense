using System.Collections.Generic;
using UnityEngine;

public abstract class StateMachineController<K, T> where K : MonoBehaviour
{
    private List<BaseState> _states;
    private Dictionary<T, int> _statesLinks;
    private int _statesCount;
    private K _parent;
    private T _currentStateType;
    private BaseState _activeState;

    public K Parent => _parent;
    public T CurrentStateType => _currentStateType;
    public BaseState ActiveState => _activeState;

    public void Initialise(K stateMachineParent, T defaultState)
    {
        _parent = stateMachineParent;
        _states = new List<BaseState>();
        _statesLinks = new Dictionary<T, int>();
        _statesCount = 0;
        RegisterStates();
        SetState(defaultState);
    }

    public void SetState(T stateType)
    {
        if (!_statesLinks.ContainsKey(stateType))
        {
            Debug.LogError($"StateMachine: state '{stateType}' is not registered.");
            return;
        }

        if (_activeState != null)
            _activeState.OnStateDisabled();

        _currentStateType = stateType;
        _activeState = _states[_statesLinks[_currentStateType]];
        _activeState.OnStateActivated();
    }

    public BaseState GetState(T state)
    {
        if (!_statesLinks.ContainsKey(state))
        {
            Debug.LogError($"StateMachine: state '{state}' is not registered.");
            return null;
        }

        return _states[_statesLinks[state]];
    }

    protected abstract void RegisterStates();

    protected void RegisterState(BaseState state, T tState)
    {
        _states.Add(state);
        _statesLinks.Add(tState, _statesCount);
        _statesCount++;

        state.OnStateRegistered();
    }
}
