using UnityEngine;
using GameData;

public class EnemyShooter : EnemyController
{
    [Header("Shooting Settings")]
    [SerializeField]
    protected GameObject _enemyProjectilePrefab;

    [SerializeField]
    protected float _shootCooldown = 2f;

    [SerializeField]
    protected float _shootRange = 6f;

    [SerializeField]
    protected float _projectileSpeed = 6f;

    protected float _shootTimer;

    protected float MoveSpeedAccessor => MoveSpeed;
    protected float DamageAccessor => Damage;

    protected override void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused)
        {
            _rigidbody.linearVelocity = Vector2.zero;
            return;
        }

        _shootTimer += Time.deltaTime;
        Move();
        TryShoot();
    }

    protected override void Move()
    {
        if (_player == null) return;

        float distance = Vector2.Distance(transform.position, _player.position);

        if (distance > _shootRange * 0.9f)
        {
            Vector2 direction = (_player.position - transform.position).normalized;
            _rigidbody.linearVelocity = direction * MoveSpeedAccessor;
        }
        else
        {
            _rigidbody.linearVelocity = Vector2.zero;
        }
    }

    protected virtual void TryShoot()
    {
        if (_player == null) return;

        float distance = Vector2.Distance(transform.position, _player.position);
        if (distance <= _shootRange && _shootTimer >= _shootCooldown)
        {
            _shootTimer = 0f;
            //Debug.Log($"[ENEMY FIRE] {name} is shooting at player.");
            Shoot();
        }
    }

    protected virtual void Shoot()
    {
        if (BulletPool.Instance == null)
        {
            //Debug.LogError("[ENEMY SHOOT] BulletPool instance is missing!");
            return;
        }

        GameObject projectile = BulletPool.Instance.GetEnemyBullet();
        projectile.transform.position = transform.position;
        projectile.transform.rotation = Quaternion.identity;

        EnemyProjectile proj = projectile.GetComponent<EnemyProjectile>();
         if (proj == null)
        {
            //Debug.LogError("[ENEMY SHOOT] EnemyProjectile script missing!");
            BulletPool.Instance.ReturnBullet(projectile);
            return;
        }

        proj.Init(_player.position, _projectileSpeed, DamageAccessor);
    }

    public void ConfigureShooterData(EnemyData data)
    {
        if (data != null)
        {
            _shootRange = data.shootRange;
            _projectileSpeed = data.projectileSpeed;
        }
    }
}
