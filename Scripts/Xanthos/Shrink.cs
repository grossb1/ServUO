using Server;
using Server.Commands;
using Server.Mobiles;

namespace Xanthos.Interfaces
{
    /// <summary>
    /// This interface is implemented by clients of ShrinkTarget allowing the ShrinkTarget
    /// to adjust the charges of tools without requiring they have the same base class.
    /// </summary>
    public interface IShrinkTool : IEntity
    {
        bool DeleteWhenEmpty { get; set; }
        int ShrinkCharges { get; set; }
    }

    /// <summary>
    /// Used by the auction system to validate the pet referred to by a shrink item.
    /// </summary>
    public interface IShrinkItem : IEntity
    {
        BaseCreature ShrunkenPet { get; }
    }
}

namespace Xanthos.ShrinkSystem
{
    public class Shrink
    {
        public static bool LockDown { get; set; }

        public static void Configure()
        {
            EventSink.WorldSave += e => Persistence.Serialize("Saves/Misc/ShrinkCommands.bin", Serialize);
            EventSink.WorldLoad += () => Persistence.Deserialize("Saves/Misc/ShrinkCommands.bin", Deserialize);
        }

        private static void Serialize(GenericWriter writer)
        {
            writer.Write(0);

            writer.Write(LockDown);
        }

        private static void Deserialize(GenericReader reader)
        {
            reader.ReadInt();

            LockDown = reader.ReadBool();
        }

        public static void Initialize()
        {
            CommandHandlers.Register("Shrink", AccessLevel.GameMaster, Shrink_OnCommand);
            CommandHandlers.Register("ShrinkLockDown", AccessLevel.Administrator, ShrinkLockDown_OnCommand);
            CommandHandlers.Register("ShrinkRelease", AccessLevel.Administrator, ShrinkRelease_OnCommand);
        }

        [Usage("Shrink")]
        [Description("Shrinks a creature.")]
        private static void Shrink_OnCommand(CommandEventArgs e)
        {
            ShrinkTarget.Begin(e.Mobile, null);
        }

        [Usage("ShrinkLockDown")]
        [Description("Disables all shrinkitems in the world.")]
        private static void ShrinkLockDown_OnCommand(CommandEventArgs e)
        {
            SetLockDown(true);
        }

        [Usage("ShrinkRelease")]
        [Description("Re-enables all disabled shrink items in the world.")]
        private static void ShrinkRelease_OnCommand(CommandEventArgs e)
        {
            SetLockDown(false);
        }

        private static void SetLockDown(bool lockDown)
        {
            LockDown = lockDown;

            if (LockDown)
            {
                World.Broadcast(0x22, true, "A server wide pet shrink lockout has initiated.");
                World.Broadcast(0x22, true, "All shrunken pets will remain in their state until further notice.");
            }
            else
            {
                World.Broadcast(0x35, true, "The server wide pet shrink lockout has been lifted.");
                World.Broadcast(0x35, true, "You may once again unshrink pets.");
            }
        }
    }
}
