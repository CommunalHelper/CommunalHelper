using Celeste.Mod.CommunalHelper.Entities;
using MonoMod.RuntimeDetour;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Components;

public class DreamSprite : Sprite {
    public readonly Rectangle ParticleBounds;
    public struct DreamParticle
    {
        public Vector2 Position;
        public int Layer;
        public Color EnabledColor, DisabledColor;
        public float TimeOffset;
    }
    public DreamParticle[] Particles;

    public static readonly Color EnabledLineColor = Color.White;
    public static readonly Color DisabledLineColor = Color.White;
    public static readonly Color EnabledBackColor = Color.Black;
    public static readonly Color DisabledBackColor = Color.Black;
    public static Color[] ParticleColors => CustomDreamBlock.DreamColors;

    public float Flash = 0f;
    private const float FlashTime = 0.4f;
    
    public bool Enabled = true;

    public delegate void SpriteInvertedGravityHandler(ref Vector2 position, ref Vector2 scale, ref float rotation);
    public SpriteInvertedGravityHandler InvertedGravityHandler;

    public DreamSprite(Sprite from, Rectangle particleBounds, SpriteInvertedGravityHandler invertedGravityHandler = null) : base() {
        from.CloneInto(this);

        ParticleBounds = particleBounds;
        InvertedGravityHandler = invertedGravityHandler;
        Flash = 0f;
        Enabled = true;
        
        Particles = new DreamParticle[(int)((ParticleBounds.Width / 8f) * (ParticleBounds.Height / 8f) * 0.7f)];
        for (int i = 0; i < Particles.Length; i++) {
            Particles[i].Position = new Vector2(Calc.Random.NextFloat(ParticleBounds.Width), Calc.Random.NextFloat(ParticleBounds.Height));
            Particles[i].Layer = Calc.Random.Choose(0, 1, 1, 2, 2, 2);
            Particles[i].TimeOffset = Calc.Random.NextFloat();

            Particles[i].DisabledColor = Color.LightGray * (0.5f + (Particles[i].Layer / 2f * 0.5f));
            Particles[i].DisabledColor.A = 255;

            Particles[i].EnabledColor = Particles[i].Layer switch
            {
                0 => Calc.Random.Choose(ParticleColors[0], ParticleColors[1], ParticleColors[2]),
                1 => Calc.Random.Choose(ParticleColors[3], ParticleColors[4], ParticleColors[5]),
                2 => Calc.Random.Choose(ParticleColors[6], ParticleColors[7], ParticleColors[8]),
                _ => throw new NotImplementedException()
            };
        }
    }

    private void TrackSelf() => DreamSpriteRenderer.GetDreamSpriteRenderer(Scene, Entity.Depth + 1).Track(this);
    private void UntrackSelf() => DreamSpriteRenderer.GetDreamSpriteRenderer(Scene, Entity.Depth + 1).Untrack(this);

    public override void EntityAdded(Scene scene)
    {
        base.EntityAdded(scene);
        TrackSelf();
    }

    public override void Removed(Entity entity)
    {
        UntrackSelf();
        base.Removed(entity);
    }

    public override void EntityRemoved(Scene scene)
    {
        UntrackSelf();
        base.EntityRemoved(scene);
    }

    public override void Update()
    {
        base.Update();

        Flash = Calc.Approach(Calc.Clamp(Flash, 0f, 1f), 0f, Engine.DeltaTime / FlashTime);
    }

    public override void Render() { }
    
    #region Hooks

    private static Hook hook_Entity_set_Depth;
    
    internal static void Load()
    {
        hook_Entity_set_Depth = new Hook(typeof(Entity).GetMethod("set_Depth", BindingFlags.Instance | BindingFlags.Public)!, Entity_set_Depth);
    }

    internal static void Unload()
    {
        hook_Entity_set_Depth.Dispose();
        hook_Entity_set_Depth = null;
    }

    private static void Entity_set_Depth(Action<Entity, int> orig, Entity self, int value)
    {
        if (self.Scene is null)
        {
            orig(self, value);
            return;
        }
        
        DreamSprite[] sprites = self.Components.GetAll<DreamSprite>().ToArray();
        foreach (DreamSprite sprite in sprites)
        {
            sprite.UntrackSelf();
        }
        
        orig(self, value);
        
        foreach (DreamSprite sprite in sprites)
        {
            sprite.TrackSelf();
        }
    }
    
    #endregion
}
