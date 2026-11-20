using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverView : BaseView
{
    private const string HighScoreKey = "HighScore";

    [SerializeField] private Statistics playerStatistics;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text highScoreText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    public override void Init()
    {
        base.Init();
        restartButton.onClick.AddListener(() => GameSession.Instance.RestartSession());
        mainMenuButton.onClick.AddListener(() => GameSession.Instance.GoToMainMenu());
    }

    // runs every time the screen is shown
    private void OnEnable()
    {
        if (playerStatistics == null)
            return;

        int score = playerStatistics.Score;
        int coins = playerStatistics.CoinCount;
        int best = PlayerPrefs.GetInt(HighScoreKey, 0);
        bool isNewBest = score > best;
        if (isNewBest)
        {
            best = score;
            PlayerPrefs.SetInt(HighScoreKey, best);
            PlayerPrefs.Save();
        }

        if (scoreText != null) scoreText.text = $"Score: {score}";
        if (coinsText != null) coinsText.text = $"Coins: {coins}";
        if (highScoreText != null)
            highScoreText.text = isNewBest ? $"NEW HIGH SCORE: {best}" : $"High Score: {best}";
    }
}