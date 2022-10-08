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
        ONESCREEN_HI,
        FOURSCREEN
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

        public virtual bool irqState()
        {
            return false;
        }

        public virtual void irqClear()
        {

        }

        public virtual void scanline()
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


    // Mapper 001 - MMC1
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

                // TODO: Write to file here

                return true;
            }

            if (addr >= 0x8000)
            {
                if ((data & 0x80) > 0)
                {
                    LoadRegister = 0x00;
                    LoadRegisterCount = 0;
                    ControlRegister = (byte)(ControlRegister | 0x0C);  // 12 - 0b1100
                }
                else
                {
                    // TODO: When the serial port is written to on consecutive cycles, it ignores every write after the first. In practice, this only happens when the CPU executes read-modify-write instructions, which first write the original value before writing the modified one on the next cycle.
                    LoadRegister >>= 1;
                    LoadRegister |= (byte)((data & 0x01) << 4);
                    //LoadRegister |= (byte)((data << 4) & 0x01);
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


    // Mapper 002
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


    // Mapper 003
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


    // Mapper 004
    public class Mapper_004 : Mapper
    {
        private byte targetRegister = 0x00;
        private bool PRGBankMode = false;
        private bool CHRInversion = false;
        private MIRROR mirrorMode = MIRROR.HORIZONTAL;
         
        private uint[] Register = new uint[8];
        private uint[] CHRBank = new uint[8];
        private uint[] PRGBank = new uint[4];
         
        private bool IRQActive = false;
        private bool IRQEnable = false;
        private bool IRQUpdate = false;
        private uint IRQCounter = 0x0000;
        private uint IRQReload = 0x0000;
         
        private List<byte> RAMStatic = new List<byte>();


        public Mapper_004(byte prgBanks, byte chrBanks) : base(prgBanks, chrBanks)
        {
            RAMStatic.AddRange(Enumerable.Repeat<byte>(0x00, 8 * 1024).ToArray());
        }

        public override bool cpuMapRead(ushort addr, out uint mapped_addr, out byte data)
        {
            data = 0x00;

            if (addr >= 0x6000 && addr <= 0x7FFF)
            {
                mapped_addr = 0xFFFFFFFF;
                data = RAMStatic[addr & 0x1FFF];
                return true;
            }

            if (addr >= 0x8000 && addr <= 0x9FFF)
            {
                mapped_addr = (uint)(PRGBank[0] + (addr & 0x1FFF));
                return true;
            }

            if (addr >= 0xA000 && addr <= 0xBFFF)
            {
                mapped_addr = (uint)(PRGBank[1] + (addr & 0x1FFF));
                return true;
            }

            if (addr >= 0xC000 && addr <= 0xDFFF)
            {
                mapped_addr = (uint)(PRGBank[2] + (addr & 0x1FFF));
                return true;
            }

            if (addr >= 0xE000 && addr <= 0xFFFF)
            {
                mapped_addr = (uint)(PRGBank[3] + (addr & 0x1FFF));
                return true;
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

            if (addr >= 0x8000 && addr <= 0x9FFF)
            {
                if ((addr & 0x0001) == 0)
                {
                    targetRegister = (byte)(data & 0x07);
                    PRGBankMode = (data & 0x40) > 0;
                    CHRInversion = (data & 0x80) > 0;
                }
                else
                {
                    Register[targetRegister] = data;

                    if (CHRInversion)
                    {
                        CHRBank[0] = Register[2] * 0x0400;
                        CHRBank[1] = Register[3] * 0x0400;
                        CHRBank[2] = Register[4] * 0x0400;
                        CHRBank[3] = Register[5] * 0x0400;
                        CHRBank[4] = (Register[0] & 0xFE) * 0x0400;
                        CHRBank[5] = Register[0] * 0x0400 + 0x0400;
                        CHRBank[6] = (Register[1] & 0xFE) * 0x0400;
                        CHRBank[7] = Register[1] * 0x0400 + 0x0400;
                    }
                    else
                    {
                        CHRBank[0] = Register[0] * 0x0400 + 0x0400;
                        CHRBank[1] = (Register[0] & 0xFE) * 0x0400;
                        CHRBank[2] = Register[1] * 0x0400 + 0x0400;
                        CHRBank[3] = (Register[1] & 0xFE) * 0x0400;
                        CHRBank[4] = Register[2] * 0x0400;
                        CHRBank[5] = Register[3] * 0x0400;
                        CHRBank[6] = Register[4] * 0x0400;
                        CHRBank[7] = Register[5] * 0x0400;
                    }

                    if (PRGBankMode)
                    {
                        PRGBank[2] = (Register[6] & 0x3F) * 0x2000;
                        PRGBank[0] = (uint)((PRGbanks * 2 - 2) * 0x2000);
                    }
                    else
                    {
                        PRGBank[0] = (Register[6] & 0x3F) * 0x2000;
                        PRGBank[2] = (uint)((PRGbanks * 2 - 2) * 0x2000);
                    }

                    PRGBank[1] = (Register[7] & 0x3F) * 0x2000;
                    PRGBank[3] = (uint)((PRGbanks * 2 - 1) * 0x2000);
                }

                mapped_addr = addr;
                return false;
            }

            if (addr >= 0xA000 && addr <= 0xBFFF)
            {
                if ((addr & 0x0001) == 0)
                {
                    if ((data & 0x01) > 0)
                    {
                        mirrorMode = MIRROR.HORIZONTAL;
                    }
                    else
                    {
                        mirrorMode = MIRROR.VERTICAL;
                    }
                }
                else
                {
                    //PRG Ram protect TODO

                }
                mapped_addr = addr;
                return false;
            }

            if (addr >= 0xC000 && addr <= 0xDFFF)
            {
                if ((addr & 0x0001) == 0)
                {
                    IRQReload = data;
                }
                else
                {
                    IRQCounter = 0x0000;
                }
                mapped_addr = addr;
                return false;
            }

            if (addr >= 0xE000 && addr <= 0xFFFF)
            {
                if ((addr & 0x0001) == 0)
                {
                    IRQEnable = false;
                    IRQActive = false;
                }
                else
                {
                    IRQEnable = true;
                }
                mapped_addr = addr;
                return false;
            }
            mapped_addr = addr;
            return false;
        }

        public override bool ppuMapRead(ushort addr, out uint mapped_addr)
        {
            if (addr >= 0x0000 && addr <= 0x03FF)
            {
                mapped_addr = (uint)(CHRBank[0] + (addr & 0x03FF));
            }

            if (addr >= 0x0400 && addr <= 0x07FF)
            {
                mapped_addr = (uint)(CHRBank[1] + (addr & 0x03FF));
            }

            if (addr >= 0x0800 && addr <= 0x0BFF)
            {
                mapped_addr = (uint)(CHRBank[2] + (addr & 0x03FF));
            }

            if (addr >= 0x0C00 && addr <= 0x0FFF)
            {
                mapped_addr = (uint)(CHRBank[3] + (addr & 0x03FF));
            }

            if (addr >= 0x1000 && addr <= 0x13FF)
            {
                mapped_addr = (uint)(CHRBank[4] + (addr & 0x03FF));
            }

            if (addr >= 0x1400 && addr <= 0x17FF)
            {
                mapped_addr = (uint)(CHRBank[5] + (addr & 0x03FF));
            }

            if (addr >= 0x1800 && addr <= 0x1BFF)
            {
                mapped_addr = (uint)(CHRBank[6] + (addr & 0x03FF));
            }

            if (addr >= 0x1C00 && addr <= 0x1FFF)
            {
                mapped_addr = (uint)(CHRBank[7] + (addr & 0x03FF));
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
            targetRegister = 0x00;
            PRGBankMode = false;
            CHRInversion = false;
            mirrorMode = MIRROR.HORIZONTAL;

            IRQActive = false;
            IRQEnable = false;
            IRQUpdate = false;
            IRQCounter = 0x0000;
            IRQReload = 0x0000;

            for (int i = 0; i < 4; i++) PRGBank[i] = 0;
            for (int i = 0; i < 8; i++) { CHRBank[i] = 0; Register[i] = 0; }

            PRGBank[0] = 0 * 0x2000;
            PRGBank[1] = 1 * 0x2000;
            PRGBank[2] = (uint)((PRGbanks * 2 - 2) * 0x2000);
            PRGBank[3] = (uint)((PRGbanks * 2 - 1) * 0x2000);

            base.reset();
        }

        public override MIRROR mirror()
        {
            return base.mirror();
        }

        public override bool irqState()
        {
            return IRQActive;
        }

        public override void irqClear()
        {
            IRQActive = false;
        }

        public override void scanline()
        {
            if (IRQCounter == 0)
            {
                IRQCounter = IRQReload;
            }
            else
                IRQCounter--;

            if (IRQCounter == 0 && IRQEnable)
            {
                IRQActive = true;
            }
        }
    }





}

