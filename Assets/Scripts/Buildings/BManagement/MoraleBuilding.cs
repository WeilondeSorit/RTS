using UnityEngine;

public class MoraleBuilding : BasicBulding
{
    private void Start()
    {
        Archer.churchesCount++;
    }

    private void OnDestroy()
    {
        Archer.churchesCount--;
    }
}