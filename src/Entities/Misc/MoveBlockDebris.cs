namespace Celeste.Mod.CommunalHelper.Entities;

[Pooled]
public class MoveBlockDebris : Actor
{
    public Action<Image> OnUpdateSprite;

    public Image Sprite;

    private Vector2 home;

    private Vector2 speed;

    private bool shaking;

    private bool returning;
    private float returnEase;
    private float returnDuration;
    private SimpleCurve returnCurve;

    private bool firstHit;

    private float alpha;

    private readonly Collision onCollideH;
    private readonly Collision onCollideV;

    private float spin;
    public MoveBlockDebris() : base(Vector2.Zero)
    {
        Tag = Tags.TransitionUpdate;
        Collider = new Hitbox(4f, 4f, -2f, -2f);
        Add(Sprite = new Image(Calc.Random.Choose(GFX.Game.GetAtlasSubtextures("objects/moveblock/debris"))));
        Sprite.CenterOrigin();
        Sprite.FlipX = Calc.Random.Chance(0.5f);
        onCollideH = collisiondata => speed.X = -speed.X * 0.5f;
        onCollideV = collisiondata =>
        {
            if (firstHit || speed.Y > 50f)
            {
                Audio.Play(SFX.game_gen_debris_stone, Position, "debris_velocity", Calc.ClampedMap(speed.Y, 0f, 600f, 0f, 1f));
            }
            speed.Y = speed.Y is > 0f and < 40f ? 0f : -speed.Y * 0.25f;
            firstHit = false;
        };
    }

    public override void OnSquish(CollisionData data)
    {
    }

    public MoveBlockDebris Init(Vector2 position, Vector2 center, Vector2 returnTo, Action<Image> onUpdateSprite = null)
    {
        OnUpdateSprite = onUpdateSprite;

        Collidable = true;
        Position = position;
        speed = (position - center).SafeNormalize(60f + Calc.Random.NextFloat(60f));
        home = returnTo;
        Sprite.Position = Vector2.Zero;
        Sprite.Rotation = Calc.Random.NextAngle();
        returning = false;
        shaking = false;
        Sprite.Scale = Vector2.One;
        Sprite.Color = Color.White;
        alpha = 1f;
        firstHit = false;
        spin = Calc.Random.Range(3.49065852f, 10.4719753f) * Calc.Random.Choose(1, -1);
        return this;
    }

    public override void Update()
    {
        base.Update();

        bool isInBounds = SceneAs<Level>().IsInBounds(this);

        Sprite.Color = Color.White;
        Sprite.Scale = Vector2.One;
        Sprite.Visible = isInBounds;
        OnUpdateSprite?.Invoke(Sprite);

        if (!returning)
        {
            if (Collidable && isInBounds)
            {
                speed.X = Calc.Approach(speed.X, 0f, Engine.DeltaTime * 100f);

                if (!OnGround(1))
                {
                    speed.Y += 400f * Engine.DeltaTime;
                }
                MoveH(speed.X * Engine.DeltaTime, onCollideH);
                MoveV(speed.Y * Engine.DeltaTime, onCollideV);
            }

            if (shaking && Scene.OnInterval(0.05f))
            {
                Sprite.X = -1 + Calc.Random.Next(3);
                Sprite.Y = -1 + Calc.Random.Next(3);
            }
        }
        else
        {
            Position = returnCurve.GetPoint(Ease.CubeOut(returnEase));
            returnEase = Calc.Approach(returnEase, 1f, Engine.DeltaTime / returnDuration);
            Sprite.Scale *= 1f + (returnEase * 0.5f);
        }

        if ((Scene as Level).Transitioning)
        {
            alpha = Calc.Approach(alpha, 0f, Engine.DeltaTime * 4f);
            Sprite.Color *= alpha;
        }
        Sprite.Rotation += spin * Calc.ClampedMap(Math.Abs(speed.Y), 50f, 150f, 0f, 1f) * Engine.DeltaTime;
    }

    public void StopMoving()
    {
        Collidable = false;
    }

    public void StartShaking()
    {
        shaking = true;
    }

    public void ReturnHome(float duration)
    {
        if (Scene != null)
        {
            Camera camera = (Scene as Level).Camera;
            if (X < camera.X)
            {
                X = camera.X - 8f;
            }
            if (Y < camera.Y)
            {
                Y = camera.Y - 8f;
            }
            if (X > camera.X + 320f)
            {
                X = camera.X + 320f + 8f;
            }
            if (Y > camera.Y + 180f)
            {
                Y = camera.Y + 180f + 8f;
            }
        }
        returning = true;
        returnEase = 0f;
        returnDuration = duration;
        Vector2 vector = (home - Position).SafeNormalize();
        Vector2 control = ((Position + home) / 2f) + (new Vector2(vector.Y, -vector.X) * (Calc.Random.NextFloat(16f) + 16f) * Calc.Random.Facing());
        returnCurve = new SimpleCurve(Position, home, control);
    }
}
