using UnityEngine;
using System;

public static class EnemyAlertSystem
{
    // The "Radio Channel"
    public static event Action<Vector3> OnPlayerFound;

    // The "Speak" button
    public static void TriggerAlert(Vector3 playerPos)
    {
        // Invoke sends the message to everyone listening
        OnPlayerFound?.Invoke(playerPos);
    }
}