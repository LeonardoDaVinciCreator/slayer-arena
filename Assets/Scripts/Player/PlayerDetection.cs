using UnityEngine;
using System.Collections.Generic;

public class PlayerDetection : MonoBehaviour
{
    [SerializeField]
    protected PlayerController _player;

    protected List<EnemyController> _enemiesInRange = new List<EnemyController>();

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy != null)
        {
             _enemiesInRange.Add(enemy);
            //Debug.Log($"[DETECTION] Enemy entered range: {enemy.name}");
            UpdateTarget();
        }
    }   
    

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy != null)
        {
            _player.ClearTarget(enemy);
            _enemiesInRange.Remove(enemy);

            //Debug.Log($"[DETECTION] Enemy exited  range: {enemy.name}");

            UpdateTarget();
        }
    }

    protected virtual void UpdateTarget()
    {
         _enemiesInRange.RemoveAll(e => e == null);

        if (_enemiesInRange.Count == 0)
        {
            _player.SetTarget(null);
            return;
        }

        EnemyController closest = null;
        float minDistance = float.MaxValue;

        foreach (EnemyController enemy in _enemiesInRange)
        {
            if (enemy == null) continue;

            float dist = Vector2.Distance(enemy.transform.position, _player.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = enemy;
            }
        }

        _player.SetTarget(closest);
    }
}
