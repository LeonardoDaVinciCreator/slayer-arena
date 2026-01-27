using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField]
    private float _moveSpeed = 5f;

    [SerializeField]
    private InputActionReference _moveAction;

    [Header("Combat Settings")]
    [SerializeField]
    private float _fireRate = 1f;

    [SerializeField]
    private float _projectileSpeed = 7f;

    [SerializeField]
    private float _projectileDamage = 10f;

    [SerializeField]
    private GameObject _projectilePrefab;

    [SerializeField]
    private Transform _firePoint;

    [Header("Detection Colliders")]
    [SerializeField]
    private CircleCollider2D _meleeRange;    

    [SerializeField]
    private CircleCollider2D _auraRange;

    [SerializeField]
    private CircleCollider2D _shootRange;

    [Header("Base Radius Settings")]
    [SerializeField]
    private float _baseMeleeRadius = 1f;

    [SerializeField]
    private float _baseAuraRadius = 2f;

    [SerializeField]
    private float _baseShootRadius = 4f;

    [Header("Debug Visualization")]
    [SerializeField] private bool _showDebugRanges = true;
    [SerializeField] private Color _meleeRangeColor = Color.red;
    [SerializeField] private Color _auraRangeColor = Color.blue;
    [SerializeField] private Color _shootRangeColor = Color.green;
    
    public float MoveSpeed 
    { 
        get => _moveSpeed; 
        set => _moveSpeed = value; 
    }
    
    public float FireRate 
    { 
        get => _fireRate; 
        set => _fireRate = Mathf.Max(0.1f, value); // Ограничиваем минимальное значение
    }
    
    public float ProjectileSpeed 
    { 
        get => _projectileSpeed; 
        set => _projectileSpeed = value; 
    }
    
    public float ProjectileDamage 
    { 
        get => _projectileDamage; 
        set => _projectileDamage = Mathf.Max(0, value); // Урон не может быть отрицательным
    }
    
    public float BaseMeleeRadius => _baseMeleeRadius;
    public float BaseAuraRadius => _baseAuraRadius;
    public float BaseShootRadius => _baseShootRadius;
    
    // Для получения текущих радиусов
    public float CurrentMeleeRadius => _meleeRange != null ? _meleeRange.radius : _baseMeleeRadius;
    public float CurrentAuraRadius => _auraRange != null ? _auraRange.radius : _baseAuraRadius;
    public float CurrentShootRadius => _shootRange != null ? _shootRange.radius : _baseShootRadius;
    
    // Для получения коллайдеров (только get)
    public CircleCollider2D MeleeRange => _meleeRange;
    public CircleCollider2D AuraRange => _auraRange;
    public CircleCollider2D ShootRange => _shootRange;
    
    // Для трансформа
    public Transform FirePoint => _firePoint;
    
    // Текущая цель (read-only для других скриптов)
    public EnemyController CurrentTarget => _currentTarget;

    protected Rigidbody2D _rigidbody;
    protected Vector2 _moveInput;
    protected float _fireTimer;
    protected EnemyController _currentTarget;

    protected virtual void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _rigidbody.gravityScale = 0;
    }

    protected virtual void Start()
    {
        ApplyColliderRadius(1f, 1f, 1f);
    }

    protected virtual void OnEnable()
    {
        _moveAction.action.Enable();
    }

    protected virtual void OnDisable()
    {
        _moveAction.action.Disable();
    }

    protected virtual void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused)
        return;

        ReadMovement();
        HandleAutoAttack();
        HandleMeleeAttack();
        HandleAuraEffect();
    }

    protected virtual void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused)
        {
            _rigidbody.linearVelocity = Vector2.zero;
            return;
        }

        _rigidbody.linearVelocity = _moveInput * _moveSpeed;
    }

    protected virtual void ReadMovement()
    {
        Vector2 input = _moveAction.action.ReadValue<Vector2>();        

        if (input.magnitude > 1f)
            input.Normalize();

        _moveInput = input;
    }

    protected virtual void HandleAutoAttack()
    {
        _fireTimer += Time.deltaTime;

        if (_fireTimer >= _fireRate && _currentTarget != null)
        {
            _fireTimer = 0f;
            Debug.Log($"[PLAYER FIRE] Shooting at {_currentTarget.name}");
            Shoot(_currentTarget.transform.position);
        }
    }    

    private float _meleeTimer = 0f;
    private float _meleeCooldown = 0.5f;

    protected virtual void HandleMeleeAttack()
    {
        if (_meleeRange == null) return;

        _meleeTimer += Time.deltaTime;
        if (_meleeTimer < _meleeCooldown) return;
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            _meleeRange.bounds.center, 
            _meleeRange.radius
        );
        bool hitEnemy = false;
        foreach (var hit in hits)
        {
            EnemyController enemy = hit.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(_projectileDamage * 0.5f);
                hitEnemy = true;
                StartCoroutine(FlashEnemyOnHit(enemy));
                //Debug.Log($"[MELEE] Hit enemy {enemy.name} for {_projectileDamage * 0.5f} damage");
            }
        }
        if (hitEnemy)
        {
            _meleeTimer = 0f; // Сбрасываем таймер только при попадании
        }
    }

    private IEnumerator FlashEnemyOnHit(EnemyController enemy)
    {
        SpriteRenderer renderer = enemy.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            Color originalColor = renderer.color;
            renderer.color = Color.red; // Красный при ударе
            
            yield return new WaitForSeconds(0.1f);
            
            renderer.color = originalColor; // Возвращаем исходный цвет
        }
    }

    private float _auraDamageTimer = 0f;
    private float _auraDamageInterval = 1f; // Урон раз в секунду
    private float _auraDamage = 5f;
        
    protected virtual void HandleAuraEffect()
    {
        if (_auraRange == null) return;

        _auraDamageTimer += Time.deltaTime;
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            _auraRange.bounds.center, 
            _auraRange.radius
        );
        
        bool hasEnemies = false;
        foreach (var hit in hits)
        {
            EnemyController enemy = hit.GetComponent<EnemyController>();
            if (enemy != null)
            {
                hasEnemies = true;
            
                // Замедление (опционально)
                enemy.ModifySpeed(0.7f); // Замедление на 30%
                
                // Периодический урон
                if (_auraDamageTimer >= _auraDamageInterval)
                {
                    enemy.TakeDamage(_auraDamage);
                    
                    // Эффект ауры
                    StartCoroutine(FlashEnemyOnHit(enemy));
                }
            }
        }
        if (_auraDamageTimer >= _auraDamageInterval && hasEnemies)
        {
            _auraDamageTimer = 0f;
        }
    }

    protected virtual void Shoot(Vector3 targetPosition)
    {
        if (_projectilePrefab == null)
        {
            //Debug.LogError("[PLAYER SHOOT] Projectile prefab is NOT assigned!");
            return;
        }

        if (BulletPool.Instance == null)
        {
            //Debug.LogError("[PLAYER SHOOT] BulletPool instance is missing in the scene!");
            return;
        }

        GameObject projectile = BulletPool.Instance.GetPlayerBullet();
        projectile.transform.position = _firePoint.position;
        projectile.transform.rotation = Quaternion.identity;

        Projectile proj = projectile.GetComponent<Projectile>();
        if (proj == null)
        {
            //Debug.LogError("[PLAYER SHOOT] Projectile prefab has NO Projectile script attached!");
            BulletPool.Instance.ReturnBullet(projectile);
            return;
        }

        //Debug.Log($"[PLAYER SHOOT] Shooting at {targetPosition}");
        proj.Init(targetPosition, _projectileSpeed, _projectileDamage);
    }

    // Методы для установки целей (публичные)
    public virtual void SetTarget(EnemyController enemy)
    {
        _currentTarget = enemy;
        //if (enemy != null)
            //Debug.Log($"[TARGET] New target: {enemy.name}");
        //else
            //Debug.Log("[TARGET] No target.");
    }

    public virtual void ClearTarget(EnemyController enemy)
    {
        if (_currentTarget == enemy)
            _currentTarget = null;
           // Debug.Log("[TARGET] Target cleared.");
    }

    // Публичные методы для управления радиусами
    public virtual void ApplyColliderRadius(float meleeMultiplier, float auraMultiplier, float shootMultiplier)
    {
        if (_meleeRange != null)
            _meleeRange.radius = _baseMeleeRadius * meleeMultiplier;        

        if (_auraRange != null)
            _auraRange.radius = _baseAuraRadius * auraMultiplier;

        if (_shootRange != null)
            _shootRange.radius = _baseShootRadius * shootMultiplier;
    }

    public virtual void SetMeleeRadius(float radius)
    {
        if (_meleeRange != null)
            _meleeRange.radius = radius;
    }
    
    public virtual void SetAuraRadius(float radius)
    {
        if (_auraRange != null)
            _auraRange.radius = radius;
    }
    
    public virtual void SetShootRadius(float radius)
    {
        if (_shootRange != null)
            _shootRange.radius = radius;
    }

    public virtual void UpgradeMeleeRadius(float multiplier)
    {
        ApplyColliderRadius(multiplier, GetShootMultiplier(), GetAuraMultiplier());
    }

    public virtual void UpgradeAuraRadius(float multiplier)
    {
        ApplyColliderRadius(GetMeleeMultiplier(), multiplier, GetAuraMultiplier());
    }

    public virtual void UpgradeShootRadius(float multiplier)
    {
        ApplyColliderRadius(GetMeleeMultiplier(), GetShootMultiplier(), multiplier);
    }

    // Методы для получения текущих множителей
    public float GetMeleeMultiplier()
    {
        return _meleeRange != null ? _meleeRange.radius / _baseMeleeRadius : 1f;
    }

    public float GetShootMultiplier()
    {
        return _shootRange != null ? _shootRange.radius / _baseShootRadius : 1f;
    }

    public float GetAuraMultiplier()
    {
        return _auraRange != null ? _auraRange.radius / _baseAuraRadius : 1f;
    }

    // Методы для улучшений
    public void UpgradeDamage(float percentIncrease)
    {
        _projectileDamage *= (1 + percentIncrease / 100f);
        Debug.Log($"[UPGRADE] Damage increased by {percentIncrease}% to {_projectileDamage}");
    }
    
    public void UpgradeSpeed(float percentIncrease)
    {
        _moveSpeed *= (1 + percentIncrease / 100f);
        Debug.Log($"[UPGRADE] Speed increased by {percentIncrease}% to {_moveSpeed}");
    }
    
    public void UpgradeFireRate(float percentIncrease)
    {
        _fireRate *= (1 - percentIncrease / 100f); // Уменьшаем время между выстрелами
        _fireRate = Mathf.Max(0.1f, _fireRate); // Минимальная задержка 0.1 сек
        Debug.Log($"[UPGRADE] Fire rate increased by {percentIncrease}% to {_fireRate}");
    }
    
    public void UpgradeProjectileSpeed(float percentIncrease)
    {
        _projectileSpeed *= (1 + percentIncrease / 100f);
        Debug.Log($"[UPGRADE] Projectile speed increased by {percentIncrease}% to {_projectileSpeed}");
    }

    // Метод для получения данных о здоровье (если нужно)
    public float GetCurrentHealth()
    {
        // Предполагаем, что здоровье управляется через GameManager
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.CurrentHealth;
        }
        return 0f;
    }
    
    public float GetMaxHealth()
    {
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.MaxHealth;
        }
        return 0f;
    }

    // Визуализация в редакторе
    protected virtual void OnDrawGizmos()
    {
        if (!_showDebugRanges) return;
        
        if (_meleeRange != null)
        {
            Gizmos.color = _meleeRangeColor;
            Gizmos.DrawWireSphere(_meleeRange.bounds.center, _meleeRange.radius);
        }
        
        if (_auraRange != null)
        {
            Gizmos.color = _auraRangeColor;
            Gizmos.DrawWireSphere(_auraRange.bounds.center, _auraRange.radius);
        }
        
        if (_shootRange != null)
        {
            Gizmos.color = _shootRangeColor;
            Gizmos.DrawWireSphere(_shootRange.bounds.center, _shootRange.radius);
        }
    }
}