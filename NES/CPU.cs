using System;
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
            if (v)
            {
                status |= ((byte)f);
            }
            else
            {
                status &= (byte)(~((byte)f));
            }
        }

        // Fetch data from addr_abs which should be set by the addressing mode function happening first
        // which will set addr_abs
        private byte fetch()
        {
            if (!(lookup[opcode].AddrMode == IMP))
            {
                fetched = read(addr_abs);
            }
            return fetched;
        }

        private byte fetched = 0x00;        // Fetched data
        private ushort addr_abs = 0x0000;   // The absolute address to grab data
        private ushort addr_rel = 0x0000;   // the relative address to grab data
        private byte opcode = 0x00;         // The instruction code from the program
        private int cycles = 0;             // keep track of cycles 

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
                new INSTRUCTION("BRK", BRK, IMP, 7), new INSTRUCTION("ORA", ORA, IZX, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("ORA", ORA, ZP0, 7), new INSTRUCTION("ASL", ASL, ZP0, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("PHP", PHP, IMP, 3), new INSTRUCTION("ORA", ORA, IMM, 2), new INSTRUCTION("ASL", ASL, IMP, 2), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("ORA", ORA, ABS, 4), new INSTRUCTION("ASL", ASL, ABS, 6), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("BPL", BPL, REL, 2), new INSTRUCTION("ORA", ORA, IZY, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("ORA", ORA, ZPX, 7), new INSTRUCTION("ASL", ASL, ZPX, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("CLC", CLC, IMP, 2), new INSTRUCTION("ORA", ORA, ABY, 4), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("ORA", ORA, ABX, 4), new INSTRUCTION("ASL", ASL, ABX, 7), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("JSR", JSR, ABS, 6), new INSTRUCTION("AND", AND, IZX, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("BIT", BIT, ZP0, 3), new INSTRUCTION("AND", AND, ZP0, 7), new INSTRUCTION("ROL", ROL, ZP0, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("PLP", PLP, IMP, 4), new INSTRUCTION("AND", AND, IMM, 2), new INSTRUCTION("ROL", ROL, IMP, 2), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("BIT", BIT, ABS, 4), new INSTRUCTION("AND", AND, ABS, 4), new INSTRUCTION("ROL", ROL, ABS, 6), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("BMI", BMI, REL, 2), new INSTRUCTION("AND", AND, IZY, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("AND", AND, ZPX, 7), new INSTRUCTION("ROL", ROL, ZPX, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("SEC", SEC, IMP, 2), new INSTRUCTION("AND", AND, ABY, 4), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("AND", AND, ABX, 4), new INSTRUCTION("ROL", ROL, ABX, 7), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("RTI", RTI, IMP, 6), new INSTRUCTION("EOR", EOR, IZX, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("EOR", EOR, ZP0, 7), new INSTRUCTION("LSR", LSR, ZP0, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("PHA", PHA, IMP, 3), new INSTRUCTION("EOR", EOR, IMM, 2), new INSTRUCTION("LSR", LSR, IMP, 2), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("JMP", JMP, ABS, 3), new INSTRUCTION("EOR", EOR, ABS, 4), new INSTRUCTION("LSR", LSR, ABS, 6), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("BVC", BVC, REL, 2), new INSTRUCTION("EOR", EOR, IZY, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("EOR", EOR, ZPX, 7), new INSTRUCTION("LSR", LSR, ZPX, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("CLI", CLI, IMP, 2), new INSTRUCTION("EOR", EOR, ABY, 4), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("EOR", EOR, ABX, 4), new INSTRUCTION("LSR", LSR, ABX, 7), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("RTS", RTS, IMP, 6), new INSTRUCTION("ADC", ADC, IZX, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("ADC", ADC, ZP0, 7), new INSTRUCTION("ROR", ROR, ZP0, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("PLA", PLA, IMP, 4), new INSTRUCTION("ADC", ADC, IMM, 2), new INSTRUCTION("ROR", ROR, IMP, 2), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("JMP", JMP, ABS, 6), new INSTRUCTION("ADC", ADC, ABS, 4), new INSTRUCTION("ROR", ROR, ABS, 6), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("BVS", BVS, REL, 2), new INSTRUCTION("ADC", ADC, IZY, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("ADC", ADC, ZPX, 7), new INSTRUCTION("ROR", ROR, ZPX, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("SEI", SEI, IMP, 2), new INSTRUCTION("ADC", ADC, ABY, 4), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("ADC", ADC, ABX, 4), new INSTRUCTION("ROR", ROR, ABX, 7), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("STA", STA, IZX, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("STY", STY, ZP0, 3), new INSTRUCTION("STA", STA, ZP0, 7), new INSTRUCTION("STX", STX, ZP0, 3), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("DEY", DEY, IMP, 2), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("TXA", TXA, IMP, 2), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("STY", STY, ABS, 4), new INSTRUCTION("STA", STA, ABS, 4), new INSTRUCTION("STX", STX, ABS, 4), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("BCC", BCC, REL, 2), new INSTRUCTION("STA", STA, IZY, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("STY", STY, ZPX, 4), new INSTRUCTION("STA", STA, ZPX, 7), new INSTRUCTION("STX", STX, ZPY, 4), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("TYA", TYA, IMP, 2), new INSTRUCTION("STA", STA, ABY, 5), new INSTRUCTION("TXS", TXS, IMP, 2), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("STA", STA, ABX, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("LDY", LDY, IMM, 2), new INSTRUCTION("LDA", LDA, IZX, 6), new INSTRUCTION("LDX", LDX, IMM, 2), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("LDY", LDY, ZP0, 3), new INSTRUCTION("LDA", LDA, ZP0, 7), new INSTRUCTION("LDX", LDX, ZP0, 3), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("TAY", TAY, IMP, 2), new INSTRUCTION("LDA", LDA, IMM, 2), new INSTRUCTION("TAX", TAX, IMP, 2), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("LDY", LDY, ABS, 4), new INSTRUCTION("LDA", LDA, ABS, 4), new INSTRUCTION("LDX", LDX, ABS, 4), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("BCS", BCS, REL, 2), new INSTRUCTION("LDA", LDA, IZY, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("LDY", LDY, ZPX, 4), new INSTRUCTION("LDA", LDA, ZPX, 7), new INSTRUCTION("LDX", LDX, ZPY, 4), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("CLV", CLV, IMP, 2), new INSTRUCTION("LDA", LDA, ABY, 4), new INSTRUCTION("TSX", TSX, IMP, 2), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("LDY", LDY, ABX, 4), new INSTRUCTION("LDA", LDA, ABX, 4), new INSTRUCTION("LDX", LDX, ABY, 4), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("CPY", CPY, IMM, 2), new INSTRUCTION("CMP", CMP, IZX, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("CPY", CPY, ZP0, 3), new INSTRUCTION("CMP", CMP, ZP0, 7), new INSTRUCTION("DEC", DEC, ZP0, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("INY", INY, IMP, 2), new INSTRUCTION("CMP", CMP, IMM, 2), new INSTRUCTION("DEX", DEX, IMP, 2), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("CPY", CPY, ABS, 4), new INSTRUCTION("CMP", CMP, ABS, 4), new INSTRUCTION("DEC", DEC, ABS, 6), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("BNE", BNE, REL, 2), new INSTRUCTION("CMP", CMP, IZY, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("CMP", CMP, ZPX, 7), new INSTRUCTION("DEC", DEC, ZPX, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("CLD", CLD, IMP, 2), new INSTRUCTION("CMP", CMP, ABY, 4), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("CMP", CMP, ABX, 4), new INSTRUCTION("DEC", DEC, ABX, 7), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("CPX", CPX, IMM, 2), new INSTRUCTION("SBC", SBC, IZX, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("CPX", CPX, ZP0, 3), new INSTRUCTION("SBC", SBC, ZP0, 7), new INSTRUCTION("INC", INC, ZP0, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("INX", INX, IMP, 2), new INSTRUCTION("SBC", SBC, IMM, 2), new INSTRUCTION("NOP", NOP, IMP, 2), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("CPX", CPX, ABS, 4), new INSTRUCTION("SBC", SBC, ABS, 4), new INSTRUCTION("INC", INC, ABS, 6), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("BEQ", BEQ, REL, 2), new INSTRUCTION("SBC", SBC, IZY, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("SBC", SBC, ZPX, 7), new INSTRUCTION("INC", INC, ZPX, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("SED", SED, IMP, 2), new INSTRUCTION("SBC", SBC, ABY, 4), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("SBC", SBC, ABX, 4), new INSTRUCTION("INC", INC, ABX, 7), new INSTRUCTION("???", XXX, XXX, 1)
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
            return bus.read(a);
        }

        public void write(ushort a, byte d)
        {
            bus.write(a, d);
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
            // doing everything at once
            if (cycles == 0)
            {
                opcode = read(pc);
                pc++;

                cycles = lookup[opcode].cycles;

                byte modeXtraCycle = lookup[opcode].AddrMode();

                byte opXtraCycle = lookup[opcode].Operation();

                cycles += (modeXtraCycle & opXtraCycle);

            }

            cycles--;
        }

        public void Reset()
        {
            a = 0;
            x = 0;
            y = 0;
            stkp = 0xFD;
            status = (byte)(0x00 | FLAGS6502.U);

            addr_abs = 0xFFFC;
            ushort lo = read((ushort)(addr_abs + 0));
            ushort hi = read((ushort)(addr_abs + 1));

            pc = (ushort)((hi << 8) | lo);

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

        }

        public byte ZP0() // Zero Page
        {
            addr_abs = read(pc);
            pc++;
            addr_abs &= 0x00FF;
            return 0;
        }

        public byte ZPY() // Zero Page, Y offset
        {
            addr_abs = (ushort)(read(pc) + y);
            pc++;
            addr_abs &= 0x00FF;
            return 0;
        }

        public byte ABS() // Absolute
        {
            ushort lo = read(pc);
            pc++;
            ushort hi = read(pc);
            pc++;

            addr_abs = (ushort)((hi << 8) | lo);

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

            return 0;

        }

        public byte IMM() // Immediate
        {
            addr_abs = pc++;
            return 0;
        }

        public byte ZPX() // Zero Page, X offset
        {
            addr_abs = (ushort)(read(pc) + x);
            pc++;
            addr_abs &= 0x00FF;
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

            SetFlag(FLAGS6502.C, ((ushort)(a & 0x80) > 0));

            if (a == 0)
            {
                SetFlag(FLAGS6502.Z, true);
                return 0;
            }
            else
            {
                a <<= a;
                if ((ushort)(a & 0x80) > 0)
                {
                    SetFlag(FLAGS6502.N, true);
                }
                return 0;
            }

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
            SetFlag(FLAGS6502.Z, (byte)(a & fetched) > 0);
            SetFlag(FLAGS6502.N, (byte)(fetched & 0x80) > 0);
            SetFlag(FLAGS6502.V, (byte)(fetched & 0x40) > 0);
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
            write((ushort)(0x0100 + stkp), (byte)((pc >> 8) & 0x00FF));
            stkp--;
            write((ushort)(0x0100 + stkp), (byte)(pc & 0x00FF));
            stkp--;

 
            write((ushort)(0x0100 + stkp), status);
            stkp--;
            
            addr_abs = 0xFFFE;
            ushort lo = read((ushort)(addr_abs + 0));
            ushort hi = read((ushort)(addr_abs + 1));

            pc = (ushort)((hi << 8) | lo);

            SetFlag(FLAGS6502.B, true);

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
            SetFlag(FLAGS6502.Z, a == fetched);
            SetFlag(FLAGS6502.N, (temp & 0x80) > 0);

            return 1;
        }

        public byte CPX()
        {
            fetch();

            ushort temp = (ushort)(x - fetched);
            SetFlag(FLAGS6502.C, x >= fetched);
            SetFlag(FLAGS6502.Z, x == fetched);
            SetFlag(FLAGS6502.N, (temp & 0x80) > 0);

            return 0;
        }

        public byte CPY()
        {
            fetch();

            ushort temp = (ushort)(y - fetched);
            SetFlag(FLAGS6502.C, y >= fetched);
            SetFlag(FLAGS6502.Z, y == fetched);
            SetFlag(FLAGS6502.N, (temp & 0x80) > 0);

            return 0;
        }

        // Decrement memory - 
        public byte DEC()
        {
            fetch();

            //addr_abs
            byte result = (byte)(fetched - 1);

            SetFlag(FLAGS6502.Z, result == 0);
            SetFlag(FLAGS6502.N, (byte)(result & 0x80) > 0);

            write(addr_abs, result);

            return 0;
        }

        public byte DEX()
        {
            x = (byte)(x - 1);

            SetFlag(FLAGS6502.Z, x == 0);
            SetFlag(FLAGS6502.N, (byte)(x & 0x80) > 0);

            return 0;
        }

        public byte DEY()
        {
            y = (byte)(y - 1);

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

            SetFlag(FLAGS6502.Z, result == 0);
            SetFlag(FLAGS6502.N, (byte)(result & 0x80) > 0);

            write(addr_abs, result);

            return 0;
        }

        public byte INX()
        {
            x = (byte)(x + 1);

            SetFlag(FLAGS6502.Z, x == 0);
            SetFlag(FLAGS6502.N, (byte)(x & 0x80) > 0);

            return 0;
        }

        public byte INY()
        {
            y = (byte)(y + 1);

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
            //push the address (minus 1) of the return point on to the stack
            write((byte)(0x0100 + stkp), (byte)(pc - 1));
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

            write(addr_abs, result);

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
            write((byte)(0x0100 + stkp), a);
            stkp--;
            return 0;
        }

        public byte PHP()
        {
            write((byte)(0x0100 + stkp), status);
            stkp--;
            return 0;
        }

        // pop of the stack to the accumulator
        public byte PLA()
        {
            stkp++;
            a = read((byte)(0x0100 + stkp));
            SetFlag(FLAGS6502.Z, a == 0x00);
            SetFlag(FLAGS6502.N, (a & 0x80) > 0);
            return 0;
        }

        // pop off the stack and into the status register
        public byte PLP()
        {
            stkp++;
            status = read((byte)(0x0100 + stkp));
            return 0;
        }

        // Rotate Left
        public byte ROL()
        {
            fetch();

            byte result = (byte)(fetched << 1);

            result ^= GetFlag(FLAGS6502.C);

            SetFlag(FLAGS6502.C, (byte)(fetched & 0x80) > 0);

            SetFlag(FLAGS6502.Z, result == 0);
            SetFlag(FLAGS6502.N, (result & 0x80) > 0);

            write(addr_abs, result);

            return 0;
        }

        // Rotate Right
        public byte ROR()
        {
            fetch();

            byte result = (byte)(fetched >> 1);

            result |= GetFlag(FLAGS6502.C);

            SetFlag(FLAGS6502.C, (byte)(fetched & 0x01) > 0);

            SetFlag(FLAGS6502.Z, result == 0);
            SetFlag(FLAGS6502.N, (result & 0x80) > 0);

            write(addr_abs, result);

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
            pc |= read((ushort)((0x0100 + stkp) << 8));

            return 0;
        }

        // Return from subroutine
        public byte RTS()
        {
            stkp++;
            pc = read((byte)(0x0100 + stkp));
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
            SetFlag(FLAGS6502.V, (ushort)((~(a ^ fetched)) & (a ^ temp) & 0x0080) > 0);
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
            uint addr = startAddr;
            byte value = 0x00;
            byte lo = 0x00;
            byte hi = 0x00;
            Dictionary<ushort, string> output = new Dictionary<ushort, string>();
            ushort line_addr = 0;

            while (addr <= stopAddr)
            {
                line_addr = (ushort)addr;

                string outstring = "$" + Hex(addr, 4) + ": ";

                byte op = bus.read((ushort)addr);
                addr++;
                outstring += lookup[op].Name + " ";

                if (lookup[op].AddrMode == IMP)
                {
                    outstring += " {IMP}";
                }
                else if (lookup[op].AddrMode == IMM)
                {
                    value = bus.read((ushort)addr);
                    addr++;
                    outstring += "#$" + Hex(value, 2) + " {IMM}";
                }
                else if (lookup[op].AddrMode == ZP0)
                {
                    lo = bus.read((ushort)addr);
                    addr++;
                    hi = 0x00;
                    outstring += "$" + Hex(lo, 2) + " {ZP0}";
                }
                else if (lookup[op].AddrMode == ZPX)
                {
                    lo = bus.read((ushort)addr);
                    addr++;
                    hi = 0x00;
                    outstring += "$" + Hex(lo, 2) + " X {ZPX}";
                }
                else if (lookup[op].AddrMode == ZPY)
                {
                    lo = bus.read((ushort)addr);
                    addr++;
                    hi = 0x00;
                    outstring += "$" + Hex(lo, 2) + " Y {ZPY}";
                }
                else if (lookup[op].AddrMode == IZX)
                {
                    lo = bus.read((ushort)addr);
                    addr++;
                    hi = 0x00;
                    outstring += "($" + Hex(lo, 2) + " X) {IZX}";
                }
                else if (lookup[op].AddrMode == IZY)
                {
                    lo = bus.read((ushort)addr);
                    addr++;
                    hi = 0x00;
                    outstring += "($" + Hex(lo, 2) + " Y) {IZY}";
                }
                else if (lookup[op].AddrMode == ABS)
                {
                    lo = bus.read((ushort)addr);
                    addr++;
                    hi = bus.read((ushort)addr);
                    addr++;
                    outstring += "$" + Hex((ushort)((ushort)(hi << 8) | lo), 4) + " {ABS}";
                }
                else if (lookup[op].AddrMode == ABX)
                {
                    lo = bus.read((ushort)addr);
                    addr++;
                    hi = bus.read((ushort)addr);
                    addr++;
                    outstring += "$" + Hex((ushort)((ushort)(hi << 8) | lo), 4) + " X {ABX}";
                }
                else if (lookup[op].AddrMode == ABY)
                {
                    lo = bus.read((ushort)addr);
                    addr++;
                    hi = bus.read((ushort)addr);
                    addr++;
                    outstring += "$" + Hex((ushort)((ushort)(hi << 8) | lo), 4) + " Y {ABY}";
                }
                else if (lookup[op].AddrMode == IND)
                {
                    lo = bus.read((ushort)addr);
                    addr++;
                    hi = bus.read((ushort)addr);
                    addr++;
                    outstring += "($" + Hex((ushort)((ushort)(hi << 8) | lo), 4) + ") {IND}";
                }
                else if (lookup[op].AddrMode == REL)
                {
                    value = bus.read((ushort)addr);
                    addr++;
                    outstring += "$" + Hex(value, 2) + " [$" + Hex((ushort)(addr + value), 4) + "] {REL}";
                }

                output[line_addr] = outstring;


            }

            return output;
        }

        // Utilities
        public static string Hex(uint num, int pad) 
        {
            return Convert.ToString(num, toBase: 16).ToUpper().PadLeft(pad, '0');
        }
    }
}

