using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.CommunalHelper.Entities
{
    [CustomEntity("CommunalHelper/PortalZipMover")]
    class PortalZipMover : SlicedSolid
    {
        public PortalZipMover(EntityData data, Vector2 offset) 
            : this(data.Position + offset, data.Width, data.Height)
        { }

        public PortalZipMover(Vector2 position, int width, int height)
            : base(position, width, height, safe: false)
        { }

        public override void Update()
        {
            float speed = 20f * Engine.DeltaTime;
            Vector2 move = Vector2.Zero;
            if (Input.MenuUp.Check) move.Y -= speed;
            if (Input.MenuDown.Check) move.Y += speed;
            if (Input.MenuLeft.Check) move.X -= speed;
            if (Input.MenuRight.Check) move.X += speed;

            MoveTransformed(move);
            base.Update();
        }

    }
}
