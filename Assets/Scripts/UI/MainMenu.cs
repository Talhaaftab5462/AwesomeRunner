using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private string gameSceneName = "GameScene";

    private void Start()
    {
        Time.timeScale = 1f;
        if (playButton != null)
            playButton.onClick.AddListener(Play);
        if (quitButton != null)
            quitButton.onClick.AddListener(Quit);
        if (highScoreText != null)
            highScoreText.text = "High Score: " + PlayerPrefs.GetInt("HighScore", 0);
    }

    public void Play()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}