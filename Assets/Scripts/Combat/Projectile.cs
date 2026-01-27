using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField]
    protected float _lifetime = 5f;

    protected Vector2 _direction;
    protected float _speed;
    protected float _damage;
    protected float _timer;

    public virtual void Init(Vector3 target, float speed, float damage)
    {
        _speed = speed;
        _damage = damage;
        _direction = (target - transform.position).normalized;
        _timer = 0f;

        //Debug.Log($"[PLAYER BULLET] Fired toward {target}");
    }

    protected virtual void Update()
    {
        transform.position += (Vector3)(_direction * _speed * Time.deltaTime);

        _timer += Time.deltaTime;
        if (_timer >= _lifetime)
        {
            //Debug.Log("[PLAYER BULLET] Lifetime expired, returning to pool.");
            BulletPool.Instance.ReturnBullet(gameObject);
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy != null)
        {
            //Debug.Log($"[PLAYER HIT] Enemy {enemy.name} took {_damage} damage.");
            enemy.TakeDamage(_damage);
            BulletPool.Instance.ReturnBullet(gameObject);
        }
    }
}
