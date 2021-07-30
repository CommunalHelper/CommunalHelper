using Microsoft.Xna.Framework;

namespace Celeste.Mod.CommunalHelper.Entities {

    // Could maybe use CustomDreamBlock.DreamParticle.
    public struct DreamParticle {
        public Vector2 Position;
        public int Layer;
        public Color EnabledColor, DisabledColor;
        public float TimeOffset;
    }
}
