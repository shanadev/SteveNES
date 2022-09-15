using System;
namespace NES
{
    public unsafe class Bus
    {

        // devices on the bus
        public CPU cpu;
        public byte[] ram = new byte[64 * 1024];



        public Bus()
        {
            ram = Enumerable.Repeat<byte>(0x00, ram.Length).ToArray();
            cpu = new CPU(this);
        }


        public void write(ushort addr, byte data)
        {
            if (addr >= 0x0000 && addr <= 0xFFFF)
            {
                ram[addr] = data;
            }
        }

        public byte read(ushort addr)
        {
            if (addr >= 0x0000 && addr <= 0xFFFF)
            {
                return ram[addr];
            }
            else
            {
                return 0x00;
            }
        }
    }
}

