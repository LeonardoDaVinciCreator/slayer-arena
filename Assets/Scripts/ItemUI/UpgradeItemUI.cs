using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameData;

public class UpgradeItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private Image _iconImage;
    [SerializeField] private Slider _progressSlider;
    
    [Header("Upgrade Data")]
    private UpgradeData _upgradeData;
    private Button _button;
    
    private void Awake()
    {
        _button = GetComponent<Button>();
        if (_button == null)
        {
            Debug.LogError("UpgradeItemUI: No Button component found!");
        }
    }
    
    public void Initialize(UpgradeData upgradeData, System.Action<string> onUpgradeSelected)
    {
        _upgradeData = upgradeData;
        
        // Заполняем UI
        if (_titleText != null)
        {
            _titleText.text = !string.IsNullOrEmpty(upgradeData.displayName) ? 
                upgradeData.displayName : upgradeData.name;
        }
        
        if (_levelText != null)
        {
            _levelText.text = $"Уровень: {upgradeData.currentLevel}/{upgradeData.maxLevel}";
        }
        
        if (_progressSlider != null)
        {
            _progressSlider.maxValue = upgradeData.maxLevel;
            _progressSlider.value = upgradeData.currentLevel;
        }
        
        if (_iconImage != null && !string.IsNullOrEmpty(upgradeData.icon))
        {
            Sprite icon = Resources.Load<Sprite>($"Icons/{upgradeData.icon}");
            if (icon != null)
            {
                _iconImage.sprite = icon;
                _iconImage.gameObject.SetActive(true);
            }
            else
            {
                _iconImage.gameObject.SetActive(false);
            }
        }
        else if (_iconImage != null)
        {
            _iconImage.gameObject.SetActive(false);
        }
        
        // Настраиваем кнопку
        if (_button != null)
        {
            _button.interactable = upgradeData.currentLevel < upgradeData.maxLevel;
            
            // Очищаем старые обработчики
            _button.onClick.RemoveAllListeners();
            
            // Добавляем новый обработчик
            if (upgradeData.currentLevel < upgradeData.maxLevel)
            {
                _button.onClick.AddListener(() => {
                    Debug.Log($"Upgrade selected: {upgradeData.name}");
                    onUpgradeSelected?.Invoke(upgradeData.name);
                });
            }
        }
        
        Debug.Log($"UpgradeItemUI initialized: {upgradeData.name}");
    }
    
    // Метод для обновления UI после апгрейда
    public void Refresh()
    {
        if (_upgradeData != null)
        {
            Initialize(_upgradeData, null);
        }
    }
}