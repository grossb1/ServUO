using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a cat corpse")]
    public class Cat : BaseCreature
    {
        [Constructable]
        public Cat()
            : base(AIType.AI_Melee, FightMode.Aggressor, 10, 1, 0.2, 0.4)
        {
            Name = "a cat";
            Body = 0xC9;
            Hue = Utility.RandomAnimalHue();
            BaseSoundID = 0x69;

            SetStr(9);
            SetDex(35);
            SetInt(5);

            SetHits(6);
            SetMana(0);

            SetDamage(1);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 5, 10);

            SetSkill(SkillName.MagicResist, 5.0);
            SetSkill(SkillName.Tactics, 4.0);
            SetSkill(SkillName.Wrestling, 5.0);
            SetSkill(SkillName.Hiding, 20.0);

            Fame = 0;
            Karma = 150;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = -0.9;
        }

        public Cat(Serial serial)
            : base(serial)
        {
        }

        public override int Meat => 1;
        public override FoodType FavoriteFood => FoodType.Meat | FoodType.Fish;
        public override PackInstinct PackInstinct => PackInstinct.Feline;




		public override void OnActionWander()
		{
			var defaultSpeed = PassiveSpeed;
			base.OnActionWander();
			var mobilesInRange = base.GetMobilesInRange(10);
			Mobile mobileToFollow = null;
			foreach( var mobile in mobilesInRange )
			{
				if (mobile is PlayerMobile)
				{
					var backpack = mobile.Backpack;
					if(backpack.FindItemByType<SackOfCatnip>() != null)
					{
						mobileToFollow = mobile;
						break;
					}
				}
			}

			mobilesInRange.Free();

			if (mobileToFollow == null)
			{
				// base.SummonMaster = null;
				CurrentSpeed = PassiveSpeed;
				TargetLocation = null;
				foreach (var item in base.GetItemsInRange(10))
				{
					if(item is SackOfCatnip)
					{
						TargetLocation = item.Location;
						break;
					}
				}
			}
			else
			{
				TargetLocation = mobileToFollow.Location;
				// base.SummonMaster = mobileToFollow;
				CurrentSpeed = PassiveSpeed * 3;
				// base.FollowRange = 2;
			}

		


		}

		public override void OnThink()
		{
			base.OnThink();

		}
		public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
