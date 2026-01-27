using UnityEngine;
using System.Collections.Generic;

public class AuraRangeEffect : MonoBehaviour
{
    [Header("Aura Settings")]
    [SerializeField] private float _auraDamage = 2f;
    [SerializeField] private float _damageInterval = 1f;
    [SerializeField] private float _slowAmount = 0.3f; // 30% замедление

    public float AuraDamage 
    { 
        get => _auraDamage; 
        set => _auraDamage = value; 
    }
    
    public float DamageInterval 
    { 
        get => _damageInterval; 
        set => _damageInterval = value; 
    }
    
    public float SlowAmount 
    { 
        get => _slowAmount; 
        set => _slowAmount = Mathf.Clamp01(value); 
    }
    
    [Header("Visual Effects")]
    [SerializeField] protected ParticleSystem _auraParticles;
    [SerializeField] protected SpriteRenderer _auraRing;
    [SerializeField] protected Color _auraColor = new Color(0.2f, 0.6f, 1f, 0.3f);
    
    protected List<EnemyController> _enemiesInAura = new List<EnemyController>();
    protected float _damageTimer;
    protected CircleCollider2D _collider;
    
    protected virtual void Start()
    {
        _collider = GetComponent<CircleCollider2D>();
        
        // Настройка визуальных эффектов
        if (_auraRing != null)
        {
            _auraRing.color = _auraColor;
            UpdateAuraRingSize();
        }
        
        if (_auraParticles != null)
        {
            var shape = _auraParticles.shape;
            shape.radius = _collider.radius;
        }
    }
    
    protected virtual void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused) return;

        _damageTimer += Time.deltaTime;
        
        // Периодический урон врагам в ауре
        if (_damageTimer >= _damageInterval && _enemiesInAura.Count > 0)
        {
            ApplyAuraDamage();
            _damageTimer = 0f;
        }
        
        // Плавное изменение размера ауры при апгрейдах
        if (_auraRing != null)
        {
            UpdateAuraRingSize();
        }
    }
    
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy != null && !_enemiesInAura.Contains(enemy))
        {
            _enemiesInAura.Add(enemy);
            ApplySlowEffect(enemy, true);
            //Debug.Log($"[AURA] Enemy entered aura: {enemy.name}");
        }
    }
    
    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy != null && _enemiesInAura.Contains(enemy))
        {
            _enemiesInAura.Remove(enemy);
            ApplySlowEffect(enemy, false);
            //Debug.Log($"[AURA] Enemy left aura: {enemy.name}");
        }
    }
    
    protected virtual void ApplyAuraDamage()
    {
        // Удаляем null врагов
        _enemiesInAura.RemoveAll(e => e == null);

        List<EnemyController> enemiesToAttack = new List<EnemyController>(_enemiesInAura);
        
        foreach (var enemy in enemiesToAttack)
        {
            if (enemy == null) continue;
            
            enemy.TakeDamage(_auraDamage);
            //Debug.Log($"[AURA] Damaged {enemy.name} for {_auraDamage}");
            
            // Визуальный эффект попадания
            // можно добавить вспышку на враге
        }

        _enemiesInAura.RemoveAll(e => e == null);
    }
    
    protected virtual void ApplySlowEffect(EnemyController enemy, bool apply)
    {
        if (enemy == null) return;
        
        float speedMultiplier = apply ? (1f - _slowAmount) : 1f;
        
        // Сохраняем оригинальную скорость или модифицируем
        // В зависимости от вашей реализации EnemyController
        enemy.ModifySpeed(speedMultiplier);
        
        // Визуальный эффект замедления (синий оттенок)
        SpriteRenderer enemySprite = enemy.GetComponent<SpriteRenderer>();
        if (enemySprite != null)
        {
            enemySprite.color = apply ? Color.cyan : Color.white;
        }
    }
    
    protected virtual void UpdateAuraRingSize()
    {
        if (_auraRing != null && _collider != null)
        {
            float scale = _collider.radius * 2f;
            _auraRing.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
    
    public virtual void UpdateAuraRadius(float newRadius)
    {
        if (_collider != null)
        {
            _collider.radius = newRadius;
            
            // Обновляем визуальные эффекты
            if (_auraParticles != null)
            {
                var shape = _auraParticles.shape;
                shape.radius = newRadius;
            }
            
            UpdateAuraRingSize();
        }
    }
    
    protected virtual void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.5f);
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider != null)
        {
            Gizmos.DrawWireSphere(transform.position, collider.radius);
        }
    }
}