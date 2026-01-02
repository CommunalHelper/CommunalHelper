using Celeste.Mod.Helpers;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/GlowController")]
public class GlowController(EntityData data, Vector2 offset) : Entity(data.Position + offset)
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
        ILCursor cursor = new(il);
        
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldfld, typeof(LightingRenderer).GetField("lights", BindingFlags.NonPublic | BindingFlags.Instance));
        cursor.EmitDelegate(RemoveOrphanedLights);

        return;

        // remove lights that were removed from their entity before vanilla code tries to read lights[i].Entity.Scene and crashes
        static void RemoveOrphanedLights(VertexLight[] lights)
        {
            for (int i = 0; i < Math.Min(lights.Length, LightingRenderer.MaxLights); i++)
            {
                if (lights[i] is not { Entity: null })
                    continue;
            
                lights[i].Index = -1;
                lights[i] = null;
            }
        }
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
        
        VariableDefinition allGlowControllers = new(il.Import(typeof(IEnumerable<GlowController>)));
        il.Body.Variables.Add(allGlowControllers);

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate(GetGlowControllers);
        cursor.Emit(OpCodes.Stloc, allGlowControllers);

        if (!cursor.TryGotoNextBestFit(MoveType.After,
            instr => instr.MatchLdloc(5),
            instr => instr.MatchLdarg(0),
            instr => instr.MatchCallvirt<EntityList>("get_Scene"),
            instr => instr.MatchCallvirt<Entity>("Awake")))
            return;
        
        cursor.Emit(OpCodes.Ldloc, 5);
        cursor.Emit(OpCodes.Ldloc, allGlowControllers);
        cursor.EmitDelegate(ProcessEntity);

        return;
        
        // maybe a bit expensive to be doing every frame
        static IEnumerable<GlowController> GetGlowControllers(EntityList entityList)
            => entityList.Concat(entityList.ToAdd).OfType<GlowController>();

        static void ProcessEntity(Entity entity, IEnumerable<GlowController> glowControllers)
        {
            foreach (GlowController controller in glowControllers)
                controller.Process(entity);
        }
    }
    
    #endregion

    private const StringSplitOptions SplitOptions = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;

    private readonly string[] lightWhitelist = data.Attr("lightWhitelist").Split(',', SplitOptions);
    private readonly string[] lightBlacklist = data.Attr("lightBlacklist").Split(',', SplitOptions);
    private readonly Color lightColor = data.HexColor("lightColor", Color.White);
    private readonly float lightAlpha = data.Float("lightAlpha", 1f);
    private readonly int lightStartFade = data.Int("lightStartFade", 24);
    private readonly int lightEndFade = data.Int("lightEndFade", 48);
    private readonly Vector2 lightOffset = new(data.Int("lightOffsetX"), data.Int("lightOffsetY", -10));

    private readonly string[] bloomWhitelist = data.Attr("bloomWhitelist").Split(',', SplitOptions);
    private readonly string[] bloomBlacklist = data.Attr("bloomBlacklist").Split(',', SplitOptions);
    private readonly float bloomAlpha = data.Float("bloomAlpha", 1f);
    private readonly float bloomRadius = data.Float("bloomRadius", 8f);
    private readonly Vector2 bloomOffset = new(data.Int("bloomOffsetX"), data.Int("bloomOffsetY", -10));

    private readonly string[] deathAnimationIds = data.Attr("deathAnimationIds", "death").Split(',', SplitOptions);
    private readonly string[] respawnAnimationIds = data.Attr("respawnAnimationIds", "respawn").Split(',', SplitOptions);

    private readonly string flag = data.Attr("flag");
    private readonly float flagFadeTime = data.Float("flagFadeTime", 1f);

    private void Process(Entity entity)
    {
        Type type = entity.GetType();
        string typeName = type.FullName;
        bool lightOrBloomAdded = false;

        if (lightBlacklist.Contains(typeName))
            entity.Remove(entity.Components.GetAll<VertexLight>().ToArray<Component>());
        if (lightWhitelist.Contains(typeName))
        {
            entity.Add(new VertexLight(lightOffset, lightColor, lightAlpha, lightStartFade, lightEndFade));
            lightOrBloomAdded = true;
        }

        if (bloomBlacklist.Contains(typeName))
        {
            entity.Remove(entity.Components.GetAll<BloomPoint>().ToArray<Component>());
            entity.Remove(entity.Components.GetAll<CustomBloom>().ToArray<Component>());
        }
        if (bloomWhitelist.Contains(typeName))
        {
            entity.Add(new BloomPoint(bloomOffset, bloomAlpha, bloomRadius));
            lightOrBloomAdded = true;
        }

        if (!lightOrBloomAdded)
            return;

        // list of multiplier enumerators for the alpha of lights and bloom
        List<IEnumerator> multipliers = [];
        
        // if a flag is specified, add a multiplier that will fade the entity's lights and bloom in/out with the flag
        if (!string.IsNullOrEmpty(flag))
            multipliers.Add(FlagFadeMultiplier(entity, flag));
        // some entities get a special multiplier that fades out/in lights and bloom on death/respawn if they have a sprite with an animation id contained in `deathAnimationIds`
        if (entity.Components.GetAll<Sprite>().FirstOrDefault(s => deathAnimationIds.Any(s.Has)) is { } sprite)
            multipliers.Add(DeathFadeMultiplier(entity, sprite));
        
        entity.Add(new Coroutine(AlphaFadeRoutine(entity, multipliers)));
    }

    private static IEnumerator AlphaFadeRoutine(Entity entity, List<IEnumerator> multipliers)
    {
        if (multipliers.Count <= 0)
            yield break;
        
        while (entity.Scene is not null)
        {
            float alpha = 1f;
            foreach (IEnumerator multiplier in multipliers)
            {
                if (multiplier.MoveNext() && multiplier.Current is float m)
                    alpha *= m;
            }
            
            foreach (VertexLight vertexLight in entity.Components.GetAll<VertexLight>())
                vertexLight.Alpha = alpha;
            foreach (BloomPoint bloomPoint in entity.Components.GetAll<BloomPoint>())
                bloomPoint.Alpha = alpha;
            
            yield return null;
        }
    }

    private IEnumerator FlagFadeMultiplier(Entity entity, string flag)
    {
        if (entity.Scene is not Level level)
            yield break;
        
        bool flagValue = level.Session.GetFlag(flag);
        float fade = flagValue ? 1f : 0f;
        yield return fade;

        while (true)
        {
            fade = Calc.Approach(fade, level.Session.GetFlag(flag) ? 1f : 0f, Engine.DeltaTime / flagFadeTime);
            yield return fade;
        }
    }

    private IEnumerator DeathFadeMultiplier(Entity entity, Sprite sprite)
    {
        while (true)
        {
            // wait until the sprite plays a death animation
            while (!deathAnimationIds.Contains(sprite.CurrentAnimationID))
                yield return 1f;

            // fade out over the length of that animation
            if (!sprite.Animations.TryGetValue(sprite.CurrentAnimationID, out Sprite.Animation deathAnimation)) break;
            float fadeTime = deathAnimation.Frames.Length * deathAnimation.Delay;
            float fadeRemaining = fadeTime;

            while (deathAnimationIds.Contains(sprite.CurrentAnimationID) && fadeRemaining > 0)
            {
                fadeRemaining -= Engine.DeltaTime;
                yield return Math.Max(fadeRemaining / fadeTime, 0f);
            }

            // if the sprite has a respawn animation, wait until it's playing it
            while (!respawnAnimationIds.Contains(sprite.CurrentAnimationID))
                yield return 0f;

            // fade in over the length of that animation
            if (!sprite.Animations.TryGetValue(sprite.CurrentAnimationID, out Sprite.Animation respawnAnimation)) break;
            fadeTime = respawnAnimation.Frames.Length * respawnAnimation.Delay;
            fadeRemaining = fadeTime;

            while (respawnAnimationIds.Contains(sprite.CurrentAnimationID) && fadeRemaining > 0)
            {
                fadeRemaining -= Engine.DeltaTime;
                yield return 1f - Math.Max(fadeRemaining / fadeTime, 0f);
            }
        }
    }
}
