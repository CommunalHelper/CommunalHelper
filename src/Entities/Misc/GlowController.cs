using Celeste.Mod.Helpers;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/GlowController")]
public class GlowController : Entity
{
    #region Hooks
    
    public static void Load()
    {
        IL.Celeste.LightingRenderer.BeforeRender += IL_LightingRenderer_BeforeRender;
        IL.Monocle.EntityList.UpdateLists += IL_EntityList_UpdateLists;
    }

    public static void Unload()
    {
        IL.Celeste.LightingRenderer.BeforeRender -= IL_LightingRenderer_BeforeRender;
        IL.Monocle.EntityList.UpdateLists -= IL_EntityList_UpdateLists;
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
                if (lights[i] is not null && lights[i].Entity is null)
                {
                    lights[i].Index = -1;
                    lights[i] = null;
                }
            }
        });
    }
    
    private static void IL_EntityList_UpdateLists(ILContext il)
    {
        ILCursor cursor = new(il);

        if (!cursor.TryGotoNextBestFit(MoveType.Before,
            instr => instr.MatchLdarg(0),
            instr => instr.MatchLdfld<EntityList>("toAwake"),
            instr => instr.MatchCallvirt<List<Entity>>("GetEnumerator"),
            instr => instr.MatchStloc(4)))
            return;
        
        VariableDefinition allGlowControllers = new(il.Import(typeof(GlowController[])));
        il.Body.Variables.Add(allGlowControllers);

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<EntityList, GlowController[]>>(entityList =>
        {
            // not sure whether the tracker will work here so doing this just in case
            IEnumerable<Entity> allEntities = entityList.Concat(entityList.ToAdd);
            return allEntities.Where(entity => entity is GlowController)
                              .Cast<GlowController>()
                              .ToArray();
        });
        cursor.Emit(OpCodes.Stloc, allGlowControllers);

        if (!cursor.TryGotoNextBestFit(MoveType.After,
            instr => instr.MatchLdloc(5),
            instr => instr.MatchLdarg(0),
            instr => instr.MatchCallvirt<EntityList>("get_Scene"),
            instr => instr.MatchCallvirt<Entity>("Awake")))
            return;
        
        cursor.Emit(OpCodes.Ldloc, 5);
        cursor.Emit(OpCodes.Ldloc, allGlowControllers);
        cursor.EmitDelegate<Action<Entity, GlowController[]>>((entity, glowControllers) =>
        {
            foreach (GlowController controller in glowControllers)
                controller.Process(entity);
        });
    }
    
    #endregion

    private readonly string[] lightWhitelist;
    private readonly string[] lightBlacklist;
    private readonly Color lightColor;
    private readonly float lightAlpha;
    private readonly int lightStartFade;
    private readonly int lightEndFade;
    private readonly Vector2 lightOffset;
    // private readonly string targetEntityType;

    private readonly string[] bloomWhitelist;
    private readonly string[] bloomBlacklist;
    private readonly float bloomAlpha;
    private readonly float bloomRadius;
    private readonly Vector2 bloomOffset;

    private readonly string[] deathAnimationIds;
    private readonly string[] respawnAnimationIds;

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

        deathAnimationIds = data.Attr("deathAnimationIds", "death").Split(',');
        respawnAnimationIds = data.Attr("respawnAnimationIds", "respawn").Split(',');
    }

    private void Process(Entity entity)
    {
        var type = entity.GetType();
        var typeName = type.FullName;
        var requiresRemovalRoutine = false;

        if (lightBlacklist.Contains(typeName))
        {
            entity.Remove(entity.Components.GetAll<VertexLight>().ToArray<Component>());
        }
        if (lightWhitelist.Contains(typeName))
        {
            entity.Add(new VertexLight(lightOffset, lightColor, lightAlpha, lightStartFade, lightEndFade));
            requiresRemovalRoutine = true;
        }

        if (bloomBlacklist.Contains(typeName))
        {
            entity.Remove(entity.Components.GetAll<BloomPoint>().ToArray<Component>());
            entity.Remove(entity.Components.GetAll<CustomBloom>().ToArray<Component>());
        }
        if (bloomWhitelist.Contains(typeName))
        {
            entity.Add(new BloomPoint(bloomOffset, bloomAlpha, bloomRadius));
            requiresRemovalRoutine = true;
        }

        // some entities get a special coroutine that hides lights and blooms
        // if it's a glider or otherwise has a sprite with an animation id contained in `deathAnimationIds`
        if (requiresRemovalRoutine &&
            entity.Components.GetAll<Sprite>().FirstOrDefault(s => deathAnimationIds.Any(s.Has)) is { } sprite)
        {
            entity.Add(new Coroutine(DeathRemovalRoutine(entity, sprite)));
        }
    }

    private IEnumerator DeathRemovalRoutine(Entity entity, Sprite sprite)
    {
        void SetAlpha(float alpha)
        {
            foreach (VertexLight vertexLight in entity.Components.GetAll<VertexLight>())
                vertexLight.Alpha = alpha;

            foreach (BloomPoint bloomPoint in entity.Components.GetAll<BloomPoint>())
                bloomPoint.Alpha = alpha;
        }

        if (sprite.Animations.FirstOrDefault(kvp => deathAnimationIds.Contains(kvp.Key)).Value is not { } deathAnimation)
            yield break;

        while (entity.Scene is not null)
        {
            // wait until the sprite plays the death animation
            while (entity.Scene is not null && !deathAnimationIds.Contains(sprite.CurrentAnimationID))
            {
                yield return null;
            }

            // fade out over the length of that animation
            var fadeTime = deathAnimation.Frames.Length * deathAnimation.Delay;
            var fadeRemaining = fadeTime;

            while (entity.Scene is not null && deathAnimationIds.Contains(sprite.CurrentAnimationID) && fadeRemaining > 0)
            {
                fadeRemaining -= Engine.DeltaTime;
                SetAlpha(Math.Max(fadeRemaining / fadeTime, 0f));
                yield return null;
            }
            SetAlpha(0f);

            // if the sprite has a respawn animation, wait until it's playing it
            if (sprite.Animations.FirstOrDefault(kvp => respawnAnimationIds.Contains(kvp.Key)).Value is not { } respawnAnimation) break;
            while (entity.Scene is not null && !respawnAnimationIds.Contains(sprite.CurrentAnimationID))
            {
                yield return null;
            }

            // fade in over the length of that animation
            fadeTime = respawnAnimation.Frames.Length * respawnAnimation.Delay;
            fadeRemaining = fadeTime;

            while (entity.Scene is not null && respawnAnimationIds.Contains(sprite.CurrentAnimationID) && fadeRemaining > 0)
            {
                fadeRemaining -= Engine.DeltaTime;
                SetAlpha(1f - Math.Max(fadeRemaining / fadeTime, 0f));
                yield return null;
            }
            SetAlpha(1f);
        }
    }
}
