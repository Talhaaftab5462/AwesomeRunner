using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Coin : PoolingObject<Coin>, ICollectable, IResettable
{
    [SerializeField] private int coinValue = 1;

    private SinAnimator sinAnimator;
    public Renderer Renderer { get; private set; }
    public BoxCollider Collider { get; private set; }

    public static event Action<int> OnCoinCollected;
    public static event Action<Coin> OnCoinDissapeared;

    private void Awake()
    {
        Renderer = GetComponent<Renderer>();
        Collider = GetComponent<BoxCollider>();
        sinAnimator = GetComponent<SinAnimator>();
    }

    public void Collect()
    {
        gameObject.SetActive(false);
        AudioManager.PlayCoin();
        OnCoinCollected?.Invoke(coinValue);
        OnCoinDissapeared?.Invoke(this);
    }

    public void UpdateStartPositionForSinAnimator()
    {
        if (sinAnimator)
            sinAnimator.UpdateStartPosition();
    }

    public void ResetToDefault()
    {
        transform.SetParent(OwningPool.transform, false);
        ReturnToPool();
    }
}