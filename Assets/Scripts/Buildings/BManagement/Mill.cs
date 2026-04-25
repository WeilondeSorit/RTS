using System.Collections;
using UnityEngine;

public class Mill : BasicBulding
{
    [SerializeField] private int foodPerCycle = 20;
    [SerializeField] private float cycleTime = 10f;

    private Coroutine productionCoroutine;

    protected override void Start()
    {
        base.Start();
        productionCoroutine = StartCoroutine(ProduceFood());
    }

    private IEnumerator ProduceFood()
    {
        while (true)
        {
            yield return new WaitForSeconds(cycleTime);
            if (PlayerData.Instance != null)
            {
                PlayerData.Instance.OnResourceCollected("food", foodPerCycle);
                Debug.Log($"Мельница произвела {foodPerCycle} еды. Всего еды: {PlayerData.Instance.food}");
            }
        }
    }

    protected override void HandleBuildingDestruction()
    {
        if (productionCoroutine != null)
            StopCoroutine(productionCoroutine);
        base.HandleBuildingDestruction();
    }
}