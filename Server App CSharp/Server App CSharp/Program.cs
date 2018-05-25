//#define LOG_RAW_DATA

using System;
using System.Threading;
using VMUV_TCP_CSharp;
using Comms_Protocol_CSharp;
using Trace_Logger_CSharp;

namespace Server_App_CSharp
{
    class Program
    {
        private static string _version = "1.0.2.2";
        private static DataQueue _queue = new DataQueue(1024);

        static void Main(string[] args)
        {
            Mutex mutex = null;
            try
            {
                mutex = new Mutex(false, "3fb63999603824ebd0b416f74e96505023cfcd41");
                if (mutex.WaitOne(0, false))
                {
                    Initialize();

                    BTWorker btWorker = new BTWorker();
                    MotusWorker motusWorker = new MotusWorker();
                    SocketWrapper tcpServer = new SocketWrapper(Configuration.server);

                    tcpServer.StartServer();

                    while (true)
                    {
                        if (btWorker.GetState() == ThreadState.Stopped)
                            btWorker.Start();
                        else if (btWorker.HasData())
                            btWorker.GetData(_queue);

                        if (motusWorker.GetState() == ThreadState.Stopped)
                            motusWorker.Start();
                        else if (motusWorker.HasData())
                            motusWorker.GetData(_queue);

                        if (!_queue.IsEmpty())
                        {
#if LOG_RAW_DATA
                            // Get the data
                            byte[] buff = new byte[16535];
                            int len = _queue.GetStreamable(buff);
                            byte[] rawData = new byte[len];
                            Buffer.BlockCopy(buff, 0, rawData, 0, len);

                            // Place it in the raw data logger as well
                            RawDataLogger.SetLogData(rawData);

                            // Inject it back into the queue
                            _queue.ParseStreamable(rawData, rawData.Length);
                            tcpServer.ServerSetTxData(_queue);
#else
                            tcpServer.ServerSetTxData(_queue);
#endif
                            _queue.Flush();
                        }

                        Thread.Sleep(4);
                    }
                }
            }
            catch (Exception)
            { }
            finally
            {
                if (mutex != null)
                {
                    mutex.Close();
                    mutex = null;
                }
            }
        }

        static void Initialize()
        {
#if LOG_RAW_DATA
            RawDataLogger.CreateRawDataFiles();
#if DEBUG
            RawDataLogger.PrintToConsole = true;
#endif
#endif
#if DEBUG
            Logger.PrintToConsole = true;
#else
            Logger.PrintToConsole = false;
#endif

            string startTime = DateTime.Now.ToString("h:mm:ss tt");
            Logger.CreateLogFile();
            Logger.LogMessage("Motus-1 Server version: " + _version);
            Logger.LogMessage("Motus-1 Server started at " + startTime);
            Logger.LogMessage("VMUV_TCP version: " + SocketWrapper.version);
        }

        static void TakeDown()
        {
            string endTime = DateTime.Now.ToString("h:mm:ss tt");
            Logger.LogMessage("Motus-1 Pipe Server ended at " + endTime);
        }
    }
}
