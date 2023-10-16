using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CommunalHelper.Entities.StrawberryJam;
[CustomEntity("CommunalHelper/SJ/MomentumBlock")]
public class MomentumBlock : Solid
{
    public const string game_boost_block_boost = "event:/commhelper/sj/game/boost_block/boost";
    const float MAX_SPEED = 282; //internally the player has a max lift boost in each direction
    const float MAX_SPEED_X = 250;
    const float MAX_SPEED_Y = -130;
    private Vector2 targetSpeed, targetSpeedFlagged;
    private Color speedColor, speedColorFlagged;
    private float angle, angleFlagged;
    private Color startColor, endColor;
    private MTexture arrowTexture, arrowTextureFlagged;
    private string flag;
    private bool isFlagged;
    private Player ridingPlayer;

    private MTexture flashTexture, flashTextureFlagged;
    private float flashTimer;
    private Color flashColor;
    private bool doFlash;

    public MomentumBlock(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Width, data.Height, data.Float("speed"), data.Float("direction"), data.Float("speedFlagged"), data.Float("directionFlagged"), data.Attr("startColor"), data.Attr("endColor"), data.Attr("flag"))
    {
    }

    public MomentumBlock(Vector2 position, int width, int height, float spd, float dir, float spdFlagged, float dirFlagged, string startC, string endC, string flg) : base(position, width, height, safe: false)
    {
        flag = flg;
        isFlagged = false;
        dir = Calc.ToRad(dir);  //convert to radians
        dirFlagged = Calc.ToRad(dirFlagged);

        targetSpeed = Calc.AngleToVector(dir, spd);
        targetSpeedFlagged = Calc.AngleToVector(dirFlagged, spdFlagged);

        //bound the components to their respective max for accurate angles
        targetSpeed = ClampLiftBoost(targetSpeed);
        targetSpeedFlagged = ClampLiftBoost(targetSpeedFlagged);

        angle = dir;
        angleFlagged = dirFlagged;

        //calculate the color gradient
        startColor = Calc.HexToColor(startC);
        endColor = Calc.HexToColor(endC);
        speedColor = CalculateGradient(spd);
        speedColorFlagged = CalculateGradient(spdFlagged);

        arrowTexture = GetArrowTexture(angle);
        arrowTextureFlagged = GetArrowTexture(angleFlagged);
        flashTexture = GetArrowTextureFlash(angle);
        flashTextureFlagged = GetArrowTextureFlash(angleFlagged);
        ridingPlayer = null;

        flashColor = Color.White;
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        UpdateFlag();
    }

    public MTexture GetArrowTexture(float angle)
    {
        int value = (int) Math.Floor((0f - angle + (float) Math.PI * 2f) % ((float) Math.PI * 2f) / ((float) Math.PI * 2f) * 8f + 0.5f);
        if (((8 > Width - 1) || (8 > Height - 1))) //use a different texture if the size is small for readability
            return GFX.Game.GetAtlasSubtextures("objects/StrawberryJam2021/momentumBlock/trianglearrow")[Calc.Clamp(value, 0, 7)];
        return GFX.Game.GetAtlasSubtextures("objects/moveBlock/arrow")[Calc.Clamp(value, 0, 7)];
    }

    public MTexture GetArrowTextureFlash(float angle)
    {
        int value = (int) Math.Floor((0f - angle + (float) Math.PI * 2f) % ((float) Math.PI * 2f) / ((float) Math.PI * 2f) * 8f + 0.5f);
        if (((8 > Width - 1) || (8 > Height - 1))) //use a different texture if the size is small for readability
            return GFX.Game.GetAtlasSubtextures("objects/StrawberryJam2021/momentumBlock/trianglearrowflash")[Calc.Clamp(value, 0, 7)];
        return GFX.Game.GetAtlasSubtextures("objects/StrawberryJam2021/momentumBlock/arrowflash")[Calc.Clamp(value, 0, 7)];
    }

    public static Vector2 ClampLiftBoost(Vector2 liftBoost)
    {
        //clamp to the game's cap
        liftBoost.X = Calc.Clamp(liftBoost.X, -MAX_SPEED_X, MAX_SPEED_X);
        liftBoost.Y = Calc.Clamp(liftBoost.Y, MAX_SPEED_Y, 0);
        return liftBoost;
    }

    public Color CalculateGradient(float spd)
    {
        float g = (float) (1 - Math.Abs((1.0 - spd / MAX_SPEED) % 2f - 1)); //smooth the linear gradient
        g = -g + 1;
        return Color.Lerp(startColor, endColor, g);
    }

    public override void Update()
    {
        base.Update();
        UpdateFlag();
        MoveHExact(0);  //force a lift update

        Vector2 speed = isFlagged ? targetSpeedFlagged : targetSpeed;

        Player player = GetPlayerRider();
        if (ridingPlayer != null && player == null && ridingPlayer.Speed.Y < 0 && speed.Length() > 1f)
        {
            Audio.Play(game_boost_block_boost).setVolume(0.5f);
            Flash();
        }

        if (doFlash)
        {
            flashTimer = Calc.Approach(flashTimer, 1f, Engine.DeltaTime * 20f);
            if (flashTimer >= 1f)
                doFlash = false;
        }
        else if (flashTimer > 0f)
            flashTimer = Calc.Approach(flashTimer, 0f, Engine.DeltaTime * 6f);
        ridingPlayer = player;
    }

    public void Flash()
    {
        if (!Settings.Instance.DisableFlashes)
        {
            doFlash = true;
            flashTimer = 0f;
        }
    }

    public override void MoveHExact(int move)
    {
        LiftSpeed = isFlagged ? targetSpeedFlagged : targetSpeed;
        base.MoveHExact(move);
    }

    public override void MoveVExact(int move)
    {
        LiftSpeed = isFlagged ? targetSpeedFlagged : targetSpeed;
        base.MoveVExact(move);
    }

    public override void Render()
    {
        Draw.Rect(Position, Width, Height, Color.Black);

        if (flashTimer > 0f)
            Draw.Rect(Position, Width, Height, flashColor * flashTimer);

        Draw.HollowRect(Position, Width, Height, isFlagged ? speedColorFlagged : speedColor);
        MTexture currentTexture = isFlagged ? arrowTextureFlagged : arrowTexture;
        MTexture currentTextureFlash = isFlagged ? flashTextureFlagged : flashTexture;

        //draw the colored rectangle below the arrow texture
        Draw.Rect(Center.X - currentTexture.Width / 2, Center.Y - currentTexture.Height / 2, currentTexture.Width, currentTexture.Height, isFlagged ? speedColorFlagged : speedColor);
        currentTexture.DrawCentered(Center);

        if (flashTimer > 0f)
            currentTextureFlash.DrawCentered(Center, flashColor * flashTimer);
    }

    private void UpdateFlag()
    {
        if (!string.IsNullOrEmpty(flag))
            isFlagged = SceneAs<Level>().Session.GetFlag(flag);
    }
}
