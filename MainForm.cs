using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using GameBoyEmulator.Input;

namespace GameBoyEmulator
{
    public partial class MainForm : Form
    {
        private GameBoy gameBoy;
        private Bitmap displayBitmap;
        private PictureBox screenPictureBox;
        private System.Windows.Forms.Timer emulationTimer;
        private Label statusLabel;
        private Label fpsLabel;
        private Label debugLabel;
        private TextBox logTextBox;
        private Button pauseLoggingButton;
        private bool loggingPaused = false;
        
        // Quality of life features
        private bool isFullscreen = false;
        private bool useClassicGreenScreen = false;
        private FormWindowState previousWindowState;
        private FormBorderStyle previousBorderStyle;
        private Size previousSize;
        private Point previousLocation;
        
        // Fullscreen optimization
        private Bitmap? fullscreenBitmap;
        private Size fullscreenSize;
        
        // Key mappings
        private readonly Dictionary<Keys, GameBoyButton> keyMappings = new Dictionary<Keys, GameBoyButton>
        {
            { Keys.Z, GameBoyButton.A },
            { Keys.X, GameBoyButton.B },
            { Keys.Enter, GameBoyButton.Start },
            { Keys.RShiftKey, GameBoyButton.Select },
            { Keys.Up, GameBoyButton.Up },
            { Keys.Down, GameBoyButton.Down },
            { Keys.Left, GameBoyButton.Left },
            { Keys.Right, GameBoyButton.Right }
        };
        
        private int frameCount = 0;
        private DateTime lastSecond = DateTime.Now;
        private Stopwatch fpsTimer = new Stopwatch();

        
        private void LogMessage(string message)
        {
            if (loggingPaused) return; // Skip logging if paused
            
            if (logTextBox.InvokeRequired)
            {
                logTextBox.Invoke(new Action(() => LogMessage(message)));
                return;
            }
            
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logLine = $"[{timestamp}] {message}";
            
            // Check if user is scrolled to the bottom before adding new text
            bool wasAtBottom = (logTextBox.SelectionStart >= logTextBox.Text.Length - 10);
            
            logTextBox.AppendText(logLine + Environment.NewLine);
            
            // Only auto-scroll if user was already at the bottom
            if (wasAtBottom)
            {
                logTextBox.SelectionStart = logTextBox.Text.Length;
                logTextBox.ScrollToCaret();
            }
        }
        
        public MainForm()
        {
            // Enable double buffering for smoother rendering
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            this.UpdateStyles();
            
            InitializeComponent();
            InitializeEmulator();
        }
        
        private void InitializeComponent()
        {
            this.Text = "Game Boy (DMG) Emulator";
            this.Size = new Size(800, 700);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.KeyPreview = true;
            
            // Create menu
            var menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("File");
            var openRomItem = new ToolStripMenuItem("Open ROM...", null, OpenROM_Click);
            var exitItem = new ToolStripMenuItem("Exit", null, (s, e) => Close());
            
            var emulationMenu = new ToolStripMenuItem("Emulation");
            var pauseItem = new ToolStripMenuItem("Pause/Resume", null, PauseResume_Click);
            var resetItem = new ToolStripMenuItem("Reset", null, Reset_Click);
            
            var debugMenu = new ToolStripMenuItem("Debug");
            var enableLoggingItem = new ToolStripMenuItem("Enable Debug Logging", null, ToggleDebugLogging_Click);
            var enablePerfLoggingItem = new ToolStripMenuItem("Enable Performance Logging", null, TogglePerfLogging_Click);
            
            var viewMenu = new ToolStripMenuItem("View");
            var fullscreenItem = new ToolStripMenuItem("Fullscreen (F11)", null, ToggleFullscreen_Click) { Checked = false };
            var classicGreenItem = new ToolStripMenuItem("Classic Green Screen", null, ToggleClassicGreen_Click) { Checked = false };
            
            var audioMenu = new ToolStripMenuItem("Audio");
            var audioEnabledItem = new ToolStripMenuItem("Audio Enabled (Experimental)", null, ToggleAudio_Click) { Checked = false };
            
            fileMenu.DropDownItems.AddRange(new ToolStripItem[] { openRomItem, new ToolStripSeparator(), exitItem });
            emulationMenu.DropDownItems.AddRange(new ToolStripItem[] { pauseItem, resetItem });
            viewMenu.DropDownItems.AddRange(new ToolStripItem[] { fullscreenItem, classicGreenItem });
            debugMenu.DropDownItems.AddRange(new ToolStripItem[] { enableLoggingItem, enablePerfLoggingItem });
            audioMenu.DropDownItems.AddRange(new ToolStripItem[] { audioEnabledItem });
            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, emulationMenu, viewMenu, debugMenu, audioMenu });
            
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
            
            // Create screen display
            screenPictureBox = new PictureBox
            {
                Location = new Point(10, 30),
                Size = new Size(480, 432), // 160x144 scaled 3x
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.White,
                TabStop = false // Prevent focus via Tab key
            };
            this.Controls.Add(screenPictureBox);
            
            // Ensure clicking on the screen gives focus back to the form
            screenPictureBox.Click += (s, e) => this.Focus();
            
            // Create status labels
            statusLabel = new Label
            {
                Location = new Point(500, 30),
                Size = new Size(280, 20),
                Text = "No ROM loaded"
            };
            this.Controls.Add(statusLabel);
            
            fpsLabel = new Label
            {
                Location = new Point(500, 60),
                Size = new Size(280, 20),
                Text = "FPS: 0"
            };
            this.Controls.Add(fpsLabel);
            
            debugLabel = new Label
            {
                Location = new Point(500, 90),
                Size = new Size(280, 200),
                Text = "Debug Info",
                Font = new Font("Consolas", 8),
                AutoSize = false
            };
            this.Controls.Add(debugLabel);
            
            // Create controls info
            var controlsLabel = new Label
            {
                Location = new Point(500, 300),
                Size = new Size(280, 140),
                Text = "Controls:\n" +
                      "Arrow Keys - D-Pad\n" +
                      "Z - A Button\n" +
                      "X - B Button\n" +
                      "Enter - Start\n" +
                      "RShift - Select\n\n" +
                      "F1 - Open ROM\n" +
                      "F2 - Pause/Resume\n" +
                      "F3 - Reset\n" +
                      "F11 - Fullscreen\n" +
                      "Esc - Exit Fullscreen\n\n" +
                      "🔊 Audio: Experimental\n" +
                      "(Enable in Audio menu)",
                Font = new Font("Arial", 8),
                AutoSize = false
            };
            this.Controls.Add(controlsLabel);
            
            // Create log display
            var logLabel = new Label
            {
                Location = new Point(10, 490),
                Size = new Size(100, 20),
                Text = "Debug Log:",
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            this.Controls.Add(logLabel);
            
            pauseLoggingButton = new Button
            {
                Location = new Point(120, 488),
                Size = new Size(100, 24),
                Text = "Pause Logging",
                Font = new Font("Arial", 8)
            };
            pauseLoggingButton.Click += (s, e) => {
                loggingPaused = !loggingPaused;
                pauseLoggingButton.Text = loggingPaused ? "Resume Logging" : "Pause Logging";
                this.Focus(); // Return focus to form for game input
            };
            this.Controls.Add(pauseLoggingButton);
            
            logTextBox = new TextBox
            {
                Location = new Point(10, 515),
                Size = new Size(760, 130),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 8),
                BackColor = Color.Black,
                ForeColor = Color.Lime,
                TabStop = false // Prevent focus via Tab key
            };
            this.Controls.Add(logTextBox);
            
            // Add click event to prevent focus from interfering with game input
            logTextBox.Enter += (s, e) => this.Focus();
            
            // Event handlers for form
            this.KeyDown += MainForm_KeyDown;
            this.KeyUp += MainForm_KeyUp;
            this.FormClosing += MainForm_FormClosing;
            
            // Ensure the form gets focus and keep it
            this.Load += (s, e) => this.Focus();
            this.Activated += (s, e) => this.Focus();
        }
        
        private void InitializeEmulator()
        {
            gameBoy = new GameBoy();
            gameBoy.LogCallback = LogMessage; // Connect logging
            
            // Disable performance-heavy logging by default
            gameBoy.EnableDebugLogging = false;
            gameBoy.EnablePerformanceLogging = false;
            gameBoy.EnableInstructionLogging = false;
            gameBoy.EnableGPULogging = false;
            
            displayBitmap = new Bitmap(160, 144, PixelFormat.Format32bppArgb);
            
            // Create emulation timer - GameBoy runs at 59.7 Hz
            // Use higher frequency timer for better performance
            emulationTimer = new System.Windows.Forms.Timer
            {
                Interval = 10, // Higher frequency (100fps theoretical) to overcome Windows timer limitations
                Enabled = false
            };
            emulationTimer.Tick += EmulationTimer_Tick;
            

            
            // Initialize FPS timer
            fpsTimer.Start();
            
            LogMessage("Emulator initialized");
            
            // Initialize display to black instead of test pattern
            using (var graphics = Graphics.FromImage(displayBitmap))
            {
                graphics.Clear(Color.Black);
            }
            screenPictureBox.Image = displayBitmap;
            screenPictureBox.Refresh();
        }
        
        private void EmulationTimer_Tick(object? sender, EventArgs e)
        {
            if (gameBoy.IsRunning)
            {
                try
                {
                    gameBoy.ResetFrameTimer();
                    bool frameComplete = gameBoy.Step();
                    
                    // Always update display for smooth rendering
                    if (frameComplete)
                    {
                        UpdateDisplay();
                        frameCount++;
                    }
                    
                    // Update FPS and debug info every second
                    if (fpsTimer.ElapsedMilliseconds >= 1000)
                    {
                        double actualFPS = frameCount * 1000.0 / fpsTimer.ElapsedMilliseconds;
                        fpsLabel.Text = $"FPS: {actualFPS:F1} (Target: 59.7)";
                        frameCount = 0;
                        fpsTimer.Restart();
                        
                        // Update debug info less frequently to reduce overhead
                        UpdateDebugInfo();
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"ERROR: {ex.Message}");
                    LogMessage($"Stack trace: {ex.StackTrace}");
                    debugLabel.Text = $"Error: {ex.Message}\n\n{ex.StackTrace}";
                    gameBoy.Stop();
                    emulationTimer.Enabled = false;
                }
            }
            else
            {
                // Update debug info when not running
                UpdateDebugInfo();
            }
        }
        
        private void UpdateDisplay()
        {
            if (displayBitmap == null) return;
            
            uint[] framebuffer = gameBoy.GetFramebuffer();
            
            try
            {
                // Lock bitmap data for the source 160x144 bitmap
                var bitmapData = displayBitmap.LockBits(
                    new Rectangle(0, 0, 160, 144),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppArgb);
                
                // Copy framebuffer to bitmap
                Marshal.Copy((int[])(object)framebuffer, 0, bitmapData.Scan0, framebuffer.Length);
                displayBitmap.UnlockBits(bitmapData);
                
                if (isFullscreen && fullscreenBitmap != null)
                {
                    // Use optimized fullscreen rendering
                    UpdateFullscreenDisplay();
                }
                else
                {
                    // Normal windowed mode
                    screenPictureBox.Image = displayBitmap;
                    screenPictureBox.Invalidate();
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't crash
                LogMessage($"Display update error: {ex.Message}");
            }
        }
        
        private void UpdateFullscreenDisplay()
        {
            // Safety check to prevent null reference exceptions
            if (fullscreenBitmap == null || displayBitmap == null)
                return;
                
            try
            {
                // Use fast nearest-neighbor scaling for pixel-perfect results
                using (var graphics = Graphics.FromImage(fullscreenBitmap))
                {
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                    graphics.DrawImage(displayBitmap, 0, 0, fullscreenSize.Width, fullscreenSize.Height);
                }
                
                screenPictureBox.Image = fullscreenBitmap;
                screenPictureBox.Invalidate();
            }
            catch (Exception)
            {
                // If there's any issue with fullscreen rendering, fall back to normal mode
                screenPictureBox.Image = displayBitmap;
                screenPictureBox.Invalidate();
            }
        }
        
        private void UpdateDebugInfo()
        {
            if (gameBoy.IsRunning)
            {
                debugLabel.Text = $"Status: Running\n" +
                                 $"Paused: {gameBoy.IsPaused}\n\n" +
                                 $"CPU State:\n{gameBoy.GetCPUState()}\n\n" +
                                 $"Flags:\n{gameBoy.GetFlags()}\n\n" +
                                 $"Frame Time: {gameBoy.GetFrameTime():F2}ms";
            }
            else
            {
                debugLabel.Text = "Status: Not Running\n" +
                                 "Load a ROM to start emulation.";
            }
        }
        
        private void OpenROM_Click(object? sender, EventArgs e)
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = "Game Boy ROMs (*.gb)|*.gb|All files (*.*)|*.*",
                Title = "Select Game Boy ROM"
            };
            
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                LogMessage($"Loading ROM: {openFileDialog.FileName}");
                
                if (gameBoy.LoadCartridge(openFileDialog.FileName))
                {
                    LogMessage("ROM loaded successfully");
                    statusLabel.Text = gameBoy.GetCartridgeInfo();
                    gameBoy.Start();
                    emulationTimer.Enabled = true;
                    LogMessage("Emulation started");
                    
                    // Ensure form has focus for game input
                    this.Focus();
                }
                else
                {
                    LogMessage("Failed to load ROM");
                    MessageBox.Show("Failed to load ROM file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        
        private void PauseResume_Click(object? sender, EventArgs e)
        {
            if (gameBoy.IsRunning)
            {
                gameBoy.Pause();
                statusLabel.Text = gameBoy.IsPaused ? "PAUSED - " + gameBoy.GetCartridgeInfo() : gameBoy.GetCartridgeInfo();
            }
        }
        
        private void Reset_Click(object? sender, EventArgs e)
        {
            if (gameBoy.IsRunning)
            {
                gameBoy.Reset();
            }
        }
        
        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            // Handle function keys
            switch (e.KeyCode)
            {
                case Keys.F1:
                    OpenROM_Click(null, EventArgs.Empty);
                    return;
                case Keys.F2:
                    PauseResume_Click(null, EventArgs.Empty);
                    return;
                case Keys.F3:
                    Reset_Click(null, EventArgs.Empty);
                    return;
                case Keys.F11:
                    ToggleFullscreen_Click(null, EventArgs.Empty);
                    return;
                case Keys.Escape:
                    if (isFullscreen)
                    {
                        ExitFullscreen();
                    }
                    return;
            }
            
            // Handle game controls
            if (keyMappings.TryGetValue(e.KeyCode, out GameBoyButton button))
            {
                gameBoy.SetButtonState(button, true);
            }
        }
        
        private void MainForm_KeyUp(object? sender, KeyEventArgs e)
        {
            if (keyMappings.TryGetValue(e.KeyCode, out GameBoyButton button))
            {
                gameBoy.SetButtonState(button, false);
            }
        }
        
        // Override ProcessCmdKey to catch keys at the form level before controls get them
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Strip modifiers to get the base key
            Keys baseKey = keyData & Keys.KeyCode;
            
            // Handle function keys first
            switch (baseKey)
            {
                case Keys.F1:
                    OpenROM_Click(null, EventArgs.Empty);
                    return true;
                case Keys.F2:
                    PauseResume_Click(null, EventArgs.Empty);
                    return true;
                case Keys.F3:
                    Reset_Click(null, EventArgs.Empty);
                    return true;
                case Keys.F11:
                    ToggleFullscreen_Click(null, EventArgs.Empty);
                    return true;
                case Keys.Escape:
                    if (isFullscreen)
                    {
                        ExitFullscreen();
                        return true;
                    }
                    break;
            }
            
            // Handle game controls
            if (keyMappings.ContainsKey(baseKey))
            {
                // For game controls, we want to handle both KeyDown and KeyUp events
                // ProcessCmdKey only handles the initial press, so we still need the KeyDown/KeyUp events
                // But we return false here to let them through to the normal event handlers
                return false;
            }
            
            // For all other keys, let the base class handle them
            return base.ProcessCmdKey(ref msg, keyData);
        }
        
        private void ToggleDebugLogging_Click(object? sender, EventArgs e)
        {
            if (gameBoy != null)
            {
                gameBoy.EnableDebugLogging = !gameBoy.EnableDebugLogging;
                LogMessage($"Debug logging {(gameBoy.EnableDebugLogging ? "enabled" : "disabled")}");
            }
        }
        
        private void TogglePerfLogging_Click(object? sender, EventArgs e)
        {
            if (gameBoy != null)
            {
                gameBoy.EnablePerformanceLogging = !gameBoy.EnablePerformanceLogging;
                LogMessage($"Performance logging {(gameBoy.EnablePerformanceLogging ? "enabled" : "disabled")}");
            }
        }
        
        private void ToggleAudio_Click(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem && gameBoy != null)
            {
                menuItem.Checked = !menuItem.Checked;
                gameBoy.SetAudioEnabled(menuItem.Checked);
                LogMessage($"Audio {(menuItem.Checked ? "enabled" : "disabled")} - Note: Audio is experimental and may have issues");
            }
        }
        
        private void ToggleFullscreen_Click(object? sender, EventArgs e)
        {
            if (isFullscreen)
            {
                ExitFullscreen();
            }
            else
            {
                EnterFullscreen();
            }
        }
        
        private void EnterFullscreen()
        {
            // Save current state
            previousWindowState = this.WindowState;
            previousBorderStyle = this.FormBorderStyle;
            previousSize = this.Size;
            previousLocation = this.Location;
            
            // Hide all UI elements except the screen
            this.MainMenuStrip.Visible = false;
            statusLabel.Visible = false;
            fpsLabel.Visible = false;
            debugLabel.Visible = false;
            logTextBox.Visible = false;
            pauseLoggingButton.Visible = false;
            foreach (Control control in this.Controls)
            {
                if (control != screenPictureBox && control.Visible)
                {
                    control.Visible = false;
                }
            }
            
            // Set fullscreen properties
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            
            // Resize and center the screen
            var screen = Screen.FromControl(this);
            int maxWidth = screen.Bounds.Width;
            int maxHeight = screen.Bounds.Height;
            
            // Calculate best fit maintaining aspect ratio (160:144)
            double aspectRatio = 160.0 / 144.0;
            int newWidth, newHeight;
            
            if (maxWidth / aspectRatio <= maxHeight)
            {
                newWidth = maxWidth;
                newHeight = (int)(maxWidth / aspectRatio);
            }
            else
            {
                newHeight = maxHeight;
                newWidth = (int)(maxHeight * aspectRatio);
            }
            
            // Create optimized fullscreen bitmap
            fullscreenSize = new Size(newWidth, newHeight);
            fullscreenBitmap?.Dispose();
            fullscreenBitmap = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);
            
            screenPictureBox.Size = fullscreenSize;
            screenPictureBox.Location = new Point((maxWidth - newWidth) / 2, (maxHeight - newHeight) / 2);
            screenPictureBox.BorderStyle = BorderStyle.None;
            screenPictureBox.SizeMode = PictureBoxSizeMode.Normal; // Don't let PictureBox stretch
            
            isFullscreen = true;
        }
        
        private void ExitFullscreen()
        {
            // First, restore the display bitmap to prevent rendering issues
            isFullscreen = false; // Set this first to prevent UpdateDisplay from using fullscreen mode
            screenPictureBox.Image = displayBitmap; // Restore original bitmap before cleanup
            
            // Restore window properties
            this.FormBorderStyle = previousBorderStyle;
            this.WindowState = previousWindowState;
            this.Size = previousSize;
            this.Location = previousLocation;
            
            // Show all UI elements
            this.MainMenuStrip.Visible = true;
            statusLabel.Visible = true;
            fpsLabel.Visible = true;
            debugLabel.Visible = true;
            logTextBox.Visible = true;
            pauseLoggingButton.Visible = true;
            foreach (Control control in this.Controls)
            {
                control.Visible = true;
            }
            
            // Restore screen position and size
            screenPictureBox.Location = new Point(10, 30);
            screenPictureBox.Size = new Size(480, 432);
            screenPictureBox.BorderStyle = BorderStyle.FixedSingle;
            screenPictureBox.SizeMode = PictureBoxSizeMode.StretchImage; // Restore stretch mode
            
            // Clean up fullscreen bitmap safely
            fullscreenBitmap?.Dispose();
            fullscreenBitmap = null;
            
            // Force a refresh to ensure proper display
            screenPictureBox.Invalidate();
        }
        
        private void ToggleClassicGreen_Click(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem && gameBoy != null)
            {
                menuItem.Checked = !menuItem.Checked;
                useClassicGreenScreen = menuItem.Checked;
                
                gameBoy.SetClassicGreenScreen(useClassicGreenScreen);
                
                if (useClassicGreenScreen)
                {
                    // Classic Game Boy green background and palette
                    screenPictureBox.BackColor = Color.FromArgb(139, 172, 15);
                    LogMessage("Classic green screen enabled - authentic Game Boy colors");
                }
                else
                {
                    // Default white background and palette
                    screenPictureBox.BackColor = Color.White;
                    LogMessage("Classic green screen disabled - modern colors");
                }
            }
        }
        
        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            emulationTimer?.Stop();
            gameBoy?.Stop();
            fullscreenBitmap?.Dispose();
        }
    }
} 