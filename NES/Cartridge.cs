using System;
using System.Text;
using System.IO;
//using Serilog;

namespace NES
{
    // Class representing a cartridge and the data contained. Has passthrough areas for the mappers
    public class Cartridge
    {
        // enum for the type of mirroring - sometimes hard-wired on the game chip
        public enum MIRROR
        {
            HORIZONTAL,
            VERTICAL,
            ONESCREEN_LO,
            ONESCREEN_HI
        }

        // Our Program and Character data
        private List<byte> PRG = new List<byte>();
        private List<byte> CHR = new List<byte>();

        // Info from the file
        private byte mapperID = 0;
        private byte PRGbanks = 0;
        private byte CHRbanks = 0;

        // header info
        string name;
        byte prg_rom_chunks;
        byte chr_rom_chunks;
        byte mapper1;
        byte mapper2;
        byte prg_ram_size;
        byte tv_system1;
        byte tv_system2;
        string unused;

        // The mapper instance that will be set when we know which mapper is needed
        public Mapper mapper;
        public MIRROR mirror; // represent the mirror mode


        // Constructor - open the file and read it - we're assumiung iNes format
        public Cartridge(string filename)
        {
            // open file in binary and read in the header
            using (BinaryReader binReader = new BinaryReader(File.Open(filename, FileMode.Open)))
            {
                // Read the header info
                name = string.Join(null, binReader.ReadChars(4));
                prg_rom_chunks = binReader.ReadByte();
                chr_rom_chunks = binReader.ReadByte();
                mapper1 = binReader.ReadByte();
                mapper2 = binReader.ReadByte();
                prg_ram_size = binReader.ReadByte();
                tv_system1 = binReader.ReadByte();
                tv_system2 = binReader.ReadByte();
                unused = string.Join(null,binReader.ReadChars(5));

                // Maybe this trainer area
                if ((byte)(mapper1 & 0x04) > 0)
                {
                    unused += string.Join(null,binReader.ReadChars(512));
                }

                // Determine mapper and mirroring
                mapperID = (byte)((byte)((byte)(mapper2 >> 4) << 4) | (byte)(mapper1 >> 4));
                mirror = (mapper1 & 0x01) > 0 ? MIRROR.VERTICAL : MIRROR.HORIZONTAL;

                // Hard-coding this for now
                byte fileType = 1;

                if (fileType == 0)
                {

                }

                if (fileType == 1)
                {
                    // Read in the program data which is next
                    PRGbanks = prg_rom_chunks;
                    byte[] readBytes = binReader.ReadBytes(PRGbanks * 16384);
                    PRG.AddRange(readBytes);

                    // Read in the character data
                    CHRbanks = chr_rom_chunks;
                    byte[] readchrbytes;
                    if (CHRbanks == 0)
                    {
                        readchrbytes = binReader.ReadBytes(8192);
                    }
                    else
                    {
                        readchrbytes = binReader.ReadBytes(CHRbanks * 8192);
                    }
                    CHR.AddRange(readchrbytes);

                    // for debugging
                    //var asString = string.Empty;
                    //foreach (var inst in PRG)
                    //{
                    //    asString += CPU.Hex(inst, 2) + " ";
                    //}

                    //var asString = string.Join(' ', PRG);
                    //Log.Debug($"{asString}");
                    //Log.Debug("ENDEND");
                }

                if (fileType == 2)
                {

                }

                // Based on mapper id, assign a new mapper instance of the correct
                // mapper type (Mapper is an Abstract class)
                switch (mapperID)
                {
                    case 0:
                        mapper = new Mapper_000(PRGbanks, CHRbanks);
                        break;
                    default:
                        break;
                }

            }

        }


        // Read and Write methods for both the CPU and PPU - all can be overridden by a mapper

        public bool cpuRead(int addr, out byte data)
        {
            uint mapped_addr = 0;
            if (mapper.cpuMapRead((ushort)addr, out mapped_addr))
            {
                data = PRG[(int)mapped_addr];
                //Log.Debug($"Read from Cart PRG - mapped_addr:0x{Convert.ToString(mapped_addr, toBase: 16).PadLeft(4, '0')} - data:0x{Convert.ToString(data, toBase: 16).PadLeft(2, '0')}");
                return true;
            }
            data = 0x00;
            return false;
        }

        public bool cpuWrite(ushort addr, byte data)
        {
            uint mapped_addr = 0;
            if (mapper.cpuMapWrite(addr, out mapped_addr))
            {
                PRG[(int)mapped_addr] = data;
                //Log.Debug($"Write to Cart PRG - mapped_addr:0x{Convert.ToString(mapped_addr, toBase: 16).PadLeft(4, '0')} - data:0x{Convert.ToString(data, toBase: 16).PadLeft(2, '0')}");

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool ppuRead(ushort addr, out byte data)
        {
            uint mapped_addr = 0;
            if (mapper.ppuMapRead(addr, out mapped_addr))
            {
                data = CHR[(int)mapped_addr];
                //Log.Debug($"Read from Cart CHR - mapped_addr:0x{Convert.ToString(mapped_addr, toBase: 16).PadLeft(4, '0')} - data:0x{Convert.ToString(data, toBase: 16).PadLeft(2, '0')}");

                return true;
            }
            else
            {
                data = 0x00;
                return false;
            }
        }

        public bool ppuWrite(ushort addr, byte data)
        {
            uint mapped_addr = 0;
            if (mapper.ppuMapWrite(addr, out mapped_addr))
            {
                CHR[(int)mapped_addr] = data;
                //Log.Debug($"Write to Cart CHR - mapped_addr:0x{Convert.ToString(mapped_addr, toBase: 16).PadLeft(4, '0')} - data:0x{Convert.ToString(data, toBase: 16).PadLeft(2, '0')}");

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}

