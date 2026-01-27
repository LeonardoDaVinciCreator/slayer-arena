using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _pauseButton;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _restartButtonPause;
    [SerializeField] private Button _menuButtonPause;  // В меню из паузы
    [SerializeField] private Button _menuButtonGameOver; // В меню из GameOver
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _quitButton;    

    private void Start()
    {
        // Настройка кнопок
        if (_playButton != null)
            _playButton.onClick.AddListener(() => GameManager.Instance.StartGame());
        
        if (_pauseButton != null)
            _pauseButton.onClick.AddListener(() => GameManager.Instance.TogglePause());
        
        if (_resumeButton != null)
            _resumeButton.onClick.AddListener(() => GameManager.Instance.TogglePause());
        
        if (_menuButtonPause != null)
            _menuButtonPause.onClick.AddListener(() => GameManager.Instance.ShowMainMenu());

        if (_restartButtonPause != null)
            _restartButtonPause.onClick.AddListener(() => GameManager.Instance?.RestartGame());
        
        if (_menuButtonGameOver != null)
            _menuButtonGameOver.onClick.AddListener(() => GameManager.Instance.ShowMainMenu());
        
        if (_restartButton != null)
            _restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());
        
        if (_quitButton != null)
            _quitButton.onClick.AddListener(() => GameManager.Instance.QuitGame());        
        
    }
}