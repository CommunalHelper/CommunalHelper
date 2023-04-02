using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Triggers.StrawberryJam;

[CustomEntity("CommunalHelper/SJ/ShowHitboxTrigger")]
[Tracked]
public class ShowHitboxTrigger : Trigger
{
    public static readonly HashSet<string> EnabledTypeNames = new();

    public HashSet<string> TypeNames { get; }

    public ShowHitboxTrigger(EntityData data, Vector2 offset) : base(data, offset)
    {
        TypeNames = new HashSet<string>(data.Attr("typeNames").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(str => str.Trim()));
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        RefreshEnabledTypeNames();
    }

    public override void SceneEnd(Scene scene)
    {
        base.SceneEnd(scene);
        EnabledTypeNames.Clear();
    }

    public override void OnEnter(Player player)
    {
        base.OnEnter(player);
        RefreshEnabledTypeNames();
    }

    public override void OnLeave(Player player)
    {
        base.OnLeave(player);
        RefreshEnabledTypeNames();
    }

    private static void RefreshEnabledTypeNames()
    {
        EnabledTypeNames.Clear();
        foreach (ShowHitboxTrigger trigger in Engine.Scene.Tracker.GetEntities<ShowHitboxTrigger>().Cast<ShowHitboxTrigger>())
        {
            if (trigger.PlayerIsInside)
            {
                EnabledTypeNames.UnionWith(trigger.TypeNames);
            }
        }
    }

    public static void Load()
    {
        IL.Celeste.GameplayRenderer.Render += PatchGameplayRendererRender;
        On.Celeste.SoundSource.DebugRender += SoundSourceOnDebugRender;
        // below are hitbox fixes from CelesteTAS, maybe they will get integrated into Everest
        On.Monocle.Draw.HollowRect_float_float_float_float_Color += ModDrawHollowRect;
        On.Monocle.Draw.Circle_Vector2_float_Color_int += ModDrawCircle;
    }

    public static void Unload()
    {
        IL.Celeste.GameplayRenderer.Render -= PatchGameplayRendererRender;
        On.Celeste.SoundSource.DebugRender -= SoundSourceOnDebugRender;
        On.Monocle.Draw.HollowRect_float_float_float_float_Color -= ModDrawHollowRect;
        On.Monocle.Draw.Circle_Vector2_float_Color_int -= ModDrawCircle;
    }

    private static void PatchGameplayRendererRender(ILContext il)
    {
        ILCursor cursor = new(il);
        if (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCall<GameplayRenderer>("End")))
        {
            /*
                change
                    if (GameplayRenderer.RenderDebug || Engine.Commands.Open) {
                        scene.Entities.DebugRender(this.Camera);
                    }
                to
                    if (GameplayRenderer.RenderDebug || Engine.Commands.Open) {
                        scene.Entities.DebugRender(this.Camera);
                    } else {
                        DrawEnabledHitboxes();
                    }
            */

            ILLabel beforeGameplayRendererEnd = cursor.DefineLabel();
            cursor.Emit(OpCodes.Br, beforeGameplayRendererEnd);

            cursor.MoveAfterLabels();
            cursor.Emit(OpCodes.Ldarg_0); // self
            cursor.Emit(OpCodes.Ldarg_1); // scene
            cursor.EmitDelegate<Action<GameplayRenderer, Scene>>((self, scene) =>
            {
                if (EnabledTypeNames.Count == 0)
                {
                    return;
                }

                foreach (Entity entity in scene.Entities)
                {
                    if (EnabledTypeNames.Contains(entity.GetType().FullName) || EnabledTypeNames.Contains(entity.GetType().Name))
                    {
                        entity.DebugRender(self.Camera);
                    }
                }
            });
            cursor.MarkLabel(beforeGameplayRendererEnd);
        }
    }

    private static void SoundSourceOnDebugRender(On.Celeste.SoundSource.orig_DebugRender orig, SoundSource self, Camera camera)
    {
        if (EnabledTypeNames.Count == 0 || Engine.Commands.Open)
        {
            orig(self, camera);
        }
    }

    private static void ModDrawHollowRect(On.Monocle.Draw.orig_HollowRect_float_float_float_float_Color orig, float x, float y, float width, float height, Color color)
    {
        if (EnabledTypeNames.Count == 0)
        {
            orig(x, y, width, height, color);
            return;
        }

        float fx = (float) Math.Floor(x);
        float fy = (float) Math.Floor(y);
        float cw = (float) Math.Ceiling(width + x - fx);
        float cy = (float) Math.Ceiling(height + y - fy);
        orig(fx, fy, cw, cy, color);
    }

    private static void ModDrawCircle(On.Monocle.Draw.orig_Circle_Vector2_float_Color_int orig, Vector2 center, float radius, Color color, int resolution)
    {
        // Adapted from John Kennedy, "A Fast Bresenham Type Algorithm For Drawing Circles"
        // https://web.engr.oregonstate.edu/~sllu/bcircle.pdf
        // Not as fast though because we are forced to use floating point arithmetic anyway
        // since the center and radius aren't necessarily integral.
        // For similar reasons, we can't just assume the circle has 8-fold symmetry.
        // Modified so that instead of minimizing error, we include exactly those pixels which intersect the circle.

        if (EnabledTypeNames.Count == 0)
        {
            orig(center, radius, color, resolution);
            return;
        }

        CircleOctant(center, radius, color, 1, 1, false);
        CircleOctant(center, radius, color, 1, -1, false);
        CircleOctant(center, radius, color, -1, 1, false);
        CircleOctant(center, radius, color, -1, -1, false);
        CircleOctant(center, radius, color, 1, 1, true);
        CircleOctant(center, radius, color, 1, -1, true);
        CircleOctant(center, radius, color, -1, 1, true);
        CircleOctant(center, radius, color, -1, -1, true);
    }

    private static void CircleOctant(Vector2 center, float radius, Color color, float flipX, float flipY, bool interchangeXy)
    {
        // when flipX = flipY = 1 and interchangeXY = false, we are drawing the [0, pi/4] octant.

        float cx, cy;
        if (interchangeXy)
        {
            cx = center.Y;
            cy = center.X;
        }
        else
        {
            cx = center.X;
            cy = center.Y;
        }

        float x, y;
        if (flipX > 0)
        {
            x = (float) Math.Ceiling(cx + radius - 1);
        }
        else
        {
            x = (float) Math.Floor(cx - radius + 1);
        }

        if (flipY > 0)
        {
            y = (float) Math.Floor(cy);
        }
        else
        {
            y = (float) Math.Ceiling(cy);
        }

        float startY = y;
        float e = (x - cx) * (x - cx) + (y - cy) * (y - cy) - radius * radius;
        float yc = flipY * 2 * (y - cy) + 1;
        float xc = flipX * -2 * (x - cx) + 1;
        while (flipY * (y - cy) <= flipX * (x - cx))
        {
            // Slower than using DrawLine, but more obviously correct:
            // DrawPoint((int)x + (flipX < 0 ? -1 : 0), (int)y + (flipY < 0 ? -1 : 0), interchangeXY, color);
            e += yc;
            y += flipY;
            yc += 2;
            if (e >= 0)
            {
                // We would have a 1px correction for flipY here (as we do for flipX) except for
                // the fact that our lines always include the top pixel and exclude the bottom one.
                // Because of this we would have to make two corrections which cancel each other out,
                // so we just don't do either of them.
                DrawLine((int) x + (flipX < 0 ? -1 : 0), (int) startY, (int) y, interchangeXy, color);
                startY = y;
                e += xc;
                x -= flipX;
                xc += 2;
            }
        }

        DrawLine((int) x + (flipX < 0 ? -1 : 0), (int) startY, (int) y, interchangeXy, color);
    }

    private static void DrawLine(int x, int y0, int y1, bool interchangeXy, Color color)
    {
        // x, y0, and y1 must all be integers
        int length = y1 - y0;
        Rectangle rect;
        if (interchangeXy)
        {
            rect.X = y0;
            rect.Y = x;
            rect.Width = length;
            rect.Height = 1;
        }
        else
        {
            rect.X = x;
            rect.Y = y0;
            rect.Width = 1;
            rect.Height = length;
        }

        Draw.SpriteBatch.Draw(Draw.Pixel.Texture.Texture, rect, Draw.Pixel.ClipRect, color);
    }
}
