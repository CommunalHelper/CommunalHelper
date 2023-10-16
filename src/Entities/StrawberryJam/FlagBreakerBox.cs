using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CommunalHelper.Entities.StrawberryJam;

[CustomEntity("CommunalHelper/SJ/FlagBreakerBox")]
class FlagBreakerBox : Solid
{
    private Sprite sprite;
    private SineWave sine;
    private Vector2 start;
    private float sink;
    private int health;
    private string flag; //the flag to control with the breaker box
    private bool aliveState; //the state that the flag should be set to when the box is made
    private float shakeCounter;
    private string music;
    private int musicProgress;
    private bool musicStoreInSession;
    private Vector2 bounceDir;
    private Wiggler bounce;
    private Shaker shaker;
    private bool makeSparks;
    private bool smashParticles;
    private SoundSource firstHitSfx;
    private bool spikesLeft;
    private bool spikesRight;
    private bool spikesUp;
    private bool spikesDown;

    public FlagBreakerBox(Vector2 position, bool flipX)
        : base(position, 32f, 32f, true)
    {
        health = 2;
        musicProgress = -1;
        SurfaceSoundIndex = 9;
        start = Position;

        if (GFX.SpriteBank.Has("flagBreakerBox"))
            this.sprite = GFX.SpriteBank.Create("flagBreakerBox");
        else
            this.sprite = GFX.SpriteBank.Create("breakerBox");
        Sprite sprite = this.sprite;
        sprite.OnLastFrame = (Action<string>) Delegate.Combine(sprite.OnLastFrame, new Action<string>(delegate (string anim) {
            if (anim == "break")
            {
                Visible = false;
                return;
            }
            if (anim == "open")
                makeSparks = false;
        }));
        sprite.Position = new Vector2(Width, Height) / 2f;
        sprite.FlipX = flipX;
        Add(sprite);
        sine = new SineWave(0.5f, 0f);
        Add(sine);
        bounce = Wiggler.Create(1f, 0.5f, null, false, false);
        bounce.StartZero = false;
        Add(bounce);
        Add(shaker = new Shaker(false, null));
        OnDashCollide = new DashCollision(Dashed);
    }

    public FlagBreakerBox(EntityData e, Vector2 levelOffset)
        : this(e.Position + levelOffset, e.Bool("flipX", false))
    {
        flag = e.Attr("flag");
        music = e.Attr("music", null);
        musicProgress = e.Int("music_progress", -1);
        musicStoreInSession = e.Bool("music_session", false);
        aliveState = e.Bool("aliveState", true);
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        if (!string.IsNullOrEmpty(flag))
            SceneAs<Level>().Session.SetFlag(flag, aliveState); //set the flag to default state on loading
        spikesUp = CollideCheck<Spikes>(Position - Vector2.UnitY);
        spikesDown = CollideCheck<Spikes>(Position + Vector2.UnitY);
        spikesLeft = CollideCheck<Spikes>(Position - Vector2.UnitX);
        spikesRight = CollideCheck<Spikes>(Position + Vector2.UnitX);
    }

    public DashCollisionResults Dashed(Player player, Vector2 dir)
    {
        if (!SaveData.Instance.Assists.Invincible)
        {
            if (dir == Vector2.UnitX && spikesLeft)
                return DashCollisionResults.NormalCollision;
            if (dir == -Vector2.UnitX && spikesRight)
                return DashCollisionResults.NormalCollision;
            if (dir == Vector2.UnitY && spikesUp)
                return DashCollisionResults.NormalCollision;
            if (dir == -Vector2.UnitY && spikesDown)
                return DashCollisionResults.NormalCollision;
        }

        SceneAs<Level>().DirectionalShake(dir, 0.3f);
        sprite.Scale = new Vector2(1f + Math.Abs(dir.Y) * 0.4f - Math.Abs(dir.X) * 0.4f, 1f + Math.Abs(dir.X) * 0.4f - Math.Abs(dir.Y) * 0.4f);
        health--;
        if (health > 0)
        {
            Add(firstHitSfx = new SoundSource("event:/new_content/game/10_farewell/fusebox_hit_1"));
            Celeste.Freeze(0.1f);
            shakeCounter = 0.2f;
            shaker.On = true;
            bounceDir = dir;
            bounce.Start();
            smashParticles = true;
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
        }
        else
        {
            if (firstHitSfx != null)
                firstHitSfx.Stop(true);
            Audio.Play("event:/new_content/game/10_farewell/fusebox_hit_2", Position);
            Celeste.Freeze(0.2f);
            player.RefillDash();
            Break();
            Input.Rumble(RumbleStrength.Strong, RumbleLength.Long);
            SmashParticles(dir.Perpendicular());
            SmashParticles(-dir.Perpendicular());
        }
        return DashCollisionResults.Rebound;
    }

    private void SmashParticles(Vector2 dir)
    {
        float direction;
        Vector2 position;
        Vector2 positionRange;
        int particleAmount;
        if (dir == Vector2.UnitX)
        {
            direction = 0f;
            position = CenterRight - Vector2.UnitX * 12f;
            positionRange = Vector2.UnitY * (Height - 6f) * 0.5f;
            particleAmount = (int) (Height / 8f) * 4;
        }
        else if (dir == -Vector2.UnitX)
        {
            direction = (float) Math.PI;
            position = CenterLeft + Vector2.UnitX * 12f;
            positionRange = Vector2.UnitY * (Height - 6f) * 0.5f;
            particleAmount = (int) (Height / 8f) * 4;
        }
        else if (dir == Vector2.UnitY)
        {
            direction = (float) Math.PI / 2;
            position = BottomCenter - Vector2.UnitY * 12f;
            positionRange = Vector2.UnitX * (Width - 6f) * 0.5f;
            particleAmount = (int) (Width / 8f) * 4;
        }
        else
        {
            direction = (float) -Math.PI / 2;
            position = TopCenter + Vector2.UnitY * 12f;
            positionRange = Vector2.UnitX * (Width - 6f) * 0.5f;
            particleAmount = (int) (Width / 8f) * 4;
        }
        particleAmount += 2;
        SceneAs<Level>().Particles.Emit(LightningBreakerBox.P_Smash, particleAmount, position, positionRange, direction);
    }

    public override void Update()
    {
        base.Update();
        if (makeSparks && Scene.OnInterval(0.03f))
            SceneAs<Level>().ParticlesFG.Emit(LightningBreakerBox.P_Sparks, 1, Center, Vector2.One * 12f);
        if (shakeCounter > 0f)
        {
            shakeCounter -= Engine.DeltaTime;
            if (shakeCounter <= 0f)
            {
                shaker.On = false;
                sprite.Scale = Vector2.One * 1.2f;
                sprite.Play("open", false, false);
            }
        }
        if (Collidable)
        {
            bool hasPlayerRider = HasPlayerRider();
            sink = Calc.Approach(sink, (float) (hasPlayerRider ? 1 : 0), 2f * Engine.DeltaTime);
            sine.Rate = MathHelper.Lerp(1f, 0.5f, sink);
            Vector2 target = start;
            target.Y += sink * 6f + sine.Value * MathHelper.Lerp(4f, 2f, sink);
            target += bounce.Value * bounceDir * 12f;
            MoveToX(target.X);
            MoveToY(target.Y);
            if (smashParticles)
            {
                smashParticles = false;
                SmashParticles(bounceDir.Perpendicular());
                SmashParticles(-bounceDir.Perpendicular());
            }
        }
        sprite.Scale.X = Calc.Approach(sprite.Scale.X, 1f, Engine.DeltaTime * 4f);
        sprite.Scale.Y = Calc.Approach(sprite.Scale.Y, 1f, Engine.DeltaTime * 4f);
        LiftSpeed = Vector2.Zero;
    }

    public override void Render()
    {
        Vector2 position = sprite.Position;
        sprite.Position += shaker.Value;
        base.Render();
        sprite.Position = position;
    }

    private void Break()
    {
        Session session = SceneAs<Level>().Session;
        RumbleTrigger.ManuallyTrigger(Center.X, 1.2f);
        Tag = Tags.Persistent;
        shakeCounter = 0f;
        shaker.On = false;
        sprite.Play("break", false, false);
        Collidable = false;
        DestroyStaticMovers();
        if (!string.IsNullOrEmpty(flag))
            session.SetFlag(flag, !aliveState);
        if (musicStoreInSession)
        {
            if (!string.IsNullOrEmpty(music))
            {
                session.Audio.Music.Event = SFX.EventnameByHandle(music);
            }
            if (musicProgress >= 0)
            {
                session.Audio.Music.SetProgress(musicProgress);
            }
            session.Audio.Apply(false);
        }
        else
        {
            if (!string.IsNullOrEmpty(music))
            {
                Audio.SetMusic(SFX.EventnameByHandle(music), false, true);
            }
            if (musicProgress >= 0)
            {
                Audio.SetMusicParam("progress", (float) musicProgress);
            }
            if (!string.IsNullOrEmpty(music) && Audio.CurrentMusicEventInstance != null)
            {
                Audio.CurrentMusicEventInstance.start();
            }
        }
    }
}
