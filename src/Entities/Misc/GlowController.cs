using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/GlowController")]
public class GlowController : Entity
{
    public static void Load()
    {
        IL.Celeste.LightingRenderer.BeforeRender += IL_LightingRenderer_BeforeRender;
    }

    public static void Unload()
    {
        IL.Celeste.LightingRenderer.BeforeRender -= IL_LightingRenderer_BeforeRender;
    }

    private static void IL_LightingRenderer_BeforeRender(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldfld, typeof(LightingRenderer).GetField("lights", BindingFlags.NonPublic | BindingFlags.Instance));

        cursor.EmitDelegate<Action<VertexLight[]>>(lights =>
        {
            // remove lights that were removed from their entity before vanilla code tries to read lights[i].Entity.Scene and crashes
            // LightingRenderer.MaxLights == 64
            for (int i = 0; i < 64 && i < lights.Length; i++)
            {
                if (lights[i] != null && lights[i].Entity == null)
                {
                    lights[i].Index = -1;
                    lights[i] = null;
                }
            }
        });
    }

    private readonly string[] lightWhitelist;
    private readonly string[] lightBlacklist;
    private readonly Color lightColor;
    private readonly float lightAlpha;
    private readonly int lightStartFade;
    private readonly int lightEndFade;
    private readonly Vector2 lightOffset;
    private readonly string targetEntityType;

    private readonly string[] bloomWhitelist;
    private readonly string[] bloomBlacklist;
    private readonly float bloomAlpha;
    private readonly float bloomRadius;
    private readonly Vector2 bloomOffset;

    private const string DeathAnimationId = "death";
    private const string RespawnAnimationId = "respawn";

    public GlowController(EntityData data, Vector2 offset)
        : base(data.Position + offset)
    {
        lightWhitelist = data.Attr("lightWhitelist").Split(',');
        lightBlacklist = data.Attr("lightBlacklist").Split(',');
        lightColor = data.HexColor("lightColor", Color.White);
        lightAlpha = data.Float("lightAlpha", 1f);
        lightStartFade = data.Int("lightStartFade", 24);
        lightEndFade = data.Int("lightEndFade", 48);
        lightOffset = new Vector2(data.Int("lightOffsetX"), data.Int("lightOffsetY", -10));

        bloomWhitelist = data.Attr("bloomWhitelist").Split(',');
        bloomBlacklist = data.Attr("bloomBlacklist").Split(',');
        bloomAlpha = data.Float("bloomAlpha", 1f);
        bloomRadius = data.Float("bloomRadius", 8f);
        bloomOffset = new Vector2(data.Int("bloomOffsetX"), data.Int("bloomOffsetY", -10));
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        var allEntities = scene.Entities.Concat(scene.Entities.GetToAdd());
        foreach (var entity in allEntities)
        {
            var type = entity.GetType();
            var typeName = type.FullName;
            var requiresRemovalRoutine = false;

            if (lightWhitelist.Contains(typeName))
            {
                entity.Add(new VertexLight(lightOffset, lightColor, lightAlpha, lightStartFade, lightEndFade));
                requiresRemovalRoutine = true;
            }
            else if (lightBlacklist.Contains(typeName))
            {
                entity.Remove(entity.Components.GetAll<VertexLight>().ToArray<Component>());
            }

            if (bloomWhitelist.Contains(typeName))
            {
                entity.Add(new BloomPoint(bloomOffset, bloomAlpha, bloomRadius));
                requiresRemovalRoutine = true;
            }
            else if (bloomBlacklist.Contains(typeName))
            {
                entity.Remove(entity.Components.GetAll<BloomPoint>().ToArray<Component>());
                entity.Remove(entity.Components.GetAll<CustomBloom>().ToArray<Component>());
            }

            // some entities get a special coroutine that hides lights and blooms
            // if it's a glider or otherwise has a sprite with a "death" animation
            if (requiresRemovalRoutine &&
                entity.Components.GetAll<Sprite>().FirstOrDefault(s => s.Has(DeathAnimationId)) is { } sprite)
            {
                entity.Add(new Coroutine(DeathRemovalRoutine(entity, sprite)));
            }
        }
    }

    private IEnumerator DeathRemovalRoutine(Entity entity, Sprite sprite)
    {
        void SetAlpha(float alpha)
        {
            foreach (VertexLight vertexLight in entity.Components.GetAll<VertexLight>())
            {
                vertexLight.Alpha = alpha;
            }

            foreach (BloomPoint bloomPoint in entity.Components.GetAll<BloomPoint>())
            {
                bloomPoint.Alpha = alpha;
            }
        }

        if (!sprite.Animations.TryGetValue(DeathAnimationId, out var deathAnimation))
        {
            yield break;
        }

        while (entity.Scene != null)
        {
            // wait until the sprite plays the death animation
            while (entity.Scene != null && sprite.CurrentAnimationID != DeathAnimationId)
            {
                yield return null;
            }

            // fade out over the length of that animation
            var fadeTime = deathAnimation.Frames.Length * deathAnimation.Delay;
            var fadeRemaining = fadeTime;

            while (entity.Scene != null && sprite.CurrentAnimationID == DeathAnimationId && fadeRemaining > 0)
            {
                fadeRemaining -= Engine.DeltaTime;
                SetAlpha(Math.Max(fadeRemaining / fadeTime, 0f));
                yield return null;
            }

            // if it's a respawning jelly, wait until the sprite is playing the respawn animation
            if (!sprite.Animations.TryGetValue(RespawnAnimationId, out var respawnAnimation)) break;
            while (entity.Scene != null && sprite.CurrentAnimationID != RespawnAnimationId)
            {
                yield return null;
            }

            // fade in over the length of that animation
            fadeTime = respawnAnimation.Frames.Length * respawnAnimation.Delay;
            fadeRemaining = fadeTime;

            while (entity.Scene != null && sprite.CurrentAnimationID == RespawnAnimationId && fadeRemaining > 0)
            {
                fadeRemaining -= Engine.DeltaTime;
                SetAlpha(1f - Math.Max(fadeRemaining / fadeTime, 0f));
                yield return null;
            }
        }
    }
}
