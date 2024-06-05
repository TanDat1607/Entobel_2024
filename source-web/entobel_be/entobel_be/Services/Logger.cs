using System.Text;
using System.IO;
using System;

namespace entobel_be.Services
{
    public class Logger
    {
        public static void LogFile(string path, string message)
        {
            string fileName = path;

            try
            {
                // Check if file already exists. If yes, delete it.     
                if (!File.Exists(fileName))
                {
                    File.CreateText(fileName);
                }

                // Create a new file     
                using (StreamWriter sw = File.AppendText(fileName))
                {
                    sw.WriteLine($@"{DateTime.Now}: {message}");
                }
            }
            catch (Exception Ex)
            {
                Console.WriteLine(Ex.ToString());
            }
        }
    }
}
