using UnityEngine;
using System.Collections.Generic;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance;

    [Header("Player Bullets")]
    [SerializeField] protected GameObject _playerBulletPrefab;
    [SerializeField] protected int _playerBulletCount = 20;

    [Header("Enemy Bullets")]
    [SerializeField] protected GameObject _enemyBulletPrefab;
    [SerializeField] protected int _enemyBulletCount = 10;

    protected Queue<GameObject> _playerBullets = new Queue<GameObject>();
    protected Queue<GameObject> _enemyBullets = new Queue<GameObject>();

    protected virtual void Awake()
    {
        if (Instance == null)
            Instance = this;

        // Создаем пулы отдельно
        for (int i = 0; i < _playerBulletCount; i++)
            CreateBullet(_playerBulletPrefab, _playerBullets);

        for (int i = 0; i < _enemyBulletCount; i++)
            CreateBullet(_enemyBulletPrefab, _enemyBullets);
    }

    protected virtual void CreateBullet(GameObject prefab, Queue<GameObject> pool)
    {
        GameObject bullet = Instantiate(prefab, transform);
        bullet.SetActive(false);
        pool.Enqueue(bullet);
    }

    public virtual GameObject GetPlayerBullet()
    {
        return GetBulletFromPool(_playerBullets, _playerBulletPrefab);
    }

    public virtual GameObject GetEnemyBullet()
    {
        return GetBulletFromPool(_enemyBullets, _enemyBulletPrefab);
    }

    protected virtual GameObject GetBulletFromPool(Queue<GameObject> pool, GameObject prefab)
    {
        if (pool.Count == 0)
            CreateBullet(prefab, pool);

        GameObject bullet = pool.Dequeue();
        bullet.SetActive(true);
        return bullet;
    }

    public virtual void ReturnBullet(GameObject bullet)
    {
        bullet.SetActive(false);
        
        // Определяем, в какой пул вернуть
        if (bullet.GetComponent<Projectile>() != null)
            _playerBullets.Enqueue(bullet);
        else if (bullet.GetComponent<EnemyProjectile>() != null)
            _enemyBullets.Enqueue(bullet);
    }
}