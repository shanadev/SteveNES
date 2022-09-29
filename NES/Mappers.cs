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

        public virtual bool cpuMapRead(ushort addr, out uint mapped_addr, out byte data)
        {
            mapped_addr = addr;
            data = 0x00;
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

        public override bool cpuMapRead(ushort addr, out uint mapped_addr, out byte data)
        {
            if (addr >= 0x8000 && addr <= 0xFFFF)
            {
                mapped_addr = (ushort)(addr & (PRGbanks > 1 ? 0x7FFF : 0x3FFF));
                data = 0x00;
                return true;
            }
            mapped_addr = addr;
            data = 0x00;
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

        public override bool cpuMapRead(ushort addr, out uint mapped_addr, out byte data)
        {
            if (addr >= 0x8000 && addr <= 0xBFFF)
            {
                mapped_addr = (uint)(PRGBankSelectLo * 0x4000 + (addr & 0x3FFF));
                data = 0x00;
                return true;
            }

            if (addr >= 0xC000 && addr <= 0xFFFF)
            {
                //Console.WriteLine($"mapped = {CPU.Hex((int)addr, 8)}");

                mapped_addr = (uint)(PRGBankSelectHi * 0x4000 + (addr & 0x3FFF));
                //Console.WriteLine($"mapped = {CPU.Hex((int)mapped_addr, 8)}");
                data = 0x00;
                return true;
            }

            mapped_addr = addr;
            data = 0x00;
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

        public override bool cpuMapRead(ushort addr, out uint mapped_addr, out byte data)
        {
            if (addr >= 0x8000 && addr <= 0xFFFF)
            {
                mapped_addr = (ushort)(addr & (PRGbanks > 1 ? 0x7FFF : 0x3FFF));
                data = 0x00;
                return true;
            }
            mapped_addr = addr;
            data = 0x00;
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




    public class Mapper_001 : Mapper
    {

        private byte CHRBankSelect4Lo = 0x00;
        private byte CHRBankSelect4Hi = 0x00;
        private byte CHRBankSelect8 = 0x00;

        private byte PRGBankSelect16Lo = 0x00;
        private byte PRGBankSelect16Hi = 0x00;
        private byte PRGBankSelect32 = 0x00;

        private byte LoadRegister = 0x00;
        private byte LoadRegisterCount = 0x00;
        private byte ControlRegister = 0x00;

        private MIRROR mirrorMode = MIRROR.HORIZONTAL;

        private List<byte> RAMStatic = new List<byte>();

        public Mapper_001(byte prgBanks, byte chrBanks) : base(prgBanks, chrBanks)
        {
            RAMStatic.AddRange(Enumerable.Repeat<byte>(0x00, 32 * 1024).ToArray());
        }


        public override bool cpuMapRead(ushort addr, out uint mapped_addr, out byte data)
        {
            if (addr >= 0x6000 && addr <= 0x7FFF)
            {
                mapped_addr = 0xFFFFFFFF;

                data = RAMStatic[addr & 0x1FFF];

                return true;
            }

            data = 0x00;
            if (addr >= 0x8000)
            {
                if ((ControlRegister & 0b01000) > 0)
                {
                    // 16 k '
                    if (addr >= 0x8000 && addr <= 0xBFFF)
                    {
                        mapped_addr = (uint)(PRGBankSelect16Lo * 0x4000 + (addr & 0x3FFF));
                        return true;
                    }

                    if (addr >= 0xC000 && addr <= 0xFFFF)
                    {
                        mapped_addr = (uint)(PRGBankSelect16Hi * 0x4000 + (addr & 0x3FFF));
                        return true;
                    }
                }
                else
                {
                    // 32k mode
                    mapped_addr = (uint)(PRGBankSelect32 * 0x8000 + (addr & 0x7FFF));
                    return true;
                }
            }
            mapped_addr = addr;
            return false;
        }

        public override bool cpuMapWrite(ushort addr, out uint mapped_addr, byte data)
        {
            if (addr >= 0x6000 && addr <= 0x7FFF)
            {
                mapped_addr = 0xFFFFFFFF;

                RAMStatic[addr & 0x1FFF] = data;

                return true;
            }

            if (addr >= 0x8000)
            {
                if ((data & 0x80) > 0)
                {
                    LoadRegister = 0x00;
                    LoadRegisterCount = 0;
                    ControlRegister = (byte)(ControlRegister | 0x0C);
                }
                else
                {
                    LoadRegister >>= 1;
                    LoadRegister |= (byte)((data & 0x01) << 4);
                    LoadRegisterCount++;

                    if (LoadRegisterCount == 5)
                    {
                        byte targetRegister = (byte)((addr >> 13) & 0x03);

                        if (targetRegister == 0)
                        {
                            ControlRegister = (byte)(LoadRegister & 0x1F);

                            switch (ControlRegister & 0x03)
                            {
                                case 0: mirrorMode = MIRROR.ONESCREEN_LO; break;
                                case 1: mirrorMode = MIRROR.ONESCREEN_HI; break;
                                case 2: mirrorMode = MIRROR.VERTICAL; break;
                                case 3: mirrorMode = MIRROR.HORIZONTAL; break;
                            }
                        }
                        else if (targetRegister == 1)
                        {
                            if ((ControlRegister & 0b10000) > 0)
                            {
                                CHRBankSelect4Lo = (byte)(LoadRegister & 0x1F);
                            }
                            else
                            {
                                CHRBankSelect8 = (byte)(LoadRegister & 0x1E);
                            }
                        }
                        else if (targetRegister == 2)
                        {
                            if ((ControlRegister & 0b10000) > 0)
                            {
                                CHRBankSelect4Hi = (byte)(LoadRegister & 0x1F);
                            }
                        }
                        else if (targetRegister == 3)
                        {
                            byte PRGMode = (byte)((ControlRegister >> 2) & 0x03);

                            if (PRGMode == 0 || PRGMode == 1)
                            {
                                PRGBankSelect32 = (byte)((LoadRegister & 0x0E) >> 1);
                            }
                            else if (PRGMode == 2)
                            {
                                PRGBankSelect16Lo = 0;
                                PRGBankSelect16Hi = (byte)(LoadRegister & 0x0F);
                            }
                            else if (PRGMode == 3)
                            {
                                PRGBankSelect16Lo = (byte)(LoadRegister & 0x0F);
                                PRGBankSelect16Hi = (byte)(PRGbanks - 1);
                            }
                        }

                        LoadRegister = 0x00;
                        LoadRegisterCount = 0;
                    }
                }
            }

            mapped_addr = addr;
            data = 0x00;
            return false;
        }

        public override bool ppuMapRead(ushort addr, out uint mapped_addr)
        {
            if (addr < 0x2000)
            {
                if (CHRbanks == 0)
                {
                    mapped_addr = addr;
                    return true;
                }
                else
                {
                    if ((ControlRegister & 0b10000) > 0)
                    {
                        // 4k chr bank mode
                        if (addr >= 0x0000 && addr <= 0x0FFF)
                        {
                            mapped_addr = (uint)(CHRBankSelect4Lo * 0x1000 + (addr & 0x0FFF));
                            return true;
                        }

                        if (addr >= 0x1000 && addr <= 0x1FFF)
                        {
                            mapped_addr = (uint)(CHRBankSelect4Hi * 0x1000 + (addr & 0x0FFF));
                            return true;
                        }
                    }
                    else
                    {
                        // 8k
                        mapped_addr = (uint)(CHRBankSelect8 * 0x2000 + (addr & 0x1FFF));
                        return true;
                    }
                }
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
                mapped_addr = addr;
                return true;

            }
            else
            {
                mapped_addr = addr;
                return false;
            }
        }

        public override void reset()
        {
            ControlRegister = 0x1C;
            LoadRegister = 0x00;
            LoadRegisterCount = 0x00;

            CHRBankSelect4Lo = 0x00;
            CHRBankSelect4Hi = 0x00;
            CHRBankSelect8 = 0x00;

            PRGBankSelect16Lo = 0x00;
            PRGBankSelect16Hi = (byte)(PRGbanks - 1);
            PRGBankSelect32 = 0x00;

            base.reset();
        }

        public override MIRROR mirror()
        {
            return mirrorMode;
        }

    }


}

