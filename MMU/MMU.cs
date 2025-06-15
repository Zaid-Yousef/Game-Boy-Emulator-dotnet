using System;

namespace GameBoyEmulator.MMU
{
    public class MMU
    {
        // Memory regions
        private byte[] rom = new byte[0x8000];     // 0x0000-0x7FFF: Cartridge ROM
        private byte[] vram = new byte[0x2000];    // 0x8000-0x9FFF: Video RAM
        private byte[] eram = new byte[0x2000];    // 0xA000-0xBFFF: External RAM
        private byte[] wram = new byte[0x2000];    // 0xC000-0xDFFF: Work RAM
        private byte[] oam = new byte[0xA0];       // 0xFE00-0xFE9F: Object Attribute Memory
        private byte[] hram = new byte[0x7F];      // 0xFF80-0xFFFE: High RAM
        private byte[] ioRegs = new byte[0x80];    // 0xFF00-0xFF7F: I/O Registers
        
        private byte ie = 0x00;  // 0xFFFF: Interrupt Enable register
        
        // Components
        private GPU.GPU? gpu;
        private APU.APU? apu;
        private Input.Joypad? joypad;
        private Timer.Timer? timer;
        private Cartridge.Cartridge? cartridge;
        
        public MMU()
        {
            Reset();
        }
        
        public void Reset()
        {
            Array.Clear(vram, 0, vram.Length);
            Array.Clear(eram, 0, eram.Length);
            Array.Clear(wram, 0, wram.Length);
            Array.Clear(oam, 0, oam.Length);
            Array.Clear(hram, 0, hram.Length);
            Array.Clear(ioRegs, 0, ioRegs.Length);
            
            // Initialize I/O registers to default values
            ioRegs[0x10] = 0x80; // NR10
            ioRegs[0x11] = 0xBF; // NR11
            ioRegs[0x12] = 0xF3; // NR12
            ioRegs[0x14] = 0xBF; // NR14
            ioRegs[0x16] = 0x3F; // NR21
            ioRegs[0x17] = 0x00; // NR22
            ioRegs[0x19] = 0xBF; // NR24
            ioRegs[0x1A] = 0x7F; // NR30
            ioRegs[0x1B] = 0xFF; // NR31
            ioRegs[0x1C] = 0x9F; // NR32
            ioRegs[0x1E] = 0xBF; // NR34
            ioRegs[0x20] = 0xFF; // NR41
            ioRegs[0x21] = 0x00; // NR42
            ioRegs[0x22] = 0x00; // NR43
            ioRegs[0x23] = 0xBF; // NR44
            ioRegs[0x24] = 0x77; // NR50
            ioRegs[0x25] = 0xF3; // NR51
            ioRegs[0x26] = 0xF1; // NR52
            ioRegs[0x40] = 0x91; // LCDC
            ioRegs[0x42] = 0x00; // SCY
            ioRegs[0x43] = 0x00; // SCX
            ioRegs[0x45] = 0x00; // LYC
            ioRegs[0x47] = 0xFC; // BGP
            ioRegs[0x48] = 0xFF; // OBP0
            ioRegs[0x49] = 0xFF; // OBP1
            ioRegs[0x4A] = 0x00; // WY
            ioRegs[0x4B] = 0x00; // WX
            
            // Synchronize GPU registers with MMU defaults
            if (gpu != null)
            {
                gpu.WriteRegister(0x40, ioRegs[0x40]); // LCDC
                gpu.WriteRegister(0x41, ioRegs[0x41]); // STAT
                gpu.WriteRegister(0x42, ioRegs[0x42]); // SCY
                gpu.WriteRegister(0x43, ioRegs[0x43]); // SCX
                gpu.WriteRegister(0x45, ioRegs[0x45]); // LYC
                gpu.WriteRegister(0x47, ioRegs[0x47]); // BGP
                gpu.WriteRegister(0x48, ioRegs[0x48]); // OBP0
                gpu.WriteRegister(0x49, ioRegs[0x49]); // OBP1
                gpu.WriteRegister(0x4A, ioRegs[0x4A]); // WY
                gpu.WriteRegister(0x4B, ioRegs[0x4B]); // WX
            }
            
            ie = 0x00;
        }
        
        public void ConnectComponents(GPU.GPU gpu, APU.APU apu, Input.Joypad joypad, Timer.Timer timer)
        {
            this.gpu = gpu;
            this.apu = apu;
            this.joypad = joypad;
            this.timer = timer;
        }
        
        public void LoadCartridge(Cartridge.Cartridge cartridge)
        {
            this.cartridge = cartridge;
        }
        
        public byte ReadByte(ushort address)
        {
            switch (address & 0xF000)
            {
                case 0x0000:
                case 0x1000:
                case 0x2000:
                case 0x3000:
                case 0x4000:
                case 0x5000:
                case 0x6000:
                case 0x7000:
                    // ROM Bank 0 and 1
                    return cartridge?.ReadByte(address) ?? 0xFF;
                
                case 0x8000:
                case 0x9000:
                    // VRAM
                    return vram[address - 0x8000];
                
                case 0xA000:
                case 0xB000:
                    // External RAM
                    return cartridge?.ReadRam(address) ?? 0xFF;
                
                case 0xC000:
                case 0xD000:
                    // Work RAM
                    return wram[address - 0xC000];
                
                case 0xE000:
                    // Echo of Work RAM
                    return wram[address - 0xE000];
                
                case 0xF000:
                    if (address < 0xFE00)
                    {
                        // Echo of Work RAM
                        return wram[address - 0xE000];
                    }
                    else if (address < 0xFEA0)
                    {
                        // OAM
                        return oam[address - 0xFE00];
                    }
                    else if (address < 0xFF00)
                    {
                        // Unusable area
                        return 0xFF;
                    }
                    else if (address < 0xFF80)
                    {
                        // I/O Registers
                        return ReadIORegister((byte)(address - 0xFF00));
                    }
                    else if (address < 0xFFFF)
                    {
                        // High RAM
                        return hram[address - 0xFF80];
                    }
                    else
                    {
                        // Interrupt Enable register
                        return ie;
                    }
                    break;
            }
            
            return 0xFF;
        }
        
        public void WriteByte(ushort address, byte value)
        {
            switch (address & 0xF000)
            {
                case 0x0000:
                case 0x1000:
                case 0x2000:
                case 0x3000:
                case 0x4000:
                case 0x5000:
                case 0x6000:
                case 0x7000:
                    // ROM - Write to cartridge (for MBC control)
                    cartridge?.WriteByte(address, value);
                    break;
                
                case 0x8000:
                case 0x9000:
                    // VRAM
                    vram[address - 0x8000] = value;
                    break;
                
                case 0xA000:
                case 0xB000:
                    // External RAM
                    cartridge?.WriteRam(address, value);
                    break;
                
                case 0xC000:
                case 0xD000:
                    // Work RAM
                    wram[address - 0xC000] = value;
                    break;
                
                case 0xE000:
                    // Echo of Work RAM
                    wram[address - 0xE000] = value;
                    break;
                
                case 0xF000:
                    if (address < 0xFE00)
                    {
                        // Echo of Work RAM
                        wram[address - 0xE000] = value;
                    }
                    else if (address < 0xFEA0)
                    {
                        // OAM
                        oam[address - 0xFE00] = value;
                    }
                    else if (address < 0xFF00)
                    {
                        // Unusable area - ignore writes
                    }
                    else if (address < 0xFF80)
                    {
                        // I/O Registers
                        WriteIORegister((byte)(address - 0xFF00), value);
                    }
                    else if (address < 0xFFFF)
                    {
                        // High RAM
                        hram[address - 0xFF80] = value;
                    }
                    else
                    {
                        // Interrupt Enable register
                        ie = value;
                    }
                    break;
            }
        }
        
        public ushort ReadWord(ushort address)
        {
            return (ushort)(ReadByte(address) | (ReadByte((ushort)(address + 1)) << 8));
        }
        
        public void WriteWord(ushort address, ushort value)
        {
            WriteByte(address, (byte)(value & 0xFF));
            WriteByte((ushort)(address + 1), (byte)(value >> 8));
        }
        
        private byte ReadIORegister(byte register)
        {
            switch (register)
            {
                case 0x00: // JOYP
                    return joypad?.ReadJoypad() ?? 0xFF;
                case 0x04: // DIV
                case 0x05: // TIMA
                case 0x06: // TMA
                case 0x07: // TAC
                    return timer?.ReadRegister(register) ?? ioRegs[register];
                case 0x44: // LY
                    return gpu?.GetLY() ?? 0;
                case 0x41: // STAT
                    return gpu?.GetSTAT() ?? ioRegs[register];
                default:
                    return ioRegs[register];
            }
        }
        
        private void WriteIORegister(byte register, byte value)
        {
            switch (register)
            {
                case 0x00: // JOYP
                    joypad?.WriteJoypad(value);
                    break;
                case 0x04: // DIV
                case 0x05: // TIMA
                case 0x06: // TMA
                case 0x07: // TAC
                    timer?.WriteRegister(register, value);
                    break;
                case 0x44: // LY
                    // LY is read-only, ignore writes
                    break;
                case 0x46: // DMA
                    PerformDMATransfer(value);
                    break;
                default:
                    ioRegs[register] = value;
                    gpu?.WriteRegister(register, value);
                    apu?.WriteRegister(register, value);
                    break;
            }
        }
        
        private void PerformDMATransfer(byte value)
        {
            ushort sourceBase = (ushort)(value << 8);
            for (int i = 0; i < 0xA0; i++)
            {
                oam[i] = ReadByte((ushort)(sourceBase + i));
            }
        }
        
        // Direct access methods for components
        public byte[] GetVRAM() => vram;
        public byte[] GetOAM() => oam;
        public byte ReadIORegisterDirect(byte register) => ioRegs[register];
    }
} 