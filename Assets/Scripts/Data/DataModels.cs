using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameData
{
    [Serializable]
    public class PlayerData
    {
        public float moveSpeed;
        public float maxHealth;
        public float fireRate;
        public float projectileSpeed;
        public float projectileDamage;
    }

    [Serializable]
    public class EnemyData
    {
        public string type;
        public float moveSpeed;
        public float maxHealth;
        public float damage;
        public float attackRange;
        public float attackCooldown;
        public float xpValue;
        public int spawnWeight;
        
        // Для стрелков
        public float shootRange;
        public float projectileSpeed;
    }

    [Serializable]
    public class UpgradeData
    {
        public string name;
        public string displayName;
        public string icon;
        public int currentLevel;
        public int maxLevel;
        public float[] damagePerLevel;
        public float[] speedPerLevel;
        public int[] countPerLevel;
        public float[] radiusPerLevel;
        public float[] fireRatePerLevel;
        public float[] slowPerLevel;
        public float[] attackIntervalPerLevel;
        public float[] knockbackPerLevel;
        public float[] healthPerLevel;
        public float[] regenPerLevel;
    }

    [Serializable]
    public class WaveData
    {
        public int baseEnemiesPerWave;
        public int enemiesIncreasePerWave;
        public float waveDuration;
        public float waveCooldown;
        public int bossWaveInterval;
    }

    [Serializable]
    public class ExperienceData
    {
        public float baseXPPerLevel;
        public float xpIncreasePerLevel;
        public int maxLevel;
    }

    [Serializable]
    public class GameConfig
    {
        public PlayerData player;
        public List<EnemyData> enemies;
        public List<UpgradeData> upgrades;
        public WaveData waves;
        public ExperienceData experience;
    }
}