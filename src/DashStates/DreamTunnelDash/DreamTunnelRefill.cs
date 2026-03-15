using static Celeste.Mod.CommunalHelper.DashStates.DreamTunnelDash;

namespace Celeste.Mod.CommunalHelper.DashStates;

[CustomEntity("CommunalHelper/DreamRefill", "CommunalHelper/DreamTunnelRefill")]
public class DreamTunnelRefill : DashStateRefill
{
    public static new ParticleType[] P_Shatter { get; private set; }
    private int shatterParticleIndex = 0;
    public static new ParticleType[] P_Regen { get; private set; }
    private int regenParticleIndex = 0;
    public static new ParticleType[] P_Glow { get; private set; }
    private int glowParticleIndex = 0;

    public static void InitializeParticles()
    {
        P_Shatter = [Refill.P_Shatter, null, null, null];
        P_Regen = [Refill.P_Regen, null, null, null];
        P_Glow = [Refill.P_Glow, null, null, null];

        ParticleType[][] particles = [P_Shatter, P_Regen, P_Glow];
        for (int i = 0; i < 3; ++i)
        {
            ParticleType particle = new(particles[i][0])
            {
                ColorMode = ParticleType.ColorModes.Choose
            };

            particles[i][0] = new ParticleType(particle)
            {
                Color = Calc.HexToColor("FFEF11"),
                Color2 = Calc.HexToColor("FF00D0")
            };

            particles[i][1] = new ParticleType(particle)
            {
                Color = Calc.HexToColor("08a310"),
                Color2 = Calc.HexToColor("5fcde4")
            };

            particles[i][2] = new ParticleType(particle)
            {
                Color = Calc.HexToColor("7fb25e"),
                Color2 = Calc.HexToColor("E0564C")
            };

            particles[i][3] = new ParticleType(particle)
            {
                Color = Calc.HexToColor("5b6ee1"),
                Color2 = Calc.HexToColor("CC3B3B")
            };
        }
    }

    public DreamTunnelRefill(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        TouchSFX = CustomSFX.game_dreamRefill_dream_refill_touch;
        ReturnSFX = CustomSFX.game_dreamRefill_dream_refill_return;
    }

    protected override bool CanActivate(Player player)
    {
        DreamTunnelDashComponent component = player.Get<DreamTunnelDashComponent>();
        return player.Stamina < 20f || (component?.DreamTunnelDashCount ?? 0) < (twoDashes ? 2 : 1);
    }

    protected override void Activated(Player player)
    {
        if (twoDashes)
        {
            DashStates.DreamTunnelDoubleDash.SetEnabled(true);
            player.Dashes = Math.Max(player.Dashes, 2);
        }
        else
        {
            DashStates.DreamTunnelDash.SetEnabled(true);
        }
    }

    protected override bool TryCreateCustomSprite(out Sprite sprite)
    {
        sprite = new Sprite(GFX.Game, twoDashes ? "objects/CommunalHelper/dreamRefillTwo/idle" : "objects/CommunalHelper/dreamRefill/idle");
        sprite.AddLoop("idle", "", 0.1f);
        sprite.Play("idle");
        sprite.CenterOrigin();
        return true;
    }

    protected override void EmitGlowParticles()
    {
        Level level = baseData.Get<Level>("level");
        level.ParticlesFG.Emit(P_Glow[glowParticleIndex], 1, Position, Vector2.One * 5f);
        ++glowParticleIndex;
        glowParticleIndex %= 4;
    }

    protected override void EmitShatterParticles(float angle)
    {
        Level level = baseData.Get<Level>("level");
        for (int i = 0; i < 5; ++i)
        {
            level.ParticlesFG.Emit(P_Shatter[shatterParticleIndex], 1, Position, Vector2.One * 4f, angle - ((float) Math.PI / 2f));
            ++shatterParticleIndex;
            shatterParticleIndex %= 4;
        }
        for (int i = 0; i < 5; ++i)
        {
            level.ParticlesFG.Emit(P_Shatter[shatterParticleIndex], 1, Position, Vector2.One * 4f, angle + ((float) Math.PI / 2f));
            ++shatterParticleIndex;
            shatterParticleIndex %= 4;
        }
    }

    protected override void EmitRegenParticles()
    {
        Level level = baseData.Get<Level>("level");
        for (int i = 0; i < 16; ++i)
        {
            level.ParticlesFG.Emit(P_Regen[regenParticleIndex], 1, Position, Vector2.One * 2f);
            ++regenParticleIndex;
            regenParticleIndex %= 4;
        }
    }
}
