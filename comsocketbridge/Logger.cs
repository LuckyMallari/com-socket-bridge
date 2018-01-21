using System;
using System.IO;

namespace ComSocketBridge
{
    static class Logger
    {
        static Logger()
        {

        }

        public static void Log(string s)
        {
            string output = $"[{DateTime.Now:MM/dd/yyyy hh:mm:ss tt}] {s.Trim()}{Environment.NewLine}";

            if (Environment.UserInteractive)
            {
                Console.WriteLine(output.Trim());
            }

            if (!ConfigManager.IsLog)
                return;

            if (File.Exists(ConfigManager.LogFileFolder))
            {
                Console.WriteLine($"[{DateTime.Now}] {ConfigManager.LogFileFolder + " is a file! Nothing will be logged!"}");
                ConfigManager.IsLog = false;
                return;
            }

            if (!Directory.Exists(ConfigManager.LogFileFolder))
            {
                Directory.CreateDirectory(ConfigManager.LogFileFolder);
            }
            else
            {
                string filePath = $"comsocketbridge.{DateTime.Now:MMddyyyy}.txt";
                try
                {

                    File.AppendAllText(Path.Combine(ConfigManager.LogFileFolder, filePath), output);
                }
                catch
                {
                    //ignored
                }
            }
        }

    }
}
