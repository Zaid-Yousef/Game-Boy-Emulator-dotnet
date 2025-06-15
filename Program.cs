using System;
using System.Windows.Forms;
using System.IO;

namespace GameBoyEmulator
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                // Write startup log to file
                File.WriteAllText("startup.log", $"[{DateTime.Now}] Application starting...\n");
                
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                
                File.AppendAllText("startup.log", $"[{DateTime.Now}] Creating MainForm...\n");
                var mainForm = new MainForm();
                
                File.AppendAllText("startup.log", $"[{DateTime.Now}] Running application...\n");
                Application.Run(mainForm);
                
                File.AppendAllText("startup.log", $"[{DateTime.Now}] Application exited normally.\n");
            }
            catch (Exception ex)
            {
                var errorMessage = $"[{DateTime.Now}] FATAL ERROR: {ex.Message}\nStack Trace:\n{ex.StackTrace}\n";
                File.WriteAllText("startup_error.log", errorMessage);
                MessageBox.Show($"Fatal error during startup:\n{ex.Message}\n\nSee startup_error.log for details.", 
                    "GameBoy Emulator - Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
} 