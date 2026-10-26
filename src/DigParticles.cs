using System;
using System.Collections.Generic;
using Godot;

public static class SimpleParticles
{
    public sealed class Settings
    {
        public int MinCount = 1;
        public int MaxCount = 2;

        public float MinSpeed = 1.5f;
        public float MaxSpeed = 15f;

        public float MinLifetime = 0.3f;
        public float MaxLifetime = 0.8f;

        public float MinGravity = 8f;
        public float MaxGravity = 8f;

        public float Spread = 80f;
        public float InitialVelocityRandom = 0.99f;

        public float MinSize = 0.10f;
        public float MaxSize = 0.18f;

        public Vector3 Direction = Vector3.Up;
        public Color Color = new(0.459f, 0.333f, 0.227f);
    }

    private sealed class Emitter
    {
        public Particles Node;
        public ParticlesMaterial ProcessMaterial;
        public SpatialMaterial DrawMaterial;
        public SphereMesh Mesh;
        public float Remaining;
    }

    private static readonly Stack<Emitter> Pool = new();
    private static readonly List<Emitter> Active = [];
    private static readonly Random Rng = new();

    private static Spatial _root;
    private static bool _initialized;

    public static void Initialize(Spatial root, Action<Action<float>> subscribeToUpdate)
    {
        _root = root;

        if (_initialized)
            return;

        _initialized = true;
        subscribeToUpdate(Update);
    }

    public static void Emit(Vector3 position, Settings settings = null)
    {
        if (_root == null)
            return;

        settings ??= new Settings();

        int count = RandInt(settings.MinCount, settings.MaxCount);
        float speed = Rand(settings.MinSpeed, settings.MaxSpeed);
        float lifetime = Rand(settings.MinLifetime, settings.MaxLifetime);
        float gravity = Rand(settings.MinGravity, settings.MaxGravity);
        float size = Rand(settings.MinSize, settings.MaxSize);

        if (count <= 0 || lifetime <= 0)
            return;

        var emitter = GetEmitter();

        emitter.Node.Translation = _root.ToLocal(position);
        emitter.Node.Amount = count;
        emitter.Node.Lifetime = lifetime;
        emitter.Node.Explosiveness = 1f;
        emitter.Node.Emitting = false;

        emitter.ProcessMaterial.Direction = settings.Direction;
        emitter.ProcessMaterial.Spread = settings.Spread;
        emitter.ProcessMaterial.InitialVelocity = speed;
        emitter.ProcessMaterial.InitialVelocityRandom = settings.InitialVelocityRandom;
        emitter.ProcessMaterial.Gravity = new Vector3(0, -gravity, 0);

        emitter.Mesh.Radius = size;
        emitter.Mesh.Height = size * 2;
        emitter.DrawMaterial.AlbedoColor = settings.Color;

        emitter.Remaining = lifetime + 0.15f;
        emitter.Node.Visible = true;
        emitter.Node.Restart();
        emitter.Node.Emitting = true;

        Active.Add(emitter);
    }

    private static Emitter GetEmitter()
    {
        if (Pool.Count > 0)
            return Pool.Pop();

        var mesh = new SphereMesh
        {
            Radius = 0.15f,
            Height = 0.3f,
            RadialSegments = 4,
            Rings = 2,
        };

        var drawMaterial = new SpatialMaterial { AlbedoColor = new Color(0.65f, 0.55f, 0.4f) };

        var processMaterial = new ParticlesMaterial
        {
            Direction = Vector3.Up,
            Spread = 80f,
            Gravity = new Vector3(0, -12, 0),
            InitialVelocity = 2.5f,
            InitialVelocityRandom = 0.4f,
        };

        var node = new Particles
        {
            DrawPass1 = mesh,
            DrawPasses = 1,
            MaterialOverride = drawMaterial,
            ProcessMaterial = processMaterial,
            OneShot = true,
            Explosiveness = 1f,
            LocalCoords = true,
            Emitting = false,
            Visible = false,
            VisibilityAabb = new AABB(new Vector3(-20, -20, -20), new Vector3(40, 40, 40)),
        };

        _root.AddChild(node);

        return new Emitter
        {
            Node = node,
            ProcessMaterial = processMaterial,
            DrawMaterial = drawMaterial,
            Mesh = mesh,
        };
    }

    private static void Update(float delta)
    {
        for (int i = Active.Count - 1; i >= 0; i--)
        {
            var emitter = Active[i];
            emitter.Remaining -= delta;

            if (emitter.Remaining > 0)
                continue;

            emitter.Node.Emitting = false;
            emitter.Node.Visible = false;

            Active.RemoveAt(i);
            Pool.Push(emitter);
        }
    }

    private static float Rand(float min, float max) =>
        (float)(min + Rng.NextDouble() * (max - min));

    private static int RandInt(int min, int max) => Rng.Next(min, max + 1);
}
