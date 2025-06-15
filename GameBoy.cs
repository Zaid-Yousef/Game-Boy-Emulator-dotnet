using System;
using System.Diagnostics;
using GameBoyEmulator.CPU;
using GameBoyEmulator.MMU;
using GameBoyEmulator.GPU;
using GameBoyEmulator.APU;
using GameBoyEmulator.Input;
using GameBoyEmulator.Timer;
using GameBoyEmulator.Cartridge;

namespace GameBoyEmulator
{
    public class GameBoy
    {
        // Core components
        private CPU.CPU cpu;
        private MMU.MMU mmu;
        private GPU.GPU gpu;
        private APU.APU apu;
        private Timer.Timer timer;
        private Input.Joypad joypad;
        private Cartridge.Cartridge? cartridge;
        
        // Logging callback and controls
        private Action<string>? _logCallback;
        public Action<string>? LogCallback 
        { 
            get => _logCallback;
            set 
            {
                _logCallback = value;
                // Update component logging callbacks when main callback is set
                if (cpu != null) cpu.LogCallback = value;
                if (gpu != null) gpu.LogCallback = value;
            }
        }
        
        // Logging levels for performance
        public bool EnableDebugLogging { get; set; } = false;
        public bool EnablePerformanceLogging { get; set; } = false;
        public bool EnableInstructionLogging 
        { 
            get => cpu?.EnableInstructionLogging ?? false;
            set 
            {
                if (cpu != null) cpu.EnableInstructionLogging = value;
            }
        }
        public bool EnableGPULogging 
        { 
            get => gpu?.EnableGPULogging ?? false;
            set 
            {
                if (gpu != null) gpu.EnableGPULogging = value;
            }
        }
        
        // Timing
        private const int CYCLES_PER_FRAME = 70224; // 4.194304 MHz / 59.7 Hz
        private const double TARGET_FPS = 59.7;
        private const double MS_PER_FRAME = 1000.0 / TARGET_FPS;
        
        // Performance tracking
        private int totalCyclesExecuted = 0;
        
        // State
        private bool running = false;
        private bool paused = false;
        private Stopwatch frameTimer = new Stopwatch();
        
        public GameBoy()
        {
            InitializeComponents();
        }
        
        private void InitializeComponents()
        {
            // Create components
            mmu = new MMU.MMU();
            cpu = new CPU.CPU(mmu);
            gpu = new GPU.GPU();
            apu = new APU.APU();
            timer = new Timer.Timer();
            joypad = new Input.Joypad();
            
            // Connect components
            gpu.ConnectMMU(mmu);
            timer.ConnectMMU(mmu);
            mmu.ConnectComponents(gpu, apu, joypad, timer);
        }
        
        public bool LoadCartridge(string romPath)
        {
            try
            {
                LogCallback?.Invoke($"Loading ROM: {romPath}");
                cartridge = new Cartridge.Cartridge();
                if (cartridge.LoadROM(romPath))
                {
                    LogCallback?.Invoke("ROM loaded successfully");
                    mmu.LoadCartridge(cartridge);
                    LogCallback?.Invoke("Cartridge connected to MMU");
                    Reset();
                    LogCallback?.Invoke("CPU reset complete");
                    return true;
                }
                LogCallback?.Invoke("Failed to load ROM");
                return false;
            }
            catch (Exception ex)
            {
                LogCallback?.Invoke($"Error loading cartridge: {ex.Message}");
                return false;
            }
        }
        
        public void Reset()
        {
            cpu.Reset();
            mmu.Reset();
            gpu.Reset();
            apu.Reset();
        }
        
        public void Start()
        {
            running = true;
            frameTimer.Start();
        }
        
        public void Stop()
        {
            running = false;
            frameTimer.Stop();
            apu?.Dispose();
        }
        
        public void Pause()
        {
            paused = !paused;
        }
        
        public void SetButtonState(GameBoyButton button, bool pressed)
        {
            joypad.SetButtonState(button, pressed);
            
            // Request joypad interrupt if any button is pressed
            if (pressed)
            {
                byte ifReg = mmu.ReadByte(0xFF0F);
                mmu.WriteByte(0xFF0F, (byte)(ifReg | 0x10));
            }
        }
        
        public bool Step()
        {
            if (!running || paused) return false;
            
            try
            {
                int frameCycles = 0;
                int stepCount = 0;
                
                if (EnableDebugLogging)
                {
                    LogCallback?.Invoke($"GameBoy.Step() starting - PC: 0x{cpu.PC:X4}");
                }
                
                // Execute one frame worth of cycles
                while (frameCycles < CYCLES_PER_FRAME)
                {
                    // Step CPU (returns cycles executed)
                    int cycles = cpu.Step();
                    frameCycles += cycles;
                    totalCyclesExecuted += cycles;
                    stepCount++;
                    
                    // Only log first few steps if debug logging is enabled
                    if (EnableDebugLogging && stepCount <= 3)
                    {
                        LogCallback?.Invoke($"CPU Step {stepCount}: PC=0x{cpu.PC:X4}, cycles={cycles}, total={frameCycles}");
                    }
                    
                    // Step other components
                    gpu.Step(cycles);
                    
                    timer.Step(cycles);
                    apu.Step(cycles);
                    
                    // Safety check to prevent infinite loops
                    if (stepCount > 100000)
                    {
                        LogCallback?.Invoke($"ERROR: Too many steps in one frame! stepCount={stepCount}");
                        break;
                    }
                }
                
                if (EnablePerformanceLogging)
                {
                    LogCallback?.Invoke($"Frame complete: {stepCount} steps, {frameCycles} cycles, VBlank={gpu.IsVBlank()}");
                }
                return true; // Frame processing complete
            }
            catch (Exception ex)
            {
                LogCallback?.Invoke($"GameBoy.Step() crashed: {ex.Message}");
                LogCallback?.Invoke($"Stack trace: {ex.StackTrace}");
                Stop(); // Stop emulation on crash
                throw;
            }
        }
        
        public uint[] GetFramebuffer()
        {
            return gpu.GetFramebuffer();
        }
        
        public bool IsRunning => running;
        public bool IsPaused => paused;
        
        // Audio control
        public void SetAudioEnabled(bool enabled) => apu.SetAudioEnabled(enabled);
        public bool IsAudioEnabled => apu.IsAudioEnabled;
        
        // Display control
        public void SetClassicGreenScreen(bool enabled) => gpu.SetClassicGreenScreen(enabled);
        public bool IsClassicGreenScreen => gpu.IsClassicGreenScreen;
        
        public string GetCartridgeInfo()
        {
            if (cartridge != null)
            {
                return $"{cartridge.GetTitle()} (Type: 0x{cartridge.GetCartridgeType():X2})";
            }
            return "No cartridge loaded";
        }
        
        // Debug information
        public string GetCPUState()
        {
            return $"PC: 0x{cpu.PC:X4} SP: 0x{cpu.SP:X4} AF: 0x{cpu.AF:X4} BC: 0x{cpu.BC:X4} DE: 0x{cpu.DE:X4} HL: 0x{cpu.HL:X4}";
        }
        
        public string GetFlags()
        {
            return $"Z:{(cpu.FlagZ ? '1' : '0')} N:{(cpu.FlagN ? '1' : '0')} H:{(cpu.FlagH ? '1' : '0')} C:{(cpu.FlagC ? '1' : '0')}";
        }
        
        public double GetFrameTime()
        {
            return frameTimer.Elapsed.TotalMilliseconds;
        }
        
        public void ResetFrameTimer()
        {
            frameTimer.Restart();
        }
    }
} 