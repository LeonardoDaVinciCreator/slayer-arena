using UnityEngine;
using System.Collections.Generic;

public class MeleeRangeDetector : MonoBehaviour
{
    [Header("Melee Settings")]
    [SerializeField] 
    private float _damage = 10f;
    [SerializeField] 
    private float _attackInterval = 0.5f;
    [SerializeField] 
    private float _knockbackForce = 2f;
    
    [Header("Visual Effects")]
    [SerializeField] 
    private GameObject _hitEffectPrefab;
    [SerializeField] 
    private LineRenderer _swingTrail;
    
    private List<EnemyController> _enemiesInRange = new List<EnemyController>();
    private float _attackTimer;
    private PlayerController _player;
    private CircleCollider2D _collider;

    public float Damage 
    { 
        get => _damage; 
        set => _damage = Mathf.Max(0, value); 
    }
    
    public float AttackInterval 
    { 
        get => _attackInterval; 
        set => _attackInterval = Mathf.Max(0.1f, value); 
    }
    
    public float KnockbackForce 
    { 
        get => _knockbackForce; 
        set => _knockbackForce = Mathf.Max(0, value); 
    }
    
    public CircleCollider2D Collider 
    { 
        get 
        {
            if (_collider == null)
                _collider = GetComponent<CircleCollider2D>();
            return _collider;
        }
    }
    
    public int EnemiesInRangeCount => _enemiesInRange.Count;
    public bool IsAttacking => _attackTimer < _attackInterval && _enemiesInRange.Count > 0;
    
    protected virtual void Start()
    {
        _player = GetComponentInParent<PlayerController>();
        
        // Настройка визуальных эффектов
        if (_swingTrail != null)
        {
            _swingTrail.positionCount = 0;
            _swingTrail.enabled = false;
        }
    }
    
    protected virtual void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused) return;

        _attackTimer += Time.deltaTime;
        
        if (_attackTimer >= _attackInterval && _enemiesInRange.Count > 0)
        {
            AttackAllEnemiesInRange();
            _attackTimer = 0f;
        }
        
        // Визуализация свинга
        if (_swingTrail != null && _enemiesInRange.Count > 0)
        {
            VisualizeSwing();
        }
    }
    
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy != null && !_enemiesInRange.Contains(enemy))
        {
            _enemiesInRange.Add(enemy);
            //Debug.Log($"[MELEE] Enemy entered melee range: {enemy.name}");
        }
    }
    
    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy != null && _enemiesInRange.Contains(enemy))
        {
            _enemiesInRange.Remove(enemy);
            //Debug.Log($"[MELEE] Enemy left melee range: {enemy.name}");
        }
    }
    
    protected virtual void AttackAllEnemiesInRange()
    {
        // Удаляем null врагов
        _enemiesInRange.RemoveAll(e => e == null);

        List<EnemyController> enemiesToAttack = new List<EnemyController>(_enemiesInRange);
        
        foreach (var enemy in enemiesToAttack)
        {
            if (enemy == null) continue;
            
            // Наносим урон
            enemy.TakeDamage(_damage);
            
            // Отбрасывание
            Vector2 knockbackDir = (enemy.transform.position - transform.position).normalized;
            enemy.GetComponent<Rigidbody2D>().AddForce(knockbackDir * _knockbackForce, ForceMode2D.Impulse);
            
            // Визуальный эффект
            if (_hitEffectPrefab != null)
            {
                Instantiate(_hitEffectPrefab, enemy.transform.position, Quaternion.identity);
            }
            
            //Debug.Log($"[MELEE] Hit {enemy.name} for {_damage} damage");
        }

        _enemiesInRange.RemoveAll(e => e == null);
        
        // Звук атаки
        // AudioManager.Instance.Play("MeleeSwing");
    }
    
    protected virtual void VisualizeSwing()
    {
        if (_enemiesInRange.Count == 0 || _enemiesInRange[0] == null) return;
        
        _swingTrail.enabled = true;
        _swingTrail.positionCount = 2;
        _swingTrail.SetPosition(0, transform.position);
        _swingTrail.SetPosition(1, _enemiesInRange[0].transform.position);
        
        // Плавное исчезновение
        Color endColor = _swingTrail.endColor;
        endColor.a = Mathf.Lerp(1f, 0f, _attackTimer / _attackInterval);
        _swingTrail.endColor = endColor;
    }
    
    protected virtual void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        CircleCollider2D collider = GetComponent<CircleCollider2D>();
        if (collider != null)
        {
            Gizmos.DrawWireSphere(transform.position, collider.radius);
        }
    }

    public void SetRadius(float radius)
    {
        if (Collider != null)
        {
            Collider.radius = Mathf.Max(0.1f, radius);
        }
    }

    public float GetRadius()
    {
        return Collider != null ? Collider.radius : 0f;
    }
}