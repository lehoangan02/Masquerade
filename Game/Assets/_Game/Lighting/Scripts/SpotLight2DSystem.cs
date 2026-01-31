using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class SpotLight2DSystem
{
    private static readonly List<Light2D> SpotLights = new List<Light2D>();
    private static float lastRefreshTime;
    private const float RefreshInterval = 0.5f;

    public static void Register(Light2D light)
    {
        if (light == null) return;
        if (!SpotLights.Contains(light))
        {
            SpotLights.Add(light);
        }
    }

    public static void Unregister(Light2D light)
    {
        if (light == null) return;
        SpotLights.Remove(light);
    }

    public static bool IsTargetLit(Transform target, float targetRadius, LayerMask occlusionMask)
    {
        if (target == null) return false;

        RefreshSpotLightsIfNeeded();

        Vector3 targetPos = target.position;
        for (int i = SpotLights.Count - 1; i >= 0; i--)
        {
            Light2D light = SpotLights[i];
            if (light == null)
            {
                SpotLights.RemoveAt(i);
                continue;
            }

            if (!IsSpotLight(light)) continue;
            if (!light.isActiveAndEnabled) continue;

            Vector3 lightPos = light.transform.position;
            Vector2 toTarget = targetPos - lightPos;
            float distance = toTarget.magnitude;

            if (distance > light.pointLightOuterRadius + targetRadius) continue;

            float halfAngle = light.pointLightOuterAngle * 0.5f;
            if (halfAngle < 180f)
            {
                float angle = Vector2.Angle(light.transform.right, toTarget);
                if (angle > halfAngle) continue;
            }

            if (occlusionMask.value != 0)
            {
                Vector2 direction = toTarget.normalized;
                RaycastHit2D hit = Physics2D.Raycast(lightPos, direction, distance, occlusionMask);
                if (hit.collider != null)
                {
                    Transform hitTransform = hit.transform;
                    if (hitTransform != target && !hitTransform.IsChildOf(target))
                    {
                        continue;
                    }
                }
            }

            return true;
        }

        return false;
    }

    private static bool IsSpotLight(Light2D light)
    {
        if (light == null) return false;
        if (light.lightType != Light2D.LightType.Point) return false;
        return light.pointLightOuterAngle < 360f;
    }

    private static void RefreshSpotLightsIfNeeded()
    {
        if (!Application.isPlaying)
        {
            RefreshSpotLights();
            return;
        }

        if (SpotLights.Count == 0 || Time.time - lastRefreshTime > RefreshInterval)
        {
            RefreshSpotLights();
        }
    }

    private static void RefreshSpotLights()
    {
        SpotLights.Clear();
        Light2D[] lights = Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        foreach (Light2D light in lights)
        {
            if (IsSpotLight(light))
            {
                SpotLights.Add(light);
            }
        }
        lastRefreshTime = Time.time;
    }
}

public class SpotLight2DRegister : MonoBehaviour
{
    private Light2D light2D;

    private void Awake()
    {
        light2D = GetComponent<Light2D>();
    }

    private void OnEnable()
    {
        if (light2D == null) light2D = GetComponent<Light2D>();
        SpotLight2DSystem.Register(light2D);
    }

    private void OnDisable()
    {
        SpotLight2DSystem.Unregister(light2D);
    }
}
