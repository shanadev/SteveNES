using System;
using System.Net.NetworkInformation;
using DisplayEngine;
//using Serilog;

namespace NES
{
    // This is going to represent the Nintendo-
    // it has a CPU, PPU, Cart, APU, RAM, etc...
    public class Bus
    {
        // devices on the bus
        public CPU cpu;
        public PPU ppu;
        public byte[] cpuRam = new byte[2048];
        public Cartridge cart;
        public byte[] controller = new byte[2];

        // engine insance passed from the main program
        // This is just being passed along to the PPU honestly
        //private Engine engine;

        private byte[] controller_state = new byte[2];  // the latched controller state that will be serially read

        private bool controllerStrobe = false;  // is the controller being polled?

        private uint systemClockCounter = 0; // Master counter
        // TODO: make public accessible

        // Constructor - engine comes in, set up Ram, CPU and PPU
        public Bus()
        {
            //this.engine = engine;
            cpuRam = Enumerable.Repeat<byte>(0x00, cpuRam.Length).ToArray();
            cpu = new CPU(this);
            ppu = new PPU(this);
        }

        // Main CPU Write method
        // Let eh cart/mapper have a crack first, then check each of the address regions to direct
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
            else if (addr >= 0x2000 && addr <= 0x3FFF)  // THis is a write to the PPU
            {
                ppu.cpuWrite((ushort)(addr & 0x0007), data);
            }
            else if (addr >= 0x4016 && addr <= 0x4017)  // Write to the controller addresses - sets the state
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

        // Main CPU Read method
        // Same here, let the cart/mapper look first to consume, then pass to the rest
        public byte cpuRead(int addr, bool readOnly = false)    // Readonly is for shutting off actual read during disassembly
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
            else if (addr >= 0x2000 && addr <= 0x3FFF)  // Read from PPU
            {
                data = ppu.cpuRead((ushort)(addr & 0x0007), readOnly);
            }
            else if (addr >= 0x4016 && addr <= 0x4017)      // Read from controller - 8 reads in a row to get controller status
            {
                //string thing = "";
                if (controllerStrobe)
                {
                    data = (byte)(((controller_state[addr & 0x01] & 0x40) > 0) ? 1 : 0);
                }
                else
                {
                    //thing = Convert.ToString(controller_state[addr & 0x0001], toBase: 2).PadLeft(8, '0');

                    data = (byte)(((controller_state[addr & 0x01] & 0x01) > 0) ? 1 : 0);

                    controller_state[addr & 0x0001] >>= 1;

                }
                //Log.Debug($"Read Controller - Strobe: ({(controllerStrobe ? "Y" : "N")}) - state: {thing} - data: {Convert.ToString(data, toBase: 16).PadLeft(2, '0')} ");
            }
            
            return data;

        }


        //// System methods
        
        // Set up new cartridge, connect to the PPU
        public void InsertCartridge(Cartridge cart)
        {
            this.cart = cart;
            ppu.ConnectCartridge(this.cart);

        }

        // Reset the system
        public void Reset()
        {            
            cpu.Reset();
            ppu.Reset();
            systemClockCounter = 0;
        }

        // Main NES Clock
        // The PPU is 3 times faster clocked than the CPU, so this runs at PPU speed
        public void Clock()
        {
            ppu.Clock();
            //Console.WriteLine($"Bus: system counter - {systemClockCounter}");

            if (systemClockCounter % 3 == 0)
            {
                cpu.Clock();
            }

            // Non-maskable interrupt - could happen any clock cycle and can't be stopped
            if (ppu.nmi)
            {
                ppu.nmi = false;
                cpu.NMI();
            }

            systemClockCounter++; // increment the main counter

        }

    }
}

