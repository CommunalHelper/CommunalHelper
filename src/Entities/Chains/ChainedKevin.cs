using MonoMod.Utils;
using System.Collections.Generic;
using Directions = Celeste.MoveBlock.Directions;

namespace Celeste.Mod.CommunalHelper.Entities;

[CustomEntity("CommunalHelper/ChainedKevin")]
internal class ChainedKevin : CrushBlock
{
    private Directions direction;
    private Vector2 vectorDirection;
    private Vector2 start;

    private readonly int chainLength;
    private bool centeredChain;
    private readonly bool chainOutline;
    private readonly MTexture chainTexture;

    private DynamicData crushBlockData;
    private List<Image> idleImages, activeTopImages, activeRightImages, activeLeftImages, activeBottomImages;

    public ChainedKevin(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        start = Position;
        chainLength = data.Int("chainLength", 64);
        chainOutline = data.Bool("chainOutline", true);
        if (((direction == Directions.Up || direction == Directions.Down) && Width <= 8) ||
            ((direction == Directions.Left || direction == Directions.Right) && Height <= 8))
            centeredChain = true;

        string chainTexturePath = data.Attr("chainTexture", Chain.DEFAULT_CHAIN_PATH);
        chainTexture = GFX.Game.GetOrDefault(chainTexturePath, Chain.DefaultChain);
    }

    public override void Render()
    {
        switch (direction)
        {
            case Directions.Down:
                if (centeredChain)
                    Chain.DrawChainLine(new Vector2(Left + (Width / 2f), start.Y), new Vector2(X + (Width / 2f), Top), chainTexture, chainOutline);
                else
                {
                    Chain.DrawChainLine(new Vector2(Left + 3, start.Y), new Vector2(X + 3, Y), chainTexture, chainOutline);
                    Chain.DrawChainLine(new Vector2(Left + Width - 4, start.Y), new Vector2(X + Width - 4, Top), chainTexture, chainOutline);
                }
                break;

            case Directions.Up:
                if (centeredChain)
                    Chain.DrawChainLine(new Vector2(Left + (Width / 2f), start.Y + Height), new Vector2(X + (Width / 2f), Bottom), chainTexture, chainOutline);
                else
                {
                    Chain.DrawChainLine(new Vector2(Left + 3, start.Y + Height), new Vector2(X + 3, Bottom), chainTexture, chainOutline);
                    Chain.DrawChainLine(new Vector2(Left + Width - 4, start.Y + Height), new Vector2(X + Width - 4, Bottom), chainTexture, chainOutline);
                }
                break;

            case Directions.Right:
                if (centeredChain)
                    Chain.DrawChainLine(new Vector2(start.X, Top + (Height / 2f)), new Vector2(Left, Top + (Height / 2f)), chainTexture, chainOutline);
                else
                {
                    Chain.DrawChainLine(new Vector2(start.X, Top + 5), new Vector2(Left, Top + 5), chainTexture, chainOutline);
                    Chain.DrawChainLine(new Vector2(start.X, Top + Height - 4), new Vector2(Left, Top + Height - 4), chainTexture, chainOutline);
                }
                break;

            case Directions.Left:
                if (centeredChain)
                    Chain.DrawChainLine(new Vector2(start.X + Width, Top + (Height / 2f)), new Vector2(Right, Top + (Height / 2f)), chainTexture, chainOutline);
                else
                {
                    Chain.DrawChainLine(new Vector2(start.X + Width, Top + 5), new Vector2(Right, Top + 5), chainTexture, chainOutline);
                    Chain.DrawChainLine(new Vector2(start.X + Width, Top + Height - 4), new Vector2(Right, Top + Height - 4), chainTexture, chainOutline);
                }
                break;
        }

        base.Render();
    }

    #region Hooks

    internal static void Load()
    {
        On.Celeste.CrushBlock.ctor_EntityData_Vector2 += CrushBlock_ctor_EntityData_Vector2;
        On.Celeste.CrushBlock.ctor_Vector2_float_float_Axes_bool += CrushBlock_ctor_Vector2_float_float_Axes_bool;
        On.Celeste.CrushBlock.AddImage += CrushBlock_AddImage;
        On.Celeste.CrushBlock.CanActivate += CrushBlock_CanActivate;
        On.Celeste.CrushBlock.MoveHCheck += CrushBlock_MoveHCheck;
        On.Celeste.CrushBlock.MoveVCheck += CrushBlock_MoveVCheck;
    }

    internal static void Unload()
    {
        On.Celeste.CrushBlock.ctor_EntityData_Vector2 -= CrushBlock_ctor_EntityData_Vector2;
        On.Celeste.CrushBlock.ctor_Vector2_float_float_Axes_bool -= CrushBlock_ctor_Vector2_float_float_Axes_bool;
        On.Celeste.CrushBlock.AddImage -= CrushBlock_AddImage;
        On.Celeste.CrushBlock.CanActivate -= CrushBlock_CanActivate;
        On.Celeste.CrushBlock.MoveHCheck -= CrushBlock_MoveHCheck;
        On.Celeste.CrushBlock.MoveVCheck -= CrushBlock_MoveVCheck;
    }

    private static bool CrushBlock_MoveVCheck(On.Celeste.CrushBlock.orig_MoveVCheck orig, CrushBlock self, float amount)
    {
        float target = self.Position.Y + amount;

        bool chainStop = false;
        if (self is ChainedKevin chainedKevin)
        {
            if (chainedKevin.direction == Directions.Up && target <= chainedKevin.start.Y - chainedKevin.chainLength)
            {
                chainStop = true;
                amount = chainedKevin.start.Y - chainedKevin.chainLength - self.Y;
            }
            else if (chainedKevin.direction == Directions.Down && target >= chainedKevin.start.Y + chainedKevin.chainLength)
            {
                chainStop = true;
                amount = chainedKevin.start.Y + chainedKevin.chainLength - self.Y;
            }

            if (chainStop)
            {
                Audio.Play(CustomSFX.game_chainedFallingBlock_chain_tighten_block, chainedKevin.Center);
            }

            return self.MoveVCollideSolidsAndBounds(self.SceneAs<Level>(), amount, thruDashBlocks: true, null, checkBottom: false) || chainStop;
        }
        else
            return orig(self, amount);
    }

    private static bool CrushBlock_MoveHCheck(On.Celeste.CrushBlock.orig_MoveHCheck orig, CrushBlock self, float amount)
    {
        float target = self.Position.X + amount;

        bool chainStop = false;
        if (self is ChainedKevin chainedKevin)
        {
            if (chainedKevin.direction == Directions.Left && target <= chainedKevin.start.X - chainedKevin.chainLength)
            {
                chainStop = true;
                amount = chainedKevin.start.X - chainedKevin.chainLength - self.X;
            }
            else if (chainedKevin.direction == Directions.Right && target >= chainedKevin.start.X + chainedKevin.chainLength)
            {
                chainStop = true;
                amount = chainedKevin.start.X + chainedKevin.chainLength - self.X;
            }

            if (chainStop)
            {
                Audio.Play(CustomSFX.game_chainedFallingBlock_chain_tighten_block, chainedKevin.Center);
            }

            return self.MoveHCollideSolidsAndBounds(self.SceneAs<Level>(), amount, thruDashBlocks: true) || chainStop;
        }
        else
            return orig(self, amount);
    }

    private static bool CrushBlock_CanActivate(On.Celeste.CrushBlock.orig_CanActivate orig, CrushBlock self, Vector2 direction)
    {
        bool result = orig(self, direction);
        return self is ChainedKevin chainedKevin && result ? chainedKevin.vectorDirection == direction : result;
    }

    private static void CrushBlock_ctor_EntityData_Vector2(On.Celeste.CrushBlock.orig_ctor_EntityData_Vector2 orig, CrushBlock self, EntityData data, Vector2 offset)
    {
        if (self is ChainedKevin chainedKevin)
        {
            chainedKevin.crushBlockData = new(typeof(CrushBlock), chainedKevin);
            chainedKevin.vectorDirection = (chainedKevin.direction = data.Enum("direction", Directions.Right)).Vector();
            chainedKevin.centeredChain = data.Bool("centeredChain");
        }
        orig(self, data, offset);
    }

    private static void CrushBlock_ctor_Vector2_float_float_Axes_bool(On.Celeste.CrushBlock.orig_ctor_Vector2_float_float_Axes_bool orig, CrushBlock self, Vector2 position, float width, float height, Axes axes, bool chillOut)
    {
        if (self is ChainedKevin chainedKevin)
        {
            if (chainedKevin.direction is Directions.Down or Directions.Up)
                axes = Axes.Vertical;
            else if (chainedKevin.direction is Directions.Left or Directions.Right)
                axes = Axes.Horizontal;
        }
        orig(self, position, width, height, axes, chillOut);
    }

    private static void CrushBlock_AddImage(On.Celeste.CrushBlock.orig_AddImage orig, CrushBlock self, MTexture idle, int x, int y, int tx, int ty, int borderX, int borderY)
    {
        if (self is ChainedKevin chainedKevin)
        {
            if (chainedKevin.idleImages == null)
            {
                chainedKevin.idleImages = chainedKevin.crushBlockData.Get<List<Image>>("idleImages");
                chainedKevin.activeTopImages = chainedKevin.crushBlockData.Get<List<Image>>("activeTopImages");
                chainedKevin.activeRightImages = chainedKevin.crushBlockData.Get<List<Image>>("activeRightImages");
                chainedKevin.activeLeftImages = chainedKevin.crushBlockData.Get<List<Image>>("activeLeftImages");
                chainedKevin.activeBottomImages = chainedKevin.crushBlockData.Get<List<Image>>("activeBottomImages");
            }

            MTexture subtexture = GFX.Game["objects/CommunalHelper/chainedKevin/block" + chainedKevin.direction].GetSubtexture(tx * 8, ty * 8, 8, 8);
            Vector2 vector = new(x * 8, y * 8);

            if (borderX != 0)
            {
                Image image = new(subtexture)
                {
                    Color = Color.Black,
                    Position = vector + new Vector2(borderX, 0f)
                };
                self.Add(image);
            }
            if (borderY != 0)
            {
                Image image = new(subtexture)
                {
                    Color = Color.Black,
                    Position = vector + new Vector2(0f, borderY)
                };
                self.Add(image);
            }

            Image image3 = new(subtexture)
            {
                Position = vector
            };
            self.Add(image3);

            chainedKevin.idleImages.Add(image3);

            if (borderX != 0 || borderY != 0)
            {
                if (borderX < 0 && chainedKevin.direction == Directions.Left)
                {
                    Image image = new(GFX.Game["objects/crushblock/lit_left"].GetSubtexture(0, ty * 8, 8, 8))
                    {
                        Position = vector,
                        Visible = false
                    };
                    chainedKevin.activeLeftImages.Add(image);
                    self.Add(image);
                }
                else if (borderX > 0 && chainedKevin.direction == Directions.Right)
                {
                    Image image = new(GFX.Game["objects/crushblock/lit_right"].GetSubtexture(0, ty * 8, 8, 8))
                    {
                        Position = vector,
                        Visible = false
                    };
                    chainedKevin.activeRightImages.Add(image);
                    self.Add(image);
                }
                if (borderY < 0 && chainedKevin.direction == Directions.Up)
                {
                    Image image = new(GFX.Game["objects/crushblock/lit_top"].GetSubtexture(tx * 8, 0, 8, 8))
                    {
                        Position = vector,
                        Visible = false
                    };
                    chainedKevin.activeTopImages.Add(image);
                    self.Add(image);
                }
                else if (borderY > 0 && chainedKevin.direction == Directions.Down)
                {
                    Image image = new(GFX.Game["objects/crushblock/lit_bottom"].GetSubtexture(tx * 8, 0, 8, 8))
                    {
                        Position = vector,
                        Visible = false
                    };
                    chainedKevin.activeBottomImages.Add(image);
                    self.Add(image);
                }
            }
        }
        else
        {
            orig(self, idle, x, y, tx, ty, borderX, borderY);
        }
    }

    #endregion
}
