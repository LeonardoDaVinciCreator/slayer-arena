using UnityEngine;
using System;
using GameData;
using System.Collections.Generic; 
using System.IO;

public class GameConfigManager : MonoBehaviour
{
    public static GameConfigManager Instance { get; private set; }
    
    private GameConfig _gameConfig;
    private Dictionary<string, UpgradeData> _upgrades = new Dictionary<string, UpgradeData>();
    private Dictionary<string, EnemyData> _enemyDataByName = new Dictionary<string, EnemyData>();
    
    public event Action OnConfigLoaded;
    
    [Header("Debug")]
    [SerializeField] private bool _logConfigDetails = true;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        LoadConfig();
    }
    
    public void LoadConfig()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("GameConfig");
        if (jsonFile == null)
        {
            Debug.LogError("GameConfig.json not found in Resources!");
            CreateDefaultConfig();
            return;
        }
        
        try
        {
            Debug.Log("📄 Loading game configuration...");
            _gameConfig = JsonUtility.FromJson<GameConfig>(jsonFile.text);
            
            ValidateConfig();
            InitializeDictionaries();
            
            OnConfigLoaded?.Invoke();
            Debug.Log("✅ Game configuration loaded successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to load game config: {e.Message}\n{e.StackTrace}");
            CreateDefaultConfig();
        }
    }
    
    private void ValidateConfig()
    {
        if (_gameConfig == null)
        {
            Debug.LogError("GameConfig is null after parsing!");
            return;
        }
        
        // Проверка основных секций
        if (_gameConfig.player == null)
        {
            Debug.LogWarning("Player config is null, creating default...");
            _gameConfig.player = new PlayerData();
        }
        
        if (_gameConfig.enemies == null)
        {
            Debug.LogWarning("Enemies list is null, creating empty list...");
            _gameConfig.enemies = new List<EnemyData>();
        }
        
        if (_gameConfig.upgrades == null)
        {
            Debug.LogWarning("Upgrades list is null, creating empty list...");
            _gameConfig.upgrades = new List<UpgradeData>();
        }
        
        if (_gameConfig.waves == null)
        {
            Debug.LogWarning("Wave config is null, creating default...");
            _gameConfig.waves = new WaveData();
        }
        
        if (_gameConfig.experience == null)
        {
            Debug.LogWarning("Experience config is null, creating default...");
            _gameConfig.experience = new ExperienceData();
        }
    }
    
    private void InitializeDictionaries()
    {
        // Инициализация словаря улучшений
        _upgrades.Clear();
        foreach (var upgrade in _gameConfig.upgrades)
        {
            if (!string.IsNullOrEmpty(upgrade.name))
            {
                _upgrades[upgrade.name] = upgrade;
                
                if (_logConfigDetails)
                {
                    Debug.Log($"📊 Upgrade loaded: {upgrade.name} (Max Level: {upgrade.maxLevel})");
                }
            }
            else
            {
                Debug.LogWarning("Upgrade with empty name found, skipping...");
            }
        }
        
        // Инициализация словаря данных врагов
        _enemyDataByName.Clear();
        foreach (var enemy in _gameConfig.enemies)
        {
            if (!string.IsNullOrEmpty(enemy.type))
            {
                _enemyDataByName[enemy.type] = enemy;
                
                if (_logConfigDetails)
                {
                    Debug.Log($"👾 Enemy loaded: {enemy.type} (HP: {enemy.maxHealth}, Speed: {enemy.moveSpeed})");
                }
            }
            else
            {
                Debug.LogWarning("Enemy with empty type found, skipping...");
            }
        }
        
        // Логирование итоговой информации
        if (_logConfigDetails)
        {
            Debug.Log($"🎮 Config Summary:");
            Debug.Log($"   Player: {(_gameConfig.player != null ? "OK" : "NULL")}");
            Debug.Log($"   Enemies: {_gameConfig.enemies.Count} types");
            Debug.Log($"   Upgrades: {_gameConfig.upgrades.Count} types");
            Debug.Log($"   Waves: {(_gameConfig.waves != null ? "OK" : "NULL")}");
            Debug.Log($"   Experience: {(_gameConfig.experience != null ? "OK" : "NULL")}");
        }
    }
    
    private void CreateDefaultConfig()
    {
        Debug.LogWarning("Creating default configuration...");
        
        _gameConfig = new GameConfig
        {
            player = new PlayerData
            {
                moveSpeed = 5f,
                maxHealth = 100f,
                fireRate = 1f,
                projectileSpeed = 7f,
                projectileDamage = 10f
            },
            enemies = new List<EnemyData>
            {
                new EnemyData
                {
                    type = "Normal",
                    moveSpeed = 2f,
                    maxHealth = 20f,
                    damage = 10f,
                    attackRange = 1.2f,
                    attackCooldown = 1.5f,
                    xpValue = 1f,
                    spawnWeight = 70
                },
                new EnemyData
                {
                    type = "Fast",
                    moveSpeed = 3.5f,
                    maxHealth = 15f,
                    damage = 8f,
                    attackRange = 1.0f,
                    attackCooldown = 1.0f,
                    xpValue = 2f,
                    spawnWeight = 20
                },
                new EnemyData
                {
                    type = "Shooter",
                    moveSpeed = 1.5f,
                    maxHealth = 25f,
                    damage = 15f,
                    attackRange = 1.5f,
                    attackCooldown = 2.0f,
                    xpValue = 3f,
                    spawnWeight = 10,
                    shootRange = 5f,
                    projectileSpeed = 4f
                }
            },
            upgrades = new List<UpgradeData>
            {
                new UpgradeData
                {
                    name = "Projectile",
                    displayName = "Пули",
                    currentLevel = 0,
                    maxLevel = 5,
                    damagePerLevel = new float[] {10, 15, 20, 25, 30, 35},
                    speedPerLevel = new float[] {7f, 8f, 9f, 10f, 11f, 12f},
                    fireRatePerLevel = new float[] {1f, 0.9f, 0.8f, 0.7f, 0.6f, 0.5f}
                },
                new UpgradeData
                {
                    name = "Aura",
                    displayName = "Аура",
                    currentLevel = 0,
                    maxLevel = 5,
                    damagePerLevel = new float[] {2f, 3f, 4f, 5f, 6f, 7f},
                    radiusPerLevel = new float[] {2f, 2.5f, 3f, 3.5f, 4f, 4.5f},
                    slowPerLevel = new float[] {0.1f, 0.15f, 0.2f, 0.25f, 0.3f, 0.35f}
                }
            },
            waves = new WaveData
            {
                baseEnemiesPerWave = 5,
                enemiesIncreasePerWave = 2,
                waveDuration = 30f,
                waveCooldown = 5f,
                bossWaveInterval = 5
            },
            experience = new ExperienceData
            {
                baseXPPerLevel = 100f,
                xpIncreasePerLevel = 50f,
                maxLevel = 20
            }
        };
        
        InitializeDictionaries();
        OnConfigLoaded?.Invoke();
        Debug.Log("✅ Default configuration created");
    }
    
    // Методы для получения данных
    public GameConfig GetConfig() => _gameConfig;
    public PlayerData GetPlayerConfig() => _gameConfig.player;
    public WaveData GetWaveConfig() => _gameConfig.waves;
    public ExperienceData GetExperienceConfig() => _gameConfig.experience;
    public List<EnemyData> GetEnemiesConfig() => _gameConfig.enemies;
    public List<UpgradeData> GetUpgradesConfig() => _gameConfig.upgrades;
    
    public EnemyData GetEnemyData(string enemyType)
    {
        if (_enemyDataByName.ContainsKey(enemyType))
            return _enemyDataByName[enemyType];
        
        Debug.LogWarning($"Enemy data for type '{enemyType}' not found, returning first enemy");
        return _gameConfig.enemies.Count > 0 ? _gameConfig.enemies[0] : null;
    }
    
    public UpgradeData GetUpgrade(string upgradeName)
    {
        if (_upgrades.ContainsKey(upgradeName))
            return _upgrades[upgradeName];
        
        Debug.LogWarning($"Upgrade '{upgradeName}' not found");
        return null;
    }
    
    public bool HasUpgrade(string upgradeName) => _upgrades.ContainsKey(upgradeName);
    
    public void ResetUpgradeLevels()
    {
        foreach (var upgrade in _gameConfig.upgrades)
        {
            upgrade.currentLevel = 0;
        }
        InitializeDictionaries();
        Debug.Log("Upgrade levels reset to 0");
    }
    
    public void SaveConfigToFile(string filePath = null)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            filePath = Path.Combine(Application.dataPath, "Resources", "GameConfig.json");
        }
        
        try
        {
            string json = JsonUtility.ToJson(_gameConfig, true);
            File.WriteAllText(filePath, json);
            Debug.Log($"✅ Config saved to: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to save config: {e.Message}");
        }
    }
    
    // Метод для обновления данных улучшения (например, после апгрейда)
    public void UpdateUpgradeLevel(string upgradeName, int newLevel)
    {
        if (_upgrades.ContainsKey(upgradeName))
        {
            _upgrades[upgradeName].currentLevel = newLevel;
            Debug.Log($"Upgrade '{upgradeName}' level set to {newLevel}");
        }
    }
}