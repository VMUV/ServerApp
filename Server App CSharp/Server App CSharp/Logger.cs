using System;

namespace Server_App_CSharp
{
    static class Logger
    {
        private static string currentDir = System.IO.Directory.GetCurrentDirectory();
        private static string logFileName = "log.txt";
        private static string logFilePath = System.IO.Path.Combine(currentDir, logFileName);
        private static string rawDataFileName = "rawData.csv";
        private static string rawDataFilePath = System.IO.Path.Combine(currentDir, rawDataFileName);
        private static bool logFileCreated = false;
        private static bool logRawData = false;
        private static bool logConsolData = false;

        public static void CreateLogFile()
        {
            logFileCreated = false;

            try
            {
#if DEBUG
                logConsolData = true;
                logRawData = true;
#endif

                string[] str = { "*** New log file created ***", "Raw data logging enabled = " + logRawData.ToString() };
                System.IO.File.WriteAllLines(logFilePath, str);
                logFileCreated = true;

                if (logRawData)
                {
                    string[] header = { "chan 0,chan 1,chan 2,chan 3,chan 4,chan 5,chan 6,chan 7,chan 8" };
                    System.IO.File.WriteAllLines(rawDataFilePath, header);
                }
            }
            catch (System.IO.IOException e0)
            {
                // Don't print this exception because you don't have a log file to print it to..
            }
            catch (Exception e1)
            {
                // Don't print this exception because you don't have a log file to print it to..
            }
        }

        public static void LogMessage(string msg)
        {
            if (!logFileCreated)
                return;

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(logFilePath, true))
            {
                try
                {
                    file.WriteLine(msg);

                    if (logConsolData)
                        Console.WriteLine(msg);
                }
                catch (Exception e0)
                {
                    // Don't print this exception because you don't have a log file to print it to..
                }
            }
        }

        public static void LogMessage(string[] msgs)
        {
            string msg = "";

            if (!logFileCreated)
                return;

            foreach (string element in msgs)
            {
                msg += element;
            }

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(logFilePath, true))
            {
                try
                {
                    file.WriteLine(msg);

                    if (logConsolData)
                        Console.WriteLine(msg);
                }
                catch (Exception e0)
                {
                    // Don't print this exception because you don't have a log file to print it to..
                }
            }
        }

        public static bool IsLoggingRawData()
        {
            return logRawData;
        }

        public static void LogRawData(string data)
        {
            if (!logRawData)
                return;

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(rawDataFilePath, true))
            {
                try
                {
                    file.WriteLine(data);
                }
                catch (Exception e0)
                {
                    // Don't print this exception because you don't have a log file to print it to..
                }
            }
        }
    }
}
