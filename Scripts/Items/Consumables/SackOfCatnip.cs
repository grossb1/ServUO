using System;
using Server.ContextMenus;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Mobiles;
using Xanthos.ShrinkSystem;

namespace Server.Items
{
	public class SackOfCatnip : Item
	{
		private Timer m_Timer;
		private DateTime m_End;
		private TimeSpan m_Duration = TimeSpan.Zero;

		private int m_Charges;
		[Constructable]
		public SackOfCatnip() : base(0xe76)
		{
			Name = "Sack of Catnip";
			Hue = Utility.RandomGreenHue();
			Charges = Utility.RandomMinMax(65, 100);
		}

		public SackOfCatnip(Serial serial) : base(serial)
		{
		}

        [CommandProperty(AccessLevel.GameMaster)]
        public int Charges
        {
            get
            {
                return m_Charges;
            }
            set
            {
                m_Charges = value;
                InvalidateProperties();
            }
        }

		public void ConsumeCharge(PlayerMobile from)
        {
            --Charges;

            if (Charges == 0)
			{
                from.SendLocalizedMessage(1019073); // This item is out of charges.
				GenerateCatStatuette(from);
				this.Delete();
			}
        }

		public void CreateCat(PlayerMobile from)
		{
			var cat = new Cat();
			cat.MoveToWorld(from.Location, from.Map);
			// play animation
			from.SendMessage(38, "A cat appears out of nowhere");
			ConsumeCharge(from);
		}

		public override void Serialize(GenericWriter writer)
		{
			base.Serialize(writer);

			writer.Write(0); // version
			writer.Write(m_Charges);
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize(reader);

			int version = reader.ReadInt();
			m_Charges = reader.ReadInt();
		}

    	public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add($"Charges: {m_Charges}");
        }

		public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
		{
			base.GetContextMenuEntries(from, list);

			if(from is PlayerMobile player)
				list.Add(new Shake(player, this));
		}

		private void GenerateCatStatuette(PlayerMobile from)
		{
			from.AddToBackpack(new FelineBlessedStatue());
		}

		public class Shake : ContextMenuEntry
		{
			private readonly PlayerMobile m_From;
			private readonly SackOfCatnip m_SackOfCatnip;

			public Shake(PlayerMobile from, SackOfCatnip sackOfCatnip)
				: base(1152490, 5)
			{
				m_From = from;
				m_SackOfCatnip = sackOfCatnip;
			}

			public override void OnClick()
			{
				m_SackOfCatnip.CreateCat(m_From);
			}
		}
	}
}
