using UnityEngine;
using System;

public static class EnemyAlertSystem
{
    // Action that sends the position of the found player
    public static event Action<Vector3> OnPlayerFound;

    public static void TriggerAlert(Vector3 playerPos)
    {
        OnPlayerFound?.Invoke(playerPos);
    }
}