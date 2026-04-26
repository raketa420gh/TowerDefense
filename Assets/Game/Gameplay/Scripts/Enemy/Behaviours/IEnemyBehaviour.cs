public interface IEnemyBehaviour
{
    void Initialize(EnemyBehaviourContext ctx);
    void OnActivated();
    void OnDeactivated();
}
