using System;
using DisplayEngine;
//using Serilog;

namespace NES
{
    // Specific structs for PPU registers
    public struct Status
    {
        public byte unused;
        public byte sprite_overflow;
        public byte sprite_zero_hit;
        public byte vertical_blank;

        public byte reg
        {
            get { return (byte)((vertical_blank << 7) | (sprite_zero_hit << 6) | (sprite_overflow << 5) | unused); }
            //get { return (byte)((unused << 3) | (sprite_overflow << 2) | (sprite_zero_hit << 1) | vertical_blank); }
            set
            {
                unused = (byte)((value) & 0b00011111);
                sprite_overflow = (byte)((value & 0b00100000) >> 5);
                sprite_zero_hit = (byte)((value & 0b01000000) >> 6);
                vertical_blank = (byte)((value & 0b10000000) >> 7);
            }
        }
    }

    public struct Mask
    {
        public byte grayscale;
        public byte render_background_left;
        public byte render_sprites_left;
        public byte render_background;
        public byte render_sprites;
        public byte enhance_red;
        public byte enhance_green;
        public byte enhance_blue;

        public byte reg
        {
            get { return (byte)((enhance_blue << 7) | (enhance_green << 6) | (enhance_red << 5) | (render_sprites << 4) | (render_background << 3) | (render_sprites_left << 2) | (render_background_left << 1) | grayscale); }
            set
            {
                grayscale = (byte)((value) & 0b00000001);
                render_background_left = (byte)((value & 0b00000010) >> 1);
                render_sprites_left = (byte)((value & 0b00000100) >> 2);
                render_background = (byte)((value & 0b00001000) >> 3);
                render_sprites = (byte)((value & 0b00010000) >> 4);
                enhance_red = (byte)((value & 0b00100000) >> 5);
                enhance_green = (byte)((value & 0b01000000) >> 6);
                enhance_blue = (byte)((value & 0b10000000) >> 7);
            }
        }
    }

    public struct PPUCTRL
    {
        public byte nametable_x;
        public byte nametable_y;
        public byte increment_mode;
        public byte pattern_sprite;
        public byte pattern_background;
        public byte sprite_size;
        public byte slave_mode;
        public byte enable_nmi;

        public byte reg
        {
            get { return (byte)((enable_nmi << 7) | (slave_mode << 6) | (sprite_size << 5) | (pattern_background << 4) | (pattern_sprite << 3) | (increment_mode << 2) | (nametable_y << 1) | nametable_x); }
            set
            {
                nametable_x = (byte)((value) & 0b00000001);
                nametable_y = (byte)((value & 0b00000010) >> 1);
                increment_mode = (byte)((value & 0b00000100) >> 2);
                pattern_sprite = (byte)((value & 0b00001000) >> 3);
                pattern_background = (byte)((value & 0b00010000) >> 4);
                sprite_size = (byte)((value & 0b00100000) >> 5);
                slave_mode = (byte)((value & 0b01000000) >> 6);
                enable_nmi = (byte)((value & 0b10000000) >> 7);
            }
        }
    }

    public struct loopy_register
    {
        public ushort coarse_x;
        public ushort coarse_y;
        public ushort nametable_x;
        public ushort nametable_y;
        public ushort fine_y;
        public ushort unused;

        public ushort reg
        {
            get { return (ushort)((unused << 15) | (fine_y << 12) | (nametable_y << 11) | (nametable_x << 10) | (coarse_y << 5) | coarse_x); }
            set
            {
                coarse_x = (ushort)((value) & 0b0000_0000_0001_1111);
                coarse_y = (ushort)((value & 0b0000_0011_1110_0000) >> 5);
                nametable_x = (ushort)((value & 0b0000_0100_0000_0000) >> 10);
                nametable_y = (ushort)((value & 0b0000_1000_0000_0000) >> 11);
                fine_y = (ushort)((value & 0b0111_0000_0000_0000) >> 12);
                unused = (ushort)((value & 0b1000_0000_0000_0000) >> 15);
            }
        }
    }

    public class PPU
    {
        private Status status;
        private Mask mask;
        private PPUCTRL control;

        private loopy_register vram_addr;
        private loopy_register tram_addr;

        byte fine_x = 0x00;

        byte bg_next_tile_id = 0x00;
        byte bg_next_tile_attrib = 0x00;
        byte bg_next_tile_lsb = 0x00;
        byte bg_next_tile_msb = 0x00;

        ushort bg_shifter_pattern_lo = 0x0000;
        ushort bg_shifter_pattern_hi = 0x0000;
        ushort bg_shifter_attrib_lo = 0x0000;
        ushort bg_shifter_attrib_hi = 0x0000;

        private Random rnd = new Random();

        private Cartridge cart;
        private Bus bus;
        public byte[,] nameTable = new byte[2, 1024];
        public byte[,] patternTable = new byte[2, 4096];

        public ScreenColor[] nesPalette = new ScreenColor[64];

        public bool nmi = false;

        private byte[] PaletteTable = new byte[32];
        private Sprite mainScreen = new Sprite(256, 240);
        private Sprite[] nameTableSprite = new Sprite[2] { new Sprite(256, 240), new Sprite(256, 240) };
        private Sprite[] patternTableSprite = new Sprite[2] { new Sprite(128, 128), new Sprite(128, 128) };

        private int scanline = 0; // row on screen
        private int cycle = 0; // row on screen

        public bool FrameComplete { get; set; } = false;
        public bool ScanlineComplete { get; set; } = false;

        private byte address_latch = 0x00;
        private byte ppu_data_buffer = 0x00;
        //private ushort ppu_address = 0x0000;  // oversimplification

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
            //for (int tileY = 0; tileY < 16; tileY++)
            //{
            //    for (int tileX = 0; tileX < 16; tileX++)
            //    {
            //        int offset = tileY * 256 + tileX * 16;

            //        for (int row = 0; row < 8; row++)
            //        {
            //            byte tile_lsb = ppuRead((byte)(i * 0x1000 + offset + row + 0));
            //            byte tile_msb = ppuRead((byte)(i * 0x1000 + offset + row + 8));

            //            for (int col = 0; col < 8; col++)
            //            {
            //                byte px = (byte)((tile_lsb & 0x01) + (tile_msb & 0x01));
            //                tile_lsb >>= 1;
            //                tile_msb >>= 1;

            //                patternTableSprite[i].SetPixel(tileX * 8 + (7 - col), tileY * 8 + row, GetColorFromPaletteRam(palette, px));
            //            }
            //        }

            //    }
            //}
            //return patternTableSprite[i];


            for (int tileY = 0; tileY < 16; tileY++)
            {
                for (int tileX = 0; tileX < 16; tileX++)
                {
                    int offset = (tileY) * 256 + tileX * 16;
                    //Log.Debug($"Getting Tile: X {tileX} Y {tileY} - offset {offset}");
                    for (int row = 0; row < 8; row++)
                    {

                        byte tile_lsb = ppuRead((ushort)(i * 0x1000 + offset + row + 0));
                        byte tile_msb = ppuRead((ushort)(i * 0x1000 + offset + row + 8));

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

            int index = ppuRead((ushort)(0x3F00 + (palette << 2) + pixel));
            //if (index > 64) return new ScreenColor(0, 0, 0, 255);
            //Log.Debug($"Get Color: palette:{Convert.ToString(palette, toBase:16).PadLeft(2,'0')} - pixel:{Convert.ToString(pixel, toBase: 16).PadLeft(2, '0')}, ColorIndex:{Convert.ToString(index, toBase: 16).PadLeft(2, '0')}");
            return nesPalette[index];
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

        public byte cpuRead(ushort addr, bool readOnly)
        {
            byte data = 0x00;

            if (readOnly)
            {

                switch (addr)
                {
                    case 0x0000:        // Control
                        data = control.reg;
                        break;
                    case 0x0001:        // Mask
                        data = mask.reg;
                        break;
                    case 0x0002:        // Status
                        data = status.reg;
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
            else
            {
                //Log.Debug($"PPU cpuRead: addr:{Convert.ToString(addr, toBase: 16).PadLeft(4, '0')}");
                switch (addr)
                {
                    case 0x0000:        // Control
                        break;
                    case 0x0001:        // Mask
                        break;
                    case 0x0002:        // Status
                        //status.vertical_blank = 1;
                        //data = (byte)(status.reg);
                        data = (byte)((status.reg & 0xE0) | (ppu_data_buffer & 0x1F));
                        status.vertical_blank = 0;
                        address_latch = 0;
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
                        data = ppu_data_buffer;
                        ppu_data_buffer = ppuRead(vram_addr.reg);
                        if (vram_addr.reg > 0x3F00) data = ppu_data_buffer;
                        //Log.Debug($"Read from PPU: addr {Convert.ToString(ppu_address, toBase: 16).PadLeft(4, '0')} - data {Convert.ToString(data, toBase: 16).PadLeft(2, '0')}");

                        vram_addr.reg += (ushort)(control.increment_mode > 0 ? 32 : 1);

                        break;
                }
            }

            return data;
        }

        public void cpuWrite(ushort addr, byte data)
        {
            //Log.Debug($"PPU cpuWrite: addr:{Convert.ToString(addr, toBase: 16).PadLeft(4, '0')} data:{Convert.ToString(data, toBase: 16).PadLeft(2, '0')}");

            switch (addr)
            {
                case 0x0000:        // Control
                    control.reg = data;
                    tram_addr.nametable_x = control.nametable_x;
                    tram_addr.nametable_y = control.nametable_y;
                    break;
                case 0x0001:        // Mask
                    mask.reg = data;
                    break;
                case 0x0002:        // Status
                    break;
                case 0x0003:        // OAM Address
                    break;
                case 0x0004:        // OAM Data
                    break;
                case 0x0005:        // Scroll
                    if (address_latch == 0)
                    {
                        fine_x = (byte)(data & 0x07);
                        tram_addr.coarse_x = (ushort)(data >> 3);
                        address_latch = 1;
                    }
                    else
                    {
                        tram_addr.fine_y = (ushort)(data & 0x07);
                        tram_addr.coarse_y = (ushort)(data >> 3);
                        address_latch = 0;
                    }

                    break;
                case 0x0006:        // PPU Address
                    if (address_latch == 0)
                    {
                        tram_addr.reg = (ushort)((tram_addr.reg & 0x00FF) | (data & 0x3F) << 8);
                        address_latch = 1;
                    }
                    else
                    {
                        tram_addr.reg = (ushort)((tram_addr.reg & 0xFF00) | data);
                        vram_addr = tram_addr;
                        address_latch = 0;
                    }
                    break;
                case 0x0007:        // PPU Data
                    //Log.Debug($"Write to PPU: addr {Convert.ToString(ppu_address, toBase: 16).PadLeft(4, '0')} - data: {Convert.ToString(data, toBase: 16).PadLeft(2, '0')}");
                    ppuWrite(vram_addr.reg, data);
                    vram_addr.reg += (ushort)(control.increment_mode > 0 ? 32 : 1);
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
                //Log.Debug($"Pattern Read: addr:{Convert.ToString(addr, toBase: 16).PadLeft(4, '0')} - data {Convert.ToString(data, toBase: 16).PadLeft(2, '0')}");

                data = patternTable[(addr & 0x1000) >> 12, addr & 0x0FFF];
            }
            else if (addr >= 0x2000 && addr <= 0x3EFF)
            {
                addr &= 0x0FFF;

                if (cart.mirror == Cartridge.MIRROR.VERTICAL)
                {
                    if (addr >= 0x0000 && addr <= 0x03FF)
                        data = nameTable[0, addr & 0x03FF];
                    if (addr >= 0x0400 && addr <= 0x07FF)
                        data = nameTable[1, addr & 0x03FF];
                    if (addr >= 0x0800 && addr <= 0x0BFF)
                        data = nameTable[0, addr & 0x03FF];
                    if (addr >= 0x0C00 && addr <= 0x0FFF)
                        data = nameTable[1, addr & 0x03FF];
                }
                else if (cart.mirror == Cartridge.MIRROR.HORIZONTAL)
                {
                    if (addr >= 0x0000 && addr <= 0x03FF)
                        data = nameTable[0, addr & 0x03FF];
                    if (addr >= 0x0400 && addr <= 0x07FF)
                        data = nameTable[0, addr & 0x03FF];
                    if (addr >= 0x0800 && addr <= 0x0BFF)
                        data = nameTable[1, addr & 0x03FF];
                    if (addr >= 0x0C00 && addr <= 0x0FFF)
                        data = nameTable[1, addr & 0x03FF];
                }
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
                //Log.Debug($"Pattern Write: addr:{Convert.ToString(addr, toBase: 16).PadLeft(4, '0')} - data: {Convert.ToString(data, toBase: 16).PadLeft(2, '0')}");

                patternTable[(addr & 0x1000) >> 12, addr & 0x0FFF] = data;
            }
            else if (addr >= 0x2000 && addr <= 0x3EFF)
            {
                addr &= 0x0FFF;
                if (cart.mirror == Cartridge.MIRROR.VERTICAL)
                {
                    if (addr >= 0x0000 && addr <= 0x03FF)
                        nameTable[0, addr & 0x03FF] = data;
                    if (addr >= 0x0400 && addr <= 0x07FF)
                        nameTable[1, addr & 0x03FF] = data;
                    if (addr >= 0x0800 && addr <= 0x0BFF)
                        nameTable[0, addr & 0x03FF] = data;
                    if (addr >= 0x0C00 && addr <= 0x0FFF)
                        nameTable[1, addr & 0x03FF] = data;
                }
                else if (cart.mirror == Cartridge.MIRROR.HORIZONTAL)
                {
                    if (addr >= 0x0000 && addr <= 0x03FF)
                        nameTable[0, addr & 0x03FF] = data;
                    if (addr >= 0x0400 && addr <= 0x07FF)
                        nameTable[0, addr & 0x03FF] = data;
                    if (addr >= 0x0800 && addr <= 0x0BFF)
                        nameTable[1, addr & 0x03FF] = data;
                    if (addr >= 0x0C00 && addr <= 0x0FFF)
                        nameTable[1, addr & 0x03FF] = data;
                }
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

        public void Reset()
        {
            fine_x = 0x00;
            address_latch = 0x00;
            ppu_data_buffer = 0x00;
            scanline = 0;
            cycle = 0;
            bg_next_tile_id = 0x00;
            bg_next_tile_attrib = 0x00;
            bg_next_tile_lsb = 0x00;
            bg_next_tile_msb = 0x00;
            status.reg = 0x00;
            mask.reg = 0x00;
            control.reg = 0x00;
            vram_addr.reg = 0x00;
            tram_addr.reg = 0x00;

            bg_shifter_pattern_lo = 0x0000;
            bg_shifter_pattern_hi = 0x0000;
            bg_shifter_attrib_lo = 0x0000;
            bg_shifter_attrib_hi = 0x0000;
        }


        private void IncrementScrollX()
        {
            if (mask.render_background > 0 || mask.render_sprites > 0)
            {
                if (vram_addr.coarse_x == 31)
                {
                    vram_addr.coarse_x = 0;
                    vram_addr.nametable_x = (ushort)(~vram_addr.nametable_x & 0x0001);
                }
                else
                {
                    vram_addr.coarse_x++;
                }
            }
        }

        private void IncrementScrollY()
        {
            if (mask.render_background > 0 || mask.render_sprites > 0)
            {
                if (vram_addr.fine_y < 7)
                {
                    vram_addr.fine_y++;
                }
                else
                {
                    vram_addr.fine_y = 0;
                    if (vram_addr.coarse_y == 29)
                    {
                        vram_addr.coarse_y = 0;
                        vram_addr.nametable_y = (ushort)(~vram_addr.nametable_y);
                    }
                    else if (vram_addr.coarse_y == 31)
                    {
                        vram_addr.coarse_y = 0;
                    }
                    else
                    {
                        vram_addr.coarse_y++;
                    }
                }
            }
        }

        private void TransferAddressX()
        {
            if (mask.render_background > 0 || mask.render_sprites > 0)
            {
                vram_addr.nametable_x = tram_addr.nametable_x;
                vram_addr.coarse_x = tram_addr.coarse_x;
            }
        }

        private void TransferAddressY()
        {
            if (mask.render_background > 0 || mask.render_sprites > 0)
            {
                vram_addr.fine_y = tram_addr.fine_y;
                vram_addr.nametable_y = tram_addr.nametable_y;
                vram_addr.coarse_y = tram_addr.coarse_y;
            }
        }

        private void LoadBackgroundShifters()
        {
            bg_shifter_pattern_lo = (ushort)((bg_shifter_pattern_lo & 0xFF00) | bg_next_tile_lsb);
            bg_shifter_pattern_hi = (ushort)((bg_shifter_pattern_hi & 0xFF00) | bg_next_tile_msb);

            bg_shifter_attrib_lo = (ushort)((bg_shifter_attrib_lo & 0xFF00) | ((bg_next_tile_attrib & 0b01) > 0 ? 0xFF : 0x00));
            bg_shifter_attrib_hi = (ushort)((bg_shifter_attrib_hi & 0xFF00) | ((bg_next_tile_attrib & 0b10) > 0 ? 0xFF : 0x00));
        }

        private void UpdateShifters()
        {
            if (mask.render_background > 0)
            {
                bg_shifter_pattern_lo <<= 1;
                bg_shifter_pattern_hi <<= 1;

                bg_shifter_attrib_lo <<= 1;
                bg_shifter_attrib_hi <<= 1;
            }
        }

        public void Clock()
        {
            if (scanline >= -1 && scanline < 240)
            {
                if (scanline == -1 && cycle == 1)
                {
                    status.vertical_blank = 0;
                }

                if ((cycle >= 2 && cycle < 258) || (cycle >= 321 && cycle < 338))
                {
                    UpdateShifters();
                    switch ((cycle - 1) % 8)
                    {
                        case 0:
                            LoadBackgroundShifters();
                            bg_next_tile_id = ppuRead((ushort)(0x2000 | (vram_addr.reg & 0x0FFF)));
                            break;
                        case 2:
                            bg_next_tile_attrib = ppuRead((ushort)(0x23C0 | (vram_addr.nametable_y << 11)
                                                                 | (vram_addr.nametable_x << 10)
                                                                 | ((vram_addr.coarse_y >> 2) << 3)
                                                                 | (vram_addr.coarse_x >> 2)));
                            if ((vram_addr.coarse_y & 0x02) > 0) bg_next_tile_attrib >>= 4;
                            if ((vram_addr.coarse_x & 0x02) > 0) bg_next_tile_attrib >>= 2;
                            bg_next_tile_attrib &= 0x03;
                            break;
                        case 4:
                            bg_next_tile_lsb = ppuRead((ushort)((control.pattern_background << 12)
                                                        + ((ushort)(bg_next_tile_id << 4))
                                                        + (vram_addr.fine_y) + 0));
                            break;
                        case 6:
                            bg_next_tile_msb = ppuRead((ushort)((control.pattern_background << 12)
                            + ((ushort)(bg_next_tile_id << 4))
                            + (vram_addr.fine_y) + 8));
                            break;
                        case 7:
                            IncrementScrollX();
                            break;
                    }
                }

                if (cycle == 256)
                {
                    IncrementScrollY();
                }

                if (cycle == 257)
                {
                    LoadBackgroundShifters();
                    TransferAddressX();
                }

                if (cycle == 338 || cycle == 340)
                {
                    bg_next_tile_id = ppuRead((ushort)(0x2000 | (vram_addr.reg & 0x0FFF)));
                }

                if (scanline == -1 && cycle >= 280 && cycle < 305)
                {
                    TransferAddressY();
                }
            }

            if (scanline == 240)
            {

            }

            if (scanline == 241 && cycle == 1)
            {
                status.vertical_blank = 1;
                if (control.enable_nmi > 0)
                {
                    nmi = true;
                }
            }

            //if (!mainScreen.SetPixel(cycle - 1, scanline, nesPalette[rnd.Next(64)]))
            //{
            //    //Console.WriteLine($"SetPixel error on NES main screen - cycle {cycle}");
            //}
            //engine.DrawPixel(cycle - 1, scanline, nesPalette[rnd.Next(64)]);

            byte bg_pixel = 0x00;
            byte bg_palette = 0x00;

            if (mask.render_background > 0)
            {
                ushort bit_mux = (ushort)(0x8000 >> fine_x);
                byte p0_pixel = (byte)((bg_shifter_pattern_lo & bit_mux) > 0 ? 1 : 0);
                byte p1_pixel = (byte)((bg_shifter_pattern_hi & bit_mux) > 0 ? 1 : 0);

                bg_pixel = (byte)((p1_pixel << 1) | p0_pixel);

                byte bg_pal0 = (byte)((bg_shifter_attrib_lo & bit_mux) > 0 ? 1 : 0);
                byte bg_pal1 = (byte)((bg_shifter_attrib_hi & bit_mux) > 0 ? 1 : 0);
                bg_palette = (byte)((bg_pal1 << 1) | bg_pal0);
            }

            mainScreen.SetPixel(cycle - 1, scanline, GetColorFromPaletteRam(bg_palette, bg_pixel));

            cycle++;

            if (cycle >= 341)
            {
                cycle = 0;
                scanline++;
                ScanlineComplete = true;
                if (scanline >= 261)
                {
                    scanline = -1;
                    FrameComplete = true;
                }
            }
        }
    }
}

