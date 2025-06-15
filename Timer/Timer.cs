using System;

namespace GameBoyEmulator.Timer
{
    public class Timer
    {
        // Timer registers
        private ushort divider = 0xABCC; // DIV register (upper 8 bits)
        private byte tima = 0x00;        // Timer counter
        private byte tma = 0x00;         // Timer modulo
        private byte tac = 0x00;         // Timer control
        
        private MMU.MMU? mmu;
        
        public void ConnectMMU(MMU.MMU mmu)
        {
            this.mmu = mmu;
        }
        
        public void Step(int cycles)
        {
            // Update divider (always counting)
            ushort oldDivider = divider;
            divider = (ushort)(divider + cycles);
            
            // Check if timer is enabled
            if ((tac & 0x04) != 0)
            {
                // Get timer frequency
                int frequency = GetTimerFrequency();
                
                // Check if we should increment TIMA
                bool oldBit = GetFrequencyBit(oldDivider, frequency);
                bool newBit = GetFrequencyBit(divider, frequency);
                
                // Falling edge detection
                if (oldBit && !newBit)
                {
                    tima++;
                    
                    // Check for overflow
                    if (tima == 0)
                    {
                        tima = tma; // Reset to modulo value
                        RequestTimerInterrupt();
                    }
                }
            }
        }
        
        private int GetTimerFrequency()
        {
            return (tac & 0x03) switch
            {
                0 => 1024,  // 4096 Hz
                1 => 16,    // 262144 Hz
                2 => 64,    // 65536 Hz
                3 => 256,   // 16384 Hz
                _ => 1024
            };
        }
        
        private bool GetFrequencyBit(ushort dividerValue, int frequency)
        {
            return frequency switch
            {
                1024 => (dividerValue & (1 << 9)) != 0,  // Bit 9
                16 => (dividerValue & (1 << 3)) != 0,    // Bit 3
                64 => (dividerValue & (1 << 5)) != 0,    // Bit 5
                256 => (dividerValue & (1 << 7)) != 0,   // Bit 7
                _ => false
            };
        }
        
        private void RequestTimerInterrupt()
        {
            if (mmu != null)
            {
                byte ifReg = mmu.ReadByte(0xFF0F);
                mmu.WriteByte(0xFF0F, (byte)(ifReg | 0x04));
            }
        }
        
        public byte ReadRegister(byte register)
        {
            return register switch
            {
                0x04 => (byte)(divider >> 8), // DIV
                0x05 => tima,                 // TIMA
                0x06 => tma,                  // TMA
                0x07 => (byte)(tac | 0xF8),  // TAC (upper 5 bits always set)
                _ => 0xFF
            };
        }
        
        public void WriteRegister(byte register, byte value)
        {
            switch (register)
            {
                case 0x04: // DIV
                    divider = 0; // Writing any value resets DIV to 0
                    break;
                case 0x05: // TIMA
                    tima = value;
                    break;
                case 0x06: // TMA
                    tma = value;
                    break;
                case 0x07: // TAC
                    tac = (byte)(value & 0x07); // Only lower 3 bits are writable
                    break;
            }
        }
    }
} 