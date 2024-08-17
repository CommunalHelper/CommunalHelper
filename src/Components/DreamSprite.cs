using Celeste.Mod.CommunalHelper.Entities;
using Celeste.Mod.CommunalHelper.Imports;
using Microsoft.Xna.Framework.Graphics;

namespace Celeste.Mod.CommunalHelper.Components;

/// <summary>
/// Component to render a Sprite as a mask onto a dream particle background, as seen in DreamJellyfish. <br/>
/// You probably want to set the Sprite you give to this component to be invisible, as it is only used for the mask images. <br/>
/// For the effect to work properly, sprites must be 128x128 (<em>technically</em> they don't have to be but this is the size that works no matter your particleBounds), but the region containing all the mask pixels must be no larger than 64x64. <br/>
/// The mask image will be drawn centered on the center of the particleBounds by default and can be offset using maskOffset. <br/>
/// As such, the particleBounds must be at least as large as the region containing all the mask pixels. <br/>
/// The particleBounds are relative to the entity's Position and the current system supports particleBounds up to 64x64 in size. <br/>
/// The outline sprites are also relative to the entity's Position, and can be offset relative to the entity via outlineOffset. <br/>
/// </summary>
public class DreamSprite : Component
{
    private readonly Rectangle particleBounds;

    private readonly Sprite sprite;

    // Could maybe use CustomDreamBlock.DreamParticle.
    public struct DreamParticle
    {
        public Vector2 Position;
        public int Layer;
        public Color EnabledColor, DisabledColor;
        public float TimeOffset;
    }
    public DreamParticle[] Particles { get; private set; }
    public static MTexture[] ParticleTextures { get; private set; }

    public static DreamSpriteColorController controller;

    public static Color LineColor => controller?.LineColor ?? Color.White;
    public static Color BackColor => controller?.BackColor ?? Color.Black;
    public static Color[] DreamColors => controller?.DreamColors ?? CustomDreamBlock.DreamColors;

    public float Flash;

    private readonly bool? gravityNaive;
    private bool GravityNaive => gravityNaive ?? Entity is not Actor;

    public bool DreamEnabled;

    private readonly Vector2 outlineOffset;
    private readonly Vector2 maskOffset;

    private readonly int invertedSpriteYOffset;

    private static VirtualRenderTarget renderTarget;
    private readonly BlendState alphaMaskBlendState = new()
    {
        ColorSourceBlend = Blend.Zero,
        ColorBlendFunction = BlendFunction.Add,
        ColorDestinationBlend = Blend.SourceAlpha,
        AlphaSourceBlend = Blend.One,
        AlphaBlendFunction = BlendFunction.Add,
        AlphaDestinationBlend = Blend.Zero,
    };

    private float animTimer;

    public DreamSprite(Sprite sprite, Rectangle particleBounds, Vector2? outlineOffset = null, Vector2? maskOffset = null, int invertedSpriteYOffset = 0, bool? gravityNaive = null, bool dreamEnabled = true) : base(true, true)
    {
        this.sprite = sprite;
        this.particleBounds = particleBounds;
        this.gravityNaive = gravityNaive;
        DreamEnabled = dreamEnabled;
        this.outlineOffset = outlineOffset ?? Vector2.Zero;
        this.maskOffset = maskOffset ?? Vector2.Zero;
        this.invertedSpriteYOffset = invertedSpriteYOffset;
    }

    public override void EntityAwake()
    {
        controller ??= SceneAs<Level>().Tracker.GetEntity<DreamSpriteColorController>();

        int w = particleBounds.Width;
        int h = particleBounds.Height;
        Particles = new DreamParticle[(int) (w / 8f * (h / 8f) * 1.5f)];
        for (int i = 0; i < Particles.Length; i++)
        {
            Particles[i].Position = new Vector2(Calc.Random.NextFloat(w), Calc.Random.NextFloat(h));
            Particles[i].Layer = Calc.Random.Choose(0, 1, 1, 2, 2, 2);
            Particles[i].TimeOffset = Calc.Random.NextFloat();

            Particles[i].DisabledColor = Color.LightGray * (0.5f + (Particles[i].Layer / 2f * 0.5f));
            Particles[i].DisabledColor.A = 255;

            Particles[i].EnabledColor = Particles[i].Layer switch
            {
                0 => Calc.Random.Choose(DreamColors[0], DreamColors[1], DreamColors[2]),
                1 => Calc.Random.Choose(DreamColors[3], DreamColors[4], DreamColors[5]),
                2 => Calc.Random.Choose(DreamColors[6], DreamColors[7], DreamColors[8]),
                _ => throw new NotImplementedException()
            };
        }
    }

    public override void Update()
    {
        base.Update();

        controller ??= SceneAs<Level>().Tracker.GetEntity<DreamSpriteColorController>();

        animTimer += 6f * Engine.DeltaTime;
        Flash = Calc.Approach(Flash, 0f, Engine.DeltaTime * 2.5f);
    }

    public override void Render()
    {
        if (renderTarget is null || renderTarget.IsDisposed)
        {
            renderTarget = VirtualContent.CreateRenderTarget("CommunalHelper/dreamSpriteRenderer", 64, 64);
        }

        Camera camera = SceneAs<Level>().Camera;
        Vector2 pos = Entity.Position + new Vector2(particleBounds.X, particleBounds.Y);

        float left = pos.X;
        float right = pos.X + particleBounds.Width;
        float top = pos.Y;
        float bottom = pos.Y + particleBounds.Height;
        if (right < camera.Left || left > camera.Right || bottom < camera.Top || top > camera.Bottom)
            return; // Skip rendering if it's not on screen.

        MTexture frame = sprite.Texture;
        Vector2 scale = sprite.Scale;
        float rotation = sprite.Rotation;

        bool inverted = !GravityNaive && (Entity as Actor).GetGravity() == GravityType.Inverted;
        int yOffset = inverted ? invertedSpriteYOffset : 0;

        // Outline
        Color lineColor = Color.Lerp(LineColor, Color.White, Flash);
        frame.DrawCentered(Entity.Position + new Vector2(0, yOffset + 1) + outlineOffset, lineColor, scale, rotation);
        frame.DrawCentered(Entity.Position + new Vector2(0, yOffset - 1) + outlineOffset, lineColor, scale, rotation);
        frame.DrawCentered(Entity.Position + new Vector2(-1, yOffset) + outlineOffset, lineColor, scale, rotation);
        frame.DrawCentered(Entity.Position + new Vector2(1, yOffset) + outlineOffset, lineColor, scale, rotation);

        GameplayRenderer.End();
        // Here we start drawing on a virtual texture.
        Engine.Graphics.GraphicsDevice.SetRenderTarget(renderTarget);
        Engine.Graphics.GraphicsDevice.Clear(Color.Lerp(BackColor, Color.White, Flash));

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, ColorGrade.Effect);
        for (int i = 0; i < Particles.Length; i++)
        {
            int layer = Particles[i].Layer;

            Vector2 particlePos = Particles[i].Position;
            particlePos += camera.Position * (0.3f + (0.25f * layer));
            while (particlePos.X < left)
            {
                particlePos.X += particleBounds.Width;
            }
            while (particlePos.X > right)
            {
                particlePos.X -= particleBounds.Width;
            }
            while (particlePos.Y < top)
            {
                particlePos.Y += particleBounds.Height;
            }
            while (particlePos.Y > bottom)
            {
                particlePos.Y -= particleBounds.Height;
            }

            Color color = Color.Lerp(DreamEnabled ? Particles[i].EnabledColor : Particles[i].DisabledColor, Color.White, Flash);
            MTexture mTexture;
            switch (layer)
            {
                case 0:
                {
                    int num2 = (int) (((Particles[i].TimeOffset * 4f) + animTimer) % 4f);
                    mTexture = ParticleTextures[3 - num2];
                    break;
                }
                case 1:
                {
                    int num = (int) (((Particles[i].TimeOffset * 2f) + animTimer) % 2f);
                    mTexture = ParticleTextures[1 + num];
                    break;
                }
                default:
                    mTexture = ParticleTextures[2];
                    break;
            }
            mTexture.DrawCentered(particlePos - pos, color);
        }
        Draw.SpriteBatch.End();

        // We have drawn the dream block background and the stars, and we want to mask it using an alpha mask.
        // The alpha masks are the same images that Gliders have, only with alpha information, no color, so they overlap the region in which we want the background to be visible.

        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, alphaMaskBlendState, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, ColorGrade.Effect);
        frame.DrawCentered(
            maskOffset + (new Vector2(particleBounds.Width, particleBounds.Height) / 2f),
            Color.White,
            scale,
            rotation
        );
        Draw.SpriteBatch.End();

        // Mask is drawn, we'll switch back to where the game was drawing entities previously.
        Engine.Graphics.GraphicsDevice.SetRenderTarget(GameplayBuffers.Gameplay);
        GameplayRenderer.Begin();
        // Draw Virtual Texture
        pos.Y += yOffset;
        Draw.SpriteBatch.Draw(renderTarget, pos, Color.White);
    }

    public override void Removed(Entity entity)
    {
        Dispose();
        base.Removed(entity);
    }

    public override void EntityRemoved(Scene scene)
    {
        Dispose();
        base.EntityRemoved(scene);
    }

    public override void SceneEnd(Scene scene)
    {
        Dispose();
        base.SceneEnd(scene);
    }

    private static void Dispose()
    {
        controller = null;
        renderTarget?.Dispose();
        renderTarget = null;
    }

    public static void InitializeTextures()
    {
        ParticleTextures = new MTexture[4] {
            GFX.Game["objects/dreamblock/particles"].GetSubtexture(14, 0, 7, 7),
            GFX.Game["objects/dreamblock/particles"].GetSubtexture(7, 0, 7, 7),
            GFX.Game["objects/dreamblock/particles"].GetSubtexture(0, 0, 7, 7),
            GFX.Game["objects/dreamblock/particles"].GetSubtexture(7, 0, 7, 7),
        };
    }

    #region Hooks

    public static void Load()
    {
        // Stop outline rendering on invisible sprites used by DreamSprites (relevant for DreamJellyfish)
        On.Monocle.GraphicsComponent.DrawSimpleOutline += GraphicsComponent_DrawSimpleOutline;
        On.Monocle.GraphicsComponent.DrawOutline_int += GraphicsComponent_DrawOutline_int;
        On.Monocle.GraphicsComponent.DrawOutline_Color_int += GraphicsComponent_DrawOutline_Color_int;
    }

    public static void Unload()
    {
        On.Monocle.GraphicsComponent.DrawSimpleOutline -= GraphicsComponent_DrawSimpleOutline;
        On.Monocle.GraphicsComponent.DrawOutline_int -= GraphicsComponent_DrawOutline_int;
        On.Monocle.GraphicsComponent.DrawOutline_Color_int -= GraphicsComponent_DrawOutline_Color_int;
    }

    private static void GraphicsComponent_DrawSimpleOutline(On.Monocle.GraphicsComponent.orig_DrawSimpleOutline orig, GraphicsComponent self)
    {
        if (self.Entity?.Get<DreamSprite>() is DreamSprite dreamSprite && dreamSprite.sprite == self)
        {
            if (self.Visible)
            {
                orig(self);
            }
        }
        else
        {
            orig(self);
        }
    }

    private static void GraphicsComponent_DrawOutline_int(On.Monocle.GraphicsComponent.orig_DrawOutline_int orig, GraphicsComponent self, int offset)
    {
        if (self.Entity?.Get<DreamSprite>() is DreamSprite dreamSprite && dreamSprite.sprite == self)
        {
            if (self.Visible)
            {
                orig(self, offset);
            }
        }
        else
        {
            orig(self, offset);
        }
    }
    private static void GraphicsComponent_DrawOutline_Color_int(On.Monocle.GraphicsComponent.orig_DrawOutline_Color_int orig, GraphicsComponent self, Color color, int offset)
    {
        if (self.Entity?.Get<DreamSprite>() is DreamSprite dreamSprite && dreamSprite.sprite == self)
        {
            if (self.Visible)
            {
                orig(self, color, offset);
            }
        }
        else
        {
            orig(self, color, offset);
        }
    }

    #endregion
}