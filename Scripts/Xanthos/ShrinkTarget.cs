using Server;
using Server.Mobiles;
using Server.Regions;
using Server.Spells;
using System.Linq;
using Xanthos.Interfaces;

namespace Xanthos.ShrinkSystem
{
    public static class ShrinkTarget
    {
        public static void Begin(Mobile from, IShrinkTool tool)
        {
            var staff = from.AccessLevel >= AccessLevel.GameMaster;

            if (!staff)
            {
                if (tool != null)
                {
                    if (from.Skills[SkillName.AnimalTaming].Value < ShrinkConfig.TamingRequired)
                    {
                        from.SendMessage("You must have at least " + ShrinkConfig.TamingRequired + " animal taming to use that.");
                        return;
                    }

                    if (tool.ShrinkCharges == 0)
                    {
                        if (tool.DeleteWhenEmpty)
                            tool.Delete();
                        else
                            from.SendMessage("That does not have enough charges remaining to shrink a pet.");

                        return;
                    }
                }
            }

            from.SendMessage("Target the pet you wish to shrink...");
            from.BeginTarget(-1, false, 0, End, tool);
        }

        private static void End(Mobile from, object target, IShrinkTool tool)
        {
            End(from, target as BaseCreature, tool);
        }

        public static void End(Mobile from, BaseCreature pet, IShrinkTool tool)
        {
            if (!Validate(from, pet, true))
                return;

            var staff = from.AccessLevel >= AccessLevel.GameMaster;

            if (!staff)
            {
                if (tool != null)
                {
                    if (from.Skills[SkillName.AnimalTaming].Value < ShrinkConfig.TamingRequired)
                    {
                        from.SendMessage("You must have at least " + ShrinkConfig.TamingRequired + " animal taming to use that.");
                        return;
                    }

                    if (tool.ShrinkCharges == 0)
                    {
                        if (tool.DeleteWhenEmpty)
                            tool.Delete();
                        else
                            from.SendMessage("That does not have enough charges remaining to shrink a pet.");

                        return;
                    }
                }
            }

            if (pet.ControlMaster != from && !pet.Controlled)
            {
                if (pet.Spawner is SpawnEntry se && se.UnlinkOnTaming)
                {
                    pet.Spawner.Remove(pet);
                    pet.Spawner = null;
                }

                pet.CurrentWayPoint = null;
                pet.ControlMaster = from;
                pet.Controlled = true;
                pet.ControlTarget = null;
                pet.ControlOrder = OrderType.Come;
                pet.Guild = null;

                pet.Delta(MobileDelta.Noto);
            }

            var p1 = new Entity(Serial.Zero, new Point3D(from.X, from.Y, from.Z), from.Map);
            var p2 = new Entity(Serial.Zero, new Point3D(from.X, from.Y, from.Z + 50), from.Map);

            Effects.SendMovingParticles(p2, p1, ShrinkTable.Lookup(pet), 1, 0, true, false, 0, 3, 1153, 1, 0, EffectLayer.Head, 0x100);

            from.PlaySound(492);
            from.AddToBackpack(new ShrinkItem(pet));

            if (!staff && tool != null && tool.ShrinkCharges > 0 && --tool.ShrinkCharges == 0 && tool.DeleteWhenEmpty && !tool.Deleted)
                tool.Delete();
        }

        public static bool Validate(Mobile from, BaseCreature pet, bool message)
        {
            var staff = from.AccessLevel >= AccessLevel.GameMaster;

            if (pet == null || pet.Deleted)
            {
                if (message)
                    from.SendMessage("You cannot shrink that!");

                return false;
            }

            if (pet == from)
            {
                if (message)
                    from.SendMessage("You cannot shrink yourself!");

                return false;
            }

            if (SpellHelper.CheckCombat(from))
            {
                if (message)
                    from.SendMessage("You cannot shrink your pet while you are fighting.");

                return false;
            }

            if (pet is BaseTalismanSummon)
            {
                if (message)
                    from.SendMessage("You cannot shrink a summoned creature!");

                return false;
            }

            if (pet.Summoned)
            {
                if (message)
                    from.SendMessage("You cannot shrink a summoned creature!");

                return false;
            }

            if (pet.IsDeadPet)
            {
                if (message)
                    from.SendMessage("You cannot shrink the dead!");

                return false;
            }

            if (pet.Allured)
            {
                if (message)
                    from.SendMessage("You cannot shrink an allured creature!");

                return false;
            }

            if (pet.BodyMod != 0)
            {
                if (message)
                    from.SendMessage("You cannot shrink your pet while it is polymorphed.");

                return false;
            }

            if (!staff)
            {
                if (pet.Combatant != null && pet.InRange(pet.Combatant, 12) && pet.Map == pet.Combatant.Map)
                {
                    if (message)
                        from.SendMessage("Your pet is fighting; you cannot shrink it yet.");

                    return false;
                }

                if (!pet.Controlled)
                {
                    if (message)
                        from.SendMessage("You cannot not shrink wild creatures.");

                    return false;
                }

                if (pet.ControlMaster != from)
                {
                    if (message)
                        from.SendMessage("That is not your pet.");

                    return false;
                }

                if (pet.Loyalty < BaseCreature.MaxLoyalty * 0.60)
                {
                    if (message)
                        from.SendMessage("Your pet loyalty rating must be happy or greater to be shrunk.");

                    return false;
                }

                if (pet.Backpack != null && pet.Backpack.Items.Count(o => o.Movable && o.Visible && o.IsStandardLoot()) > 0)
                {
                    if (message)
                        from.SendMessage("You must unload this pet's pack before it can be shrunk.");

                    return false;
                }
            }

            return true;
        }
    }
}
