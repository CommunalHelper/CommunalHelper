namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/ChainedDreamFallingBlock")]
public class ChainedDreamFallingBlock : DreamFallingBlock
{
    private class DreamFallingBlockChainRenderer : Entity
    {
        private readonly ChainedDreamFallingBlock block;

        public DreamFallingBlockChainRenderer(ChainedDreamFallingBlock dreamFallingBlock)
        {
            block = dreamFallingBlock;
            Depth = Depths.FGTerrain + 1;
        }

        public override void Render()
        {
            if (block.centeredChain)
                Chain.DrawChainLine(new Vector2(block.X + (block.Width / 2f), block.startY), new Vector2(block.X + (block.Width / 2f), block.Y), block.chainTexture, block.chainOutline);
            else
            {
                Chain.DrawChainLine(new Vector2(block.X + 3, block.startY), new Vector2(block.X + 3, block.Y), block.chainTexture, block.chainOutline);
                Chain.DrawChainLine(new Vector2(block.X + block.Width - 4, block.startY), new Vector2(block.X + block.Width - 4, block.Y), block.chainTexture, block.chainOutline);
            }
        }
    }

    private DreamFallingBlockChainRenderer chainRenderer;

    private readonly MTexture chainTexture;

    private bool heldByChain;
    protected override bool Held => heldByChain || base.Held;

    private readonly float chainStopY, startY;
    private readonly bool centeredChain;
    private readonly bool chainOutline;

    private readonly bool indicator, indicatorAtStart;
    private float pathLerp;

    private readonly SoundSource rattle;

    public ChainedDreamFallingBlock(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        removeWhenOutOfLevel = true;

        startY = Y;
        chainStopY = startY + data.Int("fallDistance", 64);
        centeredChain = data.Bool("centeredChain") || Width <= 8;
        chainOutline = data.Bool("chainOutline", true);
        indicator = data.Bool("indicator");
        indicatorAtStart = data.Bool("indicatorAtStart");
        pathLerp = Util.ToInt(indicatorAtStart);

        string chainTexturePath = data.Attr("chainTexture", Chain.DEFAULT_CHAIN_PATH);
        chainTexture = GFX.Game.GetOrDefault(chainTexturePath, Chain.DefaultChain);

        Add(rattle = new SoundSource()
        {
            Position = Vector2.UnitX * Width / 2f
        });
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        scene.Add(chainRenderer = new DreamFallingBlockChainRenderer(this));
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        chainRenderer.RemoveSelf();
    }

    public override void Update()
    {
        base.Update();

        if (HasStartedFalling && indicator && !indicatorAtStart)
            pathLerp = Calc.Approach(pathLerp, 1f, Engine.DeltaTime * 2f);
    }

    public override void Render()
    {
        if ((HasStartedFalling || indicatorAtStart) && indicator && !heldByChain)
        {
            float toY = startY + ((chainStopY + Height - startY) * Ease.ExpoOut(pathLerp));
            Draw.Rect(X, Y, Width, toY - Y, Color.Black * 0.75f);
        }

        base.Render();
    }

    protected override bool ShouldStopFalling()
    {
        if (hasLanded)
        {
            heldByChain = Y == chainStopY;
            return true;
        }
        else if (Y > chainStopY)
        {
            heldByChain = true;
            MoveToY(chainStopY, LiftSpeed.Y);
            return true;
        }
        return base.ShouldStopFalling();
    }

    protected override void ImpactSfx()
    {
        base.ImpactSfx();

        rattle.Stop();
        if (heldByChain)
        {
            Audio.Play(CustomSFX.game_chainedFallingBlock_chain_tighten_block, TopCenter);
            Audio.Play(CustomSFX.game_chainedFallingBlock_chain_tighten_ceiling, new Vector2(Center.X, startY));
        }
    }

    protected override void FallSFX()
    {
        base.FallSFX();
        rattle.Play(CustomSFX.game_chainedFallingBlock_chain_rattle);
    }

}
