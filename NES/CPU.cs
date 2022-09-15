using System;
namespace NES
{
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

    public class CPU
    {
        private Bus bus;

        private INSTRUCTION[] lookup;

        // private helpers
        public byte GetFlag(FLAGS6502 f)
        {
            if ((byte)(status & ((byte)f)) > 0)
            {
                return 1;
            }
            return 0;
        }

        public void SetFlag(FLAGS6502 f, bool v)
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

        // Flags
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

        // Registers
        public byte a = 0x00;       // Accumulator
        public byte x = 0x00;       // X register
        public byte y = 0x00;       // Y register
        public byte stkp = 0x00;    // Stack pointer
        public ushort pc = 0x0000;  // program counter
        public byte status = 0x00;  // Status register (See FLAGS6502 above)

        private byte fetch()
        {
            if (!(lookup[opcode].AddrMode == IMP))
            {
                fetched = read(addr_abs);
            }
            return fetched;
        }

        private byte fetched = 0x00;
        private ushort addr_abs = 0x0000;
        private ushort addr_rel = 0x0000;
        private byte opcode = 0x00;
        private int cycles = 0;



        public CPU(Bus busIn)
        {
            this.bus = busIn;

            lookup = new INSTRUCTION[]
            {
                new INSTRUCTION("BRK", BRK, IMP, 7), new INSTRUCTION("ORA", ORA, IZX, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("ORA", ORA, ZP0, 7), new INSTRUCTION("ASL", ASL, ZP0, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("PHP", PHP, IMP, 3), new INSTRUCTION("ORA", ORA, IMM, 2), new INSTRUCTION("ASL", ASL, IMP, 2), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("ORA", ORA, ABS, 4), new INSTRUCTION("ASL", ASL, ABS, 6), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("BPL", BPL, REL, 2), new INSTRUCTION("ORA", ORA, IZY, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("ORA", ORA, ZPX, 7), new INSTRUCTION("ASL", ASL, ZPX, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("CLC", CLC, IMP, 2), new INSTRUCTION("ORA", ORA, ABY, 4), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("ORA", ORA, ABX, 4), new INSTRUCTION("ASL", ASL, ABX, 7), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("STA", STA, IZX, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("STY", STY, ZP0, 3), new INSTRUCTION("STA", STA, ZP0, 7), new INSTRUCTION("STX", STX, ZP0, 3), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("DEY", DEY, IMP, 2), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("TXA", TXA, IMP, 2), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("STY", STY, ABS, 4), new INSTRUCTION("STA", STA, ABS, 4), new INSTRUCTION("ROL", ROL, ABS, 6), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("JSR", JSR, ABS, 6), new INSTRUCTION("AND", AND, IZX, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("BIT", BIT, ZP0, 3), new INSTRUCTION("AND", AND, ZP0, 7), new INSTRUCTION("ROL", ROL, ZP0, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("PLP", PLP, IMP, 4), new INSTRUCTION("AND", AND, IMM, 2), new INSTRUCTION("ROL", ROL, IMP, 2), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("BIT", BIT, ABS, 4), new INSTRUCTION("AND", AND, ABS, 4), new INSTRUCTION("ROL", ROL, ABX, 7), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("BMI", BMI, REL, 2), new INSTRUCTION("AND", AND, IZY, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("AND", AND, ZPX, 7), new INSTRUCTION("ROL", ROL, ZPX, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("SEC", SEC, IMP, 2), new INSTRUCTION("AND", AND, ABY, 4), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("AND", AND, ABX, 4), new INSTRUCTION("LSR", LSR, ABS, 6), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("RTI", RTI, IMP, 6), new INSTRUCTION("EOR", EOR, IZX, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("EOR", EOR, ZP0, 7), new INSTRUCTION("LSR", LSR, ZP0, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("PHA", PHA, IMP, 3), new INSTRUCTION("EOR", EOR, IMM, 2), new INSTRUCTION("LSR", LSR, IMP, 2), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("JMP", JMP, ABS, 3), new INSTRUCTION("EOR", EOR, ABS, 4), new INSTRUCTION("LSR", LSR, ABX, 7), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("BVC", BVC, REL, 2), new INSTRUCTION("EOR", EOR, IZY, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("EOR", EOR, ZPX, 7), new INSTRUCTION("LSR", LSR, ZPX, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("CLI", CLI, IMP, 2), new INSTRUCTION("EOR", EOR, ABY, 4), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("EOR", EOR, ABX, 4), new INSTRUCTION("ROR", ROR, ABS, 6), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("RTS", RTS, IMP, 6), new INSTRUCTION("ADC", ADC, IZX, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("ADC", ADC, ZP0, 7), new INSTRUCTION("ROR", ROR, ZP0, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("PLA", PLA, IMP, 4), new INSTRUCTION("ADC", ADC, IMM, 2), new INSTRUCTION("ROR", ROR, IMP, 2), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("JMP", JMP, ABS, 6), new INSTRUCTION("ADC", ADC, ABS, 4), new INSTRUCTION("ROR", ROR, ABX, 7), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("BVS", BVS, REL, 2), new INSTRUCTION("ADC", ADC, IZY, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("ADC", ADC, ZPX, 7), new INSTRUCTION("ROR", ROR, ZPX, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("SEI", SEI, IMP, 2), new INSTRUCTION("ADC", ADC, ABY, 4), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("ADC", ADC, ABX, 4), new INSTRUCTION("STX", STX, ABS, 4), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("BCC", BCC, REL, 2), new INSTRUCTION("STA", STA, IZY, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("STY", STY, ZPX, 4), new INSTRUCTION("STA", STA, ZPX, 7), new INSTRUCTION("STX", STX, ZPY, 4), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("TYA", TYA, IMP, 2), new INSTRUCTION("STA", STA, ABY, 5), new INSTRUCTION("TXS", TXS, IMP, 2), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("STA", STA, ABX, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("LDY", LDY, IMM, 2), new INSTRUCTION("LDA", LDA, IZX, 6), new INSTRUCTION("LDX", LDX, IMM, 2), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("LDY", LDY, ZP0, 3), new INSTRUCTION("LDA", LDA, ZP0, 7), new INSTRUCTION("LDX", LDX, ZP0, 3), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("TAY", TAY, IMP, 2), new INSTRUCTION("LDA", LDA, IMM, 2), new INSTRUCTION("TAX", TAX, IMP, 2), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("LDY", LDY, ABS, 4), new INSTRUCTION("LDA", LDA, ABS, 4), new INSTRUCTION("LDX", LDX, ABS, 4), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("BCS", BCS, REL, 2), new INSTRUCTION("LDA", LDA, IZY, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("LDY", LDY, ZPX, 4), new INSTRUCTION("LDA", LDA, ZPX, 7), new INSTRUCTION("LDX", LDX, ZPY, 4), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("CLV", CLV, IMP, 2), new INSTRUCTION("LDA", LDA, ABY, 4), new INSTRUCTION("TSX", TSX, IMP, 2), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("LDY", LDY, ABX, 4), new INSTRUCTION("LDA", LDA, ABX, 4), new INSTRUCTION("LDX", LDX, ABY, 4), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("CPY", CPY, IMM, 2), new INSTRUCTION("CMP", CMP, IZX, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("CPY", CPY, ZP0, 3), new INSTRUCTION("CMP", CMP, ZP0, 7), new INSTRUCTION("DEC", DEC, ZP0, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("INY", INY, IMP, 2), new INSTRUCTION("CMP", CMP, IMM, 2), new INSTRUCTION("DEX", DEX, IMP, 2), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("CPY", CPY, ABS, 4), new INSTRUCTION("CMP", CMP, ABS, 4), new INSTRUCTION("DEC", DEC, ABS, 6), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("BNE", BNE, REL, 2), new INSTRUCTION("CMP", CMP, IZY, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("CMP", CMP, ZPX, 7), new INSTRUCTION("DEC", DEC, ZPX, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("CLD", CLD, IMP, 2), new INSTRUCTION("CMP", CMP, ABY, 4), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("CMP", CMP, ABX, 4), new INSTRUCTION("DEC", DEC, ABX, 7), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("CPX", CPX, IMM, 2), new INSTRUCTION("SBC", SBC, IZX, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("CPX", CPX, ZP0, 3), new INSTRUCTION("SBC", SBC, ZP0, 7), new INSTRUCTION("INC", INC, ZP0, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("INX", INX, IMP, 2), new INSTRUCTION("SBC", SBC, IMM, 2), new INSTRUCTION("NOP", NOP, IMP, 2), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("CPX", CPX, ABS, 4), new INSTRUCTION("SBC", SBC, ABS, 4), new INSTRUCTION("INC", INC, ABS, 6), new INSTRUCTION("???", XXX, XXX, 1),
                new INSTRUCTION("BEQ", BEQ, REL, 2), new INSTRUCTION("SBC", SBC, IZY, 5), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("SBC", SBC, ZPX, 7), new INSTRUCTION("INC", INC, ZPX, 6), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("SED", SED, IMP, 2), new INSTRUCTION("SBC", SBC, ABY, 4), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("???", XXX, XXX, 1), new INSTRUCTION("SBC", SBC, ABX, 4), new INSTRUCTION("INC", INC, ABX, 7), new INSTRUCTION("???", XXX, XXX, 1)
            };
        }


        public byte read(ushort a)
        {
            return bus.read(a);
        }

        public void write(ushort a, byte d)
        {
            bus.write(a, d);
        }



        // illegal
        public byte XXX()
        {
            return 0x00;
        }


        // More!

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

        }

        public void IRQ()
        {

        }

        public void NMI()
        {

        }



        // Addressing Modes
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


        // Opcodes

        public byte ADC()
        {
            return 0x00;
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

        public byte ASL()
        {
            return 0x00;
        }

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

        public byte BIT()
        {
            return 0x00;
        }

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

        public byte BRK()
        {
            return 0x00;
        }

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
            return 0x00;
        }

        public byte CPX()
        {
            return 0x00;
        }

        public byte CPY()
        {
            return 0x00;
        }

        public byte DEC()
        {
            return 0x00;
        }

        public byte DEX()
        {
            return 0x00;
        }

        public byte DEY()
        {
            return 0x00;
        }

        public byte EOR()
        {
            return 0x00;
        }

        public byte INC()
        {
            return 0x00;
        }

        public byte INX()
        {
            return 0x00;
        }

        public byte INY()
        {
            return 0x00;
        }

        public byte JMP()
        {
            return 0x00;
        }

        public byte JSR()
        {
            return 0x00;
        }

        public byte LDA()
        {
            return 0x00;
        }

        public byte LDX()
        {
            return 0x00;
        }

        public byte LDY()
        {
            return 0x00;
        }

        public byte LSR()
        {
            return 0x00;
        }

        public byte NOP()
        {
            return 0x00;
        }

        public byte ORA()
        {
            return 0x00;
        }

        public byte PHA()
        {
            return 0x00;
        }

        public byte PHP()
        {
            return 0x00;
        }

        public byte PLA()
        {
            return 0x00;
        }

        public byte PLP()
        {
            return 0x00;
        }

        public byte ROL()
        {
            return 0x00;
        }

        public byte ROR()
        {
            return 0x00;
        }

        public byte RTI()
        {
            return 0x00;
        }

        public byte RTS()
        {
            return 0x00;
        }

        public byte SBC()
        {
            return 0x00;
        }

        public byte SEC()
        {
            return 0x00;
        }

        public byte SED()
        {
            return 0x00;
        }

        public byte SEI()
        {
            return 0x00;
        }

        public byte STA()
        {
            return 0x00;
        }

        public byte STX()
        {
            return 0x00;
        }

        public byte STY()
        {
            return 0x00;
        }

        public byte TAX()
        {
            return 0x00;
        }

        public byte TAY()
        {
            return 0x00;
        }

        public byte TSX()
        {
            return 0x00;
        }

        public byte TXA()
        {
            return 0x00;
        }

        public byte TXS()
        {
            return 0x00;
        }

        public byte TYA()
        {
            return 0x00;
        }



    }
}

