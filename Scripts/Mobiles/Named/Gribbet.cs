using Server.Items;

namespace Server.Mobiles
{
	[CorpseName("a gribbet's corpse")]
	public class Gribbet : BaseCreature
	{
		[Constructable]
		public Gribbet()
			: base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4)
		{
			Name = "Gribbet";
			Body = 80;
			Hue = 0x8FD;
			BaseSoundID = 0x26B;

			SetStr(845, 871);
			SetDex(121, 134);
			SetInt(128, 142);

			SetHits(7470, 7540);

			SetDamage(26, 31);

			SetDamageType(ResistanceType.Physical, 50);
			SetDamageType(ResistanceType.Fire, 0);
			SetDamageType(ResistanceType.Cold, 0);
			SetDamageType(ResistanceType.Poison, 40);
			SetDamageType(ResistanceType.Energy, 10);

			SetResistance(ResistanceType.Physical, 65, 75);
			SetResistance(ResistanceType.Fire, 70, 80);
			SetResistance(ResistanceType.Cold, 25, 35);
			SetResistance(ResistanceType.Poison, 65, 75);
			SetResistance(ResistanceType.Energy, 25, 35);

			SetSkill(SkillName.Wrestling, 132.3, 143.8);
			SetSkill(SkillName.Tactics, 121.0, 130.5);
			SetSkill(SkillName.MagicResist, 102.9, 119.0);
			SetSkill(SkillName.Anatomy, 91.8, 94.3);

			SetSpecialAbility(SpecialAbility.DragonBreath);
		}

		public Gribbet(Serial serial)
			: base(serial)
		{
		}
		public override int Hides => 100;
		public override HideType HideType => HideType.Horned;
		public override bool AutoDispel => true;
		public override int TreasureMapLevel => 5;
		public override int Meat => 19;
		public override bool BardImmune => true;
		public override Poison PoisonImmune => Poison.Deadly;
		public override Poison HitPoison => (0.8 >= Utility.RandomDouble() ? Poison.Deadly : Poison.Lethal);


		public override void GenerateLoot()
		{
			AddLoot(LootPack.UltraRich, 4);
			AddLoot(LootPack.LootItemCallback(GenerateSeed));
		}

		private Item GenerateSeed(IEntity e)
		{
			var catnipSeed = new Engines.Plants.Seed(Engines.Plants.PlantType.Catnip, Engines.Plants.PlantHue.BrightGreen, false);
			var killersLuck = base.KillersLuck;
			return catnipSeed;
			if (killersLuck < 750) return new Engines.Plants.Seed();
			return Utility.RandomMinMax(0, 3000) < killersLuck ? catnipSeed : new Engines.Plants.Seed();
		}

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
