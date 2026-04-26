using UnityEngine;

public class ChaseMovementBehaviour : EnemyBehaviourBase
{
    private bool _active;

    public override void OnActivated()   => _active = true;
    public override void OnDeactivated() => _active = false;

    public void PauseChase()  => _active = false;
    public void ResumeChase() => _active = true;

    private void FixedUpdate()
    {
        if (!_active || Ctx.Target == null) return;

        var rb  = Ctx.Rb;
        var dir = Ctx.Target.position - rb.position;
        dir.y = 0f;
        dir.Normalize();

        rb.MovePosition(rb.position + dir * Ctx.Config.moveSpeed * Time.fixedDeltaTime);

        if (dir.sqrMagnitude > 0.01f)
            rb.rotation = Quaternion.LookRotation(dir);
    }
}
