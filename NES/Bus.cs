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
        // for disassembling
        // I need to get refreshed prg info after bank changes - which will be writes to mappers
        // just refreshig 0x8000 -> 0xFFFF
        public Dictionary<ushort, string> asmPRG = new Dictionary<ushort, string>();

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

        // Handling DMA
        byte dma_page = 0x00;   // page we're accessing, top byte
        byte dma_addr = 0x00;   // byte within the page, bottom byte
        byte dma_data = 0x00;   // the actual data being transferred

        bool dma_transfer = false; // is it happening?
        bool dma_wait = true;  // are we waiting to start DMA?

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
                // since there was a picked up write, I'm assuming a PRG bank change, so
                //if (addr >= 0x8000) asmPRG = cpu.Disassemble(0x8000, 0xFFFF);
            }
            else if (addr >= 0x0000 && addr <= 0x1FFF) // write to ram - 2k mirrored to 8k
            {
                cpuRam[addr & 0x07FF] = data;
            }
            else if (addr >= 0x2000 && addr <= 0x3FFF)  // THis is a write to the PPU
            {
                ppu.cpuWrite((ushort)(addr & 0x0007), data);
            }
            else if (addr == 0x4014)
            {
                dma_page = data;
                dma_addr = 0x00;
                dma_transfer = true;
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
            cart.Reset();
            cpu.Reset();
            ppu.Reset();
            systemClockCounter = 0;

            dma_page = 0x00;
            dma_addr = 0x00;
            dma_data = 0x00;
            dma_wait = true;
            dma_transfer = false;
        }

        // Main NES Clock
        // The PPU is 3 times faster clocked than the CPU, so this runs at PPU speed
        public void Clock()
        {
            ppu.Clock();
            //Console.WriteLine($"Bus: system counter - {systemClockCounter}");

            if (systemClockCounter % 3 == 0)
            {
                if (dma_transfer)
                {
                    if (dma_wait)
                    {
                        if (systemClockCounter % 2 == 1)
                        {
                            dma_wait = false;
                        }
                    }
                    else
                    {
                        if (systemClockCounter % 2 == 0)
                        {
                            dma_data = cpuRead(dma_page << 8 | dma_addr);
                        }
                        else
                        {
                            ppu.OAM[dma_addr] = dma_data;
                            dma_addr++;

                            if (dma_addr == 0x00)
                            {
                                dma_transfer = false;
                                dma_wait = true;
                            }
                        }
                    }
                }
                else
                {
                    cpu.Clock();
                    // if debugging
                    
                }
            }

            // Non-maskable interrupt - could happen any clock cycle and can't be stopped
            if (ppu.nmi)
            {
                ppu.nmi = false;
                cpu.NMI();
            }

            // check if cartridge is requesting IRQ
            if (cart.mapper.irqState())
            {
                cart.mapper.irqClear();
                cpu.IRQ();
            }

            systemClockCounter++; // increment the main counter

        }

    }
}

