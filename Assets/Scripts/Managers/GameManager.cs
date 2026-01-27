using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using GameData;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI References")]
    [SerializeField] 
    private Slider _healthSlider;
    [SerializeField] 
    private Slider _xpSlider;
    [SerializeField] 
    private TMP_Text _levelText;
    [SerializeField] 
    private TMP_Text _healthText;
    [SerializeField] 
    private TMP_Text _xpText;
    [SerializeField] 
    private TMP_Text _waveText;
    [SerializeField] 
    private TMP_Text _timerText;    
    
    [Header("UI Panels")]
    [SerializeField] 
    private GameObject _hudPanel;
    [SerializeField] 
    private GameObject _mainMenuPanel;
    [SerializeField] 
    private GameObject _pauseMenuPanel;
    [SerializeField] 
    private GameObject _levelUpPanel;
    [SerializeField] 
    private GameObject _gameOverPanel;    

    [Header("Level Up UI")]
    [SerializeField] 
    private Transform _upgradesContent; // Content в LevelUp_Panel
    [SerializeField] 
    private GameObject _upgradeItemPrefab;

    // Game Data
    private GameConfig _gameConfig;
    private PlayerController _player;

    private bool _isGamePaused = false;

    // Свойство для проверки паузы из других классов
    public bool IsGamePaused => _isGamePaused;

    // Событие для уведомления о паузе
    public event Action<bool> OnGamePaused;
    
    // Player Stats
    private float _currentHealth;
    private float _maxHealth;
    private int _currentLevel = 1;
    private float _currentXP = 0f;
    private float _xpToNextLevel;
    
    // Wave System
    private int _currentWave = 0;
    private int _enemiesAlive = 0;
    private int _enemiesKilled = 0;
    private float _waveTimer = 0f;
    private bool _isWaveActive = false;
    private Coroutine _spawnCoroutine;
    
    // Upgrades
    private Dictionary<string, UpgradeData> _upgrades = new Dictionary<string, UpgradeData>();
    
    // Events
    public event Action OnLevelUp;
    public event Action OnHealthChanged;
    public event Action OnWaveChanged;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;
    public int CurrentLevel => _currentLevel;
    public float CurrentXP => _currentXP;
    public float XPToNextLevel => _xpToNextLevel;
    public int CurrentWave => _currentWave;
    public int EnemiesAlive => _enemiesAlive;
    public int EnemiesKilled => _enemiesKilled;
    public float WaveTimer => _waveTimer;
    public bool IsWaveActive => _isWaveActive;
    public PlayerController Player => _player;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadGameConfig();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        _player = FindObjectOfType<PlayerController>();

        Debug.Log($"[UI CHECK] _levelText: {_levelText?.name ?? "NULL"}");
        Debug.Log($"[UI CHECK] _healthText: {_healthText?.name ?? "NULL"}");
        Debug.Log($"[UI CHECK] _xpText: {_xpText?.name ?? "NULL"}");

        InitializeGame();
        ShowMainMenu();
    }
    
    private void Update()
    {
        if (!_isWaveActive) return;
        
        _waveTimer += Time.deltaTime;
        UpdateTimerUI();
        
        ApplyHealthRegen();
    }

    #region JSON Configuration

    private void LoadGameConfig()
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
            Debug.Log($"📄 JSON текст (первые 500 символов):\n{jsonFile.text.Substring(0, Mathf.Min(500, jsonFile.text.Length))}");

            _gameConfig = JsonUtility.FromJson<GameConfig>(jsonFile.text);

            Debug.Log($"🎮 Player config: {(_gameConfig.player != null ? "OK" : "NULL")}");
            Debug.Log($"👾 Enemies: {(_gameConfig.enemies?.Count ?? 0)}");
            Debug.Log($"⚡ Upgrades: {(_gameConfig.upgrades?.Count ?? 0)}");
            Debug.Log($"🌊 Waves: {(_gameConfig.waves != null ? "OK" : "NULL")}");
            Debug.Log($"📈 Experience: {(_gameConfig.experience != null ? "OK" : "NULL")}");

            if (_gameConfig.experience != null)
            {
                Debug.Log($"   baseXPPerLevel: {_gameConfig.experience.baseXPPerLevel}");
                Debug.Log($"   xpIncreasePerLevel: {_gameConfig.experience.xpIncreasePerLevel}");
                Debug.Log($"   maxLevel: {_gameConfig.experience.maxLevel}");
            }

            if (_gameConfig.enemies != null)
            {
                Debug.Log($"Enemies loaded: {_gameConfig.enemies.Count}");
                foreach (var enemy in _gameConfig.enemies)
                {
                    Debug.Log($"- {enemy.type}: HP={enemy.maxHealth}, Speed={enemy.moveSpeed}");
                }
            }

            
            foreach (var upgrade in _gameConfig.upgrades)
            {
                _upgrades[upgrade.name] = upgrade;
            }
            
            Debug.Log("Game configuration loaded successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game config: {e.Message}");
            CreateDefaultConfig();
        }
    }

    private void CreateDefaultConfig()
    {
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
    }

    public PlayerData GetPlayerConfig() => _gameConfig.player;
    public WaveData GetWaveConfig() => _gameConfig.waves;
    public ExperienceData GetExperienceConfig() => _gameConfig.experience;
    public List<EnemyData> GetEnemiesConfig() => _gameConfig.enemies;
    public UpgradeData GetUpgrade(string name) => _upgrades.ContainsKey(name) ? _upgrades[name] : null;

    #endregion

    #region Game Initialization

    private void InitializeGame()
    {
        var playerConfig = GetPlayerConfig();
        if (playerConfig == null) return;

        _maxHealth = playerConfig.maxHealth;
        _currentHealth = _maxHealth;
        
        if (_player != null)
        {
            _player.MoveSpeed = playerConfig.moveSpeed;
            _player.FireRate = playerConfig.fireRate;
            _player.ProjectileSpeed = playerConfig.projectileSpeed;
            _player.ProjectileDamage = playerConfig.projectileDamage;
        }
        
        var expConfig = GetExperienceConfig();
        if (expConfig != null)
        {
            _xpToNextLevel = expConfig.baseXPPerLevel;
        }

        _currentLevel = 1;
        _currentXP = 0f;
        
        _currentWave = 0;
        _enemiesAlive = 0;
        _enemiesKilled = 0;
        _waveTimer = 0f;
        
        UpdateAllUI();
    }

    #endregion

    #region Player Stats

    public void AddXP(float xpAmount)
    {
        _currentXP += Mathf.Max(0f, xpAmount);
        
        Debug.Log($"[XP] Added {xpAmount} XP. Total: {_currentXP}/{_xpToNextLevel}");

        var expConfig = GetExperienceConfig();
        if (expConfig == null) return;
        
        while (_currentXP >= _xpToNextLevel && _currentLevel < expConfig.maxLevel)
        {
            LevelUp();
        }
        
        UpdateXPUI();
    }

    private void LevelUp()
    {
        _currentXP -= _xpToNextLevel;
        _currentLevel++;
        
        var expConfig = GetExperienceConfig();
        if (expConfig != null)
        {
            _xpToNextLevel = expConfig.baseXPPerLevel + (expConfig.xpIncreasePerLevel * (_currentLevel - 1));
        }
        
        Debug.Log($"[LEVEL UP] New level: {_currentLevel}. Next level at: {_xpToNextLevel} XP");
        
        OnLevelUp?.Invoke();
        ShowLevelUpPanel();
        UpdateLevelUI();
        UpdateXPUI();
    }

    public void TakeDamage(float damage)
    {
        _currentHealth -= damage;
        _currentHealth = Mathf.Max(0, _currentHealth);
        
        Debug.Log($"[DAMAGE] Took {damage} damage. Health: {_currentHealth}/{_maxHealth}");
        
        OnHealthChanged?.Invoke();
        
        if (_currentHealth <= 0)
        {
            Die();
        }
        
        UpdateHealthUI();
    }

    public void Heal(float amount)
    {
        _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
        UpdateHealthUI();
    }
    
    public void SetMaxHealth(float maxHealth)
    {
        _maxHealth = Mathf.Max(0, maxHealth);
        if (_currentHealth > _maxHealth)
        {
            _currentHealth = _maxHealth;
        }
        UpdateHealthUI();
    }

    private void ApplyHealthRegen()
    {
        var healthUpgrade = GetUpgrade("Health");
        if (healthUpgrade != null && healthUpgrade.currentLevel > 0)
        {
            float regenAmount = healthUpgrade.regenPerLevel[healthUpgrade.currentLevel] * Time.deltaTime;
            Heal(regenAmount);
        }
    }

    private void Die()
    {
        Debug.Log("[PLAYER] Died!");
        ShowGameOver();
    }

    #endregion

    #region Wave System

    public void StartWave()
    {
        if (_isWaveActive) return;
        
        _currentWave++;
        _isWaveActive = true;
        _waveTimer = 0f;
        
        var waveConfig = GetWaveConfig();
        int enemiesToSpawn = waveConfig.baseEnemiesPerWave + (waveConfig.enemiesIncreasePerWave * (_currentWave - 1));
        
        Debug.Log($"[WAVE] Starting wave {_currentWave}. Enemies: {enemiesToSpawn}");
        
        _spawnCoroutine = StartCoroutine(SpawnWave(enemiesToSpawn));
        
        OnWaveChanged?.Invoke();
        UpdateWaveUI();
    }

    private IEnumerator SpawnWave(int enemyCount)
    {
        var enemiesConfig = GetEnemiesConfig();
        if (enemiesConfig == null || enemiesConfig.Count == 0) yield break;

        _enemiesAlive = 0;
        
        for (int i = 0; i < enemyCount; i++)
        {
            SpawnRandomEnemy(enemiesConfig);
            _enemiesAlive++;
            
            yield return new WaitForSeconds(0.5f);
        }
        
        while (_enemiesAlive > 0)
        {
            yield return new WaitForSeconds(1f);
        }
        
        CompleteWave();
    }

    private void SpawnRandomEnemy(List<EnemyData> enemiesConfig)
    {
        if (enemiesConfig == null || enemiesConfig.Count == 0)
        {
            Debug.LogError("No enemy configurations found!");
            return;
        }
        
        int totalWeight = 0;
        foreach (var enemy in enemiesConfig)
        {
            totalWeight += enemy.spawnWeight;
        }
        
        int randomValue = UnityEngine.Random.Range(0, totalWeight);
        int cumulativeWeight = 0;
        EnemyData selectedEnemy = null;
        
        foreach (var enemy in enemiesConfig)
        {
            cumulativeWeight += enemy.spawnWeight;
            if (randomValue < cumulativeWeight)
            {
                selectedEnemy = enemy;
                break;
            }
        }
        
        if (selectedEnemy != null)
        {
            SpawnEnemy(selectedEnemy);
        }
    }

    private void SpawnEnemy(EnemyData enemyData)
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        
        Vector2 spawnPosition = GetSpawnPosition(cam);
        
        GameObject enemyPrefab = GetEnemyPrefab(enemyData.type);
        if (enemyPrefab != null)
        {
            GameObject enemyObj = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            EnemyController enemy = enemyObj.GetComponent<EnemyController>();
            
           if (enemy != null)
            {
                enemy.ConfigureFromData(enemyData);
                
                EnemyShooter shooter = enemy as EnemyShooter;
                if (shooter != null)
                {
                    shooter.ConfigureShooterData(enemyData);
                }
            }
        }
    }

    private Vector2 GetSpawnPosition(Camera camera)
    {
        int side = UnityEngine.Random.Range(0, 4);
        float viewportX = 0f, viewportY = 0f;
        
        switch (side)
        {
            case 0: // Верх
                viewportX = UnityEngine.Random.Range(0.1f, 0.9f);
                viewportY = 1.1f;
                break;
            case 1: // Низ
                viewportX = UnityEngine.Random.Range(0.1f, 0.9f);
                viewportY = -0.1f;
                break;
            case 2: // Лево
                viewportX = -0.1f;
                viewportY = UnityEngine.Random.Range(0.1f, 0.9f);
                break;
            case 3: // Право
                viewportX = 1.1f;
                viewportY = UnityEngine.Random.Range(0.1f, 0.9f);
                break;
        }
        
        return camera.ViewportToWorldPoint(new Vector3(viewportX, viewportY, 0));
    }

    private GameObject GetEnemyPrefab(string type)
    {   
        switch (type)
        {
            case "Normal":
                return Resources.Load<GameObject>("Prefabs/Enemies/Enemy_Normal");
            case "Fast":
                return Resources.Load<GameObject>("Prefabs/Enemies/Enemy_Fast");
            case "Shooter":
                return Resources.Load<GameObject>("Prefabs/Enemies/Enemy_Shooter");
            default:
                return Resources.Load<GameObject>("Prefabs/Enemies/Enemy_Normal");
        }        
    }

    public void EnemyKilled(EnemyData enemyData)
    {
        _enemiesAlive--;
        _enemiesKilled++;
        
        AddXP(enemyData.xpValue);        
        
        if (_enemiesAlive <= 0 && _isWaveActive)
        {
            CompleteWave();
        }
    }

    private void CompleteWave()
    {
        _isWaveActive = false;
        Debug.Log($"[WAVE] Wave {_currentWave} completed!");
        
        StartCoroutine(WaveCooldown());
    }

    private IEnumerator WaveCooldown()
    {
        var waveConfig = GetWaveConfig();
        if (waveConfig != null)
        {
            yield return new WaitForSeconds(waveConfig.waveCooldown);
        }
        
        StartWave();
    }

    #endregion

    #region Upgrades System

    public void ApplyUpgrade(string upgradeName)
    {
        if (!_upgrades.ContainsKey(upgradeName)) return;
        
        var upgrade = _upgrades[upgradeName];
        
        if (upgrade.currentLevel >= upgrade.maxLevel)
        {
            Debug.Log($"[UPGRADE] {upgradeName} already at max level!");
            return;
        }
        
        upgrade.currentLevel++;
        Debug.Log($"[UPGRADE] {upgradeName} upgraded to level {upgrade.currentLevel}");
        
        ApplyUpgradeEffects(upgradeName, upgrade);        

        RefreshAllUpgradeItems();
        
        // Скрываем панель после выбора улучшения
        HideLevelUpPanel();
    }

    private void RefreshAllUpgradeItems()
    {
        foreach (Transform child in _upgradesContent)
        {
            UpgradeItemUI upgradeUI = child.GetComponent<UpgradeItemUI>();
            if (upgradeUI != null)
            {
                upgradeUI.Refresh();
            }
        }
    }

    private void ApplyUpgradeEffects(string upgradeName, UpgradeData upgrade)
    {
        switch (upgradeName)
        {
            case "Projectile":
                ApplyProjectileUpgrade(upgrade);
                break;
            case "Aura":
                ApplyAuraUpgrade(upgrade);
                break;
            case "Melee":
                ApplyMeleeUpgrade(upgrade);
                break;
            case "Magnet":
                ApplyMagnetUpgrade(upgrade);
                break;
            case "Health":
                ApplyHealthUpgrade(upgrade);
                break;
            case "Speed":
                ApplySpeedUpgrade(upgrade);
                break;
        }
    }

    private void ApplyProjectileUpgrade(UpgradeData upgrade)
    {
        int level = upgrade.currentLevel;

        if (_player == null) return;
        
        if (upgrade.damagePerLevel != null && level < upgrade.damagePerLevel.Length)
            _player.ProjectileDamage = upgrade.damagePerLevel[level];
        
        if (upgrade.speedPerLevel != null && level < upgrade.speedPerLevel.Length)
            _player.ProjectileSpeed = upgrade.speedPerLevel[level];
        
        if (upgrade.fireRatePerLevel != null && level < upgrade.fireRatePerLevel.Length)
            _player.FireRate = upgrade.fireRatePerLevel[level];
    }

    private void ApplyAuraUpgrade(UpgradeData upgrade)
    {
        int level = upgrade.currentLevel;
        
        AuraRangeEffect aura = FindObjectOfType<AuraRangeEffect>();
        if (aura == null) return;

        if (upgrade.damagePerLevel != null && level < upgrade.damagePerLevel.Length)
            aura.AuraDamage = upgrade.damagePerLevel[level];
        
        if (upgrade.radiusPerLevel != null && level < upgrade.radiusPerLevel.Length)
            aura.UpdateAuraRadius(upgrade.radiusPerLevel[level]);
        
        if (upgrade.slowPerLevel != null && level < upgrade.slowPerLevel.Length)
            aura.SlowAmount = upgrade.slowPerLevel[level];
    }

    private void ApplyMeleeUpgrade(UpgradeData upgrade)
    {
        int level = upgrade.currentLevel;
        
        MeleeRangeDetector melee = FindObjectOfType<MeleeRangeDetector>();

        if (melee == null) return;

        if (upgrade.damagePerLevel != null && level < upgrade.damagePerLevel.Length)
            melee.Damage = upgrade.damagePerLevel[level];
        
        if (upgrade.radiusPerLevel != null && level < upgrade.radiusPerLevel.Length)
        {
            melee.SetRadius(upgrade.radiusPerLevel[level]);
        }
        
        if (upgrade.attackIntervalPerLevel != null && level < upgrade.attackIntervalPerLevel.Length)
            melee.AttackInterval = upgrade.attackIntervalPerLevel[level];
        
        if (upgrade.knockbackPerLevel != null && level < upgrade.knockbackPerLevel.Length)
            melee.KnockbackForce = upgrade.knockbackPerLevel[level];
    }

    private void ApplyMagnetUpgrade(UpgradeData upgrade)
    {
        int level = upgrade.currentLevel;
        
        ExperienceOrb[] orbs = FindObjectsOfType<ExperienceOrb>();
        foreach (var orb in orbs)
        {
            if (upgrade.radiusPerLevel != null && level < upgrade.radiusPerLevel.Length)
                orb.AttractionRadius = upgrade.radiusPerLevel[level];
            
            if (upgrade.speedPerLevel != null && level < upgrade.speedPerLevel.Length)
                orb.AttractionSpeed = upgrade.speedPerLevel[level];
        }
    }

    private void ApplyHealthUpgrade(UpgradeData upgrade)
    {
        int level = upgrade.currentLevel;
        
        if (upgrade.healthPerLevel != null && level < upgrade.healthPerLevel.Length)
        {
           SetMaxHealth(upgrade.healthPerLevel[level]);
        }
    }

    private void ApplySpeedUpgrade(UpgradeData upgrade)
    {
        int level = upgrade.currentLevel;
        
        if (_player != null && upgrade.speedPerLevel != null && level < upgrade.speedPerLevel.Length)
        {
            _player.MoveSpeed = upgrade.speedPerLevel[level];
        }
    }

    #endregion

    #region UI Management

    private void UpdateAllUI()
    {
        UpdateHealthUI();
        UpdateXPUI();
        UpdateLevelUI();
        UpdateWaveUI();
        UpdateTimerUI();
    }

    private void UpdateHealthUI()
    {
        if (_healthSlider != null)
        {
            _healthSlider.maxValue = _maxHealth;
            _healthSlider.value = _currentHealth;
        }
        
        if (_healthText != null)
        {
            _healthText.text = $"{Mathf.Round(_currentHealth)}/{Mathf.Round(_maxHealth)}";
        }
    }

    private void UpdateXPUI()
    {
        if (_xpSlider != null)
        {
            _xpSlider.maxValue = _xpToNextLevel;
            _xpSlider.value = _currentXP;
        }
        
        if (_xpText != null)
        {
            _xpText.text = $"{Mathf.Round(_currentXP)}/{Mathf.Round(_xpToNextLevel)}";
        }
    }

    private void UpdateLevelUI()
    {
        if (_levelText != null)
        {
            _levelText.text = $"Lvl {_currentLevel}";
        }
        else
        {
            Debug.LogError("[UPDATE LEVEL UI] _levelText is NULL!");
        }
    }

    private void UpdateWaveUI()
    {
        if (_waveText != null)
        {
            _waveText.text = $"Wave: {_currentWave}";
        }
    }

    private void UpdateTimerUI()
    {
        if (_timerText != null)
        {
            int minutes = Mathf.FloorToInt(_waveTimer / 60);
            int seconds = Mathf.FloorToInt(_waveTimer % 60);
            _timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }    

    public void ShowMainMenu()
    {
        SetPanelState(_mainMenuPanel, true);
        SetPanelState(_hudPanel, false);
        SetPanelState(_pauseMenuPanel, false);
        SetPanelState(_levelUpPanel, false);
        SetPanelState(_gameOverPanel, false);        
        
        SetGamePaused(true);
    }

    public void StartGame()
    {
        DestroyAllEnemies();
        DestroyAllProjectiles();
        DestroyAllOrbs();

        SetPanelState(_mainMenuPanel, false);
        SetPanelState(_hudPanel, true);
        SetPanelState(_pauseMenuPanel, false);
        SetPanelState(_levelUpPanel, false);
        SetPanelState(_gameOverPanel, false);
        
        InitializeGame();
        SetGamePaused(false);
        
        StartWave();
    }

    public void SetGamePaused(bool paused)
    {
        _isGamePaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        OnGamePaused?.Invoke(paused);
        Debug.Log($"[PAUSE] Game {(paused ? "PAUSED" : "RESUMED")}");
    }

    public void TogglePause()
    {
        bool isPaused = !_pauseMenuPanel.activeSelf;
        _pauseMenuPanel.SetActive(isPaused);
        
        SetGamePaused(isPaused);
    }

    public void ShowLevelUpPanel()
    {
        SetPanelState(_levelUpPanel, true);

        SetGamePaused(true);

        foreach (var kvp in _upgrades)
        {
            Debug.Log($"- {kvp.Key}: Lvl {kvp.Value.currentLevel}/{kvp.Value.maxLevel}, DamagePerLevel: {kvp.Value.damagePerLevel?.Length ?? 0}");
        }
        
        SetupLevelUpUpgrades();
    }

    private void SetupLevelUpUpgrades()
    {
        if(_upgradesContent == null || _upgradeItemPrefab == null) return;        

        //очистка прошлых элементов
        foreach (Transform child in _upgradesContent)
        {
            Debug.Log($"Destroying old upgrade: {child.name}");
            Destroy(child.gameObject);
        }

        List<UpgradeData> availableUpgrades = new List<UpgradeData>();
        
        foreach (var upgrade in _upgrades.Values)
        {
            if (upgrade.currentLevel < upgrade.maxLevel)
            {
                availableUpgrades.Add(upgrade);
                Debug.Log($"[LEVEL UP] Available: {upgrade.name} (Lvl {upgrade.currentLevel}/{upgrade.maxLevel})");
            }
        }

        if (availableUpgrades.Count == 0)
        {
            Debug.Log("[LEVEL UP] No upgrades available!");
            HideLevelUpPanel();
            return;
        }

        // Выбираем 3 случайных улучшения без повторений
        int upgradesToShow = Mathf.Min(3, availableUpgrades.Count);
        List<UpgradeData> selectedUpgrades = new List<UpgradeData>();

        // Создаем временный список для выборки
        List<UpgradeData> tempList = new List<UpgradeData>(availableUpgrades);
        
        for (int i = 0; i < upgradesToShow; i++)
        {
            if (tempList.Count == 0) break;

            int randomIndex = UnityEngine.Random.Range(0, tempList.Count);
            UpgradeData selected = tempList[randomIndex];

            if (!selectedUpgrades.Contains(selected))
            {
                selectedUpgrades.Add(selected);                
            }
            // Удаляем из доступных
            tempList.RemoveAt(randomIndex);
        }

        foreach (var upgrade in selectedUpgrades)
        {
            CreateUpgradeItem(upgrade);
        }
        
    }    

    private void CreateUpgradeItem(UpgradeData upgrade)
    {
        GameObject upgradeItem = Instantiate(_upgradeItemPrefab, _upgradesContent);
        
        // Получаем компонент UpgradeItemUI
        UpgradeItemUI upgradeUI = upgradeItem.GetComponent<UpgradeItemUI>();
        
        if (upgradeUI == null)
        {
            Debug.LogError("[UPGRADE] No UpgradeItemUI component found on prefab!");
            Destroy(upgradeItem);
            return;
        }
        
        // Инициализируем UI
        upgradeUI.Initialize(upgrade, (upgradeName) => {
            ApplyUpgrade(upgradeName);
            HideLevelUpPanel();
        });
        
        Debug.Log($"[UPGRADE UI] Created UI for: {upgrade.name}");
    }

    private void OnUpgradeSelected(string upgradeName)
    {
        Debug.Log($"[UPGRADE] Selected: {upgradeName}");
        ApplyUpgrade(upgradeName);
        HideLevelUpPanel();
    }
    
    private string GetShortUpgradeDescription(UpgradeData upgrade)
    {
        int nextLevel = upgrade.currentLevel + 1;
        if (nextLevel > upgrade.maxLevel) return "MAX";
        
        switch (upgrade.name)
        {
            case "Projectile":
                return $"Урон: +{upgrade.damagePerLevel[nextLevel]}\nСкорость: x{upgrade.speedPerLevel[nextLevel]}\nСкорострельность: {upgrade.fireRatePerLevel[nextLevel]}";
            case "Aura":
                return $"Урон: +{upgrade.damagePerLevel[nextLevel]}\nРадиус: {upgrade.radiusPerLevel[nextLevel]}\nЗамедление: {upgrade.slowPerLevel[nextLevel]}";
            case "Melee":
                return $"Урон: +{upgrade.damagePerLevel[nextLevel]}\nРадиус: {upgrade.radiusPerLevel[nextLevel]}\nИнтервал: {upgrade.attackIntervalPerLevel[nextLevel]}с";
            case "Magnet":
                return $"Радиус: {upgrade.radiusPerLevel[nextLevel]}\nСкорость: {upgrade.speedPerLevel[nextLevel]}";
            case "Health":
                return $"Здоровье: {upgrade.healthPerLevel[nextLevel]}\nРегенерация: +{upgrade.regenPerLevel[nextLevel]}/с";
            case "Speed":
                return $"Скорость: {upgrade.speedPerLevel[nextLevel]}";
            default:
                return "УЛУЧШЕНИЕ";
        }
    }

    public void HideLevelUpPanel()
    {
        SetPanelState(_levelUpPanel, false);
        SetGamePaused(false);
    }

    public void ShowGameOver()
    {
        SetPanelState(_gameOverPanel, true);
        SetGamePaused(true);
    }

    public void RestartGame()
    {
        _isWaveActive = false;
        _currentWave = 0;
        _waveTimer = 0f;
        _enemiesAlive = 0;

        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }

        SetPanelState(_gameOverPanel, false);
        SetPanelState(_pauseMenuPanel, false);
        SetPanelState(_levelUpPanel, false);

        DestroyAllEnemies();
        DestroyAllProjectiles();
        DestroyAllOrbs();

        InitializeGame();

        SetPanelState(_hudPanel, true);
        SetGamePaused(false);

        StartWave();        
    }

    public void RestartFromPause()
    {        
        // Сначала снимаем паузу
        SetGamePaused(false);
        
        // Скрываем панель паузы
        SetPanelState(_pauseMenuPanel, false);
        
        // Ждем один кадр и рестартим
        StartCoroutine(RestartDelayed());
    }

    private IEnumerator RestartDelayed()
    {
        yield return null; // Ждем один кадр
        
        // Теперь делаем полный рестарт
        RestartGame();
    }

    private void DestroyAllEnemies()
    {
        EnemyController[] enemies = FindObjectsOfType<EnemyController>();
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.gameObject != null)
                Destroy(enemy.gameObject);
        }
        Debug.Log($"[GAME] Destroyed {enemies.Length} enemies");
    }

    private void DestroyAllProjectiles()
    {
        Projectile[] projectiles = FindObjectsOfType<Projectile>();
        EnemyProjectile[] enemyProjectiles = FindObjectsOfType<EnemyProjectile>();
        
        foreach (var proj in projectiles)
        {
            if (proj != null && proj.gameObject != null)
                Destroy(proj.gameObject);
        }
        
        foreach (var proj in enemyProjectiles)
        {
            if (proj != null && proj.gameObject != null)
                Destroy(proj.gameObject);
        }
        
        Debug.Log($"[GAME] Destroyed {projectiles.Length + enemyProjectiles.Length} projectiles");
    }

    private void DestroyAllOrbs()
    {
        ExperienceOrb[] orbs = FindObjectsOfType<ExperienceOrb>();
        foreach (var orb in orbs)
        {
            if (orb != null && orb.gameObject != null)
                Destroy(orb.gameObject);
        }
        Debug.Log($"[GAME] Destroyed {orbs.Length} orbs");
    }

    public void QuitToMenu()
    {
        SetPanelState(_gameOverPanel, false);
        ShowMainMenu();
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private void SetPanelState(GameObject panel, bool isActive)
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
        }
    }
    
    #endregion    
}