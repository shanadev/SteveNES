using System;
using DisplayEngine;

namespace NES
{
    public class PPU
    {
        private Random rnd = new Random();

        private Cartridge cart;
        private Bus bus;
        private byte[,] nameTable = new byte[2, 1024];
        private byte[,] patternTable = new byte[2, 4096];

        public ScreenColor[] nesPalette = new ScreenColor[64];

        private byte[] PaletteTable = new byte[32];
        private Sprite mainScreen = new Sprite(256, 240);
        private Sprite[] nameTableSprite = new Sprite[2] { new Sprite(256, 240), new Sprite(256, 240) };
        private Sprite[] patternTableSprite = new Sprite[2] { new Sprite(128, 128), new Sprite(128, 128) };

        private int scanline = 0; // row on screen
        private int cycle = 0; // row on screen

        public bool FrameComplete { get; set; } = false;

        private Engine engine;

        public Sprite GetScreen()
        {
            return mainScreen;
        }

        public Sprite GetNameTable(byte i)
        {
            return nameTableSprite[i];
        }

        public Sprite GetPatternTable(byte i, byte palette)
        {
            for (int tileY = 0; tileY < 16; tileY++)
            {
                for (int tileX = 0; tileX < 16; tileX++)
                {
                    int offset = tileY * 256 + tileX * 16;

                    for (int row = 0; row < 8; row++)
                    {
                        byte tile_lsb = ppuRead((byte)(i * 0x1000 + offset + row + 0));
                        byte tile_msb = ppuRead((byte)(i * 0x1000 + offset + row + 8));

                        for (int col = 0; col < 8; col++)
                        {
                            byte px = (byte)((tile_lsb & 0x01) + (tile_msb & 0x01));
                            tile_lsb >>= 1;
                            tile_msb >>= 1;

                            patternTableSprite[i].SetPixel(tileX * 8 + (7 - col), tileY * 8 + row, GetColorFromPaletteRam(palette, px));
                        }
                    }

                }
            }
            return patternTableSprite[i];
        }

        public ScreenColor GetColorFromPaletteRam(byte palette, byte pixel)
        {
            return nesPalette[ppuRead((ushort)(0x3F00 + (palette << 2) + pixel))];
        }



        public PPU(Bus b, Engine eng)
        {
            this.engine = eng;
            bus = b;

            nesPalette[0x00] = new ScreenColor(84, 84, 84, 255);
            nesPalette[0x01] = new ScreenColor(0, 30, 116, 255);
            nesPalette[0x02] = new ScreenColor(8, 16, 144, 255);
            nesPalette[0x03] = new ScreenColor(48, 0, 136, 255);
            nesPalette[0x04] = new ScreenColor(68, 0, 100, 255);
            nesPalette[0x05] = new ScreenColor(92, 0, 48, 255);
            nesPalette[0x06] = new ScreenColor(84, 4, 0, 255);
            nesPalette[0x07] = new ScreenColor(60, 24, 0, 255);
            nesPalette[0x08] = new ScreenColor(32, 42, 0, 255);
            nesPalette[0x09] = new ScreenColor(8, 58, 0, 255);
            nesPalette[0x0A] = new ScreenColor(0, 64, 0, 255);
            nesPalette[0x0B] = new ScreenColor(0, 60, 0, 255);
            nesPalette[0x0C] = new ScreenColor(0, 50, 60, 255);
            nesPalette[0x0D] = new ScreenColor(0, 0, 0, 255);
            nesPalette[0x0E] = new ScreenColor(0, 0, 0, 255);
            nesPalette[0x0F] = new ScreenColor(0, 0, 0, 255);
            nesPalette[0x10] = new ScreenColor(152, 150, 152, 255);
            nesPalette[0x11] = new ScreenColor(8, 76, 196, 255);
            nesPalette[0x12] = new ScreenColor(48, 50, 236, 255);
            nesPalette[0x13] = new ScreenColor(92, 30, 228, 255);
            nesPalette[0x14] = new ScreenColor(136, 20, 176, 255);
            nesPalette[0x15] = new ScreenColor(160, 20, 100, 255);
            nesPalette[0x16] = new ScreenColor(152, 34, 32, 255);
            nesPalette[0x17] = new ScreenColor(120, 60, 0, 255);
            nesPalette[0x18] = new ScreenColor(84, 90, 0, 255);
            nesPalette[0x19] = new ScreenColor(40, 114, 0, 255);
            nesPalette[0x1A] = new ScreenColor(8, 124, 0, 255);
            nesPalette[0x1B] = new ScreenColor(0, 118, 40, 255);
            nesPalette[0x1C] = new ScreenColor(0, 102, 120, 255);
            nesPalette[0x1D] = new ScreenColor(0, 0, 0, 255);
            nesPalette[0x1E] = new ScreenColor(0, 0, 0, 255);
            nesPalette[0x1F] = new ScreenColor(0, 0, 0, 255);
            nesPalette[0x20] = new ScreenColor(236, 238, 236, 255);
            nesPalette[0x21] = new ScreenColor(76, 154, 236, 255);
            nesPalette[0x22] = new ScreenColor(120, 124, 236, 255);
            nesPalette[0x23] = new ScreenColor(176, 98, 236, 255);
            nesPalette[0x24] = new ScreenColor(228, 84, 236, 255);
            nesPalette[0x25] = new ScreenColor(236, 88, 180, 255);
            nesPalette[0x26] = new ScreenColor(236, 106, 100, 255);
            nesPalette[0x27] = new ScreenColor(212, 136, 32, 255);
            nesPalette[0x28] = new ScreenColor(160, 170, 0, 255);
            nesPalette[0x29] = new ScreenColor(116, 196, 0, 255);
            nesPalette[0x2A] = new ScreenColor(76, 208, 32, 255);
            nesPalette[0x2B] = new ScreenColor(56, 204, 108, 255);
            nesPalette[0x2C] = new ScreenColor(56, 180, 204, 255);
            nesPalette[0x2D] = new ScreenColor(60, 60, 60, 255);
            nesPalette[0x2E] = new ScreenColor(0, 0, 0, 255);
            nesPalette[0x2F] = new ScreenColor(0, 0, 0, 255);
            nesPalette[0x30] = new ScreenColor(236, 238, 236, 255);
            nesPalette[0x31] = new ScreenColor(168, 204, 236, 255);
            nesPalette[0x32] = new ScreenColor(188, 188, 236, 255);
            nesPalette[0x33] = new ScreenColor(212, 178, 236, 255);
            nesPalette[0x34] = new ScreenColor(236, 174, 236, 255);
            nesPalette[0x35] = new ScreenColor(236, 174, 212, 255);
            nesPalette[0x36] = new ScreenColor(236, 180, 176, 255);
            nesPalette[0x37] = new ScreenColor(228, 196, 144, 255);
            nesPalette[0x38] = new ScreenColor(204, 210, 120, 255);
            nesPalette[0x39] = new ScreenColor(180, 222, 120, 255);
            nesPalette[0x3A] = new ScreenColor(168, 226, 144, 255);
            nesPalette[0x3B] = new ScreenColor(152, 226, 180, 255);
            nesPalette[0x3C] = new ScreenColor(160, 214, 228, 255);
            nesPalette[0x3D] = new ScreenColor(160, 162, 160, 255);
            nesPalette[0x3E] = new ScreenColor(0, 0, 0, 255);
            nesPalette[0x3F] = new ScreenColor(0, 0, 0, 255);
        }

        public byte cpuRead(ushort addr)
        {
            byte data = 0x00;

            switch (addr)
            {
                case 0x0000:        // Control
                    break;
                case 0x0001:        // Mask
                    break;
                case 0x0002:        // Status
                    break;
                case 0x0003:        // OAM Address
                    break;
                case 0x0004:        // OAM Data
                    break;
                case 0x0005:        // Scroll
                    break;
                case 0x0006:        // PPU Address 
                    break;
                case 0x0007:        // PPU Data
                    break;
            }

            return data;
        }

        public void cpuWrite(ushort addr, byte data)
        {
            switch (addr)
            {
                case 0x0000:        // Control
                    break;
                case 0x0001:        // Mask
                    break;
                case 0x0002:        // Status
                    break;
                case 0x0003:        // OAM Address
                    break;
                case 0x0004:        // OAM Data
                    break;
                case 0x0005:        // Scroll
                    break;
                case 0x0006:        // PPU Address
                    break;
                case 0x0007:        // PPU Data
                    break;
            }
        }

        public byte ppuRead(ushort addr)
        {
            byte data = 0x00;
            addr &= 0x3FFF;

            if (cart.ppuRead(addr, out data))
            {

            }
            else if (addr >= 0x0000 && addr <= 0x1FFF)
            {
                data = patternTable[(addr & 0x1000) >> 12, addr & 0x0FFF];
            }
            else if (addr >= 0x2000 && addr <= 0x3EFF)
            {

            }
            else if (addr >= 0x3F00 && addr <= 0x3FFF)
            {
                addr &= 0x001F;
                if (addr == 0x0010) addr = 0x0000;
                if (addr == 0x0014) addr = 0x0004;
                if (addr == 0x0018) addr = 0x0008;
                if (addr == 0x001C) addr = 0x000C;
                data = PaletteTable[addr];
            }
            return data;
        }

        public void ppuWrite(ushort addr, byte data)
        {
            addr &= 0x3FFF;

            if (cart.ppuWrite(addr, data))
            {

            }
            else if (addr >= 0x0000 && addr <= 0x1FFF)
            {
                patternTable[(addr & 0x1000) >> 12, addr & 0x0FFF] = data;
            }
            else if (addr >= 0x2000 && addr <= 0x3EFF)
            {

            }
            else if (addr >= 0x3F00 && addr <= 0x3FFF)
            {
                addr &= 0x001F;
                if (addr == 0x0010) addr = 0x0000;
                if (addr == 0x0014) addr = 0x0004;
                if (addr == 0x0018) addr = 0x0008;
                if (addr == 0x001C) addr = 0x000C;
                PaletteTable[addr] = data;
            }
        }

        public void ConnectCartridge(Cartridge c)
        {
            this.cart = c;

        }

        public void Clock()
        {
            if (!mainScreen.SetPixel(cycle - 1, scanline, nesPalette[rnd.Next(64)]))
            {
                //Console.WriteLine($"SetPixel error on NES main screen - cycle {cycle}");
            }
            //engine.DrawPixel(cycle - 1, scanline, nesPalette[rnd.Next(64)]);

            cycle++;

            if (cycle >= 341)
            {
                cycle = 0;
                scanline++;
                if (scanline >= 261)
                {
                    scanline = -1;
                    FrameComplete = true;
                }
            }
        }
    }
}

