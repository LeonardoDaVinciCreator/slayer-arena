using UnityEngine;

public class EnemyFast : EnemyController
{
    protected override void Awake()
    {
        base.Awake();
        _moveSpeed *= 1.5f;
        _maxHealth *= 0.7f;
        _currentHealth = _maxHealth;
    }
}
