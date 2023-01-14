using Celeste.Mod.CommunalHelper.DashStates;
using Celeste.Mod.CommunalHelper.Imports;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.Collections.Generic;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper.Entities;

[Tracked]
[CustomEntity("CommunalHelper/ConnectedTempleCrackedBlock")]
public class ConnectedTempleCrackedBlock : ConnectedSolid
{
    protected static MTexture[,] masterInnerCorners = new MTexture[2, 2];
    protected static List<MTexture> atlasSubtextures;

    // Hooks & stuff

    private static FieldInfo Fieldinfo_SeekerRegen_this;
    private static ILHook Seeker_Regen_Hook;

    public static void Load()
    {
        HookSeekerRegen();
        On.Celeste.Seeker.SlammedIntoWall += OnSeekerBonk;
        On.Celeste.Puffer.Explode += OnPufferExplode;
        On.Celeste.PlayerSeeker.OnCollide += OnPlayerSeekerCollide;
    }

    public static void Unload()
    {
        Seeker_Regen_Hook?.Dispose();
        Seeker_Regen_Hook = null;
        On.Celeste.Seeker.SlammedIntoWall -= OnSeekerBonk;
        On.Celeste.Puffer.Explode -= OnPufferExplode;
        On.Celeste.PlayerSeeker.OnCollide -= OnPlayerSeekerCollide;
    }

    private static void HookSeekerRegen()
    {
        MethodInfo minfo = typeof(Seeker).GetMethod("RegenerateCoroutine", BindingFlags.Instance | BindingFlags.NonPublic);
        Fieldinfo_SeekerRegen_this = minfo.GetStateMachineTarget().DeclaringType.GetField("<>4__this", BindingFlags.Public | BindingFlags.Instance);
        Seeker_Regen_Hook = new ILHook(minfo.GetStateMachineTarget(), ILSetupSeekerRegen);
    }

    private static void ILSetupSeekerRegen(ILContext il)
    {
        ILCursor cursor = new(il);
        if (cursor.TryGotoNext(instr => instr.Next?.MatchLdfld<Seeker>("physicsHitbox") ?? false))
        {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, Fieldinfo_SeekerRegen_this);
            cursor.Emit(OpCodes.Call, typeof(ConnectedTempleCrackedBlock).GetMethod("OnSeekerRegen"));
        }
    }

    public static void OnSeekerRegen(Seeker self)
    {
        foreach (ConnectedTempleCrackedBlock entity in self.Scene.Tracker.GetEntities<ConnectedTempleCrackedBlock>())
        {
            if (self.CollideCheck(entity))
            {
                entity.Break(self.Position);
            }
        }
    }

    private static void OnSeekerBonk(On.Celeste.Seeker.orig_SlammedIntoWall orig, Seeker self, CollisionData data)
    {
        foreach (ConnectedTempleCrackedBlock entity in self.Scene.Tracker.GetEntities<ConnectedTempleCrackedBlock>())
        {
            if (self.CollideCheck(entity, self.Position + (Vector2.UnitX * Math.Sign(self.Speed.X))))
            {
                entity.Break(self.Center);
            }
        }
        orig(self, data);
    }

    private static void OnPufferExplode(On.Celeste.Puffer.orig_Explode orig, Puffer self)
    {
        DynamicData dd = new(self);
        Collider collider = self.Collider;
        self.Collider = dd.Get<Collider>("pushRadius");
        foreach (ConnectedTempleCrackedBlock entity in self.Scene.Tracker.GetEntities<ConnectedTempleCrackedBlock>())
        {
            if (self.CollideCheck(entity))
            {
                entity.Break(self.Position);
            }
        }
        self.Collider = collider;
        orig(self);
    }

    private static void OnPlayerSeekerCollide(On.Celeste.PlayerSeeker.orig_OnCollide orig, PlayerSeeker self, CollisionData data)
    {
        orig(self, data);
        DynamicData dd = new(self);
        if (dd.Get<float>("dashTimer") > 0f && data.Hit is ConnectedTempleCrackedBlock)
        {
            Celeste.Freeze(0.15f);
            Input.Rumble(RumbleStrength.Strong, RumbleLength.Long);
            (data.Hit as ConnectedTempleCrackedBlock).Break(self.Position);
        }
    }

    // Actual entity stuff

    private readonly MTexture[,,] texture;
    private bool autoTiled = false;

    private EntityID eid;
    private readonly bool persistent;
    private Tuple<int, int>[,] tiles;
    private float frame;
    private bool broken;
    private readonly int frames;

    public ConnectedTempleCrackedBlock(EntityData data, Vector2 offset)
        : this(new EntityID(data.Level.Name, data.ID), data, offset) { }

    public ConnectedTempleCrackedBlock(EntityID eid, EntityData data, Vector2 offset)
        : this(eid, data.Position + offset, data.Width, data.Height, data.Bool("persistent")) { }

    public ConnectedTempleCrackedBlock(EntityID eid, Vector2 position, int width, int height, bool persistent)
        : base(position, width, height, safe: true)
    {
        const int tilesetW = 7, tilesetH = 6;

        this.eid = eid;
        this.persistent = persistent;
        Collidable = Visible = false;
        int tilesW = (int) (width / 8f);
        int tilesH = (int) (height / 8f);
        List<MTexture> atlasSubtextures = GFX.Game.GetAtlasSubtextures("objects/CommunalHelper/connectedTempleCrackedBlock/breakBlock");
        tiles = new Tuple<int, int>[tilesW, tilesH];
        frames = atlasSubtextures.Count;
        texture = new MTexture[tilesetW, tilesetH, frames];
        for (int tx = 0; tx < tilesetW; tx++)
        {
            for (int ty = 0; ty < tilesetH; ty++)
            {
                for (int k = 0; k < frames; k++)
                {
                    texture[tx, ty, k] = atlasSubtextures[k].GetSubtexture(tx * 8, ty * 8, 8, 8);
                }
            }
        }
        Add(new LightOcclude(0.5f));

        Component explosionCollider = CavernHelper.GetCrystalBombExplosionCollider?.Invoke(Break, null);
        if (explosionCollider != null)
        {
            Add(explosionCollider);
        }

        OnDashCollide = (player, dir) =>
        {
            if (SeekerDash.SeekerAttacking)
            {
                Break(player.Center);
                return DashCollisionResults.Rebound;
            }
            return DashCollisionResults.NormalCollision;
        };
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        if (CollideCheck<Player>())
        {
            if (persistent)
            {
                SceneAs<Level>().Session.DoNotLoad.Add(eid);
            }
            RemoveSelf();
        }
        else
        {
            Collidable = Visible = true;
        }
    }

    public override void Update()
    {
        base.Update();
        if (broken)
        {
            frame += Engine.DeltaTime * 15f;
            if (frame >= frames)
            {
                RemoveSelf();
            }
        }
    }

    public override void Render()
    {
        if (!autoTiled)
        {
            AutoTile(texture);
        }
        int num = (int) frame;
        if (num >= frames)
        {
            return;
        }
        for (int i = 0; i < tiles.GetLength(0); i++)
        {
            for (int j = 0; j < tiles.GetLength(1); j++)
            {
                Tuple<int, int> tile = tiles[i, j];
                if (tile != null)
                {
                    texture[tile.Item1, tile.Item2, num].Draw(GroupBoundsMin + (new Vector2(i, j) * 8f));
                }
            }
        }
    }

    public void Break(Vector2 from)
    {
        DestroyStaticMovers();
        Audio.Play("event:/game/05_mirror_temple/crackedwall_vanish", base.Center);
        if (persistent)
        {
            SceneAs<Level>().Session.DoNotLoad.Add(eid);
        }
        broken = true;
        Collidable = false;
        for (int i = 0; i < tiles.GetLength(0); i++)
        {
            for (int j = 0; j < tiles.GetLength(1); j++)
            {
                if (tiles[i, j] != null)
                {
                    Scene.Add(Engine.Pooler.Create<Debris>().Init(GroupBoundsMin + new Vector2((i * 8) + 4, (j * 8) + 4), '1', playSound: true).BlastFrom(from));
                }
            }
        }
    }

    // Tiling

    private void AutoTile(MTexture[,,] tex)
    {
        int tWidth = (int) ((GroupBoundsMax.X - GroupBoundsMin.X) / 8);
        int tHeight = (int) ((GroupBoundsMax.Y - GroupBoundsMin.Y) / 8);

        Tuple<int, int>[,] res = new Tuple<int, int>[tWidth, tHeight];
        for (int x = 0; x < tWidth; x++)
        {
            for (int y = 0; y < tHeight; y++)
            {
                if (GetGridSafe(GroupTiles, x + 1, y + 1))
                {
                    res[x, y] = GetTile(GroupTiles, x + 1, y + 1, tex);
                }
            }
        }

        tiles = res;
        autoTiled = true;
    }

    private Tuple<int, int> GetTile(bool[,] grid, int x, int y, MTexture[,,] tex)
    {
        bool up = GetGridSafe(grid, x, y - 1), down = GetGridSafe(grid, x, y + 1),
            left = GetGridSafe(grid, x - 1, y), right = GetGridSafe(grid, x + 1, y),
            upleft = GetGridSafe(grid, x - 1, y - 1), upright = GetGridSafe(grid, x + 1, y - 1),
            downleft = GetGridSafe(grid, x - 1, y + 1), downright = GetGridSafe(grid, x + 1, y + 1),
            farUp = GetGridSafe(grid, x, y - 2), farDown = GetGridSafe(grid, x, y + 2),
            farLeft = GetGridSafe(grid, x - 2, y), farRight = GetGridSafe(grid, x + 2, y);

        // Check for inner corners
        if (up && down && left && right)
        {
            if (!upleft)
            {
                if (downright)
                    return new Tuple<int, int>(6, 1);  // inner corner upleft
                return new Tuple<int, int>(6, 4);  // double inner corner ul/dr
            }
            if (!upright)
            {
                if (downleft)
                    return new Tuple<int, int>(6, 0);  // inner corner upright
                return new Tuple<int, int>(6, 5);  // double inner corner ur/dl
            }
            if (!downleft)
                return new Tuple<int, int>(6, 2);  // inner corner downleft
            if (!downright)
                return new Tuple<int, int>(6, 3);  // inner corner downright
        }

        int tx, ty;

        // Get X coord
        if (!left)
            tx = 0;
        else if (!right)
            tx = 5;
        else tx = !farLeft ? 1 : !farRight ? 4 : 2 + (x % 2);

        // Get Y coord
        if (!up)
            ty = 0;
        else if (!down)
            ty = 5;
        else ty = !farUp ? 1 : !farDown ? 4 : 2 + (y % 2);

        return new Tuple<int, int>(tx, ty);
    }

    private bool GetGridSafe(bool[,] grid, int x, int y)
    {
        return grid == null ? false : x < 0 || y < 0 || x >= grid.GetLength(0) || y >= grid.GetLength(1) ? false : grid[x, y];
    }
}
