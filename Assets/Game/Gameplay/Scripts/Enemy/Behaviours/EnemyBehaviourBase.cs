using UnityEngine;

public abstract class EnemyBehaviourBase : MonoBehaviour, IEnemyBehaviour
{
    protected EnemyBehaviourContext Ctx { get; private set; }

    public virtual void Initialize(EnemyBehaviourContext ctx) => Ctx = ctx;
    public virtual void OnActivated()   { }
    public virtual void OnDeactivated() { }
}
