using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CommunalHelper.Entities
{
    [CustomEntity("CommunalHelper/PortalZipMover")]
    class PortalZipMover : SlicedSolid {

        public PortalZipMover(EntityData data, Vector2 offset) 
            : this(data.Position + offset, data.Width, data.Height)
        { }

        public PortalZipMover(Vector2 position, int width, int height)
            : base(position, width, height, safe: false)
        {
        }

        public override void Update()
        {
            base.Update();
            Move(new Vector2(80, 0) * Engine.DeltaTime);
        }
    }
}
