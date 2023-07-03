using Mono.Cecil.Pdb;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/AeroBlockCharged")]
[Tracked]
public class AeroBlockCharged : AeroBlockFlying
{
    [Flags]
    private enum ButtonCombination : byte
    {
        NONE    = 0,
        TOP     = 1 << 0,
        LEFT    = 1 << 1,
        RIGHT   = 1 << 2,

        HORIZONTAL = LEFT | RIGHT,
        VERTICAL = TOP,

        ALL = TOP | LEFT | RIGHT,
    }

    public static MTexture ButtonFillTexture, ButtonOutlineTexture;
    private static readonly Color offColor = Calc.HexToColor("FFFFFF");
    private static readonly Color onColor = Calc.HexToColor("4BC0C8");

    private class Button
    {
        private readonly Image[] buttonImages, buttonOutlineImages;

        private bool visible;
        public bool Visible
        {
            get => visible;
            set
            {
                if (visible != value)
                    for (int i = 0; i < buttonImages.Length; i++)
                        buttonImages[i].Visible = buttonOutlineImages[i].Visible = value;
                visible = value;
            }
        }

        private bool pressed;
        public bool Pressed
        {
            get => pressed;
            private set
            {
                if (pressed != value)
                {
                    Vector2 offset = perp * (pressed ? -2 : +2);
                    for (int i = 0; i < buttonImages.Length; i++)
                    {
                        buttonImages[i].Position += offset;
                        buttonOutlineImages[i].Position += offset;
                    }
                }
                pressed = value;
            }
        }

        private readonly Vector2 perp;

        private float lerp;

        private Button(Entity entity, int length, bool visible, Vector2 offset, Vector2 dir, float angle)
        {
            buttonImages = new Image[length];
            buttonOutlineImages = new Image[length];

            this.visible = visible;
            perp = dir.Perpendicular();

            for (int i = 0; i < length; ++i)
            {
                int tx = i == 0 ? 0 : (i == length - 1 ? 16 : 8);
                Vector2 pos = offset + dir * (i * 8);

                buttonImages[i] = new Image(ButtonFillTexture.GetSubtexture(tx, 0, 8, 8))
                {
                    Rotation = angle,
                    Position = pos,
                    Visible = visible,
                };
                buttonOutlineImages[i] = new Image(ButtonOutlineTexture.GetSubtexture(tx, 0, 8, 8))
                {
                    Rotation = angle,
                    Position = pos,
                    Visible = visible,
                };
                buttonImages[i].CenterOrigin();
                buttonOutlineImages[i].CenterOrigin();
                entity.Add(buttonImages[i], buttonOutlineImages[i]);
            }
        }

        public void Update(AeroBlockCharged self)
        {
            Pressed = self.CollideCheck<Player>(self.Position - perp * 3);

            lerp = pressed
                ? 1.0f
                : Calc.Approach(lerp, 0.0f, Engine.DeltaTime * 4f);

            Color color = Color.Lerp(offColor, onColor, lerp);
            for (int i = 0; i < buttonImages.Length; i++)
                buttonImages[i].Color = color;
        }

        public static Button LeftButton(AeroBlockCharged entity, bool visible)
            => new(entity, (int) entity.Height / 8, visible, Vector2.UnitY * (entity.Height - 4), -Vector2.UnitY, -MathHelper.PiOver2);

        public static Button RightButton(AeroBlockCharged entity, bool visible)
            => new(entity, (int) entity.Height / 8, visible, new(entity.Width, 4), Vector2.UnitY, +MathHelper.PiOver2);

        public static Button TopButton(AeroBlockCharged entity, bool visible)
            => new(entity, (int) entity.Width / 8, visible, Vector2.UnitX * 4, Vector2.UnitX, 0.0f);
    }

    private const string DEFAULT_BUTTON_SEQUENCE = "horizontal";
    private ButtonCombination[] sequence;
    private int index;

    private Button leftButton, rightButton, topButton;

    private bool buttonSfxOn = false;
    private float buttonSfxLerp;
    private SoundSource buttonSfx;

    private readonly AeroScreen_Wind windLayer;
    private readonly SineWave windSine;

    public AeroBlockCharged(EntityData data, Vector2 offset)
        : this(data.NodesWithPosition(offset), data.Width, data.Height, data.Attr("buttonSequence", DEFAULT_BUTTON_SEQUENCE))
    { }

    public AeroBlockCharged(Vector2[] positions, int width, int height, string buttonSequence = DEFAULT_BUTTON_SEQUENCE)
        : base(positions[0], width, height)
    {
        if (positions.Length is 0)
            throw new ArgumentException(nameof(positions), "The array of positions must have at least one element (the first one being the starting position of the entity).");

        sequence = ParseButtonSequence(buttonSequence, positions.Length);
        BlockPath = GetBlockPath(sequence[0]);

        if (sequence[0].HasFlag(ButtonCombination.LEFT)) leftButton = Button.LeftButton(this, true);
        if (sequence[0].HasFlag(ButtonCombination.RIGHT)) rightButton = Button.RightButton(this, true);
        if (sequence[0].HasFlag(ButtonCombination.TOP)) topButton = Button.TopButton(this, true);

        Add(buttonSfx = new(CustomSFX.game_aero_block_button_charge)
        {
            Position = new Vector2(width, height) / 2.0f,
            RemoveOnOneshotEnd = false,
        });

        AddScreenLayer(windLayer = new(width, height));
        Add(windSine = new(2.0f));
    }

    private static ButtonCombination[] ParseButtonSequence(string sequence, int max)
    {
        static ButtonCombination Parse(string s)
        {
            ButtonCombination result = ButtonCombination.NONE;
            foreach (string word in s.Trim().Split('+'))
            {
                if (string.IsNullOrWhiteSpace(s))
                    continue;
                var keyword = word.Trim().ToUpper();
                if (Enum.TryParse<ButtonCombination>(keyword, out var but))
                    result |= but;
                else
                    Util.Log(LogLevel.Warn, $"invalid button sequence word: \"{keyword}\"");
            }
            return result;
        }

        return sequence.Split(new string[] { "->" }, StringSplitOptions.None)
                                   .Select(Parse)
                                   .Take(max)
                                   .ToArray();
    }

    private static string GetBlockPath(ButtonCombination combination)
    {
        bool left = combination.HasFlag(ButtonCombination.LEFT);
        bool top = combination.HasFlag(ButtonCombination.TOP);
        bool right = combination.HasFlag(ButtonCombination.RIGHT);

        const string y = "y";
        const string n = "n";

        return $"objects/CommunalHelper/aero_block/blocks/{(left ? y : n)}{(top ? y : n)}{(right ? y : n)}";
    }

    public bool CheckTopButton() => topButton?.Pressed ?? false;
    public bool CheckLeftButton() => leftButton?.Pressed ?? false;
    public bool CheckRightButton() => rightButton?.Pressed ?? false;
    public bool CheckAnyButton() => CheckLeftButton() || CheckTopButton() || CheckRightButton();

    private void Smash(Player player, Vector2 speed)
    {
        player.Speed = speed;
        player.StateMachine.State = Player.StLaunch;
        Celeste.Freeze(0.05f);
        Audio.Play(CustomSFX.game_aero_block_smash, Center, "magic", 1.0f);
        Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);
        (Scene as Level).DirectionalShake(Vector2.UnitY);
        windLayer.MulitplyVelocities(-0.5f);
    }

    public override void Update()
    {
        base.Update();

        leftButton?.Update(this);
        rightButton?.Update(this);
        topButton?.Update(this);

        bool check = CheckAnyButton();
        if (check)
        {
            if (!buttonSfxOn)
            {
                Audio.Play(CustomSFX.game_aero_block_button_press);
                buttonSfxOn = true;
            }
            buttonSfxLerp = Calc.Approach(buttonSfxLerp, 1.0f, Engine.DeltaTime / 2f);
        }
        else
        {
            if (buttonSfxOn)
            {
                Audio.Play(CustomSFX.game_aero_block_button_let_go);
                buttonSfxOn = false;
            }
            buttonSfxLerp = Calc.Approach(buttonSfxLerp, 0.0f, Engine.DeltaTime / 3f);
        }

        buttonSfx.Param("charge", buttonSfxLerp);

        if (CheckLeftButton())
            windLayer.Wind = new(-300, windSine.Value * 100);
        else if (CheckRightButton())
            windLayer.Wind = new(300, windSine.Value * 100);
        else if (CheckTopButton())
            windLayer.Wind = new(windSine.Value * 100, -300);
        else
            windLayer.Wind = new(windSine.Value * 3, 16);

        windLayer.Color = Color.Lerp(Color.Transparent, onColor, buttonSfxLerp * 0.5f + 0.5f);
    }

    #region Hooks

    internal static void Load()
    {
        On.Celeste.Player.Jump += Player_Jump;
        On.Celeste.Player.WallJump += Player_WallJump;
        On.Celeste.Player.ClimbJump += Player_ClimbJump;
        On.Celeste.Player.SuperWallJump += Player_SuperWallJump;
        On.Celeste.Player.SuperJump += Player_SuperJump;
    }

    internal static void Unload()
    {
        On.Celeste.Player.Jump -= Player_Jump;
        On.Celeste.Player.WallJump -= Player_WallJump;
        On.Celeste.Player.ClimbJump -= Player_ClimbJump;
        On.Celeste.Player.SuperWallJump -= Player_SuperWallJump;
        On.Celeste.Player.SuperJump -= Player_SuperJump;
    }

    private static void Player_Jump(On.Celeste.Player.orig_Jump orig, Player self, bool particles, bool playSfx)
    {
        orig(self, particles, playSfx);

        if (!self.OnGround())
            return;

        // jump
        var block = self.CollideFirst<AeroBlockCharged>(self.Position + Vector2.UnitY);
        if (block is not null && block.CheckTopButton())
            block.Smash(self, Vector2.UnitY * -350);
    }

    private static void Player_WallJump(On.Celeste.Player.orig_WallJump orig, Player self, int dir)
    {
        orig(self, dir);

        // walljump
        var block = self.CollideFirst<AeroBlockCharged>(self.Position - Vector2.UnitX * dir * 3);
        if (block is not null && (dir < 0 ? block.CheckLeftButton() : block.CheckRightButton()))
            block.Smash(self, new Vector2(dir * 300, -300));
    }

    private static void Player_ClimbJump(On.Celeste.Player.orig_ClimbJump orig, Player self)
    {
        orig(self);

        // climbjump
        int dir = (int) self.Facing;
        var block = self.CollideFirst<AeroBlockCharged>(self.Position + Vector2.UnitX * dir);
        if (block is not null && (dir > 0 ? block.CheckLeftButton() : block.CheckRightButton()))
        {
            float speed = dir == Math.Sign(Input.MoveX.Value) ? 300 : -300;
            block.Smash(self, new Vector2(dir * speed, -300));
        }
    }

    private static void Player_SuperWallJump(On.Celeste.Player.orig_SuperWallJump orig, Player self, int dir)
    {
        orig(self, dir);

        // wallbounce
        var block = self.CollideFirst<AeroBlockCharged>(self.Position - Vector2.UnitX * dir * 3);
        if (block is not null && (dir < 0 ? block.CheckLeftButton() : block.CheckRightButton()))
            block.Smash(self, new Vector2(300 * dir, -400));
    }

    private static void Player_SuperJump(On.Celeste.Player.orig_SuperJump orig, Player self)
    {
        orig(self);

        if (!self.OnGround())
            return;

        var block = self.CollideFirst<AeroBlockCharged>(self.Position + Vector2.UnitY);
        if (block is not null && block.CheckTopButton())
            block.Smash(self, new Vector2(self.Speed.X * 1.2f, -350));
    }

    #endregion
}
