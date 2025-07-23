using MonoMod.RuntimeDetour;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/AeroBlockCharged")]
[Tracked]
public class AeroBlockCharged : AeroBlockFlying
{
    internal static bool SpirialisHelperLoaded = false;

    [Flags]
    private enum ButtonCombination : byte
    {
        NONE = 0,
        TOP = 1 << 0,
        LEFT = 1 << 1,
        RIGHT = 1 << 2,

        HORIZONTAL = LEFT | RIGHT,
        VERTICAL = TOP,

        ALL = TOP | LEFT | RIGHT,
    }

    internal static MTexture ButtonFillTexture, ButtonOutlineTexture;
    private static readonly Color DefaultOnColor = Calc.HexToColor("4BC0C8");
    private static readonly Color DefaultEndColor = Color.Tomato;

    private sealed class Button
    {
        private readonly AeroBlockCharged block;
        
        private readonly Image[] buttonImages, buttonOutlineImages;
        private readonly Vector2[] buttonPositions;

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

        public enum PressState
        {
            Unpressed = 0,
            HalfPressed = 1,
            Pressed = 2,
        }
        private PressState defaultPressState = PressState.Unpressed; // press state to return to when button is not pressed
        private bool alreadyUpdatedPressState = false; // used to make sure that when UpdatePressState is called externally, we don't overwrite it in the same frame
        
        public bool Pressed { get; private set; }
        
        private Color unpressedColor, pressedColor;
        private float colorLerp;

        private readonly Vector2 perp;
        
        private Button(AeroBlockCharged block, int length, bool visible, Vector2 offset, Vector2 dir, float angle)
        {
            this.block = block;
            
            buttonImages = new Image[length];
            buttonOutlineImages = new Image[length];
            buttonPositions = new Vector2[length];

            this.visible = visible;
            perp = dir.Perpendicular();

            for (int i = 0; i < length; ++i)
            {
                int tx = i == 0 ? 0 : (i == length - 1 ? 16 : 8);
                buttonPositions[i] = offset + dir * (i * 8);

                buttonImages[i] = new Image(ButtonFillTexture.GetSubtexture(tx, 0, 8, 8))
                {
                    Rotation = angle,
                    Position = buttonPositions[i],
                    Visible = visible,
                };
                buttonOutlineImages[i] = new Image(ButtonOutlineTexture.GetSubtexture(tx, 0, 8, 8))
                {
                    Rotation = angle,
                    Position = buttonPositions[i],
                    Visible = visible,
                };
                buttonImages[i].CenterOrigin();
                buttonOutlineImages[i].CenterOrigin();
                
                this.block.Add(buttonImages[i], buttonOutlineImages[i]);
            }
        }

        public void Update()
        {
            if (!alreadyUpdatedPressState)
                UpdatePressState();
            
            colorLerp = Pressed
                ? 1f
                : Calc.Approach(colorLerp, 0f, Engine.DeltaTime * 4f);

            Color color = Color.Lerp(unpressedColor, pressedColor, colorLerp);
            for (int i = 0; i < buttonImages.Length; i++)
                buttonImages[i].Color = color;

            alreadyUpdatedPressState = false;
        }
        
        // updates button's Pressed field + position accordingly
        public void UpdatePressState(float checkDistance = 3f)
        {
            Pressed = block.CollideCheck<Player>(block.Position - perp * checkDistance);
            PressState pressState = Pressed ? PressState.Pressed : defaultPressState;

            for (int i = 0; i < buttonPositions.Length; i++)
                buttonImages[i].Position = buttonOutlineImages[i].Position = buttonPositions[i] + perp * (int) pressState;

            alreadyUpdatedPressState = true;
        }

        public void SetProperties(PressState defaultPressState, Color unpressedColor, Color pressedColor)
        {
            this.defaultPressState = defaultPressState;
            this.unpressedColor = unpressedColor;
            this.pressedColor = pressedColor;
        }

        public static Button LeftButton(AeroBlockCharged entity, bool visible)
            => new(entity, (int) entity.Height / 8, visible, new(0f, entity.Height - 4f), -Vector2.UnitY, -MathHelper.PiOver2);

        public static Button RightButton(AeroBlockCharged entity, bool visible)
            => new(entity, (int) entity.Height / 8, visible, new(entity.Width, 4f), Vector2.UnitY, +MathHelper.PiOver2);

        public static Button TopButton(AeroBlockCharged entity, bool visible)
            => new(entity, (int) entity.Width / 8, visible, new(4f, 0f), Vector2.UnitX, 0f);
    }

    private const string DEFAULT_BUTTON_SEQUENCE = "horizontal";
    private readonly ButtonCombination[] sequence;
    private int positionIndex, combinationIndex;

    private readonly bool loop;

    private bool alive = true;
    private readonly bool SpirialisBug = false;
    private readonly bool wallbounceLeniency;

    private Button leftButton, rightButton, topButton;

    private bool buttonSfxOn = false;
    private float buttonSfxLerp;
    private readonly SoundSource buttonSfx;

    private readonly AeroScreen_Wind windLayer;
    private readonly SineWave windSine;

    private readonly Vector2[] positions;

    private readonly Color activeColor, inactiveColor;

    private readonly int cassetteIndex;
    private readonly CassetteListener listener;
    private readonly AeroScreen_Cassette cassetteLayer;
    private readonly bool moveOnCassetteTick;

    private bool activatedPreviously = false, activatedThisTick = false;

    public AeroBlockCharged(EntityData data, Vector2 offset)
        : this(data.NodesWithPosition(offset), data.Width, data.Height, data.Bool("loop"), data.HexColor("activeColor", DefaultOnColor), data.HexColor("inactiveColor", DefaultEndColor), data.Bool("hover", true), data.Attr("buttonSequence", DEFAULT_BUTTON_SEQUENCE), data.Bool("wallbounceLeniency", false), data.Int("cassetteIndex", -1), data.Float("cassetteTempo", 1f), data.Bool("moveOnCassetteTick", false))
    {
        // if the new attribute exists use that, but otherwise enable the bug if either the old attribute is set *or* spirialis helper is loaded
        SpirialisBug = data.Bool("SpirialisBugV2", SpirialisHelperLoaded || data.Bool("SpirialisBug", false));
    }

    public AeroBlockCharged(Vector2[] positions, int width, int height, bool loop, Color activeColor, Color inactiveColor, bool hover = true, string buttonSequence = DEFAULT_BUTTON_SEQUENCE, bool wallbounceLeniency = true, int cassetteIndex = -1, float cassetteTempo = 1f, bool moveOnCassetteTick = false)
        : base(positions[0], width, height)
    {
        Hover = hover;

        if (positions.Length is 0)
            throw new ArgumentException("The array of positions must have at least one element (the first one being the starting position of the entity).", nameof(positions));
        this.positions = positions;
        this.loop = loop;
        
        this.activeColor = activeColor;
        this.inactiveColor = inactiveColor;

        sequence = ParseButtonSequence(buttonSequence, positions.Length);
        ChangeCombination(sequence[0], makeTiles: false);
        SetButtonProperties(Button.PressState.Unpressed, Color.White, this.activeColor);

        Add(buttonSfx = new(CustomSFX.game_aero_block_button_charge)
        {
            Position = new Vector2(width, height) / 2f,
            RemoveOnOneshotEnd = false,
        });

        AddScreenLayer(windLayer = new(width, height));
        Add(windSine = new(2f));

        this.wallbounceLeniency = wallbounceLeniency;
        
        this.cassetteIndex = cassetteIndex;
        this.moveOnCassetteTick = moveOnCassetteTick;
        if (cassetteIndex != -1)
        {
            Add(listener = new CassetteListener(cassetteIndex, cassetteTempo)
            {
                OnStart = OnStart,
                OnWillActivate = OnWillActivate,
                OnActivated = OnActivated,
                OnWillDeactivate = OnWillDeactivate,
                OnDeactivated = OnDeactivated,
                OnFinish = OnFinish
            });
            AddScreenLayer(cassetteLayer = new AeroScreen_Cassette(Width, Height, listener, activeColor));
        }
    }

    private void SetButtonProperties(Button.PressState defaultPressState, Color unpressedColor, Color pressedColor)
    {
        leftButton?.SetProperties(defaultPressState, unpressedColor, pressedColor);
        rightButton?.SetProperties(defaultPressState, unpressedColor, pressedColor);
        topButton?.SetProperties(defaultPressState, unpressedColor, pressedColor);
    }

    #region Cassette Listener Callbacks
    
    private void OnStart(bool activated)
    {
        if (activated)
            SetButtonProperties(Button.PressState.Unpressed, Color.White, activeColor);
        else
            SetButtonProperties(Button.PressState.Pressed, inactiveColor, inactiveColor);
        
        activatedThisTick = false;
    }
    
    private void OnWillActivate()
        => SetButtonProperties(Button.PressState.HalfPressed, Color.Lerp(Color.White, inactiveColor, 0.5f), Color.Lerp(activeColor, inactiveColor, 0.5f));
    
    private void OnActivated()
    {
        SetButtonProperties(Button.PressState.Unpressed, Color.White, activeColor);
        
        activatedThisTick = false;
    }
    
    private void OnWillDeactivate() 
        => SetButtonProperties(Button.PressState.HalfPressed, Color.Lerp(Color.White, inactiveColor, 0.5f), Color.Lerp(activeColor, inactiveColor, 0.5f));
    
    private void OnDeactivated()
    {
        SetButtonProperties(Button.PressState.Pressed, inactiveColor, inactiveColor);
        
        if (alive && moveOnCassetteTick && activatedPreviously && !activatedThisTick)
            IncrementPosition();
    }

    private void OnFinish()
        => SetButtonProperties(Button.PressState.Pressed, inactiveColor, inactiveColor);
    
    #endregion

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

        return sequence.Split("->")
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
    
    public bool CheckTopButton() => (topButton?.Pressed ?? false) && (listener?.Activated ?? true);
    public bool CheckLeftButton() => (leftButton?.Pressed ?? false) && (listener?.Activated ?? true);
    public bool CheckRightButton() => (rightButton?.Pressed ?? false) && (listener?.Activated ?? true);
    public bool CheckAnyButton() => CheckLeftButton() || CheckTopButton() || CheckRightButton();

    private void ChangeCombination(ButtonCombination combination, bool makeTiles = true)
    {
        if (makeTiles)
            RemakeBlockTiles(GetBlockPath(combination));

        leftButton ??= Button.LeftButton(this, true);
        if (leftButton is not null)
            leftButton.Visible = combination.HasFlag(ButtonCombination.LEFT);

        rightButton ??= Button.RightButton(this, true);
        if (rightButton is not null)
            rightButton.Visible = combination.HasFlag(ButtonCombination.RIGHT);

        topButton ??= Button.TopButton(this, true);
        if (topButton is not null)
            topButton.Visible = combination.HasFlag(ButtonCombination.TOP);
    }

    private void IncrementPosition()
    {
        positionIndex++;
        if (loop)
            positionIndex %= positions.Length;
        
        combinationIndex = (combinationIndex + 1) % sequence.Length;
        
        if (positionIndex < positions.Length)
        {
            Home = positions[positionIndex];
            ChangeCombination(sequence[combinationIndex]);
        }
        else
            EndSequence();
    }

    private void EndSequence()
    {
        alive = false;

        Deactivate();
        SetButtonProperties(Button.PressState.Unpressed, Color.White, inactiveColor);
        
        if (cassetteIndex != -1)
            Remove(listener);

        MTexture icon = GFX.Game["objects/CommunalHelper/aero_block/icons/x5"];
        AeroScreen_Blinker blinker;
        AddScreenLayer(blinker = new(icon)
        {
            Offset = new Vector2(Width / 2f - 2f, Height / 2f - 2f),
            BackgroundColor = inactiveColor,
            IconColor = Color.White,
            Sound = CustomSFX.game_aero_block_success,
            FadeIn = 0f,
            Hold = 0.5f,
            FadeOut = 0.5f,
        });
        
        Alarm.Set(this, 0.5f, () =>
        {
            blinker.Complete = true;
            RemoveScreenLayer(windLayer);

            if (cassetteIndex != -1)
                RemoveScreenLayer(cassetteLayer);
        });
    }

    private void Smash(Player player, Vector2 speed)
    {
        if (!alive)
            return;

        activatedPreviously = activatedThisTick = true;

        player.Speed = speed;
        player.StateMachine.State = Player.StLaunch;
        Celeste.Freeze(0.05f);
        Audio.Play(CustomSFX.game_aero_block_smash, Center, "magic", 1f);
        Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);
        SceneAs<Level>().DirectionalShake(Vector2.UnitY);
        windLayer.MulitplyVelocities(-0.5f);

        IncrementPosition();
    }

    public override void Added(Scene scene)
    {
        RemakeBlockTiles(GetBlockPath(sequence[0]));
        base.Added(scene);
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        
        // visually deactivate if there is no CassetteBlockManager, since listener.Activated will always be false
        if (cassetteIndex != -1 && scene.Tracker.GetEntity<CassetteBlockManager>() is null)
            OnFinish();
    }

    public override void Update()
    {
        base.Update();

        leftButton?.Update();
        rightButton?.Update();
        topButton?.Update();

        if (CheckAnyButton() && !buttonSfxOn)
        {
            Audio.Play(CustomSFX.game_aero_block_button_press, Center);
            buttonSfxOn = true;
        }
        else if (!CheckAnyButton() && buttonSfxOn)
        {
            Audio.Play(CustomSFX.game_aero_block_button_let_go, Center);
            buttonSfxOn = false;
        }

        float chargeSoundRate = buttonSfxOn ? 2f : (alive ? 3f : 4f);
        buttonSfxLerp = Calc.Approach(buttonSfxLerp, buttonSfxOn && alive ? 1f : 0f, Engine.DeltaTime / chargeSoundRate);
        buttonSfx.Param("charge", buttonSfxLerp);

        if (alive)
        {
            if (CheckLeftButton())
                windLayer.Wind = new(-300f, windSine.Value * 100f);
            else if (CheckRightButton())
                windLayer.Wind = new(300f, windSine.Value * 100f);
            else if (CheckTopButton())
                windLayer.Wind = new(windSine.Value * 100f, -300f);
            else
                windLayer.Wind = new(windSine.Value * 3f, 16f);

            windLayer.Color = Color.Lerp(Color.Transparent, activeColor, buttonSfxLerp * 0.5f + 0.5f);
        }
    }

    #region Hooks

    internal static void Load()
    {
        using (new DetourContext { After = { "*" } })
        {
            On.Celeste.Player.Jump += Player_Jump;
            On.Celeste.Player.WallJump += Player_WallJump;
            On.Celeste.Player.ClimbJump += Player_ClimbJump;
            On.Celeste.Player.SuperJump += Player_SuperJump;
            On.Celeste.Player.SuperWallJump += Player_SuperWallJump;
        }
    }

    internal static void Unload()
    {
        On.Celeste.Player.Jump -= Player_Jump;
        On.Celeste.Player.WallJump -= Player_WallJump;
        On.Celeste.Player.ClimbJump -= Player_ClimbJump;
        On.Celeste.Player.SuperJump -= Player_SuperJump;
        On.Celeste.Player.SuperWallJump -= Player_SuperWallJump;
    }

    private static void Player_Jump(On.Celeste.Player.orig_Jump orig, Player self, bool particles, bool playSfx)
        => SmashFirstTouchingAeroBlock(() => orig(self, particles, playSfx), self, Vector2.UnitY, block => self.OnGround() && block.CheckTopButton(), Vector2.UnitY * -350f, true);

    private static void Player_WallJump(On.Celeste.Player.orig_WallJump orig, Player self, int dir)
        => SmashFirstTouchingAeroBlock(() => orig(self, dir), self, -Vector2.UnitX * dir * 3f, block => dir < 0 ? block.CheckLeftButton() : block.CheckRightButton(), new Vector2(dir * 300f, -300f), true);

    private static void Player_ClimbJump(On.Celeste.Player.orig_ClimbJump orig, Player self)
        => SmashFirstTouchingAeroBlock(() => orig(self), self, Vector2.UnitX * (int) self.Facing * 3, block => self.Facing == Facings.Right ? block.CheckLeftButton() : block.CheckRightButton(), new Vector2((int) self.Facing * ((int) self.Facing == Math.Sign(Input.MoveX.Value) ? 300f : -300f), -300f), false);

    private static void Player_SuperJump(On.Celeste.Player.orig_SuperJump orig, Player self)
        => SmashFirstTouchingAeroBlock(() => orig(self), self, Vector2.UnitY, block => self.OnGround() && block is not null && block.CheckTopButton(), new Vector2(self.Speed.X * 1.2f, -350f), true);

    private static void Player_SuperWallJump(On.Celeste.Player.orig_SuperWallJump orig, Player self, int dir)
        => SmashFirstTouchingAeroBlock(() => orig(self, dir), self, -Vector2.UnitX * dir * 5f, block =>
        {
            Button button = dir < 0 ? block.leftButton : block.rightButton;
            // force a larger check distance if necessary
            if (block.wallbounceLeniency)
                button?.UpdatePressState(5f);
            
            return (button?.Pressed ?? false) && (block.listener?.Activated ?? true);
        }, new Vector2(300f * dir, -400f), true);

    private static void SmashFirstTouchingAeroBlock(Action callOrig, Player player, Vector2 checkOffset, Func<AeroBlockCharged, bool> smashCheck, Vector2 smashSpeed, bool spirialisAffected)
    {
        if (player.Scene.Tracker.GetEntities<AeroBlockCharged>().FirstOrDefault(b => player.CollideCheck(b, player.Position + checkOffset)) is not AeroBlockCharged block)
        {
            callOrig();
            return;
        }

        callOrig();

        if (smashCheck(block))
        {
            block.Smash(player, smashSpeed);

            if (block.SpirialisBug && spirialisAffected)
                player.varJumpSpeed = player.Speed.Y;
        }
    }

    #endregion
}
