using System;

namespace GameBoyEmulator.Input
{
    public class Joypad
    {
        // Button states
        private bool aPressed = false;
        private bool bPressed = false;
        private bool selectPressed = false;
        private bool startPressed = false;
        private bool rightPressed = false;
        private bool leftPressed = false;
        private bool upPressed = false;
        private bool downPressed = false;
        
        // Joypad register
        private byte joypadRegister = 0xFF;
        
        public void SetButtonState(GameBoyButton button, bool pressed)
        {
            switch (button)
            {
                case GameBoyButton.A:
                    aPressed = pressed;
                    break;
                case GameBoyButton.B:
                    bPressed = pressed;
                    break;
                case GameBoyButton.Select:
                    selectPressed = pressed;
                    break;
                case GameBoyButton.Start:
                    startPressed = pressed;
                    break;
                case GameBoyButton.Right:
                    rightPressed = pressed;
                    break;
                case GameBoyButton.Left:
                    leftPressed = pressed;
                    break;
                case GameBoyButton.Up:
                    upPressed = pressed;
                    break;
                case GameBoyButton.Down:
                    downPressed = pressed;
                    break;
            }
        }
        
        public byte ReadJoypad()
        {
            byte result = 0xFF;
            
            // Check which button group is selected
            bool selectActionButtons = (joypadRegister & 0x20) == 0;
            bool selectDirectionButtons = (joypadRegister & 0x10) == 0;
            
            if (selectActionButtons)
            {
                // Action buttons (A, B, Select, Start)
                if (aPressed) result &= 0xFE;      // Clear bit 0
                if (bPressed) result &= 0xFD;      // Clear bit 1
                if (selectPressed) result &= 0xFB; // Clear bit 2
                if (startPressed) result &= 0xF7;  // Clear bit 3
            }
            
            if (selectDirectionButtons)
            {
                // Direction buttons (Right, Left, Up, Down)
                if (rightPressed) result &= 0xFE;  // Clear bit 0
                if (leftPressed) result &= 0xFD;   // Clear bit 1
                if (upPressed) result &= 0xFB;     // Clear bit 2
                if (downPressed) result &= 0xF7;   // Clear bit 3
            }
            
            // Keep the upper 2 bits from the register
            result = (byte)((joypadRegister & 0x30) | (result & 0x0F));
            
            return result;
        }
        
        public void WriteJoypad(byte value)
        {
            // Only bits 4-5 are writable
            joypadRegister = (byte)((value & 0x30) | (joypadRegister & 0xCF));
        }
        
        public bool IsAnyButtonPressed()
        {
            return aPressed || bPressed || selectPressed || startPressed ||
                   rightPressed || leftPressed || upPressed || downPressed;
        }
    }
    
    public enum GameBoyButton
    {
        A,
        B,
        Select,
        Start,
        Right,
        Left,
        Up,
        Down
    }
} 