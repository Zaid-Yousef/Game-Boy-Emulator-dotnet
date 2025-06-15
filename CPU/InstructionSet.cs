using System;

namespace GameBoyEmulator.CPU
{
    public static class InstructionSet
    {
        public static int Execute(CPU cpu, MMU.MMU mmu, byte opcode)
        {
            // Comprehensive instruction set implementation based on working GameBoy CPU
            switch (opcode)
            {
                // 0x00 - NOP
                case 0x00:
                    return 4;

                // 0x01 - LD BC,d16
                case 0x01:
                    cpu.BC = cpu.ReadWord();
                    return 12;

                // 0x02 - LD (BC),A
                case 0x02:
                    mmu.WriteByte(cpu.BC, cpu.A);
                    return 8;

                // 0x03 - INC BC
                case 0x03:
                    cpu.BC++;
                    return 8;

                // 0x04 - INC B
                case 0x04:
                    cpu.B = Inc8(cpu, cpu.B);
                    return 4;

                // 0x05 - DEC B
                case 0x05:
                    cpu.B = Dec8(cpu, cpu.B);
                    return 4;

                // 0x06 - LD B,d8
                case 0x06:
                    cpu.B = cpu.ReadByte();
                    return 8;

                // 0x07 - RLCA
                case 0x07:
                    cpu.A = RLCA(cpu, cpu.A);
                    return 4;

                // 0x08 - LD (a16),SP
                case 0x08:
                    {
                        ushort address = cpu.ReadWord();
                        mmu.WriteByte(address, (byte)(cpu.SP & 0xFF));
                        mmu.WriteByte((ushort)(address + 1), (byte)(cpu.SP >> 8));
                        return 20;
                    }

                // 0x09 - ADD HL,BC
                case 0x09:
                    cpu.HL = Add16(cpu, cpu.HL, cpu.BC);
                    return 8;

                // 0x0A - LD A,(BC)
                case 0x0A:
                    cpu.A = mmu.ReadByte(cpu.BC);
                    return 8;

                // 0x0B - DEC BC
                case 0x0B:
                    cpu.BC--;
                    return 8;

                // 0x0C - INC C
                case 0x0C:
                    cpu.C = Inc8(cpu, cpu.C);
                    return 4;

                // 0x0D - DEC C
                case 0x0D:
                    cpu.C = Dec8(cpu, cpu.C);
                    return 4;

                // 0x0E - LD C,d8
                case 0x0E:
                    cpu.C = cpu.ReadByte();
                    return 8;

                // 0x0F - RRCA
                case 0x0F:
                    cpu.A = RRCA(cpu, cpu.A);
                    return 4;

                // 0x10 - STOP
                case 0x10:
                    cpu.ReadByte(); // Consume the next byte
                    cpu.Stopped = true;
                    return 4;

                // 0x11 - LD DE,d16
                case 0x11:
                    cpu.DE = cpu.ReadWord();
                    return 12;

                // 0x12 - LD (DE),A
                case 0x12:
                    mmu.WriteByte(cpu.DE, cpu.A);
                    return 8;

                // 0x13 - INC DE
                case 0x13:
                    cpu.DE++;
                    return 8;

                // 0x14 - INC D
                case 0x14:
                    cpu.D = Inc8(cpu, cpu.D);
                    return 4;

                // 0x15 - DEC D
                case 0x15:
                    cpu.D = Dec8(cpu, cpu.D);
                    return 4;

                // 0x16 - LD D,d8
                case 0x16:
                    cpu.D = cpu.ReadByte();
                    return 8;

                // 0x17 - RLA
                case 0x17:
                    cpu.A = RLA(cpu, cpu.A);
                    return 4;

                // 0x18 - JR r8
                case 0x18:
                    {
                        sbyte offset = (sbyte)cpu.ReadByte();
                        cpu.PC = (ushort)(cpu.PC + offset);
                        return 12;
                    }

                // 0x19 - ADD HL,DE
                case 0x19:
                    cpu.HL = Add16(cpu, cpu.HL, cpu.DE);
                    return 8;

                // 0x1A - LD A,(DE)
                case 0x1A:
                    cpu.A = mmu.ReadByte(cpu.DE);
                    return 8;

                // 0x1B - DEC DE
                case 0x1B:
                    cpu.DE--;
                    return 8;

                // 0x1C - INC E
                case 0x1C:
                    cpu.E = Inc8(cpu, cpu.E);
                    return 4;

                // 0x1D - DEC E
                case 0x1D:
                    cpu.E = Dec8(cpu, cpu.E);
                    return 4;

                // 0x1E - LD E,d8
                case 0x1E:
                    cpu.E = cpu.ReadByte();
                    return 8;

                // 0x1F - RRA
                case 0x1F:
                    cpu.A = RRA(cpu, cpu.A);
                    return 4;

                // 0x20 - JR NZ,r8
                case 0x20:
                    {
                        sbyte offset = (sbyte)cpu.ReadByte();
                        if (!cpu.FlagZ)
                        {
                            cpu.PC = (ushort)(cpu.PC + offset);
                            return 12;
                        }
                        return 8;
                    }

                // 0x21 - LD HL,d16
                case 0x21:
                    cpu.HL = cpu.ReadWord();
                    return 12;

                // 0x22 - LD (HL+),A
                case 0x22:
                    mmu.WriteByte(cpu.HL, cpu.A);
                    cpu.HL++;
                    return 8;

                // 0x23 - INC HL
                case 0x23:
                    cpu.HL++;
                    return 8;

                // 0x24 - INC H
                case 0x24:
                    cpu.H = Inc8(cpu, cpu.H);
                    return 4;

                // 0x25 - DEC H
                case 0x25:
                    cpu.H = Dec8(cpu, cpu.H);
                    return 4;

                // 0x26 - LD H,d8
                case 0x26:
                    cpu.H = cpu.ReadByte();
                    return 8;

                // 0x27 - DAA
                case 0x27:
                    DAA(cpu);
                    return 4;

                // 0x28 - JR Z,r8
                case 0x28:
                    {
                        sbyte offset = (sbyte)cpu.ReadByte();
                        if (cpu.FlagZ)
                        {
                            cpu.PC = (ushort)(cpu.PC + offset);
                            return 12;
                        }
                        return 8;
                    }

                // 0x29 - ADD HL,HL
                case 0x29:
                    cpu.HL = Add16(cpu, cpu.HL, cpu.HL);
                    return 8;

                // 0x2A - LD A,(HL+)
                case 0x2A:
                    cpu.A = mmu.ReadByte(cpu.HL);
                    cpu.HL++;
                    return 8;

                // 0x2B - DEC HL
                case 0x2B:
                    cpu.HL--;
                    return 8;

                // 0x2C - INC L
                case 0x2C:
                    cpu.L = Inc8(cpu, cpu.L);
                    return 4;

                // 0x2D - DEC L
                case 0x2D:
                    cpu.L = Dec8(cpu, cpu.L);
                    return 4;

                // 0x2E - LD L,d8
                case 0x2E:
                    cpu.L = cpu.ReadByte();
                    return 8;

                // 0x2F - CPL
                case 0x2F:
                    cpu.A = (byte)~cpu.A;
                    cpu.FlagN = true;
                    cpu.FlagH = true;
                    return 4;

                // 0x30 - JR NC,r8
                case 0x30:
                    {
                        sbyte offset = (sbyte)cpu.ReadByte();
                        if (!cpu.FlagC)
                        {
                            cpu.PC = (ushort)(cpu.PC + offset);
                            return 12;
                        }
                        return 8;
                    }

                // 0x31 - LD SP,d16
                case 0x31:
                    cpu.SP = cpu.ReadWord();
                    return 12;

                // 0x32 - LD (HL-),A
                case 0x32:
                    mmu.WriteByte(cpu.HL, cpu.A);
                    cpu.HL--;
                    return 8;

                // 0x33 - INC SP
                case 0x33:
                    cpu.SP++;
                    return 8;

                // 0x34 - INC (HL)
                case 0x34:
                    {
                        byte value = mmu.ReadByte(cpu.HL);
                        value = Inc8(cpu, value);
                        mmu.WriteByte(cpu.HL, value);
                        return 12;
                    }

                // 0x35 - DEC (HL)
                case 0x35:
                    {
                        byte value = mmu.ReadByte(cpu.HL);
                        value = Dec8(cpu, value);
                        mmu.WriteByte(cpu.HL, value);
                        return 12;
                    }

                // 0x36 - LD (HL),d8
                case 0x36:
                    mmu.WriteByte(cpu.HL, cpu.ReadByte());
                    return 12;

                // 0x37 - SCF
                case 0x37:
                    cpu.FlagN = false;
                    cpu.FlagH = false;
                    cpu.FlagC = true;
                    return 4;

                // 0x38 - JR C,r8
                case 0x38:
                    {
                        sbyte offset = (sbyte)cpu.ReadByte();
                        if (cpu.FlagC)
                        {
                            cpu.PC = (ushort)(cpu.PC + offset);
                            return 12;
                        }
                        return 8;
                    }

                // 0x39 - ADD HL,SP
                case 0x39:
                    cpu.HL = Add16(cpu, cpu.HL, cpu.SP);
                    return 8;

                // 0x3A - LD A,(HL-)
                case 0x3A:
                    cpu.A = mmu.ReadByte(cpu.HL);
                    cpu.HL--;
                    return 8;

                // 0x3B - DEC SP
                case 0x3B:
                    cpu.SP--;
                    return 8;

                // 0x3C - INC A
                case 0x3C:
                    cpu.A = Inc8(cpu, cpu.A);
                    return 4;

                // 0x3D - DEC A
                case 0x3D:
                    cpu.A = Dec8(cpu, cpu.A);
                    return 4;

                // 0x3E - LD A,d8
                case 0x3E:
                    cpu.A = cpu.ReadByte();
                    return 8;

                // 0x3F - CCF
                case 0x3F:
                    cpu.FlagN = false;
                    cpu.FlagH = false;
                    cpu.FlagC = !cpu.FlagC;
                    return 4;

                // LD r,r instructions (0x40-0x7F)
                case 0x40: cpu.B = cpu.B; return 4; // LD B,B
                case 0x41: cpu.B = cpu.C; return 4; // LD B,C
                case 0x42: cpu.B = cpu.D; return 4; // LD B,D
                case 0x43: cpu.B = cpu.E; return 4; // LD B,E
                case 0x44: cpu.B = cpu.H; return 4; // LD B,H
                case 0x45: cpu.B = cpu.L; return 4; // LD B,L
                case 0x46: cpu.B = mmu.ReadByte(cpu.HL); return 8; // LD B,(HL)
                case 0x47: cpu.B = cpu.A; return 4; // LD B,A

                case 0x48: cpu.C = cpu.B; return 4; // LD C,B
                case 0x49: cpu.C = cpu.C; return 4; // LD C,C
                case 0x4A: cpu.C = cpu.D; return 4; // LD C,D
                case 0x4B: cpu.C = cpu.E; return 4; // LD C,E
                case 0x4C: cpu.C = cpu.H; return 4; // LD C,H
                case 0x4D: cpu.C = cpu.L; return 4; // LD C,L
                case 0x4E: cpu.C = mmu.ReadByte(cpu.HL); return 8; // LD C,(HL)
                case 0x4F: cpu.C = cpu.A; return 4; // LD C,A

                case 0x50: cpu.D = cpu.B; return 4; // LD D,B
                case 0x51: cpu.D = cpu.C; return 4; // LD D,C
                case 0x52: cpu.D = cpu.D; return 4; // LD D,D
                case 0x53: cpu.D = cpu.E; return 4; // LD D,E
                case 0x54: cpu.D = cpu.H; return 4; // LD D,H
                case 0x55: cpu.D = cpu.L; return 4; // LD D,L
                case 0x56: cpu.D = mmu.ReadByte(cpu.HL); return 8; // LD D,(HL)
                case 0x57: cpu.D = cpu.A; return 4; // LD D,A

                case 0x58: cpu.E = cpu.B; return 4; // LD E,B
                case 0x59: cpu.E = cpu.C; return 4; // LD E,C
                case 0x5A: cpu.E = cpu.D; return 4; // LD E,D
                case 0x5B: cpu.E = cpu.E; return 4; // LD E,E
                case 0x5C: cpu.E = cpu.H; return 4; // LD E,H
                case 0x5D: cpu.E = cpu.L; return 4; // LD E,L
                case 0x5E: cpu.E = mmu.ReadByte(cpu.HL); return 8; // LD E,(HL)
                case 0x5F: cpu.E = cpu.A; return 4; // LD E,A

                case 0x60: cpu.H = cpu.B; return 4; // LD H,B
                case 0x61: cpu.H = cpu.C; return 4; // LD H,C
                case 0x62: cpu.H = cpu.D; return 4; // LD H,D
                case 0x63: cpu.H = cpu.E; return 4; // LD H,E
                case 0x64: cpu.H = cpu.H; return 4; // LD H,H
                case 0x65: cpu.H = cpu.L; return 4; // LD H,L
                case 0x66: cpu.H = mmu.ReadByte(cpu.HL); return 8; // LD H,(HL)
                case 0x67: cpu.H = cpu.A; return 4; // LD H,A

                case 0x68: cpu.L = cpu.B; return 4; // LD L,B
                case 0x69: cpu.L = cpu.C; return 4; // LD L,C
                case 0x6A: cpu.L = cpu.D; return 4; // LD L,D
                case 0x6B: cpu.L = cpu.E; return 4; // LD L,E
                case 0x6C: cpu.L = cpu.H; return 4; // LD L,H
                case 0x6D: cpu.L = cpu.L; return 4; // LD L,L
                case 0x6E: cpu.L = mmu.ReadByte(cpu.HL); return 8; // LD L,(HL)
                case 0x6F: cpu.L = cpu.A; return 4; // LD L,A

                case 0x70: mmu.WriteByte(cpu.HL, cpu.B); return 8; // LD (HL),B
                case 0x71: mmu.WriteByte(cpu.HL, cpu.C); return 8; // LD (HL),C
                case 0x72: mmu.WriteByte(cpu.HL, cpu.D); return 8; // LD (HL),D
                case 0x73: mmu.WriteByte(cpu.HL, cpu.E); return 8; // LD (HL),E
                case 0x74: mmu.WriteByte(cpu.HL, cpu.H); return 8; // LD (HL),H
                case 0x75: mmu.WriteByte(cpu.HL, cpu.L); return 8; // LD (HL),L
                case 0x76: cpu.Halted = true; return 4; // HALT
                case 0x77: mmu.WriteByte(cpu.HL, cpu.A); return 8; // LD (HL),A

                case 0x78: cpu.A = cpu.B; return 4; // LD A,B
                case 0x79: cpu.A = cpu.C; return 4; // LD A,C
                case 0x7A: cpu.A = cpu.D; return 4; // LD A,D
                case 0x7B: cpu.A = cpu.E; return 4; // LD A,E
                case 0x7C: cpu.A = cpu.H; return 4; // LD A,H
                case 0x7D: cpu.A = cpu.L; return 4; // LD A,L
                case 0x7E: cpu.A = mmu.ReadByte(cpu.HL); return 8; // LD A,(HL)
                case 0x7F: cpu.A = cpu.A; return 4; // LD A,A

                // ADD A,r instructions (0x80-0x87)
                case 0x80: cpu.A = Add8(cpu, cpu.A, cpu.B); return 4; // ADD A,B
                case 0x81: cpu.A = Add8(cpu, cpu.A, cpu.C); return 4; // ADD A,C
                case 0x82: cpu.A = Add8(cpu, cpu.A, cpu.D); return 4; // ADD A,D
                case 0x83: cpu.A = Add8(cpu, cpu.A, cpu.E); return 4; // ADD A,E
                case 0x84: cpu.A = Add8(cpu, cpu.A, cpu.H); return 4; // ADD A,H
                case 0x85: cpu.A = Add8(cpu, cpu.A, cpu.L); return 4; // ADD A,L
                case 0x86: cpu.A = Add8(cpu, cpu.A, mmu.ReadByte(cpu.HL)); return 8; // ADD A,(HL)
                case 0x87: cpu.A = Add8(cpu, cpu.A, cpu.A); return 4; // ADD A,A

                // ADC A,r instructions (0x88-0x8F)
                case 0x88: cpu.A = ADC8(cpu, cpu.A, cpu.B); return 4; // ADC A,B
                case 0x89: cpu.A = ADC8(cpu, cpu.A, cpu.C); return 4; // ADC A,C
                case 0x8A: cpu.A = ADC8(cpu, cpu.A, cpu.D); return 4; // ADC A,D
                case 0x8B: cpu.A = ADC8(cpu, cpu.A, cpu.E); return 4; // ADC A,E
                case 0x8C: cpu.A = ADC8(cpu, cpu.A, cpu.H); return 4; // ADC A,H
                case 0x8D: cpu.A = ADC8(cpu, cpu.A, cpu.L); return 4; // ADC A,L
                case 0x8E: cpu.A = ADC8(cpu, cpu.A, mmu.ReadByte(cpu.HL)); return 8; // ADC A,(HL)
                case 0x8F: cpu.A = ADC8(cpu, cpu.A, cpu.A); return 4; // ADC A,A

                // SUB r instructions (0x90-0x97)
                case 0x90: cpu.A = Sub8(cpu, cpu.A, cpu.B); return 4; // SUB B
                case 0x91: cpu.A = Sub8(cpu, cpu.A, cpu.C); return 4; // SUB C
                case 0x92: cpu.A = Sub8(cpu, cpu.A, cpu.D); return 4; // SUB D
                case 0x93: cpu.A = Sub8(cpu, cpu.A, cpu.E); return 4; // SUB E
                case 0x94: cpu.A = Sub8(cpu, cpu.A, cpu.H); return 4; // SUB H
                case 0x95: cpu.A = Sub8(cpu, cpu.A, cpu.L); return 4; // SUB L
                case 0x96: cpu.A = Sub8(cpu, cpu.A, mmu.ReadByte(cpu.HL)); return 8; // SUB (HL)
                case 0x97: cpu.A = Sub8(cpu, cpu.A, cpu.A); return 4; // SUB A

                // SBC A,r instructions (0x98-0x9F)
                case 0x98: cpu.A = SBC8(cpu, cpu.A, cpu.B); return 4; // SBC A,B
                case 0x99: cpu.A = SBC8(cpu, cpu.A, cpu.C); return 4; // SBC A,C
                case 0x9A: cpu.A = SBC8(cpu, cpu.A, cpu.D); return 4; // SBC A,D
                case 0x9B: cpu.A = SBC8(cpu, cpu.A, cpu.E); return 4; // SBC A,E
                case 0x9C: cpu.A = SBC8(cpu, cpu.A, cpu.H); return 4; // SBC A,H
                case 0x9D: cpu.A = SBC8(cpu, cpu.A, cpu.L); return 4; // SBC A,L
                case 0x9E: cpu.A = SBC8(cpu, cpu.A, mmu.ReadByte(cpu.HL)); return 8; // SBC A,(HL)
                case 0x9F: cpu.A = SBC8(cpu, cpu.A, cpu.A); return 4; // SBC A,A

                // AND r instructions (0xA0-0xA7)
                case 0xA0: cpu.A = And8(cpu, cpu.A, cpu.B); return 4; // AND B
                case 0xA1: cpu.A = And8(cpu, cpu.A, cpu.C); return 4; // AND C
                case 0xA2: cpu.A = And8(cpu, cpu.A, cpu.D); return 4; // AND D
                case 0xA3: cpu.A = And8(cpu, cpu.A, cpu.E); return 4; // AND E
                case 0xA4: cpu.A = And8(cpu, cpu.A, cpu.H); return 4; // AND H
                case 0xA5: cpu.A = And8(cpu, cpu.A, cpu.L); return 4; // AND L
                case 0xA6: cpu.A = And8(cpu, cpu.A, mmu.ReadByte(cpu.HL)); return 8; // AND (HL)
                case 0xA7: cpu.A = And8(cpu, cpu.A, cpu.A); return 4; // AND A

                // XOR r instructions (0xA8-0xAF)
                case 0xA8: cpu.A = Xor8(cpu, cpu.A, cpu.B); return 4; // XOR B
                case 0xA9: cpu.A = Xor8(cpu, cpu.A, cpu.C); return 4; // XOR C
                case 0xAA: cpu.A = Xor8(cpu, cpu.A, cpu.D); return 4; // XOR D
                case 0xAB: cpu.A = Xor8(cpu, cpu.A, cpu.E); return 4; // XOR E
                case 0xAC: cpu.A = Xor8(cpu, cpu.A, cpu.H); return 4; // XOR H
                case 0xAD: cpu.A = Xor8(cpu, cpu.A, cpu.L); return 4; // XOR L
                case 0xAE: cpu.A = Xor8(cpu, cpu.A, mmu.ReadByte(cpu.HL)); return 8; // XOR (HL)
                case 0xAF: cpu.A = 0; cpu.FlagZ = true; cpu.FlagN = false; cpu.FlagH = false; cpu.FlagC = false; return 4; // XOR A

                // OR r instructions (0xB0-0xB7)
                case 0xB0: cpu.A = Or8(cpu, cpu.A, cpu.B); return 4; // OR B
                case 0xB1: cpu.A = Or8(cpu, cpu.A, cpu.C); return 4; // OR C
                case 0xB2: cpu.A = Or8(cpu, cpu.A, cpu.D); return 4; // OR D
                case 0xB3: cpu.A = Or8(cpu, cpu.A, cpu.E); return 4; // OR E
                case 0xB4: cpu.A = Or8(cpu, cpu.A, cpu.H); return 4; // OR H
                case 0xB5: cpu.A = Or8(cpu, cpu.A, cpu.L); return 4; // OR L
                case 0xB6: cpu.A = Or8(cpu, cpu.A, mmu.ReadByte(cpu.HL)); return 8; // OR (HL)
                case 0xB7: cpu.FlagZ = (cpu.A == 0); cpu.FlagN = false; cpu.FlagH = false; cpu.FlagC = false; return 4; // OR A

                // CP r instructions (0xB8-0xBF)
                case 0xB8: Sub8(cpu, cpu.A, cpu.B); return 4; // CP B
                case 0xB9: Sub8(cpu, cpu.A, cpu.C); return 4; // CP C
                case 0xBA: Sub8(cpu, cpu.A, cpu.D); return 4; // CP D
                case 0xBB: Sub8(cpu, cpu.A, cpu.E); return 4; // CP E
                case 0xBC: Sub8(cpu, cpu.A, cpu.H); return 4; // CP H
                case 0xBD: Sub8(cpu, cpu.A, cpu.L); return 4; // CP L
                case 0xBE: Sub8(cpu, cpu.A, mmu.ReadByte(cpu.HL)); return 8; // CP (HL)
                case 0xBF: Sub8(cpu, cpu.A, cpu.A); return 4; // CP A

                // Conditional returns and jumps (0xC0-0xFF)
                case 0xC0: // RET NZ
                    if (!cpu.FlagZ)
                    {
                        cpu.PC = cpu.Pop();
                        return 20;
                    }
                    return 8;

                case 0xC1: // POP BC
                    cpu.BC = cpu.Pop();
                    return 12;

                case 0xC2: // JP NZ,a16
                    {
                        ushort address = cpu.ReadWord();
                        if (!cpu.FlagZ)
                        {
                            cpu.PC = address;
                            return 16;
                        }
                        return 12;
                    }

                case 0xC3: // JP a16
                    cpu.PC = cpu.ReadWord();
                    return 16;

                case 0xC4: // CALL NZ,a16
                    {
                        ushort address = cpu.ReadWord();
                        if (!cpu.FlagZ)
                        {
                            cpu.Push(cpu.PC);
                            cpu.PC = address;
                            return 24;
                        }
                        return 12;
                    }

                case 0xC5: // PUSH BC
                    cpu.Push(cpu.BC);
                    return 16;

                case 0xC6: // ADD A,d8
                    cpu.A = Add8(cpu, cpu.A, cpu.ReadByte());
                    return 8;

                case 0xC7: // RST 00H
                    cpu.Push(cpu.PC);
                    cpu.PC = 0x0000;
                    return 16;

                case 0xC8: // RET Z
                    if (cpu.FlagZ)
                    {
                        cpu.PC = cpu.Pop();
                        return 20;
                    }
                    return 8;

                case 0xC9: // RET
                    cpu.PC = cpu.Pop();
                    return 16;

                case 0xCA: // JP Z,a16
                    {
                        ushort address = cpu.ReadWord();
                        if (cpu.FlagZ)
                        {
                            cpu.PC = address;
                            return 16;
                        }
                        return 12;
                    }

                case 0xCB: // CB prefix (extended instructions)
                    return ExecuteCBInstruction(cpu, mmu);

                case 0xCC: // CALL Z,a16
                    {
                        ushort address = cpu.ReadWord();
                        if (cpu.FlagZ)
                        {
                            cpu.Push(cpu.PC);
                            cpu.PC = address;
                            return 24;
                        }
                        return 12;
                    }

                case 0xCD: // CALL a16
                    {
                        ushort address = cpu.ReadWord();
                        cpu.Push(cpu.PC);
                        cpu.PC = address;
                        return 24;
                    }

                case 0xCE: // ADC A,d8
                    cpu.A = ADC8(cpu, cpu.A, cpu.ReadByte());
                    return 8;

                case 0xCF: // RST 08H
                    cpu.Push(cpu.PC);
                    cpu.PC = 0x0008;
                    return 16;

                case 0xD0: // RET NC
                    if (!cpu.FlagC)
                    {
                        cpu.PC = cpu.Pop();
                        return 20;
                    }
                    return 8;

                case 0xD1: // POP DE
                    cpu.DE = cpu.Pop();
                    return 12;

                case 0xD2: // JP NC,a16
                    {
                        ushort address = cpu.ReadWord();
                        if (!cpu.FlagC)
                        {
                            cpu.PC = address;
                            return 16;
                        }
                        return 12;
                    }

                case 0xD4: // CALL NC,a16
                    {
                        ushort address = cpu.ReadWord();
                        if (!cpu.FlagC)
                        {
                            cpu.Push(cpu.PC);
                            cpu.PC = address;
                            return 24;
                        }
                        return 12;
                    }

                case 0xD5: // PUSH DE
                    cpu.Push(cpu.DE);
                    return 16;

                case 0xD6: // SUB d8
                    cpu.A = Sub8(cpu, cpu.A, cpu.ReadByte());
                    return 8;

                case 0xD7: // RST 10H
                    cpu.Push(cpu.PC);
                    cpu.PC = 0x0010;
                    return 16;

                case 0xD8: // RET C
                    if (cpu.FlagC)
                    {
                        cpu.PC = cpu.Pop();
                        return 20;
                    }
                    return 8;

                case 0xD9: // RETI
                    cpu.PC = cpu.Pop();
                    cpu.IME = true;
                    return 16;

                case 0xDA: // JP C,a16
                    {
                        ushort address = cpu.ReadWord();
                        if (cpu.FlagC)
                        {
                            cpu.PC = address;
                            return 16;
                        }
                        return 12;
                    }

                case 0xDC: // CALL C,a16
                    {
                        ushort address = cpu.ReadWord();
                        if (cpu.FlagC)
                        {
                            cpu.Push(cpu.PC);
                            cpu.PC = address;
                            return 24;
                        }
                        return 12;
                    }

                case 0xDE: // SBC A,d8
                    cpu.A = SBC8(cpu, cpu.A, cpu.ReadByte());
                    return 8;

                case 0xDF: // RST 18H
                    cpu.Push(cpu.PC);
                    cpu.PC = 0x0018;
                    return 16;

                case 0xE0: // LD (FF00+a8),A
                    {
                        byte offset = cpu.ReadByte();
                        mmu.WriteByte((ushort)(0xFF00 + offset), cpu.A);
                        return 12;
                    }

                case 0xE1: // POP HL
                    cpu.HL = cpu.Pop();
                    return 12;

                case 0xE2: // LD (FF00+C),A
                    mmu.WriteByte((ushort)(0xFF00 + cpu.C), cpu.A);
                    return 8;

                case 0xE5: // PUSH HL
                    cpu.Push(cpu.HL);
                    return 16;

                case 0xE6: // AND d8
                    cpu.A = And8(cpu, cpu.A, cpu.ReadByte());
                    return 8;

                case 0xE7: // RST 20H
                    cpu.Push(cpu.PC);
                    cpu.PC = 0x0020;
                    return 16;

                case 0xE8: // ADD SP,r8
                    {
                        sbyte offset = (sbyte)cpu.ReadByte();
                        int result = cpu.SP + offset;
                        cpu.FlagZ = false;
                        cpu.FlagN = false;
                        cpu.FlagH = ((cpu.SP ^ offset ^ result) & 0x10) != 0;
                        cpu.FlagC = ((cpu.SP ^ offset ^ result) & 0x100) != 0;
                        cpu.SP = (ushort)result;
                        return 16;
                    }

                case 0xE9: // JP HL
                    cpu.PC = cpu.HL;
                    return 4;

                case 0xEA: // LD (a16),A
                    mmu.WriteByte(cpu.ReadWord(), cpu.A);
                    return 16;

                case 0xEE: // XOR d8
                    cpu.A = Xor8(cpu, cpu.A, cpu.ReadByte());
                    return 8;

                case 0xEF: // RST 28H
                    cpu.Push(cpu.PC);
                    cpu.PC = 0x0028;
                    return 16;

                case 0xF0: // LD A,(FF00+a8)
                    {
                        byte offset = cpu.ReadByte();
                        cpu.A = mmu.ReadByte((ushort)(0xFF00 + offset));
                        return 12;
                    }

                case 0xF1: // POP AF
                    cpu.AF = (ushort)(cpu.Pop() & 0xFFF0); // Lower 4 bits of F are always 0
                    return 12;

                case 0xF2: // LD A,(FF00+C)
                    cpu.A = mmu.ReadByte((ushort)(0xFF00 + cpu.C));
                    return 8;

                case 0xF3: // DI (Disable Interrupts)
                    cpu.IME = false;
                    return 4;

                case 0xF5: // PUSH AF
                    cpu.Push(cpu.AF);
                    return 16;

                case 0xF6: // OR d8
                    cpu.A = Or8(cpu, cpu.A, cpu.ReadByte());
                    return 8;

                case 0xF7: // RST 30H
                    cpu.Push(cpu.PC);
                    cpu.PC = 0x0030;
                    return 16;

                case 0xF8: // LD HL,SP+r8
                    {
                        sbyte offset = (sbyte)cpu.ReadByte();
                        int result = cpu.SP + offset;
                        cpu.FlagZ = false;
                        cpu.FlagN = false;
                        cpu.FlagH = ((cpu.SP ^ offset ^ result) & 0x10) != 0;
                        cpu.FlagC = ((cpu.SP ^ offset ^ result) & 0x100) != 0;
                        cpu.HL = (ushort)result;
                        return 12;
                    }

                case 0xF9: // LD SP,HL
                    cpu.SP = cpu.HL;
                    return 8;

                case 0xFA: // LD A,(a16)
                    cpu.A = mmu.ReadByte(cpu.ReadWord());
                    return 16;

                case 0xFB: // EI (Enable Interrupts)
                    cpu.IME = true;
                    return 4;

                case 0xFE: // CP d8
                    Sub8(cpu, cpu.A, cpu.ReadByte());
                    return 8;

                case 0xFF: // RST 38H
                    cpu.Push(cpu.PC);
                    cpu.PC = 0x0038;
                    return 16;

                // Invalid opcodes (0xD3, 0xDB, 0xDD, 0xE3, 0xE4, 0xEB, 0xEC, 0xED, 0xF4, 0xFC, 0xFD)
                case 0xD3:
                case 0xDB:
                case 0xDD:
                case 0xE3:
                case 0xE4:
                case 0xEB:
                case 0xEC:
                case 0xED:
                case 0xF4:
                case 0xFC:
                case 0xFD:
                    cpu.LogCallback?.Invoke($"ILLEGAL OPCODE 0x{opcode:X2} at PC 0x{cpu.PC - 1:X4}");
                    return 4;

                default:
                    cpu.LogCallback?.Invoke($"Warning: Unimplemented opcode 0x{opcode:X2} at PC 0x{cpu.PC - 1:X4}");
                    return 4;
            }
        }
        
        // Helper methods for arithmetic operations
        private static byte Inc8(CPU cpu, byte value)
        {
            cpu.FlagH = (value & 0x0F) == 0x0F;
            value++;
            cpu.FlagZ = value == 0;
            cpu.FlagN = false;
            return value;
        }
        
        private static byte Dec8(CPU cpu, byte value)
        {
            cpu.FlagH = (value & 0x0F) == 0;
            value--;
            cpu.FlagZ = value == 0;
            cpu.FlagN = true;
            return value;
        }
        
        private static ushort Add16(CPU cpu, ushort a, ushort b)
        {
            uint result = (uint)(a + b);
            cpu.FlagN = false;
            cpu.FlagH = ((a ^ b ^ result) & 0x1000) != 0;
            cpu.FlagC = result > 0xFFFF;
            return (ushort)result;
        }
        
        private static byte Add8(CPU cpu, byte a, byte b)
        {
            uint result = (uint)(a + b);
            cpu.FlagZ = (result & 0xFF) == 0;
            cpu.FlagN = false;
            cpu.FlagH = ((a ^ b ^ result) & 0x10) != 0;
            cpu.FlagC = result > 0xFF;
            return (byte)result;
        }
        
        private static byte Sub8(CPU cpu, byte a, byte b)
        {
            int result = a - b;
            cpu.FlagZ = (result & 0xFF) == 0;
            cpu.FlagN = true;
            cpu.FlagH = ((a ^ b ^ result) & 0x10) != 0;
            cpu.FlagC = result < 0;
            return (byte)result;
        }
        
        private static byte SBC8(CPU cpu, byte a, byte b)
        {
            int carry = cpu.FlagC ? 1 : 0;
            int result = a - b - carry;
            cpu.FlagZ = (result & 0xFF) == 0;
            cpu.FlagN = true;
            cpu.FlagH = ((a ^ b ^ result) & 0x10) != 0;
            cpu.FlagC = result < 0;
            return (byte)result;
        }
        
        private static byte Xor8(CPU cpu, byte a, byte b)
        {
            byte result = (byte)(a ^ b);
            cpu.FlagZ = result == 0;
            cpu.FlagN = false;
            cpu.FlagH = false;
            cpu.FlagC = false;
            return result;
        }
        
        private static byte RLC(CPU cpu, byte value)
        {
            bool bit7 = (value & 0x80) != 0;
            value = (byte)((value << 1) | (bit7 ? 1 : 0));
            cpu.FlagZ = value == 0;
            cpu.FlagN = false;
            cpu.FlagH = false;
            cpu.FlagC = bit7;
            return value;
        }
        
        // CB prefix instruction handler (bit operations)
        private static int ExecuteCBInstruction(CPU cpu, MMU.MMU mmu)
        {
            byte suffix = cpu.ReadByte();
            
            switch (suffix)
            {
                // RLC instructions (0x00-0x07)
                case 0x00: cpu.B = RLC(cpu, cpu.B); return 8;
                case 0x01: cpu.C = RLC(cpu, cpu.C); return 8;
                case 0x02: cpu.D = RLC(cpu, cpu.D); return 8;
                case 0x03: cpu.E = RLC(cpu, cpu.E); return 8;
                case 0x04: cpu.H = RLC(cpu, cpu.H); return 8;
                case 0x05: cpu.L = RLC(cpu, cpu.L); return 8;
                case 0x06: 
                    {
                        byte value = mmu.ReadByte(cpu.HL);
                        value = RLC(cpu, value);
                        mmu.WriteByte(cpu.HL, value);
                        return 16;
                    }
                case 0x07: cpu.A = RLC(cpu, cpu.A); return 8;

                // RRC instructions (0x08-0x0F)
                case 0x08: cpu.B = RRC(cpu, cpu.B); return 8;
                case 0x09: cpu.C = RRC(cpu, cpu.C); return 8;
                case 0x0A: cpu.D = RRC(cpu, cpu.D); return 8;
                case 0x0B: cpu.E = RRC(cpu, cpu.E); return 8;
                case 0x0C: cpu.H = RRC(cpu, cpu.H); return 8;
                case 0x0D: cpu.L = RRC(cpu, cpu.L); return 8;
                case 0x0E: 
                    {
                        byte value = mmu.ReadByte(cpu.HL);
                        value = RRC(cpu, value);
                        mmu.WriteByte(cpu.HL, value);
                        return 16;
                    }
                case 0x0F: cpu.A = RRC(cpu, cpu.A); return 8;

                // RL instructions (0x10-0x17)
                case 0x10: cpu.B = RL(cpu, cpu.B); return 8;
                case 0x11: cpu.C = RL(cpu, cpu.C); return 8;
                case 0x12: cpu.D = RL(cpu, cpu.D); return 8;
                case 0x13: cpu.E = RL(cpu, cpu.E); return 8;
                case 0x14: cpu.H = RL(cpu, cpu.H); return 8;
                case 0x15: cpu.L = RL(cpu, cpu.L); return 8;
                case 0x16: 
                    {
                        byte value = mmu.ReadByte(cpu.HL);
                        value = RL(cpu, value);
                        mmu.WriteByte(cpu.HL, value);
                        return 16;
                    }
                case 0x17: cpu.A = RL(cpu, cpu.A); return 8;

                // RR instructions (0x18-0x1F)
                case 0x18: cpu.B = RR(cpu, cpu.B); return 8;
                case 0x19: cpu.C = RR(cpu, cpu.C); return 8;
                case 0x1A: cpu.D = RR(cpu, cpu.D); return 8;
                case 0x1B: cpu.E = RR(cpu, cpu.E); return 8;
                case 0x1C: cpu.H = RR(cpu, cpu.H); return 8;
                case 0x1D: cpu.L = RR(cpu, cpu.L); return 8;
                case 0x1E: 
                    {
                        byte value = mmu.ReadByte(cpu.HL);
                        value = RR(cpu, value);
                        mmu.WriteByte(cpu.HL, value);
                        return 16;
                    }
                case 0x1F: cpu.A = RR(cpu, cpu.A); return 8;

                // SLA instructions (0x20-0x27)
                case 0x20: cpu.B = SLA(cpu, cpu.B); return 8;
                case 0x21: cpu.C = SLA(cpu, cpu.C); return 8;
                case 0x22: cpu.D = SLA(cpu, cpu.D); return 8;
                case 0x23: cpu.E = SLA(cpu, cpu.E); return 8;
                case 0x24: cpu.H = SLA(cpu, cpu.H); return 8;
                case 0x25: cpu.L = SLA(cpu, cpu.L); return 8;
                case 0x26: 
                    {
                        byte value = mmu.ReadByte(cpu.HL);
                        value = SLA(cpu, value);
                        mmu.WriteByte(cpu.HL, value);
                        return 16;
                    }
                case 0x27: cpu.A = SLA(cpu, cpu.A); return 8;

                // SRA instructions (0x28-0x2F)
                case 0x28: cpu.B = SRA(cpu, cpu.B); return 8;
                case 0x29: cpu.C = SRA(cpu, cpu.C); return 8;
                case 0x2A: cpu.D = SRA(cpu, cpu.D); return 8;
                case 0x2B: cpu.E = SRA(cpu, cpu.E); return 8;
                case 0x2C: cpu.H = SRA(cpu, cpu.H); return 8;
                case 0x2D: cpu.L = SRA(cpu, cpu.L); return 8;
                case 0x2E: 
                    {
                        byte value = mmu.ReadByte(cpu.HL);
                        value = SRA(cpu, value);
                        mmu.WriteByte(cpu.HL, value);
                        return 16;
                    }
                case 0x2F: cpu.A = SRA(cpu, cpu.A); return 8;

                // SWAP instructions (0x30-0x37)
                case 0x30: cpu.B = SWAP(cpu, cpu.B); return 8;
                case 0x31: cpu.C = SWAP(cpu, cpu.C); return 8;
                case 0x32: cpu.D = SWAP(cpu, cpu.D); return 8;
                case 0x33: cpu.E = SWAP(cpu, cpu.E); return 8;
                case 0x34: cpu.H = SWAP(cpu, cpu.H); return 8;
                case 0x35: cpu.L = SWAP(cpu, cpu.L); return 8;
                case 0x36: 
                    {
                        byte value = mmu.ReadByte(cpu.HL);
                        value = SWAP(cpu, value);
                        mmu.WriteByte(cpu.HL, value);
                        return 16;
                    }
                case 0x37: cpu.A = SWAP(cpu, cpu.A); return 8;

                // SRL instructions (0x38-0x3F)
                case 0x38: cpu.B = SRL(cpu, cpu.B); return 8;
                case 0x39: cpu.C = SRL(cpu, cpu.C); return 8;
                case 0x3A: cpu.D = SRL(cpu, cpu.D); return 8;
                case 0x3B: cpu.E = SRL(cpu, cpu.E); return 8;
                case 0x3C: cpu.H = SRL(cpu, cpu.H); return 8;
                case 0x3D: cpu.L = SRL(cpu, cpu.L); return 8;
                case 0x3E: 
                    {
                        byte value = mmu.ReadByte(cpu.HL);
                        value = SRL(cpu, value);
                        mmu.WriteByte(cpu.HL, value);
                        return 16;
                    }
                case 0x3F: cpu.A = SRL(cpu, cpu.A); return 8;

                // BIT 0 instructions (0x40-0x47)
                case 0x40: BIT(cpu, 0, cpu.B); return 8;
                case 0x41: BIT(cpu, 0, cpu.C); return 8;
                case 0x42: BIT(cpu, 0, cpu.D); return 8;
                case 0x43: BIT(cpu, 0, cpu.E); return 8;
                case 0x44: BIT(cpu, 0, cpu.H); return 8;
                case 0x45: BIT(cpu, 0, cpu.L); return 8;
                case 0x46: BIT(cpu, 0, mmu.ReadByte(cpu.HL)); return 12;
                case 0x47: BIT(cpu, 0, cpu.A); return 8;

                // BIT 1 instructions (0x48-0x4F)
                case 0x48: BIT(cpu, 1, cpu.B); return 8;
                case 0x49: BIT(cpu, 1, cpu.C); return 8;
                case 0x4A: BIT(cpu, 1, cpu.D); return 8;
                case 0x4B: BIT(cpu, 1, cpu.E); return 8;
                case 0x4C: BIT(cpu, 1, cpu.H); return 8;
                case 0x4D: BIT(cpu, 1, cpu.L); return 8;
                case 0x4E: BIT(cpu, 1, mmu.ReadByte(cpu.HL)); return 12;
                case 0x4F: BIT(cpu, 1, cpu.A); return 8;

                // BIT 2 instructions (0x50-0x57)
                case 0x50: BIT(cpu, 2, cpu.B); return 8;
                case 0x51: BIT(cpu, 2, cpu.C); return 8;
                case 0x52: BIT(cpu, 2, cpu.D); return 8;
                case 0x53: BIT(cpu, 2, cpu.E); return 8;
                case 0x54: BIT(cpu, 2, cpu.H); return 8;
                case 0x55: BIT(cpu, 2, cpu.L); return 8;
                case 0x56: BIT(cpu, 2, mmu.ReadByte(cpu.HL)); return 12;
                case 0x57: BIT(cpu, 2, cpu.A); return 8;

                // BIT 3 instructions (0x58-0x5F)
                case 0x58: BIT(cpu, 3, cpu.B); return 8;
                case 0x59: BIT(cpu, 3, cpu.C); return 8;
                case 0x5A: BIT(cpu, 3, cpu.D); return 8;
                case 0x5B: BIT(cpu, 3, cpu.E); return 8;
                case 0x5C: BIT(cpu, 3, cpu.H); return 8;
                case 0x5D: BIT(cpu, 3, cpu.L); return 8;
                case 0x5E: BIT(cpu, 3, mmu.ReadByte(cpu.HL)); return 12;
                case 0x5F: BIT(cpu, 3, cpu.A); return 8;

                // BIT 4 instructions (0x60-0x67)
                case 0x60: BIT(cpu, 4, cpu.B); return 8;
                case 0x61: BIT(cpu, 4, cpu.C); return 8;
                case 0x62: BIT(cpu, 4, cpu.D); return 8;
                case 0x63: BIT(cpu, 4, cpu.E); return 8;
                case 0x64: BIT(cpu, 4, cpu.H); return 8;
                case 0x65: BIT(cpu, 4, cpu.L); return 8;
                case 0x66: BIT(cpu, 4, mmu.ReadByte(cpu.HL)); return 12;
                case 0x67: BIT(cpu, 4, cpu.A); return 8;

                // BIT 5 instructions (0x68-0x6F)
                case 0x68: BIT(cpu, 5, cpu.B); return 8;
                case 0x69: BIT(cpu, 5, cpu.C); return 8;
                case 0x6A: BIT(cpu, 5, cpu.D); return 8;
                case 0x6B: BIT(cpu, 5, cpu.E); return 8;
                case 0x6C: BIT(cpu, 5, cpu.H); return 8;
                case 0x6D: BIT(cpu, 5, cpu.L); return 8;
                case 0x6E: BIT(cpu, 5, mmu.ReadByte(cpu.HL)); return 12;
                case 0x6F: BIT(cpu, 5, cpu.A); return 8;

                // BIT 6 instructions (0x70-0x77)
                case 0x70: BIT(cpu, 6, cpu.B); return 8;
                case 0x71: BIT(cpu, 6, cpu.C); return 8;
                case 0x72: BIT(cpu, 6, cpu.D); return 8;
                case 0x73: BIT(cpu, 6, cpu.E); return 8;
                case 0x74: BIT(cpu, 6, cpu.H); return 8;
                case 0x75: BIT(cpu, 6, cpu.L); return 8;
                case 0x76: BIT(cpu, 6, mmu.ReadByte(cpu.HL)); return 12;
                case 0x77: BIT(cpu, 6, cpu.A); return 8;

                // BIT 7 instructions (0x78-0x7F)
                case 0x78: BIT(cpu, 7, cpu.B); return 8;
                case 0x79: BIT(cpu, 7, cpu.C); return 8;
                case 0x7A: BIT(cpu, 7, cpu.D); return 8;
                case 0x7B: BIT(cpu, 7, cpu.E); return 8;
                case 0x7C: BIT(cpu, 7, cpu.H); return 8;
                case 0x7D: BIT(cpu, 7, cpu.L); return 8;
                case 0x7E: BIT(cpu, 7, mmu.ReadByte(cpu.HL)); return 12;
                case 0x7F: BIT(cpu, 7, cpu.A); return 8;

                // RES 0 instructions (0x80-0x87)
                case 0x80: cpu.B = RES(0, cpu.B); return 8;
                case 0x81: cpu.C = RES(0, cpu.C); return 8;
                case 0x82: cpu.D = RES(0, cpu.D); return 8;
                case 0x83: cpu.E = RES(0, cpu.E); return 8;
                case 0x84: cpu.H = RES(0, cpu.H); return 8;
                case 0x85: cpu.L = RES(0, cpu.L); return 8;
                case 0x86: 
                    {
                        byte value = mmu.ReadByte(cpu.HL);
                        value = RES(0, value);
                        mmu.WriteByte(cpu.HL, value);
                        return 16;
                    }
                case 0x87: cpu.A = RES(0, cpu.A); return 8;

                // RES 1 instructions (0x88-0x8F)
                case 0x88: cpu.B = RES(1, cpu.B); return 8;
                case 0x89: cpu.C = RES(1, cpu.C); return 8;
                case 0x8A: cpu.D = RES(1, cpu.D); return 8;
                case 0x8B: cpu.E = RES(1, cpu.E); return 8;
                case 0x8C: cpu.H = RES(1, cpu.H); return 8;
                case 0x8D: cpu.L = RES(1, cpu.L); return 8;
                case 0x8E: 
                    {
                        byte value = mmu.ReadByte(cpu.HL);
                        value = RES(1, value);
                        mmu.WriteByte(cpu.HL, value);
                        return 16;
                    }
                case 0x8F: cpu.A = RES(1, cpu.A); return 8;

                // RES 2 instructions (0x90-0x97)
                case 0x90: cpu.B = RES(2, cpu.B); return 8;
                case 0x91: cpu.C = RES(2, cpu.C); return 8;
                case 0x92: cpu.D = RES(2, cpu.D); return 8;
                case 0x93: cpu.E = RES(2, cpu.E); return 8;
                case 0x94: cpu.H = RES(2, cpu.H); return 8;
                case 0x95: cpu.L = RES(2, cpu.L); return 8;
                case 0x96: 
                    {
                        byte value = mmu.ReadByte(cpu.HL);
                        value = RES(2, value);
                        mmu.WriteByte(cpu.HL, value);
                        return 16;
                    }
                case 0x97: cpu.A = RES(2, cpu.A); return 8;

                // RES 3 instructions (0x98-0x9F)
                case 0x98: cpu.B = RES(3, cpu.B); return 8;
                case 0x99: cpu.C = RES(3, cpu.C); return 8;
                case 0x9A: cpu.D = RES(3, cpu.D); return 8;
                case 0x9B: cpu.E = RES(3, cpu.E); return 8;
                case 0x9C: cpu.H = RES(3, cpu.H); return 8;
                case 0x9D: cpu.L = RES(3, cpu.L); return 8;
                case 0x9E: 
                    {
                        byte value = mmu.ReadByte(cpu.HL);
                        value = RES(3, value);
                        mmu.WriteByte(cpu.HL, value);
                        return 16;
                    }
                case 0x9F: cpu.A = RES(3, cpu.A); return 8;

                // RES 4 instructions (0xA0-0xA7)
                case 0xA0: cpu.B = RES(4, cpu.B); return 8;
                case 0xA1: cpu.C = RES(4, cpu.C); return 8;
                case 0xA2: cpu.D = RES(4, cpu.D); return 8;
                case 0xA3: cpu.E = RES(4, cpu.E); return 8;
                case 0xA4: cpu.H = RES(4, cpu.H); return 8;
                case 0xA5: cpu.L = RES(4, cpu.L); return 8;
                case 0xA6: 
                    {
                        byte value = mmu.ReadByte(cpu.HL);
                        value = RES(4, value);
                        mmu.WriteByte(cpu.HL, value);
                        return 16;
                    }
                case 0xA7: cpu.A = RES(4, cpu.A); return 8;

                // RES 5 instructions (0xA8-0xAF)
                case 0xA8: cpu.B = RES(5, cpu.B); return 8;
                case 0xA9: cpu.C = RES(5, cpu.C); return 8;
                case 0xAA: cpu.D = RES(5, cpu.D); return 8;
                case 0xAB: cpu.E = RES(5, cpu.E); return 8;
                case 0xAC: cpu.H = RES(5, cpu.H); return 8;
                case 0xAD: cpu.L = RES(5, cpu.L); return 8;
                case 0xAE: 
                    {
                        byte value = mmu.ReadByte(cpu.HL);
                        value = RES(5, value);
                        mmu.WriteByte(cpu.HL, value);
                        return 16;
                    }
                case 0xAF: cpu.A = RES(5, cpu.A); return 8;

                // RES 6 instructions (0xB0-0xB7)
                case 0xB0: cpu.B = RES(6, cpu.B); return 8;
                case 0xB1: cpu.C = RES(6, cpu.C); return 8;
                case 0xB2: cpu.D = RES(6, cpu.D); return 8;
                case 0xB3: cpu.E = RES(6, cpu.E); return 8;
                case 0xB4: cpu.H = RES(6, cpu.H); return 8;
                case 0xB5: cpu.L = RES(6, cpu.L); return 8;
                case 0xB6: 
                    {
                        byte value = mmu.ReadByte(cpu.HL);
                        value = RES(6, value);
                        mmu.WriteByte(cpu.HL, value);
                        return 16;
                    }
                case 0xB7: cpu.A = RES(6, cpu.A); return 8;

                // RES 7 instructions (0xB8-0xBF)
                case 0xB8: cpu.B = RES(7, cpu.B); return 8;
                case 0xB9: cpu.C = RES(7, cpu.C); return 8;
                case 0xBA: cpu.D = RES(7, cpu.D); return 8;
                case 0xBB: cpu.E = RES(7, cpu.E); return 8;
                case 0xBC: cpu.H = RES(7, cpu.H); return 8;
                case 0xBD: cpu.L = RES(7, cpu.L); return 8;
                case 0xBE: 
                    {
                        byte value = mmu.ReadByte(cpu.HL);
                        value = RES(7, value);
                        mmu.WriteByte(cpu.HL, value);
                        return 16;
                    }
                case 0xBF: cpu.A = RES(7, cpu.A); return 8;

                // SET 0 instructions (0xC0-0xC7)
                case 0xC0: cpu.B = SET(0, cpu.B); return 8;
                case 0xC1: cpu.C = SET(0, cpu.C); return 8;
                case 0xC2: cpu.D = SET(0, cpu.D); return 8;
                case 0xC3: cpu.E = SET(0, cpu.E); return 8;
                case 0xC4: cpu.H = SET(0, cpu.H); return 8;
                case 0xC5: cpu.L = SET(0, cpu.L); return 8;
                case 0xC6: 
                    {
                        byte value = mmu.ReadByte(cpu.HL);
                        value = SET(0, value);
                        mmu.WriteByte(cpu.HL, value);
                        return 16;
                    }
                case 0xC7: cpu.A = SET(0, cpu.A); return 8;

                // SET 1 instructions (0xC8-0xCF)
                case 0xC8: cpu.B = SET(1, cpu.B); return 8;
                case 0xC9: cpu.C = SET(1, cpu.C); return 8;
                case 0xCA: cpu.D = SET(1, cpu.D); return 8;
                case 0xCB: cpu.E = SET(1, cpu.E); return 8;
                case 0xCC: cpu.H = SET(1, cpu.H); return 8;
                case 0xCD: cpu.L = SET(1, cpu.L); return 8;
                case 0xCE: 
                    {
                        byte value = mmu.ReadByte(cpu.HL);
                        value = SET(1, value);
                        mmu.WriteByte(cpu.HL, value);
                        return 16;
                    }
                case 0xCF: cpu.A = SET(1, cpu.A); return 8;

                // SET 2 instructions (0xD0-0xD7)
                case 0xD0: cpu.B = SET(2, cpu.B); return 8;
                case 0xD1: cpu.C = SET(2, cpu.C); return 8;
                case 0xD2: cpu.D = SET(2, cpu.D); return 8;
                case 0xD3: cpu.E = SET(2, cpu.E); return 8;
                case 0xD4: cpu.H = SET(2, cpu.H); return 8;
                case 0xD5: cpu.L = SET(2, cpu.L); return 8;
                case 0xD6: 
                    {
                        byte value = mmu.ReadByte(cpu.HL);
                        value = SET(2, value);
                        mmu.WriteByte(cpu.HL, value);
                        return 16;
                    }
                case 0xD7: cpu.A = SET(2, cpu.A); return 8;

                // SET 3 instructions (0xD8-0xDF)
                case 0xD8: cpu.B = SET(3, cpu.B); return 8;
                case 0xD9: cpu.C = SET(3, cpu.C); return 8;
                case 0xDA: cpu.D = SET(3, cpu.D); return 8;
                case 0xDB: cpu.E = SET(3, cpu.E); return 8;
                case 0xDC: cpu.H = SET(3, cpu.H); return 8;
                case 0xDD: cpu.L = SET(3, cpu.L); return 8;
                case 0xDE: 
                    {
                        byte value = mmu.ReadByte(cpu.HL);
                        value = SET(3, value);
                        mmu.WriteByte(cpu.HL, value);
                        return 16;
                    }
                case 0xDF: cpu.A = SET(3, cpu.A); return 8;

                // SET 4 instructions (0xE0-0xE7)
                case 0xE0: cpu.B = SET(4, cpu.B); return 8;
                case 0xE1: cpu.C = SET(4, cpu.C); return 8;
                case 0xE2: cpu.D = SET(4, cpu.D); return 8;
                case 0xE3: cpu.E = SET(4, cpu.E); return 8;
                case 0xE4: cpu.H = SET(4, cpu.H); return 8;
                case 0xE5: cpu.L = SET(4, cpu.L); return 8;
                case 0xE6: 
                    {
                        byte value = mmu.ReadByte(cpu.HL);
                        value = SET(4, value);
                        mmu.WriteByte(cpu.HL, value);
                        return 16;
                    }
                case 0xE7: cpu.A = SET(4, cpu.A); return 8;

                // SET 5 instructions (0xE8-0xEF)
                case 0xE8: cpu.B = SET(5, cpu.B); return 8;
                case 0xE9: cpu.C = SET(5, cpu.C); return 8;
                case 0xEA: cpu.D = SET(5, cpu.D); return 8;
                case 0xEB: cpu.E = SET(5, cpu.E); return 8;
                case 0xEC: cpu.H = SET(5, cpu.H); return 8;
                case 0xED: cpu.L = SET(5, cpu.L); return 8;
                case 0xEE: 
                    {
                        byte value = mmu.ReadByte(cpu.HL);
                        value = SET(5, value);
                        mmu.WriteByte(cpu.HL, value);
                        return 16;
                    }
                case 0xEF: cpu.A = SET(5, cpu.A); return 8;

                // SET 6 instructions (0xF0-0xF7)
                case 0xF0: cpu.B = SET(6, cpu.B); return 8;
                case 0xF1: cpu.C = SET(6, cpu.C); return 8;
                case 0xF2: cpu.D = SET(6, cpu.D); return 8;
                case 0xF3: cpu.E = SET(6, cpu.E); return 8;
                case 0xF4: cpu.H = SET(6, cpu.H); return 8;
                case 0xF5: cpu.L = SET(6, cpu.L); return 8;
                case 0xF6: 
                    {
                        byte value = mmu.ReadByte(cpu.HL);
                        value = SET(6, value);
                        mmu.WriteByte(cpu.HL, value);
                        return 16;
                    }
                case 0xF7: cpu.A = SET(6, cpu.A); return 8;

                // SET 7 instructions (0xF8-0xFF)
                case 0xF8: cpu.B = SET(7, cpu.B); return 8;
                case 0xF9: cpu.C = SET(7, cpu.C); return 8;
                case 0xFA: cpu.D = SET(7, cpu.D); return 8;
                case 0xFB: cpu.E = SET(7, cpu.E); return 8;
                case 0xFC: cpu.H = SET(7, cpu.H); return 8;
                case 0xFD: cpu.L = SET(7, cpu.L); return 8;
                case 0xFE: 
                    {
                        byte value = mmu.ReadByte(cpu.HL);
                        value = SET(7, value);
                        mmu.WriteByte(cpu.HL, value);
                        return 16;
                    }
                case 0xFF: cpu.A = SET(7, cpu.A); return 8;

                default:
                    cpu.LogCallback?.Invoke($"Warning: Unimplemented CB opcode 0x{suffix:X2} at PC 0x{cpu.PC - 2:X4}");
                    return 8;
            }
        }
        
        // Additional bit operation helpers
        private static byte RRC(CPU cpu, byte value)
        {
            bool bit0 = (value & 0x01) != 0;
            value = (byte)((value >> 1) | (bit0 ? 0x80 : 0));
            cpu.FlagZ = value == 0;
            cpu.FlagN = false;
            cpu.FlagH = false;
            cpu.FlagC = bit0;
            return value;
        }
        
        private static byte RL(CPU cpu, byte value)
        {
            bool bit7 = (value & 0x80) != 0;
            value = (byte)((value << 1) | (cpu.FlagC ? 1 : 0));
            cpu.FlagZ = value == 0;
            cpu.FlagN = false;
            cpu.FlagH = false;
            cpu.FlagC = bit7;
            return value;
        }
        
        private static byte RR(CPU cpu, byte value)
        {
            bool bit0 = (value & 0x01) != 0;
            value = (byte)((value >> 1) | (cpu.FlagC ? 0x80 : 0));
            cpu.FlagZ = value == 0;
            cpu.FlagN = false;
            cpu.FlagH = false;
            cpu.FlagC = bit0;
            return value;
        }
        
        private static byte SLA(CPU cpu, byte value)
        {
            bool bit7 = (value & 0x80) != 0;
            value = (byte)(value << 1);
            cpu.FlagZ = value == 0;
            cpu.FlagN = false;
            cpu.FlagH = false;
            cpu.FlagC = bit7;
            return value;
        }
        
        private static byte SRA(CPU cpu, byte value)
        {
            bool bit0 = (value & 0x01) != 0;
            bool bit7 = (value & 0x80) != 0;
            value = (byte)((value >> 1) | (bit7 ? 0x80 : 0));
            cpu.FlagZ = value == 0;
            cpu.FlagN = false;
            cpu.FlagH = false;
            cpu.FlagC = bit0;
            return value;
        }
        
        private static byte SRL(CPU cpu, byte value)
        {
            bool bit0 = (value & 0x01) != 0;
            value = (byte)(value >> 1);
            cpu.FlagZ = value == 0;
            cpu.FlagN = false;
            cpu.FlagH = false;
            cpu.FlagC = bit0;
            return value;
        }
        
        private static byte SWAP(CPU cpu, byte value)
        {
            value = (byte)((value << 4) | (value >> 4));
            cpu.FlagZ = value == 0;
            cpu.FlagN = false;
            cpu.FlagH = false;
            cpu.FlagC = false;
            return value;
        }
        
        private static void BIT(CPU cpu, int bit, byte value)
        {
            bool bitSet = (value & (1 << bit)) != 0;
            cpu.FlagZ = !bitSet;
            cpu.FlagN = false;
            cpu.FlagH = true;
        }
        
        private static byte RES(int bit, byte value)
        {
            return (byte)(value & ~(1 << bit));
        }
        
        private static byte SET(int bit, byte value)
        {
            return (byte)(value | (1 << bit));
        }

        // Additional helper methods for rotate instructions
        private static byte RLCA(CPU cpu, byte value)
        {
            bool bit7 = (value & 0x80) != 0;
            value = (byte)((value << 1) | (bit7 ? 1 : 0));
            cpu.FlagZ = false; // RLCA clears Z flag
            cpu.FlagN = false;
            cpu.FlagH = false;
            cpu.FlagC = bit7;
            return value;
        }

        private static byte RRCA(CPU cpu, byte value)
        {
            bool bit0 = (value & 0x01) != 0;
            value = (byte)((value >> 1) | (bit0 ? 0x80 : 0));
            cpu.FlagZ = false; // RRCA clears Z flag
            cpu.FlagN = false;
            cpu.FlagH = false;
            cpu.FlagC = bit0;
            return value;
        }

        private static byte RLA(CPU cpu, byte value)
        {
            bool bit7 = (value & 0x80) != 0;
            value = (byte)((value << 1) | (cpu.FlagC ? 1 : 0));
            cpu.FlagZ = false; // RLA clears Z flag
            cpu.FlagN = false;
            cpu.FlagH = false;
            cpu.FlagC = bit7;
            return value;
        }

        private static byte RRA(CPU cpu, byte value)
        {
            bool bit0 = (value & 0x01) != 0;
            value = (byte)((value >> 1) | (cpu.FlagC ? 0x80 : 0));
            cpu.FlagZ = false; // RRA clears Z flag
            cpu.FlagN = false;
            cpu.FlagH = false;
            cpu.FlagC = bit0;
            return value;
        }

        private static void DAA(CPU cpu)
        {
            byte adjustment = 0;
            bool carry = false;

            if (cpu.FlagN) // Subtraction
            {
                if (cpu.FlagC) adjustment |= 0x60;
                if (cpu.FlagH) adjustment |= 0x06;
                cpu.A -= adjustment;
            }
            else // Addition
            {
                if (cpu.FlagC || cpu.A > 0x99)
                {
                    adjustment |= 0x60;
                    carry = true;
                }
                if (cpu.FlagH || (cpu.A & 0x0F) > 0x09)
                {
                    adjustment |= 0x06;
                }
                cpu.A += adjustment;
            }

            cpu.FlagZ = cpu.A == 0;
            cpu.FlagH = false;
            cpu.FlagC = carry;
        }

        private static byte ADC8(CPU cpu, byte a, byte b)
        {
            int carry = cpu.FlagC ? 1 : 0;
            int result = a + b + carry;
            cpu.FlagZ = (result & 0xFF) == 0;
            cpu.FlagN = false;
            cpu.FlagH = ((a ^ b ^ result) & 0x10) != 0;
            cpu.FlagC = result > 0xFF;
            return (byte)result;
        }

        private static byte And8(CPU cpu, byte a, byte b)
        {
            byte result = (byte)(a & b);
            cpu.FlagZ = result == 0;
            cpu.FlagN = false;
            cpu.FlagH = true;
            cpu.FlagC = false;
            return result;
        }

        private static byte Or8(CPU cpu, byte a, byte b)
        {
            byte result = (byte)(a | b);
            cpu.FlagZ = result == 0;
            cpu.FlagN = false;
            cpu.FlagH = false;
            cpu.FlagC = false;
            return result;
        }
    }
} 