# 🎮 Game Boy (DMG) Emulator - C#

A high-performance Game Boy (DMG) emulator written in C# with WinForms, featuring accurate emulation, 60 FPS performance, and quality-of-life enhancements.

![GameBoy Emulator](https://img.shields.io/badge/Platform-.NET%208.0-blue)
![License](https://img.shields.io/badge/License-MIT-green)
![Performance](https://img.shields.io/badge/Performance-60%20FPS-brightgreen)

**Developer's Note**: It was a fun and incredibly challenging personal goal to dive deep into the Game Boy's architecture and see how quickly a functional and accurate emulator could be built from scratch in .NET.


## ✨ Features

### 🎯 **Core Emulation**
- **Accurate CPU**: Full Sharp LR35902 (Game Boy CPU) instruction set implementation
- **Complete GPU**: Accurate PPU with sprite rendering, background, and window layers  
- **Sound System**: 4-channel APU with square waves, custom wave, and noise generation
- **Memory Management**: Full MMU with bank switching support for various cartridge types
- **Input Handling**: Complete joypad emulation with customizable controls

### 🚀 **Performance Optimized**
- **60 FPS Target**: Optimized timing system achieving authentic 59.7 FPS
- **Efficient Rendering**: Smart display updates and optimized fullscreen scaling
- **Low Latency**: Responsive input handling with minimal input lag
- **Resource Management**: Proper memory cleanup and efficient bitmap operations

### 🎨 **Quality of Life Features**
- **Fullscreen Mode**: F11 toggles immersive fullscreen with aspect ratio preservation
- **Classic Green Screen**: Authentic Game Boy DMG color palette option
- **Debug Tools**: Real-time CPU state, performance metrics, and logging
- **Audio Control**: Toggle experimental audio on/off (disabled by default)
- **Modern UI**: Clean interface with status information and controls

## 🛠️ Installation & Setup

### Prerequisites
- Windows 10/11
- .NET 8.0 Runtime
- Visual Studio 2022 (for development)

### Quick Start
1. **Clone the repository**
   ```bash
   git clone https://github.com/Zaid-Yousef/Game-Boy-Emulator-dotnet.git
   cd gameboy-emulator
   ```

2. **Build the project**
   ```bash
   dotnet build GameBoyEmulator.csproj
   ```

3. **Run the emulator**
   ```bash
   dotnet run
   ```

4. **Load a ROM**
   - Use **File → Open ROM** or press **F1**
   - Select a `.gb` Game Boy ROM file
   - The game will start automatically!

## 🎮 Controls

### Game Controls
| Key | Game Boy Button |
|-----|----------------|
| **Arrow Keys** | D-Pad |
| **Z** | A Button |
| **X** | B Button |
| **Enter** | Start |
| **Right Shift** | Select |

### Emulator Controls
| Key | Function |
|-----|----------|
| **F1** | Open ROM |
| **F2** | Pause/Resume |
| **F3** | Reset |
| **F11** | Toggle Fullscreen |
| **Escape** | Exit Fullscreen |

### Menu Features
- **View → Fullscreen**: Immersive fullscreen mode with perfect scaling
- **View → Classic Green Screen**: Authentic Game Boy color palette
- **Audio → Enable Audio**: Experimental 4-channel sound (off by default)
- **Debug → Logging Options**: Performance and debug information

## 🏗️ Architecture

### Core Components
- **CPU (`CPU/`)**: Sharp LR35902 processor emulation with full instruction set
- **GPU (`GPU/`)**: Picture Processing Unit with accurate timing and rendering
- **APU (`APU/`)**: Audio Processing Unit with 4-channel sound synthesis
- **MMU (`MMU/`)**: Memory Management Unit with cartridge bank switching
- **Cartridge (`Cartridge/`)**: ROM loading and cartridge type detection
- **Input (`Input/`)**: Joypad handling and input mapping
- **Timer (`Timer/`)**: System timing and interrupt generation

### Technical Highlights
- **Cycle-Accurate Timing**: Proper CPU/GPU synchronization
- **Interrupt Handling**: Complete interrupt system implementation  
- **Memory Banking**: Support for various cartridge memory configurations
- **Optimized Rendering**: Efficient bitmap operations and display updates
- **Thread-Safe Audio**: Circular buffer system for smooth audio playback

## 🐛 Known Issues

- **Audio**: Experimental feature with occasional crackling (disabled by default)
- **Some ROM compatibility**: Advanced cartridge types may need additional work
- **Save States**: Not yet implemented

## 🚀 Performance

- **Target**: 59.7 FPS (authentic Game Boy speed)
- **Achieved**: Consistent 60+ FPS on modern hardware
- **Optimizations**: 
  - 10ms timer frequency for responsive emulation
  - Efficient fullscreen scaling with pre-rendered bitmaps
  - Smart display updates only when frames complete
  - Disabled debug logging by default

## 🤝 Contributing

Contributions are welcome! Here's how you can help:

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Commit** your changes (`git commit -m 'Add amazing feature'`)
4. **Push** to the branch (`git push origin feature/amazing-feature`)
5. **Open** a Pull Request

### Areas for Contribution
- Additional cartridge type support (MBC2, MBC3, MBC5)
- Save state functionality
- Audio system improvements
- Game compatibility testing
- Performance optimizations
- UI/UX enhancements


## 🙏 Acknowledgments

- **Pan Docs**: Comprehensive Game Boy technical documentation
- **Game Boy CPU Manual**: Detailed processor specifications
- **NAudio**: Audio library for .NET applications
- **Emulation Community**: For extensive research and documentation

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

This means you can:
- ✅ Use commercially
- ✅ Modify and distribute
- ✅ Private use
- ✅ Patent use
- ❌ Hold liable
- ❌ Use trademark

Jut give creddit if you use it anywhere please :)

## 🔗 Links

- [Game Boy Technical Documentation](https://gbdev.io/pandocs/)
- [.NET 8.0 Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- [NAudio Documentation](https://github.com/naudio/NAudio)

---

**Happy Gaming! 🎮** If you enjoy this emulator, please consider giving it a ⭐ star! 
