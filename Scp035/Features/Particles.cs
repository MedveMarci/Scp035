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

internal static class Particles
{
    private static readonly Dictionary<int, ParticleEffect> Active = new();

    internal static void Highlight(GameObject target, Color color, LightShadows shadows = LightShadows.None, float range = 3f, float intensity = 3f)
    {
        if (target == null)
            return;

        ParticleEffect effect = GetOrCreate(target);
        if (effect.Light != null)
            return;

        LightSourceToy light = LightSourceToy.Create(target.transform, false);
        if (light == null)
            return;

        light.Color = color;
        light.Intensity = intensity;
        light.Range = range;
        light.ShadowType = shadows;
        light.Position = target.transform.position;
        light.Spawn();

        effect.Light = light;
    }

    internal static void Field(GameObject target, Color color, Vector3 area = default, float particleSize = 0.1f, ushort colorIntensity = 80, float duration = 0f, float appearSpeed = 3f, float rotationSpeed = 30f, float disappearSpeed = 3f)
    {
        if (target == null || appearSpeed <= 0f || disappearSpeed <= 0f)
            return;

        ParticleEffect effect = GetOrCreate(target);
        if (effect.Particles.Count > 0)
            return;

        if (area == default)
            area = Vector3.one * 3f;

        int count = Mathf.Clamp(Scp035.Singleton.Config.ParticleDensity, 1, 32);
        Color particleColor = ResolveColor(target.transform.position, color, colorIntensity);

        for (int i = 0; i < count; i++)
        {
            PrimitiveObjectToy particle = PrimitiveObjectToy.Create(networkSpawn: false);
            if (particle == null)
                continue;

            particle.Type = PrimitiveType.Cube;
            particle.Color = particleColor;
            particle.Flags = PrimitiveFlags.Visible;
            particle.IsStatic = false;
            particle.SyncInterval = 0f;
            particle.Scale = Vector3.zero;
            particle.Position = target.transform.position;
            particle.Spawn();

            effect.Particles.Add(particle);
        }

        if (effect.Particles.Count == 0)
            return;

        effect.Handle = Timing.RunCoroutine(Animate(effect, target, area, particleSize, appearSpeed, rotationSpeed, disappearSpeed, duration));
    }

    internal static void Stop(GameObject target)
    {
        if (target == null)
            return;

        Stop(target.GetInstanceID());
    }

    internal static void StopAll()
    {
        if (Active.Count == 0)
            return;

        foreach (ParticleEffect effect in Active.Values.ToArray())
            effect.Dispose();

        Active.Clear();
    }

    private static void Stop(int key)
    {
        if (!Active.TryGetValue(key, out ParticleEffect effect))
            return;

        Active.Remove(key);
        effect.Dispose();
    }

    private static ParticleEffect GetOrCreate(GameObject target)
    {
        int key = target.GetInstanceID();
        if (Active.TryGetValue(key, out ParticleEffect effect))
            return effect;

        effect = new ParticleEffect();
        Active[key] = effect;
        return effect;
    }

    private static Color ResolveColor(Vector3 position, Color color, ushort intensity)
    {
        Room room = Room.GetRoomAtPosition(position);
        float scaled = room?.Zone switch
        {
            FacilityZone.LightContainment => intensity * 0.8f,
            FacilityZone.HeavyContainment => intensity * 0.6f,
            FacilityZone.Surface => intensity * 1.5f,
            _ => intensity
        };

        float alpha = room == null || room.Name == RoomName.Outside ? 1f : 0.5f;
        return new Color(color.r * scaled, color.g * scaled, color.b * scaled, alpha);
    }

    private static IEnumerator<float> Animate(ParticleEffect effect, GameObject target, Vector3 area, float size, float appearSpeed, float rotationSpeed, float disappearSpeed, float duration)
    {
        float appearTime = 1f / appearSpeed;
        float disappearTime = 1f / disappearSpeed;
        float life = appearTime + disappearTime;

        int count = effect.Particles.Count;
        float[] phases = new float[count];
        Quaternion[] rotations = new Quaternion[count];

        for (int i = 0; i < count; i++)
        {
            phases[i] = life * i / count;
            rotations[i] = Random.rotation;
            Reposition(effect.Particles[i], target.transform.position, area, rotations[i]);
        }

        float elapsed = 0f;
        int key = target.GetInstanceID();

        while (target != null && (duration <= 0f || elapsed < duration))
        {
            float delta = Time.deltaTime;
            elapsed += delta;

            Vector3 origin = target.transform.position;

            for (int i = 0; i < count; i++)
            {
                PrimitiveObjectToy particle = effect.Particles[i];
                if (particle == null || particle.IsDestroyed)
                    continue;

                float phase = phases[i] + delta;
                if (phase >= life)
                {
                    phase -= life;
                    rotations[i] = Random.rotation;
                    Reposition(particle, origin, area, rotations[i]);
                }

                phases[i] = phase;

                float scale = phase < appearTime ? phase / appearTime : 1f - (phase - appearTime) / disappearTime;

                particle.Scale = Vector3.one * (size * Mathf.Clamp01(scale));
                particle.Rotation = rotations[i] * Quaternion.Euler(0f, rotationSpeed * phase, 0f);
            }

            yield return Timing.WaitForOneFrame;
        }

        effect.Handle = default;
        Stop(key);
    }

    private static void Reposition(PrimitiveObjectToy particle, Vector3 origin, Vector3 area, Quaternion rotation)
    {
        if (particle == null || particle.IsDestroyed)
            return;

        particle.Position = origin + new Vector3(Random.Range(-area.x * 0.5f, area.x * 0.5f), Random.Range(-area.y * 0.5f, area.y * 0.5f), Random.Range(-area.z * 0.5f, area.z * 0.5f));
        particle.Rotation = rotation;
        particle.Scale = Vector3.zero;
    }

    private sealed class ParticleEffect
    {
        internal readonly List<PrimitiveObjectToy> Particles = [];

        internal CoroutineHandle Handle;
        internal LightSourceToy Light;

        internal void Dispose()
        {
            Timing.KillCoroutines(Handle);

            if (Light is { IsDestroyed: false })
                Light.Destroy();

            Light = null;

            foreach (PrimitiveObjectToy particle in Particles)
                if (particle is { IsDestroyed: false })
                    particle.Destroy();

            Particles.Clear();
        }
    }
}