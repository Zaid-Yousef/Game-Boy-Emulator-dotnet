using System;
using NAudio.Wave;

namespace GameBoyEmulator.APU
{
    public class APU : ISampleProvider, IDisposable
    {
        // Audio constants
        private const int SAMPLE_RATE = 44100;
        private const int BUFFER_SIZE = 8192; // Power of 2 for efficiency
        
        // Audio output
        private WaveOutEvent? waveOut;
        private bool audioEnabled = false; // Disabled by default due to known issues
        
        // Sound registers
        private byte[] soundRegs = new byte[0x40];
        
        // Channels
        private Channel1 channel1;
        private Channel2 channel2;
        private Channel3 channel3;
        private Channel4 channel4;
        
        // Master control
        private bool soundEnabled = false;
        
        // Circular buffer
        private float[] audioBuffer = new float[BUFFER_SIZE];
        private int writePos = 0;
        private int readPos = 0;
        private readonly object bufferLock = new object();
        
        // Timing
        private double sampleAccumulator = 0;
        private const double CYCLES_PER_SAMPLE = 4194304.0 / SAMPLE_RATE; // ~95.1 cycles per sample
        
        public WaveFormat WaveFormat { get; private set; }
        
        public APU()
        {
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(SAMPLE_RATE, 2);
            
            channel1 = new Channel1();
            channel2 = new Channel2();
            channel3 = new Channel3();
            channel4 = new Channel4();
            
            Reset();
            InitializeAudio();
        }
        
        private void InitializeAudio()
        {
            try
            {
                waveOut = new WaveOutEvent();
                waveOut.DesiredLatency = 100; // Higher latency for stability
                waveOut.Init(this);
                waveOut.Play();
            }
            catch (Exception)
            {
                audioEnabled = false;
            }
        }
        
        public void Reset()
        {
            Array.Clear(soundRegs, 0, soundRegs.Length);
            
            // Default register values
            soundRegs[0x10] = 0x80; soundRegs[0x11] = 0xBF; soundRegs[0x12] = 0xF3; soundRegs[0x14] = 0xBF;
            soundRegs[0x16] = 0x3F; soundRegs[0x17] = 0x00; soundRegs[0x19] = 0xBF;
            soundRegs[0x1A] = 0x7F; soundRegs[0x1B] = 0xFF; soundRegs[0x1C] = 0x9F; soundRegs[0x1E] = 0xBF;
            soundRegs[0x20] = 0xFF; soundRegs[0x21] = 0x00; soundRegs[0x22] = 0x00; soundRegs[0x23] = 0xBF;
            soundRegs[0x24] = 0x77; soundRegs[0x25] = 0xF3; soundRegs[0x26] = 0xF1;
            
            soundEnabled = true;
            channel1.Reset(); channel2.Reset(); channel3.Reset(); channel4.Reset();
            
            lock (bufferLock)
            {
                Array.Clear(audioBuffer, 0, audioBuffer.Length);
                writePos = readPos = 0;
            }
        }
        
        public void Step(int cycles)
        {
            if (!soundEnabled || !audioEnabled) return;
            
            // Tick channels
            for (int i = 0; i < cycles; i++)
            {
                channel1.Tick();
                channel2.Tick();
                channel3.Tick();
                channel4.Tick();
            }
            
            // Generate audio samples
            sampleAccumulator += cycles;
            while (sampleAccumulator >= CYCLES_PER_SAMPLE)
            {
                sampleAccumulator -= CYCLES_PER_SAMPLE;
                GenerateAndBufferSample();
            }
        }
        
        private void GenerateAndBufferSample()
        {
            // Get channel outputs
            int ch1 = channel1.GetOutput();
            int ch2 = channel2.GetOutput();
            int ch3 = channel3.GetOutput();
            int ch4 = channel4.GetOutput();
            
            // Get routing (NR51) and master volume (NR50)
            byte routing = soundRegs[0x25];
            byte masterVol = soundRegs[0x24];
            
            // CORRECT GameBoy stereo mixing: NR51 is ROUTING, not mixing!
            // Each channel either plays at full volume on a speaker or doesn't play at all
            int leftMix = 0, rightMix = 0;
            
            // Left channel routing (bits 7-4 of NR51)
            if ((routing & 0x10) != 0) leftMix += ch1;  // CH1 -> Left
            if ((routing & 0x20) != 0) leftMix += ch2;  // CH2 -> Left
            if ((routing & 0x40) != 0) leftMix += ch3;  // CH3 -> Left
            if ((routing & 0x80) != 0) leftMix += ch4;  // CH4 -> Left
            
            // Right channel routing (bits 3-0 of NR51)
            if ((routing & 0x01) != 0) rightMix += ch1; // CH1 -> Right
            if ((routing & 0x02) != 0) rightMix += ch2; // CH2 -> Right
            if ((routing & 0x04) != 0) rightMix += ch3; // CH3 -> Right
            if ((routing & 0x08) != 0) rightMix += ch4; // CH4 -> Right
            
            // Apply master volume (NR50) - this scales the final output
            int leftVol = ((masterVol >> 4) & 0x07) + 1;  // 1-8 range
            int rightVol = (masterVol & 0x07) + 1;        // 1-8 range
            
            leftMix = (leftMix * leftVol) / 8;
            rightMix = (rightMix * rightVol) / 8;
            
            // Convert to float - no artificial centering, just proper scaling
            // Max possible value: 4 channels * 120 (15*8) = 480
            float left = leftMix / 480.0f;
            float right = rightMix / 480.0f;
            
            // Apply gentle volume scaling to prevent clipping
            left *= 0.8f;
            right *= 0.8f;
            
            // Add to circular buffer
            lock (bufferLock)
            {
                audioBuffer[writePos] = left;
                audioBuffer[writePos + 1] = right;
                writePos = (writePos + 2) % BUFFER_SIZE;
                
                // Prevent buffer overflow by advancing read pos if necessary
                if (writePos == readPos)
                {
                    readPos = (readPos + 2) % BUFFER_SIZE;
                }
            }
        }
        
        public int Read(float[] buffer, int offset, int count)
        {
            if (!audioEnabled)
            {
                Array.Clear(buffer, offset, count);
                return count;
            }
            
            lock (bufferLock)
            {
                for (int i = 0; i < count; i += 2)
                {
                    if (i + 1 >= count) break;
                    
                    // Check if we have samples available
                    if (readPos != writePos)
                    {
                        buffer[offset + i] = audioBuffer[readPos];
                        buffer[offset + i + 1] = audioBuffer[readPos + 1];
                        readPos = (readPos + 2) % BUFFER_SIZE;
                    }
                    else
                    {
                        // No samples available - output silence
                        buffer[offset + i] = 0;
                        buffer[offset + i + 1] = 0;
                    }
                }
            }
            
            return count;
        }
        
        public void WriteRegister(byte register, byte value)
        {
            if (register >= 0x10 && register <= 0x26)
            {
                if (register == 0x26) // NR52
                {
                    soundEnabled = (value & 0x80) != 0;
                    if (!soundEnabled)
                    {
                        for (int i = 0x10; i <= 0x25; i++) soundRegs[i] = 0;
                        channel1.SetEnabled(false); channel2.SetEnabled(false);
                        channel3.SetEnabled(false); channel4.SetEnabled(false);
                    }
                    soundRegs[register] = (byte)((value & 0x80) | (soundRegs[register] & 0x7F));
                }
                else if (soundEnabled)
                {
                    soundRegs[register] = value;
                    
                    // Route to channels
                    if (register >= 0x10 && register <= 0x14) channel1.WriteRegister(register, value);
                    else if (register >= 0x16 && register <= 0x19) channel2.WriteRegister(register, value);
                    else if (register >= 0x1A && register <= 0x1E) channel3.WriteRegister(register, value);
                    else if (register >= 0x20 && register <= 0x23) channel4.WriteRegister(register, value);
                }
            }
            else if (register >= 0x30 && register <= 0x3F)
            {
                soundRegs[register] = value;
                channel3.WriteWaveRam(register - 0x30, value);
            }
        }
        
        public byte ReadRegister(byte register)
        {
            if (register >= 0x10 && register <= 0x3F && register < soundRegs.Length)
                return soundRegs[register];
            return 0xFF;
        }
        
        public void SetAudioEnabled(bool enabled)
        {
            audioEnabled = enabled;
            if (enabled && waveOut == null)
            {
                InitializeAudio();
            }
            else if (!enabled && waveOut != null)
            {
                waveOut.Stop();
                waveOut.Dispose();
                waveOut = null;
            }
        }
        
        public bool IsAudioEnabled => audioEnabled;
        
        public void Dispose()
        {
            waveOut?.Stop();
            waveOut?.Dispose();
        }
        
        // Simple channel implementations
        private class Channel1
        {
            private int frequency, timer, dutyPos, volume, dutyPattern;
            private bool enabled, dacEnabled;
            private readonly byte[] dutyWaves = { 0x01, 0x81, 0x87, 0x7E };
            
            public void Reset() 
            { 
                enabled = dacEnabled = false; 
                frequency = timer = dutyPos = volume = dutyPattern = 0; 
            }
            
            public void SetEnabled(bool enable) => enabled = enable;
            
            public void WriteRegister(byte register, byte value)
            {
                switch (register)
                {
                    case 0x11: dutyPattern = (value >> 6) & 3; break;
                    case 0x12: 
                        volume = (value >> 4) & 0x0F; 
                        dacEnabled = (value & 0xF8) != 0; 
                        if (!dacEnabled) enabled = false; 
                        break;
                    case 0x13: frequency = (frequency & 0x700) | value; break;
                    case 0x14: 
                        frequency = (frequency & 0xFF) | ((value & 0x07) << 8);
                        if ((value & 0x80) != 0 && dacEnabled) 
                        { 
                            enabled = true; 
                            if (frequency > 0) timer = (2048 - frequency) * 4;
                            dutyPos = 0;
                        }
                        break;
                }
            }
            
            public void Tick()
            {
                if (!enabled || timer <= 0) return;
                if (--timer <= 0) 
                { 
                    if (frequency > 0) timer = (2048 - frequency) * 4; 
                    dutyPos = (dutyPos + 1) % 8; 
                }
            }
            
            public int GetOutput()
            {
                if (!enabled || !dacEnabled || volume == 0) return 0;
                return ((dutyWaves[dutyPattern] >> dutyPos) & 1) * volume * 8;
            }
        }
        
        private class Channel2
        {
            private int frequency, timer, dutyPos, volume, dutyPattern;
            private bool enabled, dacEnabled;
            private readonly byte[] dutyWaves = { 0x01, 0x81, 0x87, 0x7E };
            
            public void Reset() 
            { 
                enabled = dacEnabled = false; 
                frequency = timer = dutyPos = volume = dutyPattern = 0; 
            }
            
            public void SetEnabled(bool enable) => enabled = enable;
            
            public void WriteRegister(byte register, byte value)
            {
                switch (register)
                {
                    case 0x16: dutyPattern = (value >> 6) & 3; break;
                    case 0x17: 
                        volume = (value >> 4) & 0x0F; 
                        dacEnabled = (value & 0xF8) != 0; 
                        if (!dacEnabled) enabled = false; 
                        break;
                    case 0x18: frequency = (frequency & 0x700) | value; break;
                    case 0x19: 
                        frequency = (frequency & 0xFF) | ((value & 0x07) << 8);
                        if ((value & 0x80) != 0 && dacEnabled) 
                        { 
                            enabled = true; 
                            if (frequency > 0) timer = (2048 - frequency) * 4;
                            dutyPos = 0;
                        }
                        break;
                }
            }
            
            public void Tick()
            {
                if (!enabled || timer <= 0) return;
                if (--timer <= 0) 
                { 
                    if (frequency > 0) timer = (2048 - frequency) * 4; 
                    dutyPos = (dutyPos + 1) % 8; 
                }
            }
            
            public int GetOutput()
            {
                if (!enabled || !dacEnabled || volume == 0) return 0;
                return ((dutyWaves[dutyPattern] >> dutyPos) & 1) * volume * 8;
            }
        }
        
        private class Channel3
        {
            private int frequency, timer, wavePos, volume;
            private bool enabled, dacEnabled;
            private byte[] waveRam = new byte[16];
            
            public void Reset() 
            { 
                enabled = dacEnabled = false; 
                frequency = timer = wavePos = volume = 0; 
                Array.Clear(waveRam, 0, 16); 
            }
            
            public void SetEnabled(bool enable) => enabled = enable;
            public void WriteWaveRam(int index, byte value) 
            { 
                if (index >= 0 && index < 16) waveRam[index] = value; 
            }
            
            public void WriteRegister(byte register, byte value)
            {
                switch (register)
                {
                    case 0x1A: dacEnabled = (value & 0x80) != 0; if (!dacEnabled) enabled = false; break;
                    case 0x1C: volume = (value >> 5) & 0x03; break;
                    case 0x1D: frequency = (frequency & 0x700) | value; break;
                    case 0x1E: 
                        frequency = (frequency & 0xFF) | ((value & 0x07) << 8);
                        if ((value & 0x80) != 0 && dacEnabled) 
                        { 
                            enabled = true; 
                            if (frequency > 0) timer = (2048 - frequency) * 2; 
                            wavePos = 0; 
                        }
                        break;
                }
            }
            
            public void Tick()
            {
                if (!enabled || timer <= 0) return;
                if (--timer <= 0) 
                { 
                    if (frequency > 0) timer = (2048 - frequency) * 2; 
                    wavePos = (wavePos + 1) % 32; 
                }
            }
            
            public int GetOutput()
            {
                if (!enabled || !dacEnabled || volume == 0) return 0;
                byte waveByte = waveRam[wavePos / 2];
                int sample = (wavePos % 2 == 0) ? (waveByte >> 4) : (waveByte & 0x0F);
                return volume > 0 ? (sample >> (volume - 1)) * 8 : 0;
            }
        }
        
        private class Channel4
        {
            private int timer, volume, lfsr = 0x7FFF;
            private bool enabled, dacEnabled;
            
            public void Reset() 
            { 
                enabled = dacEnabled = false; 
                timer = volume = 0; lfsr = 0x7FFF; 
            }
            
            public void SetEnabled(bool enable) => enabled = enable;
            
            public void WriteRegister(byte register, byte value)
            {
                switch (register)
                {
                    case 0x21: 
                        volume = (value >> 4) & 0x0F; 
                        dacEnabled = (value & 0xF8) != 0; 
                        if (!dacEnabled) enabled = false; 
                        break;
                    case 0x23: 
                        if ((value & 0x80) != 0 && dacEnabled) 
                        { 
                            enabled = true; 
                            lfsr = 0x7FFF; 
                            timer = 1024; 
                        }
                        break;
                }
            }
            
            public void Tick()
            {
                if (!enabled || timer <= 0) return;
                if (--timer <= 0)
                {
                    timer = 1024;
                    int bit = (lfsr & 1) ^ ((lfsr >> 1) & 1);
                    lfsr = (lfsr >> 1) | (bit << 14);
                }
            }
            
            public int GetOutput()
            {
                if (!enabled || !dacEnabled || volume == 0) return 0;
                return (lfsr & 1) == 0 ? volume * 8 : 0;
            }
        }
    }
} 