using System;
using System.Net.NetworkInformation;
using DisplayEngine;
using Serilog;

namespace NES
{
    public unsafe class Bus
    {

        // devices on the bus
        public CPU cpu;
        public PPU ppu;
        public byte[] cpuRam = new byte[2048];
        public Cartridge cart;

        public byte[] controller = new byte[2];

        private Engine engine;

        private byte[] controller_state = new byte[2];

        private bool controllerStrobe = false;

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
            //Log.Debug($"CPU Write - addr:0x{Convert.ToString(addr, toBase:16).PadLeft(4,'0')} - data:0x{Convert.ToString(data, toBase: 16).PadLeft(2, '0')}");
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
            else if (addr >= 0x4016 && addr <= 0x4017)
            {
                if (controllerStrobe && !((data & 0x01) > 0))
                {
                    controller_state[addr & 0x0001] = controller[addr & 0x0001];
                    //controller_state[addr & 0x0001] = 0x01;
                }

                controllerStrobe = ((data & 0x01) > 0);
                //Log.Debug($"Write Controller - Strobe: ({(controllerStrobe ? "Y" : "N")}) - state: {Convert.ToString(controller_state[addr & 0x0001], toBase: 2).PadLeft(8, '0')} - data: {Convert.ToString(data, toBase: 2).PadLeft(8, '0')} ");

            }
        }

        public byte cpuRead(int addr, bool readOnly = false)
        {
            byte data = 0x00;
            //Log.Debug($"CPU Read - addr:0x{Convert.ToString(addr, toBase: 16).PadLeft(4, '0')}");

            if (cart.cpuRead(addr, out data))
            {
                //return data;
            }
            else if (addr >= 0x0000 && addr <= 0x1FFF) // Read from ram - 2k mirrored
            {
                data = cpuRam[addr & 0x07FF];
            }
            else if (addr >= 0x2000 && addr <= 0x3FFF)
            {
                data = ppu.cpuRead((ushort)(addr & 0x0007), readOnly);
            }
            else if (addr >= 0x4016 && addr <= 0x4017)
            {
                string thing = "";
                if (controllerStrobe)
                {
                    data = (byte)(((controller_state[addr & 0x01] & 0x40) > 0) ? 1 : 0);
                }
                else
                {
                    thing = Convert.ToString(controller_state[addr & 0x0001], toBase: 2).PadLeft(8, '0');

                    data = (byte)(((controller_state[addr & 0x01] & 0x01) > 0) ? 1 : 0);

                    controller_state[addr & 0x0001] >>= 1;

                }
                Log.Debug($"Read Controller - Strobe: ({(controllerStrobe ? "Y" : "N")}) - state: {thing} - data: {Convert.ToString(data, toBase: 16).PadLeft(2, '0')} ");
            }
            
            return data;

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
            ppu.Reset();
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

            if (ppu.nmi)
            {
                ppu.nmi = false;
                cpu.NMI();
            }

            systemClockCounter++;

        }

    }
}

