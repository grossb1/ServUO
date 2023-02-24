using Server.Engines.Plants;
using static Server.Items.RuinedShipPlans;

namespace Server.Items
{
	public class Catnip : Item
	{
		[Constructable]
		public Catnip() : this(1)
		{
		}

		[Constructable]
		public Catnip(int amount) : base(0xF88)
		{
			Stackable = true;
			Amount = amount;
			Name = "Catnip";
			Hue = PlantHueInfo.GetInfo(PlantHue.BrightGreen).Hue;
		}

		public Catnip(Serial serial) : base(serial) { }

		public override void Serialize(GenericWriter writer)
		{
			base.Serialize(writer);

			writer.Write(0); // version
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize(reader);

			int version = reader.ReadInt();
		}


	}
}
