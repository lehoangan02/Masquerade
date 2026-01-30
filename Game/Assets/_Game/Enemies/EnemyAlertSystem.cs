using UnityEngine;
using System;

public static class EnemyAlertSystem
{
    // Updated: Now sends Target Position, Sound Origin, and Sound Range
    public static event Action<Vector3, Vector3, float> OnPlayerFound;

    public static void TriggerAlert(Vector3 targetPos, Vector3 soundOrigin, float range)
    {
        OnPlayerFound?.Invoke(targetPos, soundOrigin, range);
    }
}