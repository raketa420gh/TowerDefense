using UnityEngine;

[RequireComponent(typeof(MovementComponent))]
public class PlayerAnimator : MonoBehaviour
{
    [SerializeField]
    private Animator _animator;

    private MovementComponent _movement;
    private int _isMovingHash;

    private void Awake()
    {
        _movement = GetComponent<MovementComponent>();
        _isMovingHash = Animator.StringToHash("isMoving");
    }

    private void Update()
    {
        _animator.SetBool(_isMovingHash, _movement.IsMoving);
    }
}
