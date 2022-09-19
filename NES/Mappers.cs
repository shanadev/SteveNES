using System;
namespace NES
{

    // abstract Mapper class
    public abstract class Mapper
    {
        protected byte PRGbanks = 0;
        protected byte CHRbanks = 0;

        public Mapper(byte prgBanks, byte chrBanks)
        {
            PRGbanks = prgBanks;
            CHRbanks = chrBanks;
        }

        public virtual bool cpuMapRead(ushort addr, out uint mapped_addr)
        {
            mapped_addr = addr;
            return false;
        }

        public virtual bool cpuMapWrite(ushort addr, out uint mapped_addr)
        {
            mapped_addr = addr;
            return false;

        }

        public virtual bool ppuMapRead(ushort addr, out uint mapped_addr)
        {
            mapped_addr = addr;
            return false;
        }

        public virtual bool ppuMapWrite(ushort addr, out uint mapped_addr)
        {
            mapped_addr = addr;
            return false;
        }
    }



    // Mapper 000
    public class Mapper_000 : Mapper
    {
        public Mapper_000(byte prgBanks, byte chrBanks) : base(prgBanks, chrBanks)
        {
            
        }

        public override bool cpuMapRead(ushort addr, out uint mapped_addr)
        {
            if (addr >= 0x8000 && addr <= 0xFFFF)
            {
                mapped_addr = (ushort)(addr & (PRGbanks > 1 ? 0x7FFF : 0x3FFF));
                return true;
            }
            mapped_addr = addr;
            return false;
        }

        public override bool cpuMapWrite(ushort addr, out uint mapped_addr)
        {
            if (addr >= 0x8000 && addr <= 0xFFFF)
            {
                mapped_addr = (ushort)(addr & (PRGbanks > 1 ? 0x7FFF : 0x3FFF));
                return true;
            }
            mapped_addr = addr;
            return false;
        }

        public override bool ppuMapRead(ushort addr, out uint mapped_addr)
        {
            if (addr >= 0x0000 && addr <= 0x1FFF)
            {
                mapped_addr = addr;
                return true;
            }
            mapped_addr = addr;
            return false;
        }

        public override bool ppuMapWrite(ushort addr, out uint mapped_addr)
        {
            //if (addr >= 0x0000 && addr <= 0x1FFF)
            //{
            //    return true;
            //}
            mapped_addr = addr;
            return false;
        }
    }



}

