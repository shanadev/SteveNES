using System;
using DisplayEngine;
namespace NES
{
    public unsafe class Bus
    {

        // devices on the bus
        public CPU cpu;
        public PPU ppu;
        public byte[] cpuRam = new byte[2048];
        public Cartridge cart;

        private Engine engine;


        private uint systemClockCounter = 0;


        public Bus(Engine engine)
        {
            this.engine = engine;
            cpuRam = Enumerable.Repeat<byte>(0x00, cpuRam.Length).ToArray();
            cpu = new CPU(this);
            ppu = new PPU(this, engine);
        }


        public void cpuWrite(ushort addr, byte data)
        {
            if (cart.cpuWrite(addr, data))  // give cart first crack at the write
            {

            }
            else if (addr >= 0x0000 && addr <= 0x1FFF) // write to ram - 2k mirrored to 8k
            {
                cpuRam[addr & 0x07FF] = data;
            }
            else if (addr >= 0x2000 && addr <= 0x3FFF)
            {
                ppu.cpuWrite((ushort)(addr & 0x0007), data);
            }
        }

        public byte cpuRead(ushort addr)
        {
            byte data = 0x00;

            if (cart.cpuRead(addr, out data))
            {
                return data;
            }
            else if (addr >= 0x0000 && addr <= 0x1FFF) // Read from ram - 2k mirrored
            {
                return cpuRam[addr & 0x07FF];
            }
            else if (addr >= 0x2000 && addr <= 0x3FFF)
            {
                ppu.cpuRead((ushort)(addr & 0x0007));
            }
            return 0x00;

        }


        //// System methods
        ///

        public void InsertCartridge(Cartridge cart)
        {
            this.cart = cart;
            ppu.ConnectCartridge(this.cart);

        }

        public void Reset()
        {
            cpu.Reset();
            systemClockCounter = 0;
        }

        public void Clock()
        {
            ppu.Clock();
            //Console.WriteLine($"Bus: system counter - {systemClockCounter}");

            if (systemClockCounter % 3 == 0)
            {
                cpu.Clock();
            }

            systemClockCounter++;
        }

    }
}

