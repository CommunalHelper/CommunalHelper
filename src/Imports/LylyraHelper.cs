using Celeste.Mod.CommunalHelper.Entities;
using Celeste.Mod.CommunalHelper.Entities.StrawberryJam;
using MonoMod.ModInterop;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Celeste.Mod.CommunalHelper.Entities.StrawberryJam.SolarElevator;
using static On.Celeste.Pico8.Emulator;

namespace Celeste.Mod.CommunalHelper.Imports;

[ModImportName("LylyraHelper")]
public static class LylyraHelper
{
    //parameters are as follows:
    //Type of entity to slice
    //A function that gives the EntityData used to create the Entity in question
    //hex string of the color of the particles you want slicers to spawn upon block being cut, can be null (given as a string specifically so it can be nulled)
    //the minimum width of your solid
    //the minimum height of your solid 
    //an audio path for a sound to be played upon your block breaking, can be null
    public static Action<Type, Func<Entity, DynamicData, EntityData>, string, int, int, string> RegisterSimpleSolidSlicerAction;

    //Allows you to register custom functions for your item incase RegisterSimpleSolidSlicerAction isn't enough
    //Quick explaination of basic functions:
    //activate: aka "SecondFrameSlicing", activates post awake
    //postSlice: called immediately after slicing an entity
    //getparticlecolor: a function describing what particle color to show (as a hex string)
    public static Action<Type, Dictionary<string, Delegate>> RegisterSlicerActionSet;
    public static Action<Type> UnregisterSlicerAction;

    //this method handles attached static movers (like spikes) for Solids. Convenience Method.
    public static Action<DynamicData, Solid, Solid, List<StaticMover>> HandleStaticMovers;

    public static Func<Solid, DynamicData, Vector2[]> CalcNewBlockPosAndSize;
    public static Func<Entity, DynamicData> GetSlicer;

    public static void Load()
    {
        Func<Entity, DynamicData, EntityData> GetDreamEntityData = (Entity data, DynamicData slicer) => {
            return (data as CustomDreamBlock).creatingData;
        };

        //audio paths have been left blank for the moment
        RegisterSimpleSolidSlicerAction(typeof(DreamFallingBlock), GetDreamEntityData, "000000", 8, 8, null); //color chosen to be consistent with other dream block types
        RegisterSimpleSolidSlicerAction(typeof(DreamFloatySpaceBlock), GetDreamEntityData, "000000", 8, 8, null);
        RegisterSimpleSolidSlicerAction(typeof(DreamMoveBlock), GetDreamEntityData, "000000", 8, 8, null);
        RegisterSimpleSolidSlicerAction(typeof(LoopBlock), (Entity data, DynamicData slicer) => {return (data as LoopBlock).creatingData;}, null, 24, 24, null); //color is overridden via a custom function so it matches the loop block being cut
        RegisterSimpleSolidSlicerAction(typeof(Melvin), (Entity data, DynamicData slicer) => { return (data as Melvin).creationData; }, "62222b", 24, 24, null); // color taken from fillColor in Melvin.cs

        //vanity function registration for LoopBlocks
        Dictionary<string, Delegate> loopBlockDict = new() {
            {"getparticlecolor",  
                (Entity e, DynamicData y) =>
                {
                    return (e as LoopBlock).color;
                }
            }
        };
        RegisterSlicerActionSet(typeof(DreamFallingBlock), loopBlockDict);
        //these methods are needed to fix small things in the DreamMoveBlocks and DreamFallingBlocks

        //This method activates the DreamFallingBlock after being sliced
        Action<Entity, Entity, DynamicData> PostSlice = (Entity created, Entity data, DynamicData slicer) => {
            DreamFallingBlock block = created as DreamFallingBlock;
            block.Triggered = true;
            block.FallDelay = 0;
        };
        Dictionary<string, Delegate> fallingDreamBlockDic = new Dictionary<string, Delegate>()
        {
            { "postslice", PostSlice }
        };
        RegisterSlicerActionSet(typeof(DreamFallingBlock), fallingDreamBlockDic);

        //the start position technically changes after slicing, so this method fixes that.
        Action<Entity, Entity, DynamicData> PostSliceMoveBlock = (Entity created, Entity entity, DynamicData slicer) => {
            bool vertical = slicer.Get<Vector2>("Direction").Y != 0;
            DreamMoveBlock original = entity as DreamMoveBlock;
            DreamMoveBlock block = created as DreamMoveBlock;
            block.startPosition = vertical ? new Vector2(block.X, original.startPosition.Y + block.Y - original.Position.Y) : new Vector2(original.startPosition.X + block.X - original.Position.X, block.Y);
        };
        //this method is called on each DreamMoveBlock that is generated the frame after (at Awake()), as activating the frame of adding crashes the game
        Action<Entity, DynamicData> activate = (Entity entity, DynamicData slicer) => {
            if (entity != null)
            {

                DreamMoveBlock block = entity as DreamMoveBlock;
                block.Get<Coroutine>().enumerators.Peek().MoveNext();
                block.triggered = true;
            }
        };

        Dictionary<string, Delegate> dmbDict = new Dictionary<string, Delegate>()
        {
            { "activate", activate },
            { "postslice", PostSliceMoveBlock}
        };

        RegisterSlicerActionSet(typeof(DreamMoveBlock), dmbDict);

        //melvin behavior is based on CrushBlock behavior
        //reset the melvin return stacks so it can return to where it was
        Action<Entity, Entity, DynamicData> melvinReturn = (Entity created, Entity entity, DynamicData slicer) => {
            bool vertical = slicer.Get<Vector2>("Direction").Y != 0;
            Melvin original = entity as Melvin;
            Melvin block = created as Melvin;

            var returnStack = original.returnStack;
            Vector2 offset = block.Position - original.Position;
            List<Melvin.MoveState> newReturnStack = block.returnStack;
            newReturnStack.Clear();
            foreach (Melvin.MoveState state in returnStack)
            {
                newReturnStack.Add(new Melvin.MoveState(state.From + offset, state.Direction));
            };
        };
        //

        Action<Entity, DynamicData> melvinActivate = (Entity created, DynamicData slicer) => {
            (created as Melvin).crushDir = -slicer.Get<Vector2>("Direction");
            (created as Melvin).Attack(true); //slicer hit it counts as a dash right?
        };
        Dictionary<string, Delegate> melvinDict = new Dictionary<string, Delegate>()
        {
            { "activate", melvinActivate },
            { "postslice", melvinReturn}
        };

        RegisterSlicerActionSet(typeof(Melvin), melvinDict);
    }
}
