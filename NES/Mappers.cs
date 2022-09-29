using System;
namespace NES
{
    // enum for the type of mirroring - sometimes hard-wired on the game chip
    public enum MIRROR
    {
        HORIZONTAL,
        VERTICAL,
        HARDWARE,
        ONESCREEN_LO,
        ONESCREEN_HI
    }

    // abstract Mapper class
    public abstract class Mapper
    {
        protected byte PRGbanks = 0;
        protected byte CHRbanks = 0;
        protected MIRROR mirrorMode;

        public Mapper(byte prgBanks, byte chrBanks)
        {
            PRGbanks = prgBanks;
            CHRbanks = chrBanks;
            reset();
        }

        public virtual bool cpuMapRead(ushort addr, out uint mapped_addr)
        {
            mapped_addr = addr;
            return false;
        }

        public virtual bool cpuMapWrite(ushort addr, out uint mapped_addr, byte data)
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

        public virtual MIRROR mirror()
        {
            return MIRROR.HARDWARE;
        }

        public virtual void reset()
        {

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

        public override bool cpuMapWrite(ushort addr, out uint mapped_addr, byte data)
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
            if (addr >= 0x0000 && addr <= 0x1FFF)
            {
                if (CHRbanks == 0)
                {
                    mapped_addr = addr;
                    return true;
                }
            }
            mapped_addr = addr;
            return false;
        }

        public override void reset()
        {
            base.reset();
        }
    }



    public class Mapper_002 : Mapper
    {
        private byte PRGBankSelectLo = 0x00;
        private byte PRGBankSelectHi = 0x00;

        public Mapper_002(byte prgBanks, byte chrBanks) : base(prgBanks, chrBanks)
        {
        }

        public override bool cpuMapRead(ushort addr, out uint mapped_addr)
        {
            if (addr >= 0x8000 && addr <= 0xBFFF)
            {
                mapped_addr = (uint)(PRGBankSelectLo * 0x4000 + (addr & 0x3FFF));
                return true;
            }

            if (addr >= 0xC000 && addr <= 0xFFFF)
            {
                //Console.WriteLine($"mapped = {CPU.Hex((int)addr, 8)}");

                mapped_addr = (uint)(PRGBankSelectHi * 0x4000 + (addr & 0x3FFF));
                //Console.WriteLine($"mapped = {CPU.Hex((int)mapped_addr, 8)}");
                return true;
            }

            mapped_addr = addr;
            return false;
        }

        public override bool cpuMapWrite(ushort addr, out uint mapped_addr, byte data)
        {
            if (addr >= 0x8000 && addr <= 0xFFFF)
            {
                PRGBankSelectLo = (byte)(data & 0x0F);
                //mapped_addr = (ushort)(addr & (PRGbanks > 1 ? 0x7FFF : 0x3FFF));
                //return true;
            }
            mapped_addr = addr;
            return false;
        }

        public override bool ppuMapRead(ushort addr, out uint mapped_addr)
        {
            if (addr < 0x2000)
            {
                mapped_addr = addr;
                return true;
            }
            mapped_addr = addr;
            return false;
        }

        public override bool ppuMapWrite(ushort addr, out uint mapped_addr)
        {
            if (addr < 0x2000)
            {
                if (CHRbanks == 0)
                {
                    mapped_addr = addr;
                    return true;
                }
            }
            mapped_addr = addr;
            return false;
        }

        public override void reset()
        {
            PRGBankSelectLo = 0;
            PRGBankSelectHi = (byte)(PRGbanks - 1);
            base.reset();   
        }

    }



    public class Mapper_003 : Mapper
    {
        private byte CHRBankSelect = 0x00;

        public Mapper_003(byte prgBanks, byte chrBanks) : base(prgBanks, chrBanks)
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

        public override bool cpuMapWrite(ushort addr, out uint mapped_addr, byte data)
        {
            if (addr >= 0x8000 && addr <= 0xFFFF)
            {
                CHRBankSelect = (byte)(data & 0x03);
            }
            mapped_addr = addr;
            return false;
        }

        public override bool ppuMapRead(ushort addr, out uint mapped_addr)
        {
            if (addr < 0x2000)
            {
                mapped_addr = (uint)(CHRBankSelect * 0x2000 + addr);
                return true;
            }
            mapped_addr = addr;
            return false;
        }

        public override bool ppuMapWrite(ushort addr, out uint mapped_addr)
        {
            mapped_addr = addr;
            return false;
        }


        public override void reset()
        {
            CHRBankSelect = 0;
            base.reset();
        }

    }


}

