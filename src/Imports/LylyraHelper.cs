using Celeste.Mod.CommunalHelper.Entities;
using Celeste.Mod.CommunalHelper.Entities.StrawberryJam;
using MonoMod.ModInterop;
using MonoMod.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.CommunalHelper.Imports;

[ModImportName("LylyraHelper")]
public static class LylyraHelper
{
    // Parameters are as follows:
    // - Type of entity to slice
    // - A function that gives the EntityData used to create the Entity in question
    // - Hex string of the color of the particles you want slicers to spawn upon block being cut, can be null (given as a string specifically so it can be nulled)
    // - The minimum width of your solid
    // - The minimum height of your solid 
    // - An audio path for a sound to be played upon your block breaking, can be null
    public static Action<Type, Func<Entity, DynamicData, EntityData>, string, int, int, string> RegisterSimpleSolidSlicerAction;

    // Allows you to register custom functions for your item in case RegisterSimpleSolidSlicerAction isn't enough
    // Quick explaination of basic functions:
    // - "activate": aka "secondframeslicing", activates post-Awake
    // - "postslice": called immediately after slicing an entity
    // - "getparticlecolor": a function describing what particle color to show (as a hex string)
    public static Action<Type, Dictionary<string, Delegate>> RegisterSlicerActionSet;
    public static Action<Type> UnregisterSlicerAction;

    // This method handles attached static movers (like spikes) for Solids. Convenience method.
    public static Action<DynamicData, Solid, Solid, List<StaticMover>> HandleStaticMovers;

    public static Func<Solid, DynamicData, Vector2[]> CalcNewBlockPosAndSize;
    public static Func<Entity, DynamicData> GetSlicer;

    public static void Initialize()
    {
        typeof(LylyraHelper).ModInterop();
        
        // audio paths have been left blank for the moment
        RegisterSimpleSolidSlicerAction?.Invoke(typeof(DreamFallingBlock), GetDreamBlockEntityData, "000000", 8, 8, null); // color chosen to be consistent with other dream block types
        RegisterSimpleSolidSlicerAction?.Invoke(typeof(DreamFloatySpaceBlock), GetDreamBlockEntityData, "000000", 8, 8, null); // color chosen to be consistent with other dream block types
        RegisterSimpleSolidSlicerAction?.Invoke(typeof(DreamMoveBlock), GetDreamBlockEntityData, "000000", 8, 8, null); // color chosen to be consistent with other dream block types
        RegisterSimpleSolidSlicerAction?.Invoke(typeof(LoopBlock), (data, _) => (data as LoopBlock)!.creatingData, null, 24, 24, null); // color is overridden via a custom function so it matches the loop block being cut
        RegisterSimpleSolidSlicerAction?.Invoke(typeof(Melvin), (data, _) => (data as Melvin)!.creationData, null, 24, 24, null); //  color is overridden via a custom function so it matches the melvin being cut

        // vanity function registration for LoopBlocks
        RegisterSlicerActionSet?.Invoke(typeof(LoopBlock), new Dictionary<string, Delegate>
        {
            { "getparticlecolor", (Entity e, DynamicData _) => (e as LoopBlock)!.color }
        });
        
        // these methods are needed to fix small things in DreamMoveBlock and DreamFallingBlock
        RegisterSlicerActionSet?.Invoke(typeof(DreamFallingBlock), new Dictionary<string, Delegate>
        {
            { "postslice", (Action<Entity, Entity, DynamicData>) PostSliceDreamFallingBlock }
        });
        RegisterSlicerActionSet?.Invoke(typeof(DreamMoveBlock), new Dictionary<string, Delegate>
        {
            { "activate", (Action<Entity, DynamicData>) ActivateDreamMoveBlock },
            { "postslice", (Action<Entity, Entity, DynamicData>) PostSliceDreamMoveBlock }
        });

        // melvin behavior is based on CrushBlock behavior
        RegisterSlicerActionSet?.Invoke(typeof(Melvin), new Dictionary<string, Delegate>
        {
            { "activate", (Action<Entity, DynamicData>) ActivateMelvin },
            { "postslice", (Action<Entity, Entity, DynamicData>) PostSliceMelvin },
            { "getparticlecolor", (Func<Entity, DynamicData, Color>) ParticleColorMelvin }
        });

        return;
        
        EntityData GetDreamBlockEntityData(Entity entity, DynamicData _)
            => (entity as CustomDreamBlock)!.creatingData;
        
        // this method activates the DreamFallingBlock after being sliced
        void PostSliceDreamFallingBlock(Entity created, Entity data, DynamicData slicer)
        {
            DreamFallingBlock block = (created as DreamFallingBlock)!;
            block.Triggered = true;
            block.FallDelay = 0;
        }
        
        // this method is called on Awake for each DreamMoveBlock that is generated, as activating them the frame they are added crashes the game
        void ActivateDreamMoveBlock(Entity entity, DynamicData slicer)
        {
            if (entity is null)
                return;

            DreamMoveBlock block = (entity as DreamMoveBlock)!;
            block.Get<Coroutine>().enumerators.Peek().MoveNext();
            block.triggered = true;
        }
        // the start position technically changes after slicing, so this method fixes that.
        void PostSliceDreamMoveBlock(Entity created, Entity entity, DynamicData slicer)
        {
            bool vertical = slicer.Get<Vector2>("Direction").Y != 0;
            DreamMoveBlock original = (entity as DreamMoveBlock)!;
            DreamMoveBlock block = (created as DreamMoveBlock)!;
            block.startPosition = vertical
                ? new Vector2(block.X, original.startPosition.Y + block.Y - original.Position.Y)
                : new Vector2(original.startPosition.X + block.X - original.Position.X, block.Y);
        }
        
        void ActivateMelvin(Entity created, DynamicData slicer)
        {
            Melvin block = (created as Melvin)!;
            block.crushDir = -slicer.Get<Vector2>("Direction");
            block.Attack(true); // a slicer hitting it counts as a dash right?
        }
        // reset the melvin return stacks so it can return to where it was
        void PostSliceMelvin(Entity created, Entity entity, DynamicData slicer)
        {
            Melvin original = (entity as Melvin)!;
            Melvin block = (created as Melvin)!;

            Vector2 offset = block.Position - original.Position;
            List<Melvin.MoveState> returnStack = original.returnStack;
            List<Melvin.MoveState> newReturnStack = block.returnStack;
            newReturnStack.Clear();
            newReturnStack.AddRange(returnStack.Select(state => new Melvin.MoveState(state.From + offset, state.Direction)));
        }
        Color ParticleColorMelvin(Entity created, DynamicData _)
            => (created as Melvin)!.fill;
    }
}
