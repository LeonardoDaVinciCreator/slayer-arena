using UnityEngine;

public class EnemyProjectile : MonoBehaviour
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

        //Debug.Log($"[ENEMY BULLET] Fired toward {target}");
    }

    protected virtual void Update()
    {
        transform.position += (Vector3)(_direction * _speed * Time.deltaTime);

        _timer += Time.deltaTime;
        if (_timer >= _lifetime)
        {
            //Debug.Log("[ENEMY BULLET] Lifetime expired, returning to pool.");
            BulletPool.Instance.ReturnBullet(gameObject);
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TakeDamage(_damage);
                Debug.Log($"[ENEMY HIT] Player took {_damage} damage");
            }
            //Debug.Log($"[PLAYER HIT] Player took {_damage} damage.");
            BulletPool.Instance.ReturnBullet(gameObject);
        }
    }
}
