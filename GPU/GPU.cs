using System;
using System.Drawing;

namespace GameBoyEmulator.GPU
{
    public class GPU
    {
        // LCD Constants
        public const int SCREEN_WIDTH = 160;
        public const int SCREEN_HEIGHT = 144;
        public const int CYCLES_PER_SCANLINE = 456;
        public const int SCANLINES_PER_FRAME = 154;
        public const int VBLANK_SCANLINES = 10;
        
        // GPU Modes
        public const int MODE_HBLANK = 0;
        public const int MODE_VBLANK = 1;
        public const int MODE_OAM_SEARCH = 2;
        public const int MODE_PIXEL_TRANSFER = 3;
        
        // Registers
        private byte lcdc = 0x91; // LCD Control
        private byte stat = 0x85; // LCD Status
        private byte scy = 0x00;  // Scroll Y
        private byte scx = 0x00;  // Scroll X
        private byte ly = 0x00;   // LCD Y Coordinate
        private byte lyc = 0x00;  // LY Compare
        private byte bgp = 0xFC;  // Background Palette
        private byte obp0 = 0xFF; // Object Palette 0
        private byte obp1 = 0xFF; // Object Palette 1
        private byte wy = 0x00;   // Window Y Position
        private byte wx = 0x00;   // Window X Position
        
        // GPU State
        private int cycles = 0;
        private int mode = MODE_OAM_SEARCH;
        
        // Frame buffer
        private uint[] framebuffer = new uint[SCREEN_WIDTH * SCREEN_HEIGHT];
        
        // Palettes (converted to colors)
        private static readonly uint[] DEFAULT_COLORS = { 0xFFFFFFFF, 0xFFAAAAAA, 0xFF555555, 0xFF000000 };
        private static readonly uint[] GREEN_COLORS = { 0xFF9BBB0F, 0xFF8BAC0F, 0xFF306230, 0xFF0F380F };
        private uint[] currentColors = DEFAULT_COLORS;
        private bool useClassicGreenScreen = false;
        
        // MMU reference
        private MMU.MMU? mmu;
        
        // State tracking
        private bool vblankTriggered = false;
        private int windowLineCounter = 0;
        private bool lcdPreviouslyOff = false;
        
        public GPU()
        {
            Reset();
        }
        
        public void ConnectMMU(MMU.MMU mmu)
        {
            this.mmu = mmu;
        }
        
        // Add logging callback
        public Action<string>? LogCallback { get; set; }
        public bool EnableGPULogging { get; set; } = false;
        
        public void Reset()
        {
            lcdc = 0x91;
            stat = 0x85;
            scy = 0x00;
            scx = 0x00;
            ly = 0x00;
            lyc = 0x00;
            bgp = 0xFC;
            obp0 = 0xFF;
            obp1 = 0xFF;
            wy = 0x00;
            wx = 0x00;
            cycles = 0;
            mode = MODE_OAM_SEARCH;
            
            // Initialize framebuffer to black
            Array.Clear(framebuffer, 0, framebuffer.Length);
        }
        
        public void Step(int cpuCycles)
        {
            if (!LCDEnabled())
            {
                if (!lcdPreviouslyOff)
                {
                    // LCD was just turned off
                    ly = 0;
                    SetMode(MODE_HBLANK);
                    cycles = 0;
                    lcdPreviouslyOff = true;
                }
                return;
            }
            
            if (lcdPreviouslyOff)
            {
                // LCD was just turned on
                cycles = 0;
                SetMode(MODE_OAM_SEARCH);
                lcdPreviouslyOff = false;
            }
            
            cycles += cpuCycles;
            
            switch (mode)
            {
                case MODE_OAM_SEARCH:
                    if (cycles >= 80)
                    {
                        cycles -= 80;
                        SetMode(MODE_PIXEL_TRANSFER);
                    }
                    break;
                    
                case MODE_PIXEL_TRANSFER:
                    if (cycles >= 172)
                    {
                        cycles -= 172;
                        SetMode(MODE_HBLANK);
                        
                        // Render current scanline
                        if (ly < SCREEN_HEIGHT)
                        {
                            RenderScanline();
                        }
                        
                        // Check for HBLANK interrupt
                        if ((stat & 0x08) != 0)
                        {
                            RequestLCDInterrupt();
                        }
                    }
                    break;
                    
                case MODE_HBLANK:
                    if (cycles >= 204)
                    {
                        cycles -= 204;
                        ly++;
                        
                        CheckLYC();
                        
                        if (ly == SCREEN_HEIGHT)
                        {
                            // Enter VBlank
                            SetMode(MODE_VBLANK);
                            vblankTriggered = false;
                            
                            // Check for VBlank interrupt
                            if ((stat & 0x10) != 0)
                            {
                                RequestLCDInterrupt();
                            }
                        }
                        else
                        {
                            // Next scanline
                            SetMode(MODE_OAM_SEARCH);
                            
                            // Check for OAM interrupt
                            if ((stat & 0x20) != 0)
                            {
                                RequestLCDInterrupt();
                            }
                        }
                    }
                    break;
                    
                case MODE_VBLANK:
                    // Trigger VBlank interrupt once per frame
                    if (!vblankTriggered && ly == SCREEN_HEIGHT)
                    {
                        RequestVBlankInterrupt();
                        vblankTriggered = true;
                        if (EnableGPULogging)
                        {
                            LogCallback?.Invoke($"GPU: VBlank started at scanline {ly}");
                        }
                    }
                    
                    if (cycles >= CYCLES_PER_SCANLINE)
                    {
                        cycles -= CYCLES_PER_SCANLINE;
                        ly++;
                        
                        CheckLYC();
                        
                        if (ly == 153)
                        {
                            // End of VBlank, start new frame
                            ly = 0;
                            SetMode(MODE_OAM_SEARCH);
                            vblankTriggered = false;
                            windowLineCounter = 0;
                            
                            if (EnableGPULogging)
                            {
                                LogCallback?.Invoke("GPU: VBlank ended, frame complete");
                            }
                            
                            // Check for OAM interrupt
                            if ((stat & 0x20) != 0)
                            {
                                RequestLCDInterrupt();
                            }
                        }
                    }
                    break;
            }
        }
        
        private void RenderScanline()
        {
            if (mmu == null) return;
            
            // Clear scanline buffer
            for (int x = 0; x < SCREEN_WIDTH; x++)
            {
                framebuffer[ly * SCREEN_WIDTH + x] = currentColors[0]; // Default to color 0
            }
            
            // Render background
            if (BGEnabled())
            {
                RenderBackground();
            }
            
            // Render window
            if (WindowEnabled() && wy <= ly)
            {
                RenderWindow();
            }
            
            // Render sprites
            if (SpritesEnabled())
            {
                RenderSprites();
            }
            
            // Debug first pixel of first few scanlines
            if (ly < 3)
            {
                if (EnableGPULogging)
            {
                LogCallback?.Invoke($"GPU: Scanline {ly} first pixel: 0x{framebuffer[ly * SCREEN_WIDTH]:X8}");
            }
            }
        }
        
        private void RenderBackground()
        {
            if (mmu == null) return;
            
            ushort tileMapBase = BGTileMapArea() ? (ushort)0x9C00 : (ushort)0x9800;
            ushort tileDataBase = BGWindowTileDataArea() ? (ushort)0x8000 : (ushort)0x8800;
            bool signedTileNumbers = !BGWindowTileDataArea();
            
            int scrollY = (scy + ly) & 0xFF;
            int tileRow = scrollY / 8;
            int pixelRow = scrollY % 8;
            
            for (int x = 0; x < SCREEN_WIDTH; x++)
            {
                int scrollX = (scx + x) & 0xFF;
                int tileCol = scrollX / 8;
                int pixelCol = scrollX % 8;
                
                ushort tileMapAddr = (ushort)(tileMapBase + (tileRow * 32) + tileCol);
                byte tileNumber = mmu.ReadByte(tileMapAddr);
                
                ushort tileAddr;
                if (signedTileNumbers)
                {
                    sbyte signedTileNumber = (sbyte)tileNumber;
                    tileAddr = (ushort)(tileDataBase + ((signedTileNumber + 128) * 16));
                }
                else
                {
                    tileAddr = (ushort)(tileDataBase + (tileNumber * 16));
                }
                
                // Get pixel color from tile
                byte pixelColor = GetTilePixel(tileAddr, pixelCol, pixelRow);
                uint color = GetBGColor(pixelColor);
                
                framebuffer[ly * SCREEN_WIDTH + x] = color;
            }
        }
        
        private void RenderWindow()
        {
            if (mmu == null) return;
            
            int windowY = ly - wy;
            if (windowY < 0) return;
            
            ushort tileMapBase = WindowTileMapArea() ? (ushort)0x9C00 : (ushort)0x9800;
            ushort tileDataBase = BGWindowTileDataArea() ? (ushort)0x8000 : (ushort)0x8800;
            bool signedTileNumbers = !BGWindowTileDataArea();
            
            int tileRow = windowY / 8;
            int pixelRow = windowY % 8;
            
            for (int x = Math.Max(0, wx - 7); x < SCREEN_WIDTH; x++)
            {
                int windowX = x - (wx - 7);
                if (windowX < 0) continue;
                
                int tileCol = windowX / 8;
                int pixelCol = windowX % 8;
                
                ushort tileMapAddr = (ushort)(tileMapBase + (tileRow * 32) + tileCol);
                byte tileNumber = mmu.ReadByte(tileMapAddr);
                
                ushort tileAddr;
                if (signedTileNumbers)
                {
                    sbyte signedTileNumber = (sbyte)tileNumber;
                    tileAddr = (ushort)(tileDataBase + ((signedTileNumber + 128) * 16));
                }
                else
                {
                    tileAddr = (ushort)(tileDataBase + (tileNumber * 16));
                }
                
                // Get pixel color from tile
                byte pixelColor = GetTilePixel(tileAddr, pixelCol, pixelRow);
                uint color = GetBGColor(pixelColor);
                
                framebuffer[ly * SCREEN_WIDTH + x] = color;
            }
        }
        
        private void RenderSprites()
        {
            if (mmu == null) return;
            
            byte[] oam = mmu.GetOAM();
            int spriteHeight = SpriteSize() ? 16 : 8;
            
            // Sprites are drawn in reverse order (last sprite has priority)
            for (int i = 39; i >= 0; i--)
            {
                int oamAddr = i * 4;
                byte spriteY = oam[oamAddr];
                byte spriteX = oam[oamAddr + 1];
                byte tileNumber = oam[oamAddr + 2];
                byte attributes = oam[oamAddr + 3];
                
                // Sprite position is offset by 16,8
                int realY = spriteY - 16;
                int realX = spriteX - 8;
                
                // Check if sprite is on current scanline
                if (ly < realY || ly >= realY + spriteHeight) continue;
                
                bool flipX = (attributes & 0x20) != 0;
                bool flipY = (attributes & 0x40) != 0;
                bool priority = (attributes & 0x80) == 0; // 0 = above BG, 1 = below BG
                bool palette = (attributes & 0x10) != 0; // 0 = OBP0, 1 = OBP1
                
                int pixelY = ly - realY;
                if (flipY) pixelY = spriteHeight - 1 - pixelY;
                
                ushort tileAddr;
                if (spriteHeight == 16)
                {
                    tileAddr = (ushort)(0x8000 + ((tileNumber & 0xFE) * 16));
                    if (pixelY >= 8)
                    {
                        tileAddr += 16;
                        pixelY -= 8;
                    }
                }
                else
                {
                    tileAddr = (ushort)(0x8000 + (tileNumber * 16));
                }
                
                for (int pixelX = 0; pixelX < 8; pixelX++)
                {
                    int screenX = realX + pixelX;
                    if (screenX < 0 || screenX >= SCREEN_WIDTH) continue;
                    
                    int tilePixelX = flipX ? 7 - pixelX : pixelX;
                    byte pixelColor = GetTilePixel(tileAddr, tilePixelX, pixelY);
                    
                    // Color 0 is transparent for sprites
                    if (pixelColor == 0) continue;
                    
                    // Check priority (sprite below BG colors 1-3)
                    if (!priority)
                    {
                        uint bgColor = framebuffer[ly * SCREEN_WIDTH + screenX];
                        if (bgColor != currentColors[0]) continue; // Skip if BG is not color 0
                    }
                    
                    uint color = GetSpriteColor(pixelColor, palette);
                    framebuffer[ly * SCREEN_WIDTH + screenX] = color;
                }
            }
        }
        
        private byte GetTilePixel(ushort tileAddr, int pixelX, int pixelY)
        {
            if (mmu == null) return 0;
            
            ushort lineAddr = (ushort)(tileAddr + (pixelY * 2));
            byte low = mmu.ReadByte(lineAddr);
            byte high = mmu.ReadByte((ushort)(lineAddr + 1));
            
            int bit = 7 - pixelX;
            byte pixelColor = (byte)(((high >> bit) & 1) << 1 | ((low >> bit) & 1));
            
            return pixelColor;
        }
        
        private uint GetBGColor(byte colorIndex)
        {
            int paletteIndex = (bgp >> (colorIndex * 2)) & 3;
            return currentColors[paletteIndex];
        }
        
        private uint GetSpriteColor(byte colorIndex, bool palette)
        {
            byte paletteData = palette ? obp1 : obp0;
            int paletteIndex = (paletteData >> (colorIndex * 2)) & 3;
            return currentColors[paletteIndex];
        }
        
        private void SetMode(int newMode)
        {
            mode = newMode;
            stat = (byte)((stat & 0xFC) | mode);
            
            // Check for STAT interrupts
            bool interrupt = false;
            switch (mode)
            {
                case MODE_HBLANK:
                    interrupt = (stat & 0x08) != 0;
                    break;
                case MODE_VBLANK:
                    interrupt = (stat & 0x10) != 0;
                    break;
                case MODE_OAM_SEARCH:
                    interrupt = (stat & 0x20) != 0;
                    break;
            }
            
            if (interrupt)
            {
                RequestLCDInterrupt();
            }
        }
        
        private void CheckLYC()
        {
            bool lycMatch = ly == lyc;
            stat = (byte)((stat & 0xFB) | (lycMatch ? 0x04 : 0x00));
            
            if (lycMatch && (stat & 0x40) != 0)
            {
                RequestLCDInterrupt();
            }
        }
        
        private void RequestVBlankInterrupt()
        {
            if (mmu != null)
            {
                byte ifReg = mmu.ReadByte(0xFF0F);
                mmu.WriteByte(0xFF0F, (byte)(ifReg | 0x01));
            }
        }
        
        private void RequestLCDInterrupt()
        {
            if (mmu != null)
            {
                byte ifReg = mmu.ReadByte(0xFF0F);
                mmu.WriteByte(0xFF0F, (byte)(ifReg | 0x02));
            }
        }
        
        // Register accessors
        public byte GetLY() => ly;
        public byte GetSTAT() => stat;
        
        public void WriteRegister(byte register, byte value)
        {
            switch (register)
            {
                case 0x40: lcdc = value; break; // LCDC
                case 0x41: stat = (byte)((stat & 0x07) | (value & 0x78)); break; // STAT
                case 0x42: scy = value; break; // SCY
                case 0x43: scx = value; break; // SCX
                case 0x45: lyc = value; CheckLYC(); break; // LYC  
                case 0x47: bgp = value; break; // BGP
                case 0x48: obp0 = value; break; // OBP0
                case 0x49: obp1 = value; break; // OBP1
                case 0x4A: wy = value; break; // WY
                case 0x4B: wx = value; break; // WX
            }
        }
        
        // Control flags
        private bool LCDEnabled() => (lcdc & 0x80) != 0;
        private bool WindowTileMapArea() => (lcdc & 0x40) != 0;
        private bool WindowEnabled() => (lcdc & 0x20) != 0;
        private bool BGWindowTileDataArea() => (lcdc & 0x10) != 0;
        private bool BGTileMapArea() => (lcdc & 0x08) != 0;
        private bool SpriteSize() => (lcdc & 0x04) != 0;
        private bool SpritesEnabled() => (lcdc & 0x02) != 0;
        private bool BGEnabled() => (lcdc & 0x01) != 0;
        
        public uint[] GetFramebuffer() => framebuffer;
        public bool IsVBlank() => mode == MODE_VBLANK;
        
        public void SetClassicGreenScreen(bool enabled)
        {
            useClassicGreenScreen = enabled;
            currentColors = enabled ? GREEN_COLORS : DEFAULT_COLORS;
        }
        
        public bool IsClassicGreenScreen => useClassicGreenScreen;
    }
} 