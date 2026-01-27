using UnityEngine;

public class ExperienceOrb : MonoBehaviour
{
    [Header("XP Settings")]
    [Header("XP Settings")]
    [SerializeField]
    private float _xpValue = 1f;
    [SerializeField]
    private float _attractionSpeed = 4f;
    [SerializeField]
    private float _attractionRadius = 3f;

    private Transform _player;

    // Свойства для доступа
    public float XPValue 
    { 
        get => _xpValue; 
        set => _xpValue = Mathf.Max(0, value); 
    }
    
    public float AttractionSpeed 
    { 
        get => _attractionSpeed; 
        set => _attractionSpeed = Mathf.Max(0, value); 
    }
    
    public float AttractionRadius 
    { 
        get => _attractionRadius; 
        set => _attractionRadius = Mathf.Max(0, value); 
    }

    protected virtual void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    protected virtual void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused) return;

        if (_player == null) return;

        float distance = Vector2.Distance(transform.position, _player.position);
        if (distance <= _attractionRadius)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                _player.position,
                _attractionSpeed * Time.deltaTime
            );
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            CollectXP();
        }
    }

    protected virtual void CollectXP()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddXP(_xpValue);
            Debug.Log($"[XP ORB] Collected {_xpValue} XP");
        }
        
        // Визуальный эффект
        // Можно добавить частицы, звук
        
        Destroy(gameObject);
    }
}
