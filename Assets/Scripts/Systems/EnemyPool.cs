using UnityEngine;
using System.Collections.Generic;

public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance;
    
    [System.Serializable]
    public class EnemyTypePool
    {
        public string enemyType;
        public GameObject prefab;
        public int poolSize = 10;
    }
    
    [SerializeField] private List<EnemyTypePool> _enemyPools;
    private Dictionary<string, Queue<GameObject>> _poolDictionary = new Dictionary<string, Queue<GameObject>>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        
        // Инициализация пулов
        foreach (var pool in _enemyPools)
        {
            Queue<GameObject> enemyQueue = new Queue<GameObject>();
            
            for (int i = 0; i < pool.poolSize; i++)
            {
                GameObject enemy = Instantiate(pool.prefab, transform);
                enemy.SetActive(false);
                enemyQueue.Enqueue(enemy);
            }
            
            _poolDictionary[pool.enemyType] = enemyQueue;
        }
    }
    
    public GameObject GetEnemy(string type)
    {
        if (!_poolDictionary.ContainsKey(type))
        {
            Debug.LogError($"No pool for enemy type: {type}");
            return null;
        }
        
        if (_poolDictionary[type].Count == 0)
        {
            // Создаем дополнительный объект если пул пуст
            var poolConfig = _enemyPools.Find(p => p.enemyType == type);
            if (poolConfig != null)
            {
                GameObject newEnemy = Instantiate(poolConfig.prefab, transform);
                newEnemy.SetActive(true);
                return newEnemy;
            }
        }
        
        GameObject enemy = _poolDictionary[type].Dequeue();
        enemy.SetActive(true);
        return enemy;
    }
    
    public void ReturnEnemy(string type, GameObject enemy)
    {
        if (_poolDictionary.ContainsKey(type))
        {
            enemy.SetActive(false);
            _poolDictionary[type].Enqueue(enemy);
        }
    }
}