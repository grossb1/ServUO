using System;
using System.Collections.Generic;

using Server;
using Server.Mobiles;
using Server.ContextMenus;
using Server.Spells;

using Xanthos.Utilities;
using Xanthos.Interfaces;

namespace Xanthos.ShrinkSystem
{
    public class ShrinkItem : Item, IShrinkItem
    {
        // Persisted
        private bool m_IsStatuette;
        private bool m_Locked;

        private Mobile m_Owner;
        private BaseCreature m_Pet;

        // Not persisted; lazy loaded.
        private bool m_PropsLoaded;

        private string m_Breed;
        private string m_Gender;

        private bool m_IsBonded;

        private int m_RawStr;
        private int m_RawDex;
        private int m_RawInt;

        private double m_Wrestling;
        private double m_Tactics;
        private double m_Anatomy;
        private double m_Poisoning;
        private double m_Magery;
        private double m_EvalInt;
        private double m_MagicResist;
        private double m_Meditation;
        private double m_Archery;
        private double m_Fencing;
        private double m_Macing;
        private double m_Swords;
        private double m_Parry;

        private bool m_IgnoreLockDown;  // Is only ever changed by staff

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsStatuette
        {
            get { return m_IsStatuette; }
            set
            {
                m_IsStatuette = value;

                if (m_Pet == null)
                    ItemID = 0xFAA;
                else if (m_IsStatuette)
                    ItemID = ShrinkTable.Lookup(m_Pet);
                else
                    ItemID = 0x0E2D;
			}
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IgnoreLockDown
        {
            get { return m_IgnoreLockDown; }
            set
            {
                m_IgnoreLockDown = value;

                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Locked
        {
            get { return m_Locked; }
            set
            {
                m_Locked = value;

                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get { return m_Owner; }
            set
            {
                m_Owner = value;

                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseCreature ShrunkenPet
        {
            get { return m_Pet; }
            set
            {
                m_Pet = value;

                InvalidateProperties();
            }
        }

        [Hue, CommandProperty(AccessLevel.GameMaster)]
        public override int Hue
        {
            get
            {
                if (m_Pet != null)
                    return m_Pet.Hue;

                return base.Hue;
            }
            set
            {
                if (m_Pet != null)
                {
                    m_Pet.Hue = value;

                    ReleaseWorldPackets();

                    Delta(ItemDelta.Update);
                }
                else
                    base.Hue = value;
            }
        }

        [Hue, CommandProperty(AccessLevel.Decorator)]
        //public override int HueMod
        //{
        //    get
        //    {
        //        if (m_Pet != null)
        //            return m_Pet.HueMod;

        //        return base.HueMod;
        //    }
        //    set
        //    {
        //        if (m_Pet != null)
        //        {
        //            m_Pet.HueMod = value;

        //            ReleaseWorldPackets();

        //            Delta(ItemDelta.Update);
        //        }
        //        else
        //            base.HueMod = value;
        //    }
        //}

        public override string DefaultName => m_Pet != null ? $"{Utility.FixHtml(m_Pet.Name)} on a leash" : "a leashed pet";

        public ShrinkItem()
            : base()
        { }

        public ShrinkItem(BaseCreature pet)
            : this()
        {
            ShrinkPet(pet);

            IsStatuette = ShrinkConfig.PetAsStatuette;

            Weight = ShrinkConfig.ShrunkenWeight;

            base.Hue = 2732;
        }

        public ShrinkItem(Serial serial)
            : base(serial)
        { }

        public override void OnDoubleClick(Mobile from)
        {
            if (!m_PropsLoaded)
                PreloadProperties();

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            if (m_Pet == null || m_Pet.Deleted || ItemID == 0xFAA)
            {
                from.SendMessage("Due to unforseen circumstances your pet is lost forever.");
                return;
            }

            if (m_Locked && m_Owner != from)
            {
                from.SendMessage("This is locked and only the owner can claim this pet while locked.");
                from.SendMessage("This item is now being returned to its owner.");

                m_Owner.AddToBackpack(this);
                m_Owner.SendMessage("Your pet {0} has been returned to you because it was locked and {1} was trying to claim it.", m_Breed, from.Name);
                return;
            }

            if (from.Followers + m_Pet.ControlSlots > from.FollowersMax)
            {
                from.SendMessage("You have to many followers to claim this pet.");
                return;
            }

            if (SpellHelper.CheckCombat(from))
            {
                from.SendMessage("You cannot reclaim your pet while you are fighting.");
                return;
            }

            if (Shrink.LockDown && !m_IgnoreLockDown)
            {
                from.SendMessage(54, "The server is under shrink item lockdown. You cannot unshrink your pet at this time.");
                return;
            }

   //         if (from.AccessLevel < AccessLevel.GameMaster && m_Pet.RequiresDomination && from.Karma >= 0)
   //         {
   //             from.SendMessage("You cannot unshrink a dominated creature until you have negative karma.");
   //             return;
   //         }

			//if (from.AccessLevel < AccessLevel.GameMaster && m_Pet.RequiresCleric && from.Karma <= 0)
			//{
			//	from.SendMessage("You cannot unshrink a holy creature until you have positive karma.");
			//	return;
			//}

			if (!m_Pet.CanBeControlledBy(from))
            {
                from.SendMessage("You do not have the required skills to control this pet.");
                return;
            }

            UnshrinkPet(from);
        }

        private void ShrinkPet(BaseCreature pet)
        {
            m_Pet = pet;
            m_Owner = pet.ControlMaster;

            if (ShrinkConfig.LootStatus == ShrinkConfig.BlessStatus.All || (m_Pet.IsBonded && ShrinkConfig.LootStatus == ShrinkConfig.BlessStatus.BondedOnly))
                LootType = LootType.Blessed;
            else
                LootType = LootType.Regular;

            m_Pet.Internalize();
            m_Pet.SetControlMaster(null);

            if (m_Pet.Loyalty < BaseCreature.MaxLoyalty * 0.60)
                m_Pet.Loyalty = (int)Math.Ceiling(BaseCreature.MaxLoyalty * 0.60);

            m_Pet.ControlOrder = OrderType.Stay;
            m_Pet.SummonMaster = null;
            m_Pet.IsStabled = true;
        }

        private void UnshrinkPet(Mobile from)
        {
            if (m_Pet.Loyalty < BaseCreature.MaxLoyalty * 0.60)
                m_Pet.Loyalty = (int)Math.Ceiling(BaseCreature.MaxLoyalty * 0.60);

            m_Pet.SetControlMaster(from);

            m_Pet.IsStabled = false;

            m_Pet.MoveToWorld(from.Location, from.Map);

            if (m_Owner != from)
                m_Pet.IsBonded = false;

            m_Pet = null;

            Delete();
        }

        public void OnPetSummoned()
        {
            // Summoning ball was used so dispose of the shrink item
            m_Pet = null;

            Delete();
        }

        public override void Delete()
        {
            // Don't orphan pets on the internal map
            m_Pet?.Delete();

            base.Delete();
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if ((ShrinkConfig.AllowLocking || m_Locked) && from.Alive && m_Owner == from)
            {
                if (!m_Locked)
                    list.Add(new LockShrinkItem(from, this));
                else
                    list.Add(new UnLockShrinkItem(from, this));
            }
        }

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);

            if (m_Pet == null || m_Pet.Deleted)
                return;

            if (!m_PropsLoaded)
                PreloadProperties();

            if (m_IsBonded && ShrinkConfig.BlessStatus.None == ShrinkConfig.LootStatus) // Only show bonded when the item is not blessed
                list.Add(1049608);

            if (ShrinkConfig.AllowLocking || m_Locked)  // Only show lock status when locking enabled or already locked
                list.Add(1049644, m_Locked ? "Locked" : "Unlocked");

            if (ShrinkConfig.ShowPetDetails)
            {
                list.Add(1061640, m_Owner?.Name ?? "nobody (WILD)"); // Owner: ~1_OWNER~

                list.Add(1060658, "Info\tBreed: {0} Gender: {1}", m_Breed, m_Gender);
                list.Add(1060659, "Stats\tStrength {0}, Dexterity {1}, Intelligence {2}", m_RawStr, m_RawDex, m_RawInt);

                if (m_Wrestling != 0 || m_Tactics != 0 || m_Anatomy != 0 || m_Poisoning != 0)
                    list.Add(1060660, "Combat Skills\tWrestling {0}, Tactics {1}, Anatomy {2}, Poisoning {3}", m_Wrestling, m_Tactics, m_Anatomy, m_Poisoning);

                if (m_Magery != 0 || m_EvalInt != 0 || m_MagicResist != 0 || m_Meditation != 0)
                    list.Add(1060661, "Magic Skills\tMagery {0}, Eval Intel {1}, Magic Resist {2}, Meditation {3}", m_Magery, m_EvalInt, m_MagicResist, m_Meditation);

                if (m_Archery != 0 || m_Fencing != 0 || m_Macing != 0 || m_Parry != 0 || m_Swords != 0)
                    list.Add(1060662, "Weapon Skills\tArchery {0}, Fencing {1}, Macing {2}, Parry {3}, Swords {4}", m_Archery, m_Fencing, m_Macing, m_Parry, m_Swords);
            }
        }

        private void PreloadProperties()
        {
            if (m_Pet == null)
                return;

            m_IsBonded = m_Pet.IsBonded;

            m_Gender = m_Pet.Female ? "Female" : "Male";
            m_Breed = Misc.GetFriendlyClassName(m_Pet.GetType().Name);

            m_RawStr = m_Pet.RawStr;
            m_RawDex = m_Pet.RawDex;
            m_RawInt = m_Pet.RawInt;

            m_Wrestling = m_Pet.Skills[SkillName.Wrestling].Base;
            m_Tactics = m_Pet.Skills[SkillName.Tactics].Base;
            m_Anatomy = m_Pet.Skills[SkillName.Anatomy].Base;
            m_Poisoning = m_Pet.Skills[SkillName.Poisoning].Base;
            m_Magery = m_Pet.Skills[SkillName.Magery].Base;
            m_EvalInt = m_Pet.Skills[SkillName.EvalInt].Base;
            m_MagicResist = m_Pet.Skills[SkillName.MagicResist].Base;
            m_Meditation = m_Pet.Skills[SkillName.Meditation].Base;
            m_Parry = m_Pet.Skills[SkillName.Parry].Base;
            m_Archery = m_Pet.Skills[SkillName.Archery].Base;
            m_Fencing = m_Pet.Skills[SkillName.Fencing].Base;
            m_Swords = m_Pet.Skills[SkillName.Swords].Base;
            m_Macing = m_Pet.Skills[SkillName.Macing].Base;

            m_PropsLoaded = true;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write(m_IsStatuette);
            writer.Write(m_Locked);
            writer.Write(m_Owner);
            writer.Write(m_Pet);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var v = reader.ReadInt();

            m_IsStatuette = reader.ReadBool();
            m_Locked = reader.ReadBool();
            m_Owner = reader.ReadMobile<PlayerMobile>();
            m_Pet = reader.ReadMobile<BaseCreature>();

            if (m_Pet != null)
                m_Pet.IsStabled = true;

            if (v < 1)
                Name = null;
        }
    }

    public class LockShrinkItem : ContextMenuEntry
    {
        private readonly Mobile m_From;
        private readonly ShrinkItem m_ShrinkItem;

        public LockShrinkItem(Mobile from, ShrinkItem shrink)
            : base(2029, 5)
        {
            m_From = from;
            m_ShrinkItem = shrink;
        }

        public override void OnClick()
        {
            m_ShrinkItem.Locked = true;

            m_From.SendMessage(38, "You have locked this shrunken pet so only you can reclaim it.");
        }
    }

    public class UnLockShrinkItem : ContextMenuEntry
    {
        private readonly Mobile m_From;
        private readonly ShrinkItem m_ShrinkItem;

        public UnLockShrinkItem(Mobile from, ShrinkItem shrink)
            : base(2033, 5)
        {
            m_From = from;
            m_ShrinkItem = shrink;
        }

        public override void OnClick()
        {
            m_ShrinkItem.Locked = false;

            m_From.SendMessage(38, "You have unlocked this shrunken pet, now anyone can reclaim it as theirs.");
        }
    }
}
