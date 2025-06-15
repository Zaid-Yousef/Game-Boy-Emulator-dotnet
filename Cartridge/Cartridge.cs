using System;
using System.IO;

namespace GameBoyEmulator.Cartridge
{
    public class Cartridge
    {
        private byte[] rom = Array.Empty<byte>();
        private byte[] ram = Array.Empty<byte>();
        
        // Cartridge info
        private string title = "";
        private byte cartridgeType = 0;
        private byte romSize = 0;
        private byte ramSize = 0;
        
        // MBC state
        private bool ramEnabled = false;
        private int romBankNumber = 1;
        private int ramBankNumber = 0;
        private bool bankingMode = false; // false = ROM banking, true = RAM banking
        
        public bool LoadROM(string filePath)
        {
            try
            {
                rom = File.ReadAllBytes(filePath);
                
                // Read cartridge header
                ReadHeader();
                
                // Initialize RAM if needed
                InitializeRAM();
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading ROM: {ex.Message}");
                return false;
            }
        }
        
        private void ReadHeader()
        {
            if (rom.Length < 0x150) return;
            
            // Read title (0x134-0x143)
            title = System.Text.Encoding.ASCII.GetString(rom, 0x134, 16).TrimEnd('\0');
            
            // Read cartridge type (0x147)
            cartridgeType = rom[0x147];
            
            // Read ROM size (0x148)
            romSize = rom[0x148];
            
            // Read RAM size (0x149)
            ramSize = rom[0x149];
            
            Console.WriteLine($"Loaded ROM: {title}");
            Console.WriteLine($"Cartridge Type: 0x{cartridgeType:X2}");
            Console.WriteLine($"ROM Size: {GetROMSizeKB()}KB");
            Console.WriteLine($"RAM Size: {GetRAMSizeKB()}KB");
        }
        
        private void InitializeRAM()
        {
            int ramSizeBytes = GetRAMSizeBytes();
            if (ramSizeBytes > 0)
            {
                ram = new byte[ramSizeBytes];
            }
        }
        
        private int GetROMSizeKB()
        {
            return romSize switch
            {
                0x00 => 32,   // 2 banks
                0x01 => 64,   // 4 banks
                0x02 => 128,  // 8 banks
                0x03 => 256,  // 16 banks
                0x04 => 512,  // 32 banks
                0x05 => 1024, // 64 banks
                0x06 => 2048, // 128 banks
                0x07 => 4096, // 256 banks
                _ => 32
            };
        }
        
        private int GetRAMSizeKB()
        {
            return ramSize switch
            {
                0x00 => 0,    // No RAM
                0x01 => 2,    // 2KB
                0x02 => 8,    // 8KB
                0x03 => 32,   // 32KB (4 banks of 8KB)
                0x04 => 128,  // 128KB (16 banks of 8KB)
                0x05 => 64,   // 64KB (8 banks of 8KB)
                _ => 0
            };
        }
        
        private int GetRAMSizeBytes() => GetRAMSizeKB() * 1024;
        
        public byte ReadByte(ushort address)
        {
            if (address < 0x4000)
            {
                // ROM Bank 0
                return address < rom.Length ? rom[address] : (byte)0xFF;
            }
            else if (address < 0x8000)
            {
                // ROM Bank 1-N (switchable)
                int realAddress = GetROMBankAddress(address);
                return realAddress < rom.Length ? rom[realAddress] : (byte)0xFF;
            }
            
            return 0xFF;
        }
        
        public byte ReadRam(ushort address)
        {
            if (!ramEnabled || ram.Length == 0) return 0xFF;
            
            int ramAddress = GetRAMAddress(address);
            return ramAddress < ram.Length ? ram[ramAddress] : (byte)0xFF;
        }
        
        public void WriteByte(ushort address, byte value)
        {
            // Handle MBC control writes
            switch (cartridgeType)
            {
                case 0x00: // ROM ONLY
                    break;
                    
                case 0x01: // MBC1
                case 0x02: // MBC1+RAM
                case 0x03: // MBC1+RAM+BATTERY
                    WriteMBC1(address, value);
                    break;
                    
                case 0x05: // MBC2
                case 0x06: // MBC2+BATTERY
                    WriteMBC2(address, value);
                    break;
                    
                case 0x0F: // MBC3+TIMER+BATTERY
                case 0x10: // MBC3+TIMER+RAM+BATTERY
                case 0x11: // MBC3
                case 0x12: // MBC3+RAM
                case 0x13: // MBC3+RAM+BATTERY
                    WriteMBC3(address, value);
                    break;
                    
                default:
                    // Unknown MBC, ignore writes
                    break;
            }
        }
        
        public void WriteRam(ushort address, byte value)
        {
            if (!ramEnabled || ram.Length == 0) return;
            
            int ramAddress = GetRAMAddress(address);
            if (ramAddress < ram.Length)
            {
                ram[ramAddress] = value;
            }
        }
        
        private void WriteMBC1(ushort address, byte value)
        {
            if (address < 0x2000)
            {
                // RAM Enable (0x0000-0x1FFF)
                ramEnabled = (value & 0x0F) == 0x0A;
            }
            else if (address < 0x4000)
            {
                // ROM Bank Number (0x2000-0x3FFF)
                int bankNumber = value & 0x1F;
                if (bankNumber == 0) bankNumber = 1; // Bank 0 can't be selected
                romBankNumber = (romBankNumber & 0x60) | bankNumber;
            }
            else if (address < 0x6000)
            {
                // RAM Bank Number / Upper ROM Bank bits (0x4000-0x5FFF)
                if (bankingMode)
                {
                    ramBankNumber = value & 0x03;
                }
                else
                {
                    romBankNumber = (romBankNumber & 0x1F) | ((value & 0x03) << 5);
                }
            }
            else if (address < 0x8000)
            {
                // Banking Mode Select (0x6000-0x7FFF)
                bankingMode = (value & 0x01) != 0;
            }
        }
        
        private void WriteMBC2(ushort address, byte value)
        {
            if (address < 0x4000)
            {
                if ((address & 0x0100) == 0)
                {
                    // RAM Enable
                    ramEnabled = (value & 0x0F) == 0x0A;
                }
                else
                {
                    // ROM Bank Number
                    romBankNumber = value & 0x0F;
                    if (romBankNumber == 0) romBankNumber = 1;
                }
            }
        }
        
        private void WriteMBC3(ushort address, byte value)
        {
            if (address < 0x2000)
            {
                // RAM Enable (0x0000-0x1FFF)
                ramEnabled = (value & 0x0F) == 0x0A;
            }
            else if (address < 0x4000)
            {
                // ROM Bank Number (0x2000-0x3FFF)
                romBankNumber = value & 0x7F;
                if (romBankNumber == 0) romBankNumber = 1;
            }
            else if (address < 0x6000)
            {
                // RAM Bank Number / RTC Register Select (0x4000-0x5FFF)
                if (value <= 0x03)
                {
                    ramBankNumber = value;
                }
                // RTC registers (0x08-0x0C) not implemented
            }
            // Latch Clock Data (0x6000-0x7FFF) not implemented
        }
        
        private int GetROMBankAddress(ushort address)
        {
            int bankOffset = (address - 0x4000);
            return (romBankNumber * 0x4000) + bankOffset;
        }
        
        private int GetRAMAddress(ushort address)
        {
            int ramOffset = address - 0xA000;
            
            // MBC2 has special RAM handling (512 x 4-bit)
            if (cartridgeType == 0x05 || cartridgeType == 0x06)
            {
                return ramOffset & 0x1FF; // Only 512 bytes, 4-bit each
            }
            
            return (ramBankNumber * 0x2000) + ramOffset;
        }
        
        public string GetTitle() => title;
        public byte GetCartridgeType() => cartridgeType;
    }
} 