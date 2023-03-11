using Server;

using Xanthos.Interfaces;

namespace Xanthos.ShrinkSystem
{
    public class PetLeash : Item, IShrinkTool
    {
        private int m_ShrinkCharges;

        [CommandProperty(AccessLevel.GameMaster)]
        public int ShrinkCharges
        {
            get { return m_ShrinkCharges; }
            set
            {
                m_ShrinkCharges = value;

                if (m_ShrinkCharges == 0 && DeleteWhenEmpty)
                    Delete();
                else
                    InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool DeleteWhenEmpty { get; set; }

        [Constructable]
        public PetLeash()
            : this(ShrinkConfig.ShrinkCharges)
        { }

        [Constructable]
        public PetLeash(int charges)
            : base(0x1374)
        {
            ShrinkCharges = charges;

            Name = "Pet Leash";
            DeleteWhenEmpty = true;
            Weight = 1.0;

            LootType = ShrinkConfig.BlessedLeash ? LootType.Blessed : LootType.Regular;
        }

        public PetLeash(Serial serial)
            : base(serial)
        { }

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);

            if (ShrinkCharges >= 0)
                list.Add(1060658, "Charges\t{0}", ShrinkCharges.ToString());
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            else
                ShrinkTarget.Begin(from, this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1);

            writer.Write(DeleteWhenEmpty);
            writer.Write(ShrinkCharges);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var v = reader.ReadInt();

            DeleteWhenEmpty = v >= 1 && reader.ReadBool();
            ShrinkCharges = reader.ReadInt();
        }
    }
}
