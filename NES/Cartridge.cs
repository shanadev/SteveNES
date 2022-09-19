using System;
using System.Text;
using System.IO;

namespace NES
{

    public class Cartridge
    {
        private List<byte> PRG = new List<byte>();
        private List<byte> CHR = new List<byte>();

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

        public Mapper mapper;

        public Cartridge(string filename)
        {
            // open file in binary and read in the header
            using (BinaryReader binReader = new BinaryReader(File.Open(filename, FileMode.Open)))
            {
                name = binReader.ReadChars(4).ToString();
                //Encoding ascii = Encoding.ASCII;
                prg_rom_chunks = binReader.ReadByte();
                chr_rom_chunks = binReader.ReadByte();
                mapper1 = binReader.ReadByte();
                mapper2 = binReader.ReadByte();
                prg_ram_size = binReader.ReadByte();
                tv_system1 = binReader.ReadByte();
                tv_system2 = binReader.ReadByte();
                unused = binReader.ReadChars(5).ToString();

                if ((byte)(mapper1 & 0x04) > 0)
                {
                    unused += binReader.ReadChars(512).ToString();
                }

                mapperID = (byte)((byte)((byte)(mapper2 >> 4) << 4) | (byte)(mapper1 >> 4));

                byte fileType = 1;

                if (fileType == 0)
                {

                }

                if (fileType == 1)
                {
                    PRGbanks = prg_rom_chunks;
                    byte[] readBytes = binReader.ReadBytes(PRGbanks * 16384);
                    PRG.AddRange(readBytes);

                    CHRbanks = chr_rom_chunks;
                    byte[] readchrbytes = binReader.ReadBytes(CHRbanks * 8192);
                    CHR.AddRange(readchrbytes);

                }

                if (fileType == 2)
                {

                }

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

        public bool cpuRead(ushort addr, out byte data)
        {
            uint mapped_addr = 0;
            if (mapper.cpuMapRead(addr, out mapped_addr))
            {
                data = PRG[(int)mapped_addr];
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
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}

