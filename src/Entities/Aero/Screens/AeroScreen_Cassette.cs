namespace Celeste.Mod.CommunalHelper.Entities;

public class AeroScreen_Cassette : AeroScreen
{
    private readonly float width, height;

    private float screenW, screenH;
    private float targetScreenW, targetScreenH;
    private readonly float maxTargetScreenW, maxTargetScreenH;
    private const float BlinkThreshold = 0.3f;
    private const float BlinkSpeedFactor = 15f;

    private bool backlit;
    private const float BacklightBrightness = 0.25f;

    private readonly CassetteListener listener;

    private static readonly Color[] ColorOptions = [
        Calc.HexToColor("49aaf0"),
        Calc.HexToColor("f049be"),
        Calc.HexToColor("fcdc3a"),
        Calc.HexToColor("38e04e")
    ];
    private readonly Color screenColor;

    public AeroScreen_Cassette(float width, float height, CassetteListener listener, Color? screenColor = null)
    {
        this.width = width;
        this.height = height;

        this.listener = listener;
        this.listener.OnStart += OnStart;
        this.listener.OnWillActivate += OnWillActivate;
        this.listener.OnWillDeactivate += OnWillDeactivate;
        this.listener.OnFinish += OnFinish;

        this.screenColor = screenColor ?? ColorOptions[listener.Index];
        maxTargetScreenW = targetScreenW = width - 8;
        maxTargetScreenH = targetScreenH = height - 8;
        screenW = screenH = 0;
    }

    private void OnStart(bool activated) => backlit = activated;
    private void OnWillActivate() => backlit = true;
    private void OnWillDeactivate() => backlit = false;
    private void OnFinish() => backlit = false;

    public override void Update()
    {
        targetScreenW = listener.Activated ? maxTargetScreenW : (screenH / maxTargetScreenH > BlinkThreshold ? maxTargetScreenW : 0);
        targetScreenH = listener.Activated ? (screenW / maxTargetScreenW > 1 - BlinkThreshold ? maxTargetScreenH : 1) : 1;
        screenW = Calc.Approach(screenW, targetScreenW, BlinkSpeedFactor * maxTargetScreenW * Engine.DeltaTime);
        screenH = Calc.Approach(screenH, targetScreenH, BlinkSpeedFactor * maxTargetScreenH * Engine.DeltaTime);
    }

    private void DrawRectCentered(float w, float h, Color col)
    {
        Draw.Rect(Block.Position + new Vector2((width - w) / 2, (height - h) / 2), w, h, col);
    }

    public override void Render()
    {
        if (backlit)
            DrawRectCentered(maxTargetScreenW, maxTargetScreenH, Color.Lerp(Color.Transparent, screenColor, BacklightBrightness));
        
        DrawRectCentered(screenW, screenH, screenColor);
    }

    public override void Finish() { }
}
