using System;

namespace GameBoyEmulator.CPU
{
    public class CPU
    {
        // 8-bit registers
        public byte A { get; set; }
        public byte B { get; set; }
        public byte C { get; set; }
        public byte D { get; set; }
        public byte E { get; set; }
        public byte H { get; set; }
        public byte L { get; set; }
        public byte F { get; set; } // Flags register
        
        // 16-bit registers
        public ushort PC { get; set; } // Program Counter
        public ushort SP { get; set; } // Stack Pointer
        
        // Flag bits in F register (bits 7-4 only, bits 3-0 always 0)
        public bool FlagZ
        {
            get => (F & 0x80) != 0;
            set => F = (byte)(value ? (F | 0x80) : (F & 0x7F));
        }
        
        public bool FlagN
        {
            get => (F & 0x40) != 0;
            set => F = (byte)(value ? (F | 0x40) : (F & 0xBF));
        }
        
        public bool FlagH
        {
            get => (F & 0x20) != 0;
            set => F = (byte)(value ? (F | 0x20) : (F & 0xDF));
        }
        
        public bool FlagC
        {
            get => (F & 0x10) != 0;
            set => F = (byte)(value ? (F | 0x10) : (F & 0xEF));
        }
        
        // 16-bit register pairs
        public ushort AF
        {
            get => (ushort)((A << 8) | F);
            set { A = (byte)(value >> 8); F = (byte)(value & 0xF0); }
        }
        
        public ushort BC
        {
            get => (ushort)((B << 8) | C);
            set { B = (byte)(value >> 8); C = (byte)(value & 0xFF); }
        }
        
        public ushort DE
        {
            get => (ushort)((D << 8) | E);
            set { D = (byte)(value >> 8); E = (byte)(value & 0xFF); }
        }
        
        public ushort HL
        {
            get => (ushort)((H << 8) | L);
            set { H = (byte)(value >> 8); L = (byte)(value & 0xFF); }
        }
        
        // CPU state
        public bool IME { get; set; } // Interrupt Master Enable
        public bool Halted { get; set; }
        public bool Stopped { get; set; }
        
        private readonly MMU.MMU mmu;
        public Action<string>? LogCallback { get; set; }
        public bool EnableInstructionLogging { get; set; } = false;
        
        public CPU(MMU.MMU mmu)
        {
            this.mmu = mmu;
            Reset();
        }
        
        public void Reset()
        {
            // Initial state after boot ROM execution
            A = 0x01;
            F = 0xB0;
            B = 0x00;
            C = 0x13;
            D = 0x00;
            E = 0xD8;
            H = 0x01;
            L = 0x4D;
            PC = 0x0100;
            SP = 0xFFFE;
            
            IME = false;
            Halted = false;
            Stopped = false;
        }
        
        public int Step()
        {
            if (Stopped) return 4;
            
            // Handle interrupts
            if (IME && !Halted)
            {
                byte interrupts = (byte)(mmu.ReadByte(0xFF0F) & mmu.ReadByte(0xFFFF));
                if (interrupts != 0)
                {
                    HandleInterrupt(interrupts);
                    return 20;
                }
            }
            
            if (Halted)
            {
                byte interrupts = (byte)(mmu.ReadByte(0xFF0F) & mmu.ReadByte(0xFFFF));
                if (interrupts != 0)
                {
                    Halted = false;
                    if (!IME) return 4; // Halt bug
                }
                else
                {
                    return 4; // NOP cycle while halted
                }
            }
            
            byte opcode = mmu.ReadByte(PC++);
            return ExecuteInstruction(opcode);
        }
        
        private void HandleInterrupt(byte interrupts)
        {
            IME = false;
            Push(PC);
            
            if ((interrupts & 0x01) != 0) // V-Blank
            {
                mmu.WriteByte(0xFF0F, (byte)(mmu.ReadByte(0xFF0F) & 0xFE));
                PC = 0x0040;
            }
            else if ((interrupts & 0x02) != 0) // LCD STAT  
            {
                mmu.WriteByte(0xFF0F, (byte)(mmu.ReadByte(0xFF0F) & 0xFD));
                PC = 0x0048;
            }
            else if ((interrupts & 0x04) != 0) // Timer
            {
                mmu.WriteByte(0xFF0F, (byte)(mmu.ReadByte(0xFF0F) & 0xFB));
                PC = 0x0050;
            }
            else if ((interrupts & 0x08) != 0) // Serial
            {
                mmu.WriteByte(0xFF0F, (byte)(mmu.ReadByte(0xFF0F) & 0xF7));
                PC = 0x0058;
            }
            else if ((interrupts & 0x10) != 0) // Joypad
            {
                mmu.WriteByte(0xFF0F, (byte)(mmu.ReadByte(0xFF0F) & 0xEF));
                PC = 0x0060;
            }
        }
        
        private int ExecuteInstruction(byte opcode)
        {
            try
            {
                // Debug output for execution - show more info
                if (PC >= 0xFF00 && PC < 0xFF80)
                {
                    LogCallback?.Invoke($"WARNING: Executing from I/O registers: PC=0x{PC-1:X4} Opcode=0x{opcode:X2}");
                }
                else if (PC == 0xFFFF)
                {
                    LogCallback?.Invoke($"WARNING: Executing from Interrupt Enable register: PC=0x{PC-1:X4} Opcode=0x{opcode:X2}");
                }
                else if (EnableInstructionLogging && (PC < 0x0110 || (PC >= 0x0150 && PC < 0x0160)))
                {
                    LogCallback?.Invoke($"Executing: PC=0x{PC-1:X4} Opcode=0x{opcode:X2}");
                }
                
                return InstructionSet.Execute(this, mmu, opcode);
            }
            catch (Exception ex)
            {
                LogCallback?.Invoke($"CRASH: PC=0x{PC-1:X4} Opcode=0x{opcode:X2} Error: {ex.Message}");
                throw;
            }
        }
        
        // Stack operations
        public void Push(ushort value)
        {
            mmu.WriteByte(--SP, (byte)(value >> 8));
            mmu.WriteByte(--SP, (byte)(value & 0xFF));
        }
        
        public ushort Pop()
        {
            byte low = mmu.ReadByte(SP++);
            byte high = mmu.ReadByte(SP++);
            return (ushort)((high << 8) | low);
        }
        
        // Memory access helpers
        public byte ReadByte() => mmu.ReadByte(PC++);
        public ushort ReadWord() => (ushort)(ReadByte() | (ReadByte() << 8));
    }
} 