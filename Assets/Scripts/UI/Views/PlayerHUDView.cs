using TMPro;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class PlayerHUDView : BaseView
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text coinText;

    public override void Init()
    {
        base.Init();
        transform.position = Vector3.zero;
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.localPosition = Vector3.zero;
    }

    public void UpdateScore(string score)
    {
        scoreText.text = $"Score: {score} ";
    }

    public void UpdateCoins(int coins)
    {
        if (coinText != null)
            coinText.text = $"Coins: {coins}";
    }
}