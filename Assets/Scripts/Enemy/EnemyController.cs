using UnityEngine;
using GameData;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    [Header("Enemy Settings")]
    [SerializeField]
    private EnemyType _enemyType;

    [SerializeField]
    protected float _moveSpeed = 2f;

    [SerializeField]
    protected float _maxHealth = 20f;

    [SerializeField]
    private float _damage = 10f;

    [SerializeField]
    private float _attackRange = 1.2f;

    [SerializeField]
    private float _attackCooldown = 1.5f;

    [Header("Drop Settings")]
    [SerializeField]
    private GameObject _experienceOrbPrefab;

    [Header("Status Effects")]
    [SerializeField] private float _baseMoveSpeed;
    private float _currentSpeedMultiplier = 1f;

    [Header("XP Settings")]
    [SerializeField] private float _xpValue = 1f;

    private Color _baseColor = Color.white;
    protected bool _isSlowed = false;

    protected Rigidbody2D _rigidbody;
    protected Transform _player;
    protected float _currentHealth;
    protected float _attackTimer;

    public EnemyType EnemyType => _enemyType;
    
    public float MoveSpeed 
    { 
        get => _moveSpeed; 
        set => _moveSpeed = Mathf.Max(0, value); 
    }
    
    public float MaxHealth 
    { 
        get => _maxHealth; 
        set 
        { 
            _maxHealth = Mathf.Max(0, value); 
            if (_currentHealth > _maxHealth)
                _currentHealth = _maxHealth;
        } 
    }
    
    public float CurrentHealth => _currentHealth;
    
    public float Damage 
    { 
        get => _damage; 
        set => _damage = Mathf.Max(0, value); 
    }
    
    public float AttackRange 
    { 
        get => _attackRange; 
        set => _attackRange = Mathf.Max(0, value); 
    }
    
    public float AttackCooldown 
    { 
        get => _attackCooldown; 
        set => _attackCooldown = Mathf.Max(0.1f, value); 
    }
    
    public float XPValue 
    { 
        get => _xpValue; 
        set => _xpValue = Mathf.Max(0, value); 
    }

    public bool IsSlowed => _isSlowed;
    public Transform Player => _player;

    protected virtual void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _rigidbody.gravityScale = 0;
        _currentHealth = _maxHealth;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            _baseColor = spriteRenderer.color;
        }

        _baseMoveSpeed = _moveSpeed;
    }

    protected virtual void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    protected virtual void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                return;
            }

        _attackTimer += Time.deltaTime;
        Move();
        TryAttack();
    }

    protected virtual void Move()
    {
        if (_player == null) return;

        Vector2 direction = (_player.position - transform.position).normalized;
        float currentSpeed = _moveSpeed * _currentSpeedMultiplier;
        _rigidbody.linearVelocity = direction * currentSpeed;
    }

    protected virtual void TryAttack()
    {
        if (_player == null) return;

        float distance = Vector2.Distance(transform.position, _player.position);
        if (distance <= _attackRange && _attackTimer >= _attackCooldown)
        {
            _attackTimer = 0f;
            
            // Наносим урон игроку
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TakeDamage(_damage);
                //Debug.Log($"[ENEMY ATTACK] Dealt {_damage} damage to player");
            }
        }
    }

    public virtual void TakeDamage(float amount)
    {
        _currentHealth -= amount;

        StartCoroutine(DamageFlash());

        if (_currentHealth <= 0f)
            Die();
    }

    /// <summary>
    /// меняем цвет врага при уроне и возращаем к исходному
    /// </summary>
    /// <returns></returns>
    private IEnumerator DamageFlash()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            
            yield return new WaitForSeconds(0.1f);
            
            spriteRenderer.color = _baseColor;
        }
    }

    public virtual void Heal(float amount)
    {
        _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
    }

    protected virtual void Die()
    {
        SpawnExperienceOrb();        

        if (GameManager.Instance != null)
        {
            // Находим конфиг врага по типу
            var enemiesConfig = GameManager.Instance.GetEnemiesConfig();
            if (enemiesConfig != null)
            {
                foreach (var config in enemiesConfig)
                {
                    if (config.type == _enemyType.ToString())
                    {
                        GameManager.Instance.EnemyKilled(config);
                        break;
                    }
                }
            }
        }
        
        Destroy(gameObject);
    }

    private void SpawnExperienceOrb()
    {
        if (_experienceOrbPrefab != null)
        {
            GameObject orb = Instantiate(_experienceOrbPrefab, transform.position, Quaternion.identity);
            ExperienceOrb experienceOrb = orb.GetComponent<ExperienceOrb>();
            if (experienceOrb != null)
            {
                experienceOrb.XPValue = _xpValue;
            }
        }
    }


    public virtual void ModifySpeed(float multiplier)
    {
        _currentSpeedMultiplier = multiplier;
        _isSlowed = multiplier < 1f;
        
        UpdateVisualEffects();
    }

    private void UpdateVisualEffects()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = _isSlowed ? Color.blue : Color.white;
        }
    }

    public virtual void ConfigureFromData(EnemyData data)
    {
        _moveSpeed = data.moveSpeed;
        _maxHealth = data.maxHealth;
        _currentHealth = _maxHealth;
        _damage = data.damage;
        _attackRange = data.attackRange;
        _attackCooldown = data.attackCooldown;
        _xpValue = data.xpValue;
    }
}
