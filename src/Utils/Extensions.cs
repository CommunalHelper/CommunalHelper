using Celeste.Mod.CommunalHelper.Entities;
using Celeste.Mod.CommunalHelper.Imports;
using Celeste.Mod.Helpers;
using FMOD.Studio;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Utils;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace Celeste.Mod.CommunalHelper;

public static class Extensions
{
    public static DynamicData GetData(this Player player) => DynamicData.For(player);

    public static void FlashHair(this Player player)
        => player.GetData().Set("hairFlashTimer", 0.12f);

    public static void LoseDreamSeeds(this Player player)
    {
        Follower[] followers
            = player.Leader.Followers.Where(f => f.Entity is StrawberrySeed seed && seed.Strawberry is DreamStrawberry)
                                   .ToArray();

        foreach (Follower follower in followers)
            player.Leader.LoseFollower(follower);
    }

    public static Color Mult(this Color color, Color other)
    {
        color.R = (byte) (color.R * other.R / 256f);
        color.G = (byte) (color.G * other.G / 256f);
        color.B = (byte) (color.B * other.B / 256f);
        color.A = (byte) (color.A * other.A / 256f);
        return color;
    }

    public static Color? HexColorNullable(this EntityData data, string key)
    {
        if (data.Values.TryGetValue(key, out object value))
        {
            string text = value.ToString();
            if (text.Length is 6 or 7)
            { // we don't care about the '#' character.
                return Calc.HexToColor(text);
            }
        }
        return null;
    }

    public static Ease.Easer Easer(this EntityData data, string key, Ease.Easer defaultEaser = null)
        => data.Attr(key) switch
        {
            "Linear" => Ease.Linear,
            "SineIn" => Ease.SineIn,
            "SineOut" => Ease.SineOut,
            "SineInOut" => Ease.SineInOut,
            "QuadIn" => Ease.QuadIn,
            "QuadOut" => Ease.QuadOut,
            "QuadInOut" => Ease.QuadInOut,
            "CubeIn" => Ease.CubeIn,
            "CubeOut" => Ease.CubeOut,
            "CubeInOut" => Ease.CubeInOut,
            "QuintIn" => Ease.QuintIn,
            "QuintOut" => Ease.QuintOut,
            "QuintInOut" => Ease.QuintInOut,
            "ExpoIn" => Ease.ExpoIn,
            "ExpoOut" => Ease.ExpoOut,
            "ExpoInOut" => Ease.ExpoInOut,
            "BackIn" => Ease.BackIn,
            "BackOut" => Ease.BackOut,
            "BackInOut" => Ease.BackInOut,
            "BigBackIn" => Ease.BigBackIn,
            "BigBackOut" => Ease.BigBackOut,
            "BigBackInOut" => Ease.BigBackInOut,
            "ElasticIn" => Ease.ElasticIn,
            "ElasticOut" => Ease.ElasticOut,
            "ElasticInOut" => Ease.ElasticInOut,
            "BounceIn" => Ease.BounceIn,
            "BounceOut" => Ease.BounceOut,
            "BounceInOut" => Ease.BounceInOut,
            _ => defaultEaser ?? Ease.Linear,
        };

    public static Vector2 CorrectJoystickPrecision(this Vector2 dir)
    {
        if (dir.X != 0f && Math.Abs(dir.X) < 0.001f)
        {
            dir.X = 0f;
            dir.Y = Math.Sign(dir.Y);
        }
        else if (dir.Y != 0f && Math.Abs(dir.Y) < 0.001f)
        {
            dir.Y = 0f;
            dir.X = Math.Sign(dir.X);
        }
        return dir;
    }

    public static void PutInside(this Vector2 pos, Rectangle bounds)
    {
        while (pos.X < bounds.X)
        {
            pos.X += bounds.Width;
        }
        while (pos.X > bounds.X + bounds.Width)
        {
            pos.X -= bounds.Width;
        }
        while (pos.Y < bounds.Y)
        {
            pos.Y += bounds.Height;
        }
        while (pos.Y > bounds.Y + bounds.Height)
        {
            pos.Y -= bounds.Height;
        }
    }

    public static List<Type> GetSubClasses(this Type type)
    {
        List<Type> list = new();
        foreach (Type type2 in FakeAssembly.GetFakeEntryAssembly().GetTypes())
        {
            if (type != type2 && type.IsAssignableFrom(type2))
            {
                list.Add(type2);
            }
        }
        return list;
    }

    public static List<MethodInfo> GetOverrides(this MethodInfo method, bool returnBase)
    {
        List<MethodInfo> list = new();
        if (returnBase)
            list.Add(method);

        foreach (Type subType in method.DeclaringType.GetSubClasses())
        {
            MethodInfo overrideMethod = subType.GetMethod(method.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
            if (overrideMethod != null && overrideMethod.Attributes.HasFlag(MethodAttributes.Virtual) && overrideMethod.GetBaseDefinition() == method)
                list.Add(overrideMethod);

        }
        return list;
    }

    public static MethodInfo GetMethod(this Type type, string name, BindingFlags bindingAttr, bool throwOnNull = false)
    {
        MethodInfo method = type.GetMethod(name, bindingAttr);
        return throwOnNull && method is null
            ? throw new NullReferenceException($"Could not find method {name} in type {type.FullName}")
            : method;
    }

    public static MethodInfo GetMethod(this Type type, string name, Type[] types, bool throwOnNull = false)
    {
        MethodInfo method = type.GetMethod(name, types);
        return throwOnNull && method is null
            ? throw new NullReferenceException($"Could not find method {name}({string.Join<Type>(",", types)}) in type {type.FullName}.")
            : method;
    }

    public static string GetFullName(this MethodInfo method)
    {
        return $"{method.DeclaringType}.{method.Name}";
    }

    // Dream Tunnel Dash related extension methods located in DreamTunnelDash.cs

    internal static bool CelesteTASLoaded;
    internal static MethodInfo CelesteTAS_PlayerStates_Register;
    internal static MethodInfo CelesteTAS_PlayerStates_Unregister;

    public static void RegisterState(int state, string stateName)
    {
        if (CelesteTASLoaded)
            CelesteTAS_PlayerStates_Register.Invoke(null, new object[] { state, stateName });
    }

    public static void UnregisterState(int state)
    {
        if (CelesteTASLoaded)
            CelesteTAS_PlayerStates_Unregister.Invoke(null, new object[] { state });
    }

    internal static bool MoreDashelineLoaded;
    internal static MethodInfo MoreDasheline_GetHairColor;

    public static Color GetHairColor(this Player player, int dashCount)
    {
        if (MoreDashelineLoaded)
            return (Color) MoreDasheline_GetHairColor.Invoke(null, new object[] { player, dashCount });

        bool isBadeline = player.Sprite.Mode == PlayerSpriteMode.MadelineAsBadeline;
        return dashCount switch
        {
            0 => isBadeline ? Player.UsedBadelineHairColor : Player.UsedHairColor,
            1 => isBadeline ? Player.NormalBadelineHairColor : Player.NormalHairColor,
            _ => isBadeline ? Player.TwoDashesBadelineHairColor : Player.TwoDashesHairColor,
        };
    }

    internal static bool CollabUtilsLoaded;
    private static Type t_CollabUtils_MiniHeart;
    internal static Type CollabUtils_MiniHeart
    {
        get => t_CollabUtils_MiniHeart;
        set
        {
            t_CollabUtils_MiniHeart = value;
            m_FindFirst_MiniHeart = typeof(EntityList).GetMethod("FindFirst").MakeGenericMethod(t_CollabUtils_MiniHeart);
        }
    }
    private static MethodInfo m_FindFirst_MiniHeart;

    public static Entity FindFirst_MiniHeart(this EntityList list)
    {
        return CollabUtilsLoaded ? (Entity) m_FindFirst_MiniHeart.Invoke(list, new object[] { }) : null;
    }

    public static bool SatisfiesDependency(EverestModuleMetadata meta, EverestModuleMetadata dependency)
    {
        return meta.Name == dependency.Name && Everest.Loader.VersionSatisfiesDependency(meta.Version, dependency.Version);
    }

    // Modified version of Everest.Loader.DependencyLoaded
    public static bool TryGetModule(EverestModuleMetadata meta, out EverestModule module)
    {
        foreach (EverestModule other in Everest.Modules)
        {
            if (SatisfiesDependency(meta, other.Metadata))
            {
                module = other;
                return true;
            }
        }

        module = null;
        return false;
    }

    #region Collider

    public static T CollideFirst<T, Exclude>(this Entity from) where T : Entity where Exclude : Entity
    {
        List<Entity> list = from.Scene.Tracker.Entities[typeof(Exclude)];
        foreach (Entity entity in from.Scene.Tracker.Entities[typeof(T)])
        {
            if (!list.Contains(entity) && Collide.Check(from, entity))
            {
                return entity as T;
            }
        }
        return null;
    }

    public static T CollideFirst<T, Exclude>(this Entity from, Vector2 at) where T : Entity where Exclude : Entity
    {
        Vector2 position = from.Position;
        from.Position = at;
        T result = CollideFirst<T, Exclude>(from);
        from.Position = position;
        return result;
    }

    public static bool Contains(this Collider container, Collider contained, float padding = 0)
    {
        return container.AbsoluteLeft - padding < contained.AbsoluteLeft && container.AbsoluteTop - padding < contained.AbsoluteTop &&
            container.AbsoluteRight + padding > contained.AbsoluteRight && container.AbsoluteBottom + padding > contained.AbsoluteBottom;
    }

    // Sort of the inverse of CollideCheckOutside
    public static bool CollideCheckOutsideInside(this Entity self, Entity other, Vector2 at)
    {
        return Collide.Check(self, other) ? !Collide.Check(self, other, at) : false;
    }

    #endregion

    #region WallBoosters

    public static bool AttachedWallBoosterCheck(this Player player)
    {
        foreach (AttachedWallBooster wallbooster in player.Scene.Tracker.GetEntities<AttachedWallBooster>())
        {
            if (player.Facing == wallbooster.Facing && player.CollideCheck(wallbooster))
                return true;
        }
        return false;
    }

    #endregion

    public static void PointBounce(this Player player, Vector2 from, float force)
    {
        if (player.StateMachine.State == 2)
        {
            player.StateMachine.State = 0;
        }
        if (player.StateMachine.State == 4 && player.CurrentBooster != null)
        {
            player.CurrentBooster.PlayerReleased();
        }
        player.RefillDash();
        player.RefillStamina();
        Vector2 vector = (player.Center - from).SafeNormalize();
        if (vector.Y is > (-0.2f) and <= 0.4f)
        {
            vector.Y = -0.2f;
        }
        player.Speed = vector * force;
        player.Speed.X *= 1.5f;
        if (Math.Abs(player.Speed.X) < 100f)
        {
            if (player.Speed.X == 0f)
            {
                player.Speed.X = -(float) player.Facing * 100f;
                return;
            }
            player.Speed.X = Math.Sign(player.Speed.X) * 100f;
        }
    }

    public static void PointBounce(this Holdable holdable, Vector2 from)
    {
        Vector2 vector = (holdable.Entity.Center - from).SafeNormalize();
        if (vector.Y is > (-0.2f) and <= 0.4f)
        {
            vector.Y = -0.2f;
        }
        holdable.Release(vector);
    }

    public static void SetBoosterFacing(this Player player, Vector2 dir, int slices = 16)
    {
        player.Speed = dir;
        player.DashDir = dir.Snapped(slices);
        if (player.DashDir.X != 0.0f)
            player.Facing = (Facings) Math.Sign(player.DashDir.X);
    }

    #region EnumExtensions

    public static StationBlockTrack.TrackSwitchState Invert(this StationBlockTrack.TrackSwitchState state)
    {
        return state switch
        {
            StationBlockTrack.TrackSwitchState.On => StationBlockTrack.TrackSwitchState.Off,
            StationBlockTrack.TrackSwitchState.Off => StationBlockTrack.TrackSwitchState.On,
            _ => throw new NotImplementedException(),
        };
    }

    /// <summary>
    /// Returns the angle corresponding to the appropriate <see cref="MoveBlock.Directions"/> as defined in the <see cref="MoveBlock"/> constructor.
    /// </summary>
    /// <param name="dir"></param>
    /// <returns>Angle in Radians</returns>
    public static float Angle(this MoveBlock.Directions dir)
    {
        return dir switch
        {
            MoveBlock.Directions.Left => (float) Math.PI,
            MoveBlock.Directions.Up => -(float) Math.PI / 2f,
            MoveBlock.Directions.Down => (float) Math.PI / 2f,
            _ => 0f
        };
    }

    /// <summary>
    /// Returns the <see cref="Vector2"/> corresponding to the <see cref="MoveBlock.Directions"/>. Defaults to <see cref="Vector2.UnitX"/>.
    /// </summary>
    /// <param name="dir"></param>
    /// <param name="factor">Factor to multiply the resulting vector by.</param>
    /// <returns></returns>
    public static Vector2 Vector(this MoveBlock.Directions dir, float factor = 1f)
    {
        Vector2 result = dir switch
        {
            MoveBlock.Directions.Up => -Vector2.UnitY,
            MoveBlock.Directions.Down => Vector2.UnitY,
            MoveBlock.Directions.Left => -Vector2.UnitX,
            _ => Vector2.UnitX
        };

        return result * factor;
    }

    /// <summary>
    /// Perform the specified <see cref="MoveBlockRedirect.Operations"/> on a <paramref name="value"/> with the second argument of <paramref name="modifier"/>. Defaults to no-op.
    /// </summary>
    /// <param name="op"></param>
    /// <param name="value"></param>
    /// <param name="modifier"></param>
    /// <returns></returns>
    public static float ApplyTo(this MoveBlockRedirect.Operations op, float value, float modifier)
    {
        return op switch
        {
            MoveBlockRedirect.Operations.Add => value + modifier,
            MoveBlockRedirect.Operations.Subtract => value - modifier,
            MoveBlockRedirect.Operations.Multiply => value * modifier,
            _ => value
        };
    }

    #endregion

    private static readonly MethodInfo m_TagLists_EntityAdded = typeof(TagLists).GetMethod("EntityAdded", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo m_Tracker_EntityAdded = typeof(Tracker).GetMethod("EntityAdded", BindingFlags.NonPublic | BindingFlags.Instance);

    public static void ForceAdd(this EntityList list, params Entity[] entities)
    {
        DynamicData listData = DynamicData.For(list);
        HashSet<Entity> current = listData.Get<HashSet<Entity>>("current");
        List<Entity> listEntities = listData.Get<List<Entity>>("entities");
        Scene scene = list.Scene;

        foreach (Entity entity in entities)
        {
            if (!current.Contains(entity))
            {
                current.Add(entity);
                listEntities.Add(entity);
                if (scene != null)
                {
                    m_TagLists_EntityAdded.Invoke(scene.TagLists, new object[] { entity });
                    m_Tracker_EntityAdded.Invoke(scene.Tracker, new object[] { entity });
                    entity.Added(scene);
                }
            }
        }

        listEntities.Sort(EntityList.CompareDepth);

        foreach (Entity entity in entities)
        {
            if (entity.Scene == scene)
                entity.Awake(scene);
        }
    }


    #region JaThePlayer's state machine extension code

    /// <summary>
    /// Adds a state to a StateMachine
    /// </summary>
    /// <returns>The index of the new state</returns>
    public static int AddState(this StateMachine machine, Func<int> onUpdate, Func<IEnumerator> coroutine = null, Action begin = null, Action end = null)
    {
        Action[] begins = (Action[]) StateMachine_begins.GetValue(machine);
        Func<int>[] updates = (Func<int>[]) StateMachine_updates.GetValue(machine);
        Action[] ends = (Action[]) StateMachine_ends.GetValue(machine);
        Func<IEnumerator>[] coroutines = (Func<IEnumerator>[]) StateMachine_coroutines.GetValue(machine);
        int nextIndex = begins.Length;
        // Now let's expand the arrays
        Array.Resize(ref begins, begins.Length + 1);
        Array.Resize(ref updates, begins.Length + 1);
        Array.Resize(ref ends, begins.Length + 1);
        Array.Resize(ref coroutines, coroutines.Length + 1);
        // Store the resized arrays back into the machine
        StateMachine_begins.SetValue(machine, begins);
        StateMachine_updates.SetValue(machine, updates);
        StateMachine_ends.SetValue(machine, ends);
        StateMachine_coroutines.SetValue(machine, coroutines);
        // And now we add the new functions
        machine.SetCallbacks(nextIndex, onUpdate, coroutine, begin, end);
        return nextIndex;
    }
    private static readonly FieldInfo StateMachine_begins = typeof(StateMachine).GetField("begins", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo StateMachine_updates = typeof(StateMachine).GetField("updates", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo StateMachine_ends = typeof(StateMachine).GetField("ends", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo StateMachine_coroutines = typeof(StateMachine).GetField("coroutines", BindingFlags.Instance | BindingFlags.NonPublic);

    #endregion

    public static Vector2 PutInside(this Entity entity, Vector2 pos)
    {
        while (pos.X < entity.X)
        {
            pos.X += entity.Width;
        }
        while (pos.X > entity.X + entity.Width)
        {
            pos.X -= entity.Width;
        }
        while (pos.Y < entity.Y)
        {
            pos.Y += entity.Height;
        }
        while (pos.Y > entity.Y + entity.Height)
        {
            pos.Y -= entity.Height;
        }
        return pos;
    }

    public static void DrawOutlineOnlyCentered(this MTexture mTexture, Vector2 position, Vector2 scale, float rotation)
    {
        float scaleFix = mTexture.ScaleFix;
        scale *= scaleFix;
        Rectangle clipRect = mTexture.ClipRect;
        Vector2 origin = (mTexture.Center - mTexture.DrawOffset) / scaleFix;
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i != 0 || j != 0)
                {
                    Draw.SpriteBatch.Draw(mTexture.Texture.Texture_Safe, position + new Vector2(i, j), clipRect, Color.Black, rotation, origin, scale, SpriteEffects.None, 0f);
                }
            }
        }
    }

    public static Rectangle GetBounds(this Camera camera)
    {
        int top = (int) camera.Top;
        int bottom = (int) camera.Bottom;
        int left = (int) camera.Left;
        int right = (int) camera.Right;

        return new(left, top, right - left, bottom - top);
    }

    public static TextMenuExt.OptionSubMenu Add(this TextMenuExt.OptionSubMenu menu, string label, params TextMenu.Item[] items)
    {
        return menu.Add(label, new List<TextMenu.Item>(items));
    }

    #region Loading custom packed Atlases

    public static Atlas CreateEmptyAtlas()
    {
        return new() { Sources = new() };
    }

    public static Atlas LoadAtlasFromMod(string path, Atlas.AtlasDataFormat format)
    {
        Atlas atlas = CreateEmptyAtlas();

        if (Everest.Content.Map.TryGetValue(path, out ModAsset asset))
        {
            string directory = Path.GetDirectoryName(path);

            switch (format)
            {
                case Atlas.AtlasDataFormat.CrunchXml:
                {
                    XmlDocument xml = new();
                    xml.Load(asset.Stream);
                    atlas.LoadXmlData(xml, directory);
                    break;
                }

                default:
                    throw new NotImplementedException($"Loading {format} atlases from mods is not implemented.");
            }
        }
        else
            throw new($"Could not find mod asset with path '{path}'!");

        return atlas;
    }

    /// <summary>
    /// Loads atlas data from an XML document into a given empty atlas.
    /// <para/> See <see href="https://github.com/ChevyRay/crunch"/>.
    /// </summary>
    /// <param name="atlas">The empty atlas into which textures are put.</param>
    /// <param name="xml">The XML document containing the altas data.</param>
    /// <param name="directory">The directory used to look up images references in the XML.</param>
    public static void LoadXmlData(this Atlas atlas, XmlDocument xml, string directory)
    {
        foreach (XmlElement tex in xml["atlas"])
        {
            string sourcePath = Path.Combine(directory, tex.GetAttribute("n")).Replace('\\', '/');

            if (Everest.Content.Map.TryGetValue(sourcePath, out ModAsset asset))
            {
                VirtualTexture virtualTexture = VirtualContent.CreateTexture(asset);
                MTexture source = new(virtualTexture);
                source.SetAtlas(atlas);
                atlas.Sources.Add(virtualTexture);

                foreach (XmlElement img in tex)
                {
                    string name = img.Attr("n");
                    int x = img.AttrInt("x");
                    int y = img.AttrInt("y");
                    int w = img.AttrInt("w");
                    int h = img.AttrInt("h");
                    Rectangle rect = new(x, y, w, h);

                    if (img.HasAttr("fx"))
                    {
                        int fx = img.AttrInt("fx");
                        int fy = img.AttrInt("fy");
                        int fw = img.AttrInt("fw");
                        int fh = img.AttrInt("fh");
                        atlas.Textures[name] = new(source, name, rect, new Vector2(-fx, -fy), fw, fh);
                    }
                    else
                        atlas.Textures[name] = new(source, name, rect);
                }
            }
            else
                throw new($"Could not find atlas source image at '{sourcePath}'!");
        }
    }

    #endregion

    #region GravityHelper utilities

    public static GravityType GetGravity(this Actor actor)
    {
        return (GravityType) (GravityHelper.GetActorGravity?.Invoke(actor) ?? 0);
    }

    public static bool IsInverted(this Actor actor)
    {
        return GravityHelper.IsActorInverted?.Invoke(actor) ?? false;
    }

    #endregion

    #region 3D-arrays

    public static T[,,] RotateXCounterclockwise<T>(this T[,,] array)
    {
        int sz = array.GetLength(0), sy = array.GetLength(1), sx = array.GetLength(2);
        T[,,] result = new T[sy, sz, sx];
        for (int z = 0; z < sz; z++)
            for (int y = 0; y < sy; y++)
                for (int x = 0; x < sx; x++)
                    result[y, z, x] = array[z, sy - y - 1, x];
        return result;
    }

    public static T[,,] RotateXClockwise<T>(this T[,,] array)
    {
        int sz = array.GetLength(0), sy = array.GetLength(1), sx = array.GetLength(2);
        T[,,] result = new T[sy, sz, sx];
        for (int z = 0; z < sz; z++)
            for (int y = 0; y < sy; y++)
                for (int x = 0; x < sx; x++)
                    result[y, z, x] = array[sz - z - 1, y, x];
        return result;
    }

    public static T[,,] MirrorAboutX<T>(this T[,,] array)
    {
        int sz = array.GetLength(0), sy = array.GetLength(1), sx = array.GetLength(2);
        T[,,] result = new T[sz, sy, sx];
        for (int z = 0; z < sz; z++)
            for (int y = 0; y < sy; y++)
                for (int x = 0; x < sx; x++)
                    result[z, y, x] = array[sz - z - 1, sy - y - 1, x];
        return result;
    }

    public static T[,,] RotateYCounterclockwise<T>(this T[,,] array)
    {
        int sz = array.GetLength(0), sy = array.GetLength(1), sx = array.GetLength(2);
        T[,,] result = new T[sx, sy, sz];
        for (int z = 0; z < sz; z++)
            for (int y = 0; y < sy; y++)
                for (int x = 0; x < sx; x++)
                    result[x, y, z] = array[z, y, sx - x - 1];
        return result;
    }

    public static T[,,] RotateYClockwise<T>(this T[,,] array)
    {
        int sz = array.GetLength(0), sy = array.GetLength(1), sx = array.GetLength(2);
        T[,,] result = new T[sx, sy, sz];
        for (int z = 0; z < sz; z++)
            for (int y = 0; y < sy; y++)
                for (int x = 0; x < sx; x++)
                    result[x, y, z] = array[sz - z - 1, y, x];
        return result;
    }

    public static T[,,] MirrorAboutY<T>(this T[,,] array)
    {
        int sz = array.GetLength(0), sy = array.GetLength(1), sx = array.GetLength(2);
        T[,,] result = new T[sz, sy, sx];
        for (int z = 0; z < sz; z++)
            for (int y = 0; y < sy; y++)
                for (int x = 0; x < sx; x++)
                    result[z, y, x] = array[sz - z - 1, y, sx - x - 1];
        return result;
    }

    public static T[,,] RotateZCounterclockwise<T>(this T[,,] array)
    {
        int sz = array.GetLength(0), sy = array.GetLength(1), sx = array.GetLength(2);
        T[,,] result = new T[sz, sx, sy];
        for (int z = 0; z < sz; z++)
            for (int y = 0; y < sy; y++)
                for (int x = 0; x < sx; x++)
                    result[z, x, y] = array[z, sy - y - 1, x];
        return result;
    }

    public static T[,,] RotateZClockwise<T>(this T[,,] array)
    {
        int sz = array.GetLength(0), sy = array.GetLength(1), sx = array.GetLength(2);
        T[,,] result = new T[sz, sx, sy];
        for (int z = 0; z < sz; z++)
            for (int y = 0; y < sy; y++)
                for (int x = 0; x < sx; x++)
                    result[z, x, y] = array[z, y, sx - x - 1];
        return result;
    }

    public static T[,,] MirrorAboutZ<T>(this T[,,] array)
    {
        int sz = array.GetLength(0), sy = array.GetLength(1), sx = array.GetLength(2);
        T[,,] result = new T[sz, sy, sx];
        for (int z = 0; z < sz; z++)
            for (int y = 0; y < sy; y++)
                for (int x = 0; x < sx; x++)
                    result[z, y, x] = array[z, sy - y - 1, sx - x - 1];
        return result;
    }

    #endregion

    public static XmlDocument LoadXML(this ModAsset asset)
    {
        XmlDocument xml = new();
        xml.Load(asset.Stream);
        return xml;
    }

    public static Vector2 XY(this Vector3 v) => new(v.X, v.Y);
    
    public static Vector3 Sign(this Vector3 v)
        => new(Math.Sign(v.X), Math.Sign(v.Y), Math.Sign(v.Z));

    public static Vector3 Abs(this Vector3 v)
        => new(Math.Abs(v.X), Math.Abs(v.Y), Math.Abs(v.Z));

    public static Vector3 Floor(this Vector3 v)
        => new((float) Math.Floor(v.X), (float) Math.Floor(v.Y), (float) Math.Floor(v.Z));

    public static void Rewind(this SoundSource sfx)
        => DynamicData.For(sfx)
                      .Get<EventInstance>("instance")
                      .setTimelinePosition(0);
}
