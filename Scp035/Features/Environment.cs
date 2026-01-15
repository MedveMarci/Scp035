using System.Collections.Generic;
using System.Linq;
using AdminToys;
using LabApi.Features.Wrappers;
using MapGeneration;
using MEC;
using UnityEngine;
using LightSourceToy = LabApi.Features.Wrappers.LightSourceToy;
using PrimitiveObjectToy = LabApi.Features.Wrappers.PrimitiveObjectToy;
using Random = UnityEngine.Random;

namespace Scp035.Features;

public static class Particles
{
    public static void HighlightObject(GameObject gameObject, Color color,
        LightShadows shadowsType = LightShadows.None, float range = 3f, float intensity = 3f)
    {
        var lightObject = LightSourceToy.Create(gameObject.transform, false);
        lightObject.Color = color;
        lightObject.Intensity = intensity;
        lightObject.Range = range;
        lightObject.ShadowType = shadowsType;
        lightObject.Position = gameObject.transform.position;
        lightObject.Spawn();
    }

    public static void ProceduralParticles(GameObject gameObject, Color particleColor, float duration = 0f,
        float spawnRate = 0.01f, Vector3 fieldLocalScale = default, float particleSize = 0.1f,
        ushort intensity = 80, float appearSpeed = 3f, float idleRotateSpeed = 30f, float disappearSpeed = 3f)
    {
        if (fieldLocalScale == default) fieldLocalScale = Vector3.one * 3f;
        var room = Room.GetRoomAtPosition(gameObject.transform.position) ?? Room.Get(RoomName.Outside).First();
        switch (room.Zone)
        {
            case FacilityZone.LightContainment:
                intensity = (ushort)(intensity * 0.8f);
                break;
            case FacilityZone.HeavyContainment:
                intensity = (ushort)(intensity * 0.6f);
                break;
            case FacilityZone.Surface:
                intensity = (ushort)(intensity * 1.5f);
                break;
        }

        Timing.RunCoroutine(SpawnParticleField(gameObject, particleColor, duration, spawnRate, fieldLocalScale,
            particleSize, intensity, appearSpeed, idleRotateSpeed, disappearSpeed), gameObject);
    }

    #region IEnumerators

    #region ParticleField

    #region DynamicPassiveParticles

    private static IEnumerator<float> SpawnParticleField(GameObject playerObject, Color particleColor,
        float duration, float spawnRate, Vector3 localScale, float particleSize, ushort intensity,
        float appearSpeed, float idleRotateSpeed, float disappearSpeed)
    {
        var anchor = new GameObject("ParticleAnchor");
        anchor.transform.SetParent(playerObject.transform);
        anchor.transform.localPosition = Vector3.zero;
        anchor.transform.localScale = localScale;
        var ended = false;
        if (duration != 0) Timing.CallDelayed(duration, () => ended = true);
        while (!ended && playerObject && anchor)
        {
            yield return Timing.WaitForSeconds(spawnRate);
            if (!anchor || !playerObject) break;
            var localOffset = new Vector3(Random.Range(-localScale.x / 2f, localScale.x / 2f),
                Random.Range(-localScale.y / 2f, localScale.y / 2f),
                Random.Range(-localScale.z / 2f, localScale.z / 2f));
            var spawnPos = anchor.transform.position + anchor.transform.rotation * localOffset;
            var particle = PrimitiveObjectToy.Create(networkSpawn: false);
            particle.Type = PrimitiveType.Cube;
            particle.Base.syncInterval = 0;
            var room = Room.GetRoomAtPosition(anchor.transform.position) ?? Room.Get(RoomName.Outside).First();
            if (room is { Name: RoomName.Outside })
                particle.Color = (particleColor * intensity) with { a = 1f };
            else
                particle.Color = (particleColor * intensity) with { a = 0.5f };
            particle.Position = spawnPos;
            particle.Scale = Vector3.zero;
            particle.Flags = PrimitiveFlags.Visible;
            particle.IsStatic = false;
            var baseRotation = Random.rotation;
            particle.Rotation = baseRotation;
            particle.Spawn();
            var totalLife = 1f / appearSpeed + 1f / disappearSpeed;
            Timing.RunCoroutine(ParticleLifeCycleHandler(particle, particleSize, appearSpeed, idleRotateSpeed,
                disappearSpeed, baseRotation, totalLife), particle.GameObject);
        }

        Object.Destroy(anchor);
    }

    private static bool IsParticleValid(PrimitiveObjectToy particle)
    {
        if (particle == null)
            return false;
        try
        {
            return particle.GameObject;
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerator<float> ParticleLifeCycleHandler(PrimitiveObjectToy particle, float maxScale,
        float appearSpeed, float rotationSpeed, float disappearSpeed, Quaternion baseRotation,
        float estimatedLifetime)
    {
        var appearTime = 1f / appearSpeed;
        var disappearTime = 1f / disappearSpeed;
        var idleTime = estimatedLifetime - appearTime - disappearTime;
        var time = 0f;

        while (time < appearTime && IsParticleValid(particle))
        {
            var t = time / appearTime;
            var scale = Mathf.Lerp(0f, maxScale, t);
            particle.Scale = Vector3.one * scale;
            particle.Rotation = baseRotation * Quaternion.Euler(0f, rotationSpeed * time, 0f);
            time += Time.deltaTime;
            yield return 0f;
        }

        time = 0f;

        while (time < idleTime && IsParticleValid(particle))
        {
            particle.Scale = Vector3.one * maxScale;
            particle.Rotation = baseRotation * Quaternion.Euler(0f, rotationSpeed * (appearTime + time), 0f);
            time += Time.deltaTime;
            yield return 0f;
        }

        time = 0f;

        while (time < disappearTime && IsParticleValid(particle))
        {
            var t = time / disappearTime;
            var scale = Mathf.Lerp(maxScale, 0f, t);
            particle.Scale = Vector3.one * scale;
            particle.Rotation = baseRotation * Quaternion.Euler(0f, rotationSpeed * (appearTime + idleTime + time), 0f);
            time += Time.deltaTime;
            yield return 0f;
        }

        if (IsParticleValid(particle))
            particle.Destroy();
    }

    #endregion

    #endregion

    #endregion
}