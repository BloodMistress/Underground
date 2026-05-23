using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ToxicGasZone : MonoBehaviour
{
    [SerializeField] private bool configureColliderAsTrigger = true;

    [Header("Visuals")]
    [SerializeField] private bool createGasParticles = true;
    [SerializeField] private bool hideSolidVolumeAtRuntime = true;
    [SerializeField] private Color gasColor = new Color(0.38f, 0.48f, 0.16f, 0.16f);
    [SerializeField] private float particleRate = 95f;
    [SerializeField] private float particleLifetime = 4.5f;
    [SerializeField] private float particleSize = 0.85f;
    [SerializeField] private float gasDriftSpeed = 0.18f;

    private void Reset()
    {
        ConfigureCollider();
    }

    private void Awake()
    {
        if (configureColliderAsTrigger)
        {
            ConfigureCollider();
        }

        if (hideSolidVolumeAtRuntime)
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }
        }

        if (createGasParticles)
        {
            EnsureGasParticles();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        OxygenSystem oxygen = other.GetComponentInParent<OxygenSystem>();
        if (oxygen != null)
        {
            oxygen.EnterToxicGasZone();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        OxygenSystem oxygen = other.GetComponentInParent<OxygenSystem>();
        if (oxygen != null)
        {
            oxygen.ExitToxicGasZone();
        }
    }

    private void ConfigureCollider()
    {
        Collider zoneCollider = GetComponent<Collider>();
        if (zoneCollider != null)
        {
            zoneCollider.isTrigger = true;
        }
    }

    private void EnsureGasParticles()
    {
        Transform existing = transform.Find("GasParticles");
        if (existing != null)
        {
            return;
        }

        GameObject particlesObject = new GameObject("GasParticles");
        particlesObject.transform.SetParent(transform, false);

        ParticleSystem particles = particlesObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = particleLifetime;
        main.startSpeed = gasDriftSpeed;
        main.startSize = new ParticleSystem.MinMaxCurve(particleSize * 0.45f, particleSize);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(gasColor.r, gasColor.g, gasColor.b, gasColor.a * 0.55f),
            new Color(0.55f, 0.58f, 0.20f, gasColor.a * 0.32f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 1400;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = particleRate;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = GetLocalGasSize();
        shape.randomDirectionAmount = 0.45f;

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.02f, 0.14f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.45f;
        noise.frequency = 0.28f;
        noise.scrollSpeed = 0.13f;

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(gasColor.r, gasColor.g, gasColor.b), 0f),
                new GradientColorKey(new Color(0.55f, 0.58f, 0.20f), 0.55f),
                new GradientColorKey(new Color(gasColor.r, gasColor.g, gasColor.b), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(gasColor.a, 0.25f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = gradient;

        ParticleSystemRenderer renderer = particlesObject.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingFudge = 2f;
        renderer.material = CreateGasMaterial();
    }

    private Vector3 GetLocalGasSize()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            return boxCollider.size;
        }

        Collider zoneCollider = GetComponent<Collider>();
        if (zoneCollider != null)
        {
            Vector3 worldSize = zoneCollider.bounds.size;
            return new Vector3(
                SafeDivide(worldSize.x, transform.lossyScale.x),
                SafeDivide(worldSize.y, transform.lossyScale.y),
                SafeDivide(worldSize.z, transform.lossyScale.z));
        }

        return Vector3.one;
    }

    private static float SafeDivide(float value, float divisor)
    {
        return Mathf.Abs(divisor) <= 0.0001f ? value : value / divisor;
    }

    private Material CreateGasMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Texture2D texture = CreateSoftParticleTexture();
        Material material = new Material(shader);
        material.name = "Runtime Toxic Gas Particles";
        material.SetColor("_BaseColor", Color.white);
        material.SetTexture("_BaseMap", texture);
        material.SetTexture("_MainTex", texture);
        return material;
    }

    private static Texture2D CreateSoftParticleTexture()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Runtime Toxic Gas Soft Particle",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = alpha * alpha * alpha;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply(false, true);
        return texture;
    }
}
