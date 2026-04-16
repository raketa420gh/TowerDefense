using UnityEngine;

[RequireComponent(typeof(MovementComponent))]
public class PlayerAnimator : MonoBehaviour
{
    [SerializeField]
    Animator _animator;

    MovementComponent _movement;
    int _isMovingHash;

    void Awake()
    {
        _movement = GetComponent<MovementComponent>();
        _isMovingHash = Animator.StringToHash("isMoving");
    }

    void Update()
    {
        _animator.SetBool(_isMovingHash, _movement.IsMoving);
    }
}
