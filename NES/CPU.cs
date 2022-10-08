using System;
using Serilog;

namespace NES
{
    /// <summary>
    /// An INSTRUCTION is a struct to hold all of the pieces of an operation that I need as well as the friendly name for disassembling
    /// Friendly name, the operation is the function that will perform the actual opcode action,
    /// Addressing mode function performs the data pull based on the mode, and the number of base cycles the instruction takes the CPU
    /// to run. -- We'll make an array of these later that we will access using the opcode as the index
    /// </summary>
    public struct INSTRUCTION
    {
        public string Name;
        public Func<byte>? Operation = null;
        public Func<byte>? AddrMode = null;
        public int cycles = 0;

        public INSTRUCTION(string name, Func<byte> operation, Func<byte> addrmode, int cyclesIn)
        {
            Name = name;
            Operation = operation;
            AddrMode = addrmode;
            cycles = cyclesIn;
        }
    }


    /// <summary>
    /// Implementation of the 6502 processor - specifically for the NES
    /// </summary>
    public class CPU
    {
        private uint instructCount = 0;
        public string debugmodedata = "";

        // We'll inject the Bus that this CPU will belong to so we can call read/write on the bus
        private Bus bus;

        // Registers
        public byte a = 0x00;       // Accumulator
        public byte x = 0x00;       // X register
        public byte y = 0x00;       // Y register
        public byte stkp = 0x00;    // Stack pointer
        public ushort pc = 0x0000;  // program counter
        public byte status = 0x00;  // Status register (See FLAGS6502 above)

        // Flags enum
        public enum FLAGS6502
        {
            C = (1 << 0),   // Carry Bit
            Z = (1 << 1),   // Zero
            I = (1 << 2),   // Disable Interrupts
            D = (1 << 3),   // Decimal Mode
            B = (1 << 4),   // Break
            U = (1 << 5),   // Unused
            V = (1 << 6),   // Overflow
            N = (1 << 7)    // Negative
        }

        // Private Helpers for the FLAGS6502 - which is really the STATUS register
        private byte GetFlag(FLAGS6502 f)
        {
            if ((byte)(status & ((byte)f)) > 0)
            {
                return 1;
            }
            return 0;
        }

        private void SetFlag(FLAGS6502 f, bool v)
        {
            //Log.Debug($"SetFlag: {f} to {v}");
            if (v)
            {
                status |= ((byte)f);
            }
            else
            {
                status &= (byte)(~((byte)f));
            }
        }
        public byte debugfetched = 0x00;

        // Fetch data from addr_abs which should be set by the addressing mode function happening first
        // which will set addr_abs
        private byte fetch()
        {
            if (!(lookup[opcode].AddrMode == IMP))
            {
                fetched = read(addr_abs);
            }
            debugfetched = fetched;
            return fetched;
        }

        private byte fetched = 0x00;        // Fetched data
        private ushort addr_abs = 0x0000;   // The absolute address to grab data
        private ushort addr_rel = 0x0000;   // the relative address to grab data
        private byte opcode = 0x00;         // The instruction code from the program
        private int cycles = 0;             // keep track of cycles

        private uint clock_count = 0;

        // Here's our instruction array
        private INSTRUCTION[] lookup;

        /// <summary>
        /// CPU Constructor
        /// </summary>
        /// <param name="busIn">Pass the bus class that this CPU ultimately belongs to and is on</param>
        public CPU(Bus busIn)
        {
            this.bus = busIn;

            // Filling the instruction array
            // https://i.redd.it/m23p0jhvfwx81.jpg
            // The matrix works in such a way that the given opcode will numerically be the index, starting at 00 at the top left, to 0F, then 10-1F and so on
            lookup = new INSTRUCTION[]
            {
                new INSTRUCTION("BRK", BRK, IMP, 7), new INSTRUCTION("ORA", ORA, IZX, 6), new INSTRUCTION("???", XXX, IMP, 2), new INSTRUCTION("???", XXX, IMP, 8), new INSTRUCTION("???", NOP, IMP, 3), new INSTRUCTION("ORA", ORA, ZP0, 3), new INSTRUCTION("ASL", ASL, ZP0, 5), new INSTRUCTION("???", XXX, IMP, 5), new INSTRUCTION("PHP", PHP, IMP, 3), new INSTRUCTION("ORA", ORA, IMM, 2), new INSTRUCTION("ASL", ASL, IMP, 2), new INSTRUCTION("???", XXX, IMP, 2), new INSTRUCTION("???", NOP, IMP, 4), new INSTRUCTION("ORA", ORA, ABS, 4), new INSTRUCTION("ASL", ASL, ABS, 6), new INSTRUCTION("???", XXX, IMP, 6),
                new INSTRUCTION("BPL", BPL, REL, 2), new INSTRUCTION("ORA", ORA, IZY, 5), new INSTRUCTION("???", XXX, IMP, 2), new INSTRUCTION("???", XXX, IMP, 8), new INSTRUCTION("???", NOP, IMP, 4), new INSTRUCTION("ORA", ORA, ZPX, 4), new INSTRUCTION("ASL", ASL, ZPX, 6), new INSTRUCTION("???", XXX, IMP, 6), new INSTRUCTION("CLC", CLC, IMP, 2), new INSTRUCTION("ORA", ORA, ABY, 4), new INSTRUCTION("???", NOP, IMP, 2), new INSTRUCTION("???", XXX, IMP, 7), new INSTRUCTION("???", NOP, IMP, 4), new INSTRUCTION("ORA", ORA, ABX, 4), new INSTRUCTION("ASL", ASL, ABX, 7), new INSTRUCTION("???", XXX, IMP, 7),
                new INSTRUCTION("JSR", JSR, ABS, 6), new INSTRUCTION("AND", AND, IZX, 6), new INSTRUCTION("???", XXX, IMP, 2), new INSTRUCTION("???", XXX, IMP, 8), new INSTRUCTION("BIT", BIT, ZP0, 3), new INSTRUCTION("AND", AND, ZP0, 3), new INSTRUCTION("ROL", ROL, ZP0, 5), new INSTRUCTION("???", XXX, IMP, 5), new INSTRUCTION("PLP", PLP, IMP, 4), new INSTRUCTION("AND", AND, IMM, 2), new INSTRUCTION("ROL", ROL, IMP, 2), new INSTRUCTION("???", XXX, IMP, 2), new INSTRUCTION("BIT", BIT, ABS, 4), new INSTRUCTION("AND", AND, ABS, 4), new INSTRUCTION("ROL", ROL, ABS, 6), new INSTRUCTION("???", XXX, IMP, 6),
                new INSTRUCTION("BMI", BMI, REL, 2), new INSTRUCTION("AND", AND, IZY, 5), new INSTRUCTION("???", XXX, IMP, 2), new INSTRUCTION("???", XXX, IMP, 8), new INSTRUCTION("???", NOP, IMP, 4), new INSTRUCTION("AND", AND, ZPX, 4), new INSTRUCTION("ROL", ROL, ZPX, 6), new INSTRUCTION("???", XXX, IMP, 6), new INSTRUCTION("SEC", SEC, IMP, 2), new INSTRUCTION("AND", AND, ABY, 4), new INSTRUCTION("???", NOP, IMP, 2), new INSTRUCTION("???", XXX, IMP, 7), new INSTRUCTION("???", NOP, IMP, 4), new INSTRUCTION("AND", AND, ABX, 4), new INSTRUCTION("ROL", ROL, ABX, 7), new INSTRUCTION("???", XXX, IMP, 7),
                new INSTRUCTION("RTI", RTI, IMP, 6), new INSTRUCTION("EOR", EOR, IZX, 6), new INSTRUCTION("???", XXX, IMP, 2), new INSTRUCTION("???", XXX, IMP, 8), new INSTRUCTION("???", NOP, IMP, 3), new INSTRUCTION("EOR", EOR, ZP0, 3), new INSTRUCTION("LSR", LSR, ZP0, 5), new INSTRUCTION("???", XXX, IMP, 5), new INSTRUCTION("PHA", PHA, IMP, 3), new INSTRUCTION("EOR", EOR, IMM, 2), new INSTRUCTION("LSR", LSR, IMP, 2), new INSTRUCTION("???", XXX, IMP, 2), new INSTRUCTION("JMP", JMP, ABS, 3), new INSTRUCTION("EOR", EOR, ABS, 4), new INSTRUCTION("LSR", LSR, ABS, 6), new INSTRUCTION("???", XXX, IMP, 6),
                new INSTRUCTION("BVC", BVC, REL, 2), new INSTRUCTION("EOR", EOR, IZY, 5), new INSTRUCTION("???", XXX, IMP, 2), new INSTRUCTION("???", XXX, IMP, 8), new INSTRUCTION("???", NOP, IMP, 4), new INSTRUCTION("EOR", EOR, ZPX, 4), new INSTRUCTION("LSR", LSR, ZPX, 6), new INSTRUCTION("???", XXX, IMP, 6), new INSTRUCTION("CLI", CLI, IMP, 2), new INSTRUCTION("EOR", EOR, ABY, 4), new INSTRUCTION("???", NOP, IMP, 2), new INSTRUCTION("???", XXX, IMP, 7), new INSTRUCTION("???", NOP, IMP, 4), new INSTRUCTION("EOR", EOR, ABX, 4), new INSTRUCTION("LSR", LSR, ABX, 7), new INSTRUCTION("???", XXX, IMP, 7),
                new INSTRUCTION("RTS", RTS, IMP, 6), new INSTRUCTION("ADC", ADC, IZX, 6), new INSTRUCTION("???", XXX, IMP, 2), new INSTRUCTION("???", XXX, IMP, 8), new INSTRUCTION("???", NOP, IMP, 3), new INSTRUCTION("ADC", ADC, ZP0, 3), new INSTRUCTION("ROR", ROR, ZP0, 5), new INSTRUCTION("???", XXX, IMP, 5), new INSTRUCTION("PLA", PLA, IMP, 4), new INSTRUCTION("ADC", ADC, IMM, 2), new INSTRUCTION("ROR", ROR, IMP, 2), new INSTRUCTION("???", XXX, IMP, 2), new INSTRUCTION("JMP", JMP, IND, 5), new INSTRUCTION("ADC", ADC, ABS, 4), new INSTRUCTION("ROR", ROR, ABS, 6), new INSTRUCTION("???", XXX, IMP, 6),
                new INSTRUCTION("BVS", BVS, REL, 2), new INSTRUCTION("ADC", ADC, IZY, 5), new INSTRUCTION("???", XXX, IMP, 2), new INSTRUCTION("???", XXX, IMP, 8), new INSTRUCTION("???", NOP, IMP, 4), new INSTRUCTION("ADC", ADC, ZPX, 4), new INSTRUCTION("ROR", ROR, ZPX, 6), new INSTRUCTION("???", XXX, IMP, 6), new INSTRUCTION("SEI", SEI, IMP, 2), new INSTRUCTION("ADC", ADC, ABY, 4), new INSTRUCTION("???", NOP, IMP, 2), new INSTRUCTION("???", XXX, IMP, 7), new INSTRUCTION("???", NOP, IMP, 4), new INSTRUCTION("ADC", ADC, ABX, 4), new INSTRUCTION("ROR", ROR, ABX, 7), new INSTRUCTION("???", XXX, IMP, 7),
                new INSTRUCTION("???", NOP, IMM, 2), new INSTRUCTION("STA", STA, IZX, 6), new INSTRUCTION("???", NOP, IMP, 2), new INSTRUCTION("???", XXX, IMP, 8), new INSTRUCTION("STY", STY, ZP0, 3), new INSTRUCTION("STA", STA, ZP0, 3), new INSTRUCTION("STX", STX, ZP0, 3), new INSTRUCTION("???", XXX, IMP, 3), new INSTRUCTION("DEY", DEY, IMP, 2), new INSTRUCTION("???", NOP, IMP, 2), new INSTRUCTION("TXA", TXA, IMP, 2), new INSTRUCTION("???", XXX, IMP, 2), new INSTRUCTION("STY", STY, ABS, 4), new INSTRUCTION("STA", STA, ABS, 4), new INSTRUCTION("STX", STX, ABS, 4), new INSTRUCTION("???", XXX, IMP, 4),
                new INSTRUCTION("BCC", BCC, REL, 2), new INSTRUCTION("STA", STA, IZY, 6), new INSTRUCTION("???", XXX, IMP, 2), new INSTRUCTION("???", XXX, IMP, 8), new INSTRUCTION("STY", STY, ZPX, 4), new INSTRUCTION("STA", STA, ZPX, 4), new INSTRUCTION("STX", STX, ZPY, 4), new INSTRUCTION("???", XXX, IMP, 4), new INSTRUCTION("TYA", TYA, IMP, 2), new INSTRUCTION("STA", STA, ABY, 5), new INSTRUCTION("TXS", TXS, IMP, 2), new INSTRUCTION("???", XXX, IMP, 5), new INSTRUCTION("???", XXX, IMP, 5), new INSTRUCTION("STA", STA, ABX, 5), new INSTRUCTION("???", XXX, IMP, 5), new INSTRUCTION("???", XXX, IMP, 5),
                new INSTRUCTION("LDY", LDY, IMM, 2), new INSTRUCTION("LDA", LDA, IZX, 6), new INSTRUCTION("LDX", LDX, IMM, 2), new INSTRUCTION("???", XXX, IMP, 8), new INSTRUCTION("LDY", LDY, ZP0, 3), new INSTRUCTION("LDA", LDA, ZP0, 3), new INSTRUCTION("LDX", LDX, ZP0, 3), new INSTRUCTION("???", XXX, IMP, 3), new INSTRUCTION("TAY", TAY, IMP, 2), new INSTRUCTION("LDA", LDA, IMM, 2), new INSTRUCTION("TAX", TAX, IMP, 2), new INSTRUCTION("???", XXX, IMP, 2), new INSTRUCTION("LDY", LDY, ABS, 4), new INSTRUCTION("LDA", LDA, ABS, 4), new INSTRUCTION("LDX", LDX, ABS, 4), new INSTRUCTION("???", XXX, IMP, 4),
                new INSTRUCTION("BCS", BCS, REL, 2), new INSTRUCTION("LDA", LDA, IZY, 5), new INSTRUCTION("???", XXX, IMP, 2), new INSTRUCTION("???", XXX, IMP, 8), new INSTRUCTION("LDY", LDY, ZPX, 4), new INSTRUCTION("LDA", LDA, ZPX, 4), new INSTRUCTION("LDX", LDX, ZPY, 4), new INSTRUCTION("???", XXX, IMP, 4), new INSTRUCTION("CLV", CLV, IMP, 2), new INSTRUCTION("LDA", LDA, ABY, 4), new INSTRUCTION("TSX", TSX, IMP, 2), new INSTRUCTION("???", XXX, IMP, 4), new INSTRUCTION("LDY", LDY, ABX, 4), new INSTRUCTION("LDA", LDA, ABX, 4), new INSTRUCTION("LDX", LDX, ABY, 4), new INSTRUCTION("???", XXX, IMP, 4),
                new INSTRUCTION("CPY", CPY, IMM, 2), new INSTRUCTION("CMP", CMP, IZX, 6), new INSTRUCTION("???", NOP, IMP, 2), new INSTRUCTION("???", XXX, IMP, 8), new INSTRUCTION("CPY", CPY, ZP0, 3), new INSTRUCTION("CMP", CMP, ZP0, 3), new INSTRUCTION("DEC", DEC, ZP0, 5), new INSTRUCTION("???", XXX, IMP, 5), new INSTRUCTION("INY", INY, IMP, 2), new INSTRUCTION("CMP", CMP, IMM, 2), new INSTRUCTION("DEX", DEX, IMP, 2), new INSTRUCTION("???", XXX, IMP, 2), new INSTRUCTION("CPY", CPY, ABS, 4), new INSTRUCTION("CMP", CMP, ABS, 4), new INSTRUCTION("DEC", DEC, ABS, 6), new INSTRUCTION("???", XXX, IMP, 6),
                new INSTRUCTION("BNE", BNE, REL, 2), new INSTRUCTION("CMP", CMP, IZY, 5), new INSTRUCTION("???", XXX, IMP, 2), new INSTRUCTION("???", XXX, IMP, 8), new INSTRUCTION("???", NOP, IMP, 4), new INSTRUCTION("CMP", CMP, ZPX, 4), new INSTRUCTION("DEC", DEC, ZPX, 6), new INSTRUCTION("???", XXX, IMP, 6), new INSTRUCTION("CLD", CLD, IMP, 2), new INSTRUCTION("CMP", CMP, ABY, 4), new INSTRUCTION("???", NOP, IMP, 2), new INSTRUCTION("???", XXX, IMP, 7), new INSTRUCTION("???", NOP, IMP, 4), new INSTRUCTION("CMP", CMP, ABX, 4), new INSTRUCTION("DEC", DEC, ABX, 7), new INSTRUCTION("???", XXX, IMP, 7),
                new INSTRUCTION("CPX", CPX, IMM, 2), new INSTRUCTION("SBC", SBC, IZX, 6), new INSTRUCTION("???", NOP, IMP, 2), new INSTRUCTION("???", XXX, IMP, 8), new INSTRUCTION("CPX", CPX, ZP0, 3), new INSTRUCTION("SBC", SBC, ZP0, 3), new INSTRUCTION("INC", INC, ZP0, 5), new INSTRUCTION("???", XXX, IMP, 5), new INSTRUCTION("INX", INX, IMP, 2), new INSTRUCTION("SBC", SBC, IMM, 2), new INSTRUCTION("???", NOP, IMP, 2), new INSTRUCTION("???", SBC, IMP, 2), new INSTRUCTION("CPX", CPX, ABS, 4), new INSTRUCTION("SBC", SBC, ABS, 4), new INSTRUCTION("INC", INC, ABS, 6), new INSTRUCTION("???", XXX, IMP, 6),
                new INSTRUCTION("BEQ", BEQ, REL, 2), new INSTRUCTION("SBC", SBC, IZY, 5), new INSTRUCTION("???", XXX, IMP, 2), new INSTRUCTION("???", XXX, IMP, 8), new INSTRUCTION("???", NOP, IMP, 4), new INSTRUCTION("SBC", SBC, ZPX, 4), new INSTRUCTION("INC", INC, ZPX, 6), new INSTRUCTION("???", XXX, IMP, 6), new INSTRUCTION("SED", SED, IMP, 2), new INSTRUCTION("SBC", SBC, ABY, 4), new INSTRUCTION("???", NOP, IMP, 2), new INSTRUCTION("???", XXX, IMP, 7), new INSTRUCTION("???", NOP, IMP, 4), new INSTRUCTION("SBC", SBC, ABX, 4), new INSTRUCTION("INC", INC, ABX, 7), new INSTRUCTION("???", XXX, IMP, 7)
            };

            // Startup state
            status = 0x34;
            stkp = 0xFD;

        }

        public bool Complete()
        {
            return cycles == 0;
        }

        // Whenever we want to read, we're really wanting the BUS to read - that's where the magic is going to be
        public byte read(ushort a)
        {
            return bus.cpuRead(a);
        }

        public void write(ushort a, byte d)
        {
            bus.cpuWrite(a, d);
        }



        // illegal - does nothing
        public byte XXX()
        {
            return 0x00;
        }

        /// <summary>
        /// One clock tick - note, the actions that need to happen within an instruction are all happening on the first clock tick, then
        /// it will count down until cycles is 0
        /// </summary>
        public void Clock()
        {
            //Console.WriteLine($"CPUClock: {cycles} cycles left");

            // doing everything at once
            if (cycles == 0)
            {
                var debugpc = pc;
                if (pc == 0x6EE9)
                {
                    Console.WriteLine("9048");
                }
                opcode = read(pc);
                SetFlag(FLAGS6502.U, true);
                pc++;

                cycles = lookup[opcode].cycles;

                byte modeXtraCycle = lookup[opcode].AddrMode();

                byte opXtraCycle = lookup[opcode].Operation();

                cycles += (modeXtraCycle & opXtraCycle);


                //Log.Debug($"CPU Clock - Instruction: {lookup[opcode].Name}");
                //Log.Debug($"{clock_count-8, 10} - i{instructCount,10} A:{Hex(a, 2)} X:{Hex(x, 2)} Y:{Hex(y, 2)} S:{Hex(stkp, 2)} Status: {(GetFlag(FLAGS6502.N) > 0 ? "N" : ".")}{(GetFlag(FLAGS6502.V) > 0 ? "V" : ".")}{(GetFlag(FLAGS6502.U) > 0 ? "U" : ".")}{(GetFlag(FLAGS6502.B) > 0 ? "B" : ".")}{(GetFlag(FLAGS6502.D) > 0 ? "D" : ".")}{(GetFlag(FLAGS6502.I) > 0 ? "I" : ".")}{(GetFlag(FLAGS6502.Z) > 0 ? "Z" : ".")}{(GetFlag(FLAGS6502.C) > 0 ? "C" : ".")}  PC:{Hex(debugpc, 4)}: {Hex(opcode,2)}    {lookup[opcode].Name} {debugmodedata, 20} {Hex((int)debugfetched,2),4}");
                debugmodedata = "";
                debugfetched = 0x00;

                SetFlag(FLAGS6502.U, true);
                instructCount++;
            }

            clock_count++;
            cycles--;
        }

        public void Reset()
        {
            //Log.Debug($"CPU Reset");
            a = 0;
            x = 0;
            y = 0;
            stkp = 0xFD;
            status = (byte)(0x00 | FLAGS6502.U);

            addr_abs = 0xFFFC;
            ushort lo = read((ushort)(addr_abs + 0));
            ushort hi = read((ushort)(addr_abs + 1));

            pc = (ushort)((hi << 8) | lo);
            //pc = 0x8000;

            addr_rel = 0x0000;
            addr_abs = 0x0000;
            fetched = 0x00;

            cycles = 8;
        }

        // Interrupt request
        public void IRQ()
        {
            if (GetFlag(FLAGS6502.I) == 0)
            {
                //Log.Debug("IRQ happening");
                write((ushort)(0x0100 + stkp), (byte)((pc >> 8) & 0x00FF));
                stkp--;
                write((ushort)(0x0100 + stkp), (byte)(pc & 0x00FF));
                stkp--;

                SetFlag(FLAGS6502.B, false);
                SetFlag(FLAGS6502.U, true);
                SetFlag(FLAGS6502.I, true);
                write((ushort)(0x0100 + stkp), status);
                stkp--;

                addr_abs = 0xFFFE;
                ushort lo = read((ushort)(addr_abs + 0));
                ushort hi = read((ushort)(addr_abs + 1));

                pc = (ushort)((hi << 8) | lo);

                cycles = 7;
            }
        }

        // Non maskable interrupt (can't stop this one)
        public void NMI()
        {
            //Log.Debug("NMI Happening");
            write((ushort)(0x0100 + stkp), (byte)((pc >> 8) & 0x00FF));
            stkp--;
            write((ushort)(0x0100 + stkp), (byte)(pc & 0x00FF));
            stkp--;

            SetFlag(FLAGS6502.B, false);
            SetFlag(FLAGS6502.U, true);
            SetFlag(FLAGS6502.I, true);
            write((ushort)(0x0100 + stkp), status);
            stkp--;

            addr_abs = 0xFFFA;
            ushort lo = read((ushort)(addr_abs + 0));
            ushort hi = read((ushort)(addr_abs + 1));

            pc = (ushort)((hi << 8) | lo);

            cycles = 8;

        }


        // //////////////////////////////////////////////////////////////////
        // Addressing Modes
        // //////////////////////////////////////////////////////////////////
        public byte IMP() // Implied
        {
            fetched = a;
            return 0;
            debugmodedata += "IMP";
        }

        public byte ZP0() // Zero Page
        {

            addr_abs = read(pc);
            pc++;
            addr_abs &= 0x00FF;
            debugmodedata += $"addr({Hex(addr_abs, 4)}) ZP0";

            return 0;
        }

        public byte ZPY() // Zero Page, Y offset
        {
            addr_abs = (ushort)(read(pc) + y);
            pc++;
            addr_abs &= 0x00FF;
            debugmodedata += $"addr({Hex(addr_abs, 4)}) ZPY";

            return 0;
        }

        public byte ABS() // Absolute
        {
            ushort lo = read(pc);
            pc++;
            ushort hi = read(pc);
            pc++;

            addr_abs = (ushort)((hi << 8) | lo);
            debugmodedata += $"addr({Hex(addr_abs, 4)}) ABS";

            return 0;
        } 

        public byte ABY() // Absolute, Y offset
        {
            ushort lo = read(pc);
            pc++;
            ushort hi = read(pc);
            pc++;

            addr_abs = (ushort)((hi << 8) | lo);
            addr_abs += y;
            debugmodedata += $"addr({Hex(addr_abs, 4)}) ABY";

            // check if we went to a new page - compare our answer hi bits with the input hi bits
            if ((addr_abs & 0xFF00) != (hi << 8))
            {
                return 1;
            }
            else
            {
                return 0;
            }
        } 

        public byte IZX() // Indirect, X offset
        {
            ushort input = read(pc);
            pc++;

            ushort lo = read((ushort)((ushort)(input + x) & 0x00FF));
            ushort hi = read((ushort)((ushort)(input + x + 1) & 0x00FF));

            addr_abs = (ushort)((hi << 8) | lo);
            debugmodedata += $"addr({Hex(addr_abs, 4)}) IZX";

            return 0;

        }

        public byte IMM() // Immediate
        {
            addr_abs = pc++;
            debugmodedata += $"addr({Hex(addr_abs, 4)}) IMM";

            return 0;
        }

        public byte ZPX() // Zero Page, X offset
        {
            addr_abs = (ushort)(read(pc) + x);
            pc++;
            addr_abs &= 0x00FF;
            debugmodedata += $"addr({Hex(addr_abs, 4)}) ZPX";

            return 0;
        }

        public byte REL() // Relative Addressing
        {
            addr_rel = read(pc);
            pc++;
            if ((addr_rel & 0x80) > 0)
            {
                addr_rel |= 0xFF00;
            }
            debugmodedata += $"addrRel({Hex(addr_rel, 4)}) REL";

            return 0;
        }

        public byte ABX() // Absolute, X
        {
            ushort lo = read(pc);
            pc++;
            ushort hi = read(pc);
            pc++;

            addr_abs = (ushort)((hi << 8) | lo);
            addr_abs += x;
            debugmodedata += $"addr({Hex(addr_abs, 4)}) ABX";

            // check if we went to a new page - compare our answer hi bits with the input hi bits
            if ((addr_abs & 0xFF00) != (hi << 8))
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public byte IND() // Indirect
        {
            ushort ptr_lo = read(pc);
            pc++;
            ushort ptr_hi = read(pc);
            pc++;

            ushort ptr = (ushort)((ptr_hi << 8) | ptr_lo);

            // Simulate a bug when lo byte is at page boundary
            if (ptr_lo == 0x00FF)
            {
                addr_abs = (ushort)((ushort)(read((ushort)(ptr & 0xFF00)) << 8) | read((ushort)(ptr + 0)));
            }
            else // normal behavior
            {
                addr_abs = (ushort)((ushort)(read((ushort)(ptr + 1)) << 8) | read((ushort)(ptr + 0)));
            }
            debugmodedata += $"addr({Hex(addr_abs, 4)}) IND";

            return 0;
        }

        public byte IZY() // Indirect, Y
        {
            ushort input = read(pc);
            pc++;

            ushort lo = read((ushort)(input & 0x00FF));
            ushort hi = read((ushort)((input + 1) & 0x00FF));

            addr_abs = (ushort)((hi << 8) | lo);
            addr_abs += y;
            debugmodedata += $"addr({Hex(addr_abs, 4)}) IZY";

            if ((addr_abs & 0x00FF) != (hi << 8))
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }




        // //////////////////////////////////////////////////////////////////
        // Opcodes
        // //////////////////////////////////////////////////////////////////

        // Addition
        public byte ADC()
        {
            fetch();
            ushort temp = (ushort)(a + fetched + GetFlag(FLAGS6502.C));
            SetFlag(FLAGS6502.C, temp > 255);
            SetFlag(FLAGS6502.Z, (temp & 0x00FF) == 0);
            SetFlag(FLAGS6502.N, (temp & 0x80) > 0);
            SetFlag(FLAGS6502.V, (ushort)((~(a ^ fetched)) & (a ^ temp) & 0x0080) > 0);
            a = (byte)(temp & 0x00FF);
            return 1;
        }

        // Logic AND
        public byte AND()
        {
            fetch();
            a = (byte)(a & fetched);
            SetFlag(FLAGS6502.Z, a == 0x00);
            SetFlag(FLAGS6502.N, (a & 0x80) > 0);
            return 1;
        }

        // CHECK THIS
        public byte ASL()
        {
            fetch();

            ushort temp = (ushort)(fetched << 1);
            //temp |= GetFlag(FLAGS6502.C);

            SetFlag(FLAGS6502.C, ((ushort)(fetched & 0x80) > 0));
            SetFlag(FLAGS6502.Z, (ushort)(temp & 0x00FF) == 0);
            SetFlag(FLAGS6502.N, (ushort)(temp & 0x80) > 0);
            if (lookup[opcode].AddrMode == IMP)
            {
                a = (byte)(temp & 0x00FF);
            }
            else
            {
                write(addr_abs, (byte)(temp & 0x00FF));
            }
            return 0;
            

            //if (a == 0)
            //{
            //    SetFlag(FLAGS6502.Z, true);
            //    return 0;
            //}
            //else
            //{
            //    a <<= a;
            //    if ((ushort)(a & 0x80) > 0)
            //    {
            //        SetFlag(FLAGS6502.N, true);
            //    }
            //    return 0;
            //}

        }

        // Branch if Carry is clear
        public byte BCC()
        {
            if (GetFlag(FLAGS6502.C) == 0)
            {
                cycles++;
                addr_abs = (ushort)(pc + addr_rel);

                if ((addr_abs & 0xFF00) != (pc & 0xFF00))
                {
                    cycles++;
                }

                pc = addr_abs;
            }
            return 0;
        }

        // Branch if the carry bit is 1
        public byte BCS()
        {
            if (GetFlag(FLAGS6502.C) == 1)
            {
                cycles++;
                addr_abs = (ushort)(pc + addr_rel);

                if ((addr_abs & 0xFF00) != (pc & 0xFF00))
                {
                    cycles++;
                }

                pc = addr_abs;
            }
            return 0;
        }

        // Branch if Equal
        public byte BEQ()
        {
            if (GetFlag(FLAGS6502.Z) == 1)
            {
                cycles++;
                addr_abs = (ushort)(pc + addr_rel);

                if ((addr_abs & 0xFF00) != (pc & 0xFF00))
                {
                    cycles++;
                }

                pc = addr_abs;
            }
            return 0;
        }

        // Bit test
        public byte BIT()
        {
            fetch();
            ushort temp = (ushort)(a & fetched);
            SetFlag(FLAGS6502.Z, (temp & 0x00FF) == 0);
            SetFlag(FLAGS6502.N, (byte)(fetched & (1 << 7)) > 0);
            SetFlag(FLAGS6502.V, (byte)(fetched & (1 << 6)) > 0);
            return 0;
        }

        // Branch if negative
        public byte BMI()
        {
            if (GetFlag(FLAGS6502.N) == 1)
            {
                cycles++;
                addr_abs = (ushort)(pc + addr_rel);

                if ((addr_abs & 0xFF00) != (pc & 0xFF00))
                {
                    cycles++;
                }

                pc = addr_abs;
            }
            return 0;
        }

        // Branch if not equal
        public byte BNE()
        {
            if (GetFlag(FLAGS6502.Z) == 0)
            {
                cycles++;
                addr_abs = (ushort)(pc + addr_rel);

                if ((addr_abs & 0xFF00) != (pc & 0xFF00))
                {
                    cycles++;
                }

                pc = addr_abs;
            }
            return 0;
        }

        // Branch if positive
        public byte BPL()
        {
            if (GetFlag(FLAGS6502.N) == 0)
            {
                cycles++;
                addr_abs = (ushort)(pc + addr_rel);

                if ((addr_abs & 0xFF00) != (pc & 0xFF00))
                {
                    cycles++;
                }

                pc = addr_abs;
            }
            return 0;
        }

        // Force Interrupt
        public byte BRK()
        {
            pc++;

            SetFlag(FLAGS6502.I, true);

            write((ushort)(0x0100 + stkp), (byte)((pc >> 8) & 0x00FF));
            stkp--;
            write((ushort)(0x0100 + stkp), (byte)(pc & 0x00FF));
            stkp--;
            
            SetFlag(FLAGS6502.B, true);

            write((ushort)(0x0100 + stkp), status);
            stkp--;

            //SetFlag(FLAGS6502.B, false);

            //addr_abs = 0xFFFE;
            //ushort lo = read((ushort)(addr_abs + 0));
            //ushort hi = read((ushort)(addr_abs + 1));

            //pc = (ushort)((hi << 8) | lo);

            ushort lo = read(0xFFFE);
            ushort hi = read(0xFFFF);
            pc = (ushort)(hi << 8 | lo);

            //pc = (ushort)((read(0xFFFE) | read(0xFFFF)) << 8);


            return 0;
        }

        // Branch if Not Overflow
        public byte BVC()
        {
            if (GetFlag(FLAGS6502.V) == 0)
            {
                cycles++;
                addr_abs = (ushort)(pc + addr_rel);

                if ((addr_abs & 0xFF00) != (pc & 0xFF00))
                {
                    cycles++;
                }

                pc = addr_abs;
            }
            return 0;
        }

        // Branch if overflow
        public byte BVS()
        {
            if (GetFlag(FLAGS6502.V) == 1)
            {
                cycles++;
                addr_abs = (ushort)(pc + addr_rel);

                if ((addr_abs & 0xFF00) != (pc & 0xFF00))
                {
                    cycles++;
                }

                pc = addr_abs;
            }
            return 0;
        }

        // Clear the Carry bit
        public byte CLC()
        {
            SetFlag(FLAGS6502.C, false);
            return 0;
        }

        public byte CLD()
        {
            SetFlag(FLAGS6502.D, false);
            return 0;
        }

        public byte CLI()
        {
            SetFlag(FLAGS6502.I, false);
            return 0;
        }

        public byte CLV()
        {
            SetFlag(FLAGS6502.V, false);
            return 0;
        }

        public byte CMP()
        {
            fetch();

            ushort temp = (ushort)(a - fetched);
            SetFlag(FLAGS6502.C, a >= fetched);
            //SetFlag(FLAGS6502.Z, a == fetched);
            SetFlag(FLAGS6502.Z, (temp & 0x00FF) == 0);
            SetFlag(FLAGS6502.N, (temp & 0x80) > 0);

            return 1;
        }

        public byte CPX()
        {
            fetch();

            ushort temp = (ushort)(x - fetched);
            SetFlag(FLAGS6502.C, x >= fetched);
            //SetFlag(FLAGS6502.Z, x == fetched);
            SetFlag(FLAGS6502.Z, (temp & 0x00FF) == 0);
            SetFlag(FLAGS6502.N, (temp & 0x80) > 0);

            return 0;
        }

        public byte CPY()
        {
            fetch();

            ushort temp = (ushort)(y - fetched);
            SetFlag(FLAGS6502.C, y >= fetched);
            SetFlag(FLAGS6502.Z, (temp & 0x00FF) == 0);
            SetFlag(FLAGS6502.N, (temp & 0x80) > 0);

            return 0;
        }

        // Decrement memory - 
        public byte DEC()
        {
            fetch();

            //addr_abs
            byte result = (byte)(fetched - 1);
            write(addr_abs, (byte)(result & 0x00FF));

            SetFlag(FLAGS6502.Z, (result & 0x00FF) == 0);
            SetFlag(FLAGS6502.N, (byte)(result & 0x80) > 0);


            return 0;
        }

        public byte DEX()
        {
            x--;

            SetFlag(FLAGS6502.Z, x == 0);
            SetFlag(FLAGS6502.N, (byte)(x & 0x80) > 0);

            return 0;
        }

        public byte DEY()
        {
            y--;

            SetFlag(FLAGS6502.Z, y == 0);
            SetFlag(FLAGS6502.N, (byte)(y & 0x80) > 0);

            return 0;
        }

        public byte EOR()
        {
            fetch();

            a = (byte)(a ^ fetched);

            SetFlag(FLAGS6502.Z, a == 0);
            SetFlag(FLAGS6502.N, (byte)(a & 0x80) > 0);

            return 1;
        }

        public byte INC()
        {
            fetch();

            //addr_abs
            byte result = (byte)(fetched + 1);
            write(addr_abs, (byte)(result & 0x00FF));

            SetFlag(FLAGS6502.Z, result == 0);
            SetFlag(FLAGS6502.N, (byte)(result & 0x80) > 0);


            return 0;
        }

        public byte INX()
        {
            x++;

            SetFlag(FLAGS6502.Z, x == 0);
            SetFlag(FLAGS6502.N, (byte)(x & 0x80) > 0);

            return 0;
        }

        public byte INY()
        {
            y++;

            SetFlag(FLAGS6502.Z, y == 0);
            SetFlag(FLAGS6502.N, (byte)(y & 0x80) > 0);

            return 0;
        }

        // Jump 
        public byte JMP()
        {
            pc = addr_abs;

            return 0;
        }

        // Jump to subroutine
        public byte JSR()
        {
            pc--;

            write((ushort)(0x0100 + stkp), (byte)((pc >> 8) & 0x00FF));
            stkp--;
            write((ushort)(0x0100 + stkp), (byte)(pc & 0x00FF));
            stkp--;

            // set program counter to target address
            pc = addr_abs;

            return 0;
        }

        // Load the accumulator
        public byte LDA()
        {
            fetch();

            a = fetched;

            SetFlag(FLAGS6502.Z, a == 0x00);
            SetFlag(FLAGS6502.N, (byte)(a & 0x80) > 0);

            return 1;
        }

        // Load X reg
        public byte LDX()
        {
            fetch();

            x = fetched;

            SetFlag(FLAGS6502.Z, x == 0x00);
            SetFlag(FLAGS6502.N, (byte)(x & 0x80) > 0);

            return 1;
        }

        // Load Y reg
        public byte LDY()
        {
            fetch();

            y = fetched;

            SetFlag(FLAGS6502.Z, y == 0x00);
            SetFlag(FLAGS6502.N, (byte)(y & 0x80) > 0);

            return 1;
        }

        // Logical Shift Right
        public byte LSR()
        {
            fetch();

            byte result = (byte)(fetched >> 1);

            // 7th byte is zero
            result = (byte)(result & ~(1 << 7));

            SetFlag(FLAGS6502.C, (fetched & 0x01) > 0);
            SetFlag(FLAGS6502.Z, result == 0);
            SetFlag(FLAGS6502.N, (result & 0x80) > 0);

            if (lookup[opcode].AddrMode == IMP)
            {
                a = (byte)(result & 0x00FF);
            }
            else
            {
                write(addr_abs, (byte)(result & 0x00FF));
            }
            return 0;
        }

        public byte NOP()
        {
            switch (opcode)
            {
                case 0x1C:
                case 0x3C:
                case 0x5C:
                case 0x7C:
                case 0xDC:
                case 0XFC:
                    return 1;
                    break;
            }
            return 0;
        }

        public byte ORA()
        {
            fetch();

            a |= fetched;

            SetFlag(FLAGS6502.Z, a == 0);
            SetFlag(FLAGS6502.N, (a & 0x80) > 0);

            return 1;
        }

        //push accumulator to the stack
        public byte PHA()
        {
            write((ushort)(0x0100 + stkp), a);
            stkp--;
            return 0;
        }

        public byte PHP()
        {
            write((ushort)(0x0100 + stkp), (byte)(status | ((byte)FLAGS6502.B) | (byte)FLAGS6502.U));
            SetFlag(FLAGS6502.B, false);
            SetFlag(FLAGS6502.U, false);
            stkp--;
            return 0;
        }

        // pop of the stack to the accumulator
        public byte PLA()
        {
            stkp++;
            a = read((ushort)(0x0100 + stkp));
            SetFlag(FLAGS6502.Z, a == 0x00);
            SetFlag(FLAGS6502.N, (a & 0x80) > 0);
            return 0;
        }

        // pop off the stack and into the status register
        public byte PLP()
        {
            stkp++;
            status = read((ushort)(0x0100 + stkp));
            SetFlag(FLAGS6502.U, true);
            return 0;
        }

        // Rotate Left
        public byte ROL()
        {
            fetch();

            ushort result = (ushort)(fetched << 1); // | GetFlag(FLAGS6502.C));

            result |= GetFlag(FLAGS6502.C);

            SetFlag(FLAGS6502.C, (byte)(fetched & 0x80) > 0);
            SetFlag(FLAGS6502.Z, (byte)(result & 0x00FF) == 0);
            SetFlag(FLAGS6502.N, (result & 0x0080) > 0);

            if (lookup[opcode].AddrMode == IMP)
            {
                a = (byte)(result & 0x00FF);
            }
            else
            {
                write(addr_abs, (byte)(result & 0x00FF));
            }

            return 0;
        }

        // Rotate Right
        public byte ROR()
        {
            fetch();

            byte result = (byte)((GetFlag(FLAGS6502.C) << 7) | (byte)(fetched >> 1));

            //result |= GetFlag(FLAGS6502.C);

            SetFlag(FLAGS6502.C, (byte)(fetched & 0x01) > 0);

            SetFlag(FLAGS6502.Z, (byte)(result & 0x00FF) == 0);
            SetFlag(FLAGS6502.N, (result & 0x80) > 0);

            if (lookup[opcode].AddrMode == IMP)
            {
                a = (byte)(result & 0x00FF);
                //a = result;
            }
            else
            {
                write(addr_abs, (byte)(result & 0x00FF));
            }

            return 0;
        }

        // Return from interrupt
        public byte RTI()
        {
            stkp++;
            status = read((ushort)(0x0100 + stkp));
            status = (byte)(status & ~(byte)FLAGS6502.B);
            status = (byte)(status & ~(byte)FLAGS6502.U);

            stkp++;
            pc = read((ushort)(0x0100 + stkp));
            stkp++;
            pc |= (ushort)(read((ushort)(0x0100 + stkp)) << 8);

            return 0;
        }

        // Return from subroutine
        public byte RTS()
        {
            stkp++;
            pc = read((ushort)(0x0100 + stkp));
            stkp++;
            pc |= (ushort)(read((ushort)(0x0100 + stkp)) << 8);

            pc++;
            return 0;
        }

        // Subtraction
        public byte SBC()
        {
            fetch();

            ushort value = (ushort)(fetched ^ 0x00FF);

            ushort temp = (ushort)(a + value + GetFlag(FLAGS6502.C));
            SetFlag(FLAGS6502.C, temp > 255);
            SetFlag(FLAGS6502.Z, (temp & 0x00FF) == 0);
            SetFlag(FLAGS6502.N, (temp & 0x80) > 0);
            SetFlag(FLAGS6502.V, (ushort)(((a ^ fetched)) & (a ^ temp) & 0x0080) > 0);
            a = (byte)(temp & 0x00FF);
            return 1;
        }

        public byte SEC()
        {
            SetFlag(FLAGS6502.C, true);
            return 0;
        }

        public byte SED()
        {
            SetFlag(FLAGS6502.D, true);
            return 0;
        }

        public byte SEI()
        {
            SetFlag(FLAGS6502.I, true);
            return 0;
        }

        // Store accumulator
        public byte STA()
        {
            write(addr_abs, a);
            return 0;
        }

        public byte STX()
        {
            write(addr_abs, x);
            return 0;
        }

        public byte STY()
        {
            write(addr_abs, y);
            return 0;
        }

        // Transfer accumulator to X
        public byte TAX()
        {
            x = a;
            SetFlag(FLAGS6502.Z, x == 0);
            SetFlag(FLAGS6502.N, (x & 0x80) > 0);
            return 0;
        }

        // Transfer accumulator to Y
        public byte TAY()
        {
            y = a;
            SetFlag(FLAGS6502.Z, y == 0);
            SetFlag(FLAGS6502.N, (y & 0x80) > 0);
            return 0;
        }

        // Transfer stack pointer to X
        public byte TSX()
        {
            x = stkp;
            SetFlag(FLAGS6502.Z, x == 0);
            SetFlag(FLAGS6502.N, (x & 0x80) > 0);
            return 0;
        }

        // Transfer X to Accumulator
        public byte TXA()
        {
            a = x;
            SetFlag(FLAGS6502.Z, a == 0);
            SetFlag(FLAGS6502.N, (a & 0x80) > 0);
            return 0;
        }

        // Transfer X to stack pointer
        public byte TXS()
        {
            stkp = x;
            return 0;
        }

        public byte TYA()
        {
            a = y;
            SetFlag(FLAGS6502.Z, a == 0);
            SetFlag(FLAGS6502.N, (a & 0x80) > 0);
            return 0;
        }



        ////////////////////////////////////////////////////////
        /// Disassemble
        ////////////////////////////////////////////////////////

        public Dictionary<ushort, string> Disassemble(ushort startAddr, ushort stopAddr)
        {
            int addr = startAddr;
            byte value = 0x00;
            byte lo = 0x00;
            byte hi = 0x00;
            Dictionary<ushort, string> output = new Dictionary<ushort, string>();
            ushort line_addr = 0;

            while (addr <= stopAddr)
            {
                line_addr = (ushort)addr;

                string outstring = "$" + Hex(addr, 4) + ": ";

                byte op = bus.cpuRead(addr, true);
                addr++;
                outstring += lookup[op].Name + " ";

                if (lookup[op].AddrMode == IMP)
                {
                    outstring += " {IMP}";
                }
                else if (lookup[op].AddrMode == IMM)
                {
                    value = bus.cpuRead((ushort)addr, true);
                    addr++;
                    outstring += "#$" + Hex(value, 2) + " {IMM}";
                }
                else if (lookup[op].AddrMode == ZP0)
                {
                    lo = bus.cpuRead(addr, true);
                    addr++;
                    hi = 0x00;
                    outstring += "$" + Hex(lo, 2) + " {ZP0}";
                }
                else if (lookup[op].AddrMode == ZPX)
                {
                    lo = bus.cpuRead(addr, true);
                    addr++;
                    hi = 0x00;
                    outstring += "$" + Hex(lo, 2) + " X {ZPX}";
                }
                else if (lookup[op].AddrMode == ZPY)
                {
                    lo = bus.cpuRead(addr, true);
                    addr++;
                    hi = 0x00;
                    outstring += "$" + Hex(lo, 2) + " Y {ZPY}";
                }
                else if (lookup[op].AddrMode == IZX)
                {
                    lo = bus.cpuRead(addr, true);
                    addr++;
                    hi = 0x00;
                    outstring += "($" + Hex(lo, 2) + " X) {IZX}";
                }
                else if (lookup[op].AddrMode == IZY)
                {
                    lo = bus.cpuRead(addr, true);
                    addr++;
                    hi = 0x00;
                    outstring += "($" + Hex(lo, 2) + " Y) {IZY}";
                }
                else if (lookup[op].AddrMode == ABS)
                {
                    lo = bus.cpuRead(addr, true);
                    addr++;
                    hi = bus.cpuRead(addr, true);
                    addr++;
                    outstring += "$" + Hex((ushort)((ushort)(hi << 8) | lo), 4) + " {ABS}";
                }
                else if (lookup[op].AddrMode == ABX)
                {
                    lo = bus.cpuRead(addr, true);
                    addr++;
                    hi = bus.cpuRead(addr, true);
                    addr++;
                    outstring += "$" + Hex((ushort)((ushort)(hi << 8) | lo), 4) + " X {ABX}";
                }
                else if (lookup[op].AddrMode == ABY)
                {
                    lo = bus.cpuRead(addr, true);
                    addr++;
                    hi = bus.cpuRead(addr, true);
                    addr++;
                    outstring += "$" + Hex((ushort)((ushort)(hi << 8) | lo), 4) + " Y {ABY}";
                }
                else if (lookup[op].AddrMode == IND)
                {
                    lo = bus.cpuRead((ushort)addr, true);
                    addr++;
                    hi = bus.cpuRead((ushort)addr, true);
                    addr++;
                    outstring += "($" + Hex((ushort)((ushort)(hi << 8) | lo), 4) + ") {IND}";
                }
                else if (lookup[op].AddrMode == REL)
                {
                    value = bus.cpuRead(addr, true);

                    ushort rel_addr = value;

                    if ((rel_addr & 0x80) > 0)
                    {
                        rel_addr |= 0xFF00;
                    }

                    addr++;
                    outstring += "$" + Hex(value, 2) + " [$" + Hex((ushort)(addr + rel_addr), 4) + "] {REL}";
                    

                }

                output[line_addr] = outstring;


            }
            
            return output;
        }

        // Utilities
        public static string Hex(int num, int pad) 
        {
            return Convert.ToString(num, toBase: 16).ToUpper().PadLeft(pad, '0');
        }
    }
}

