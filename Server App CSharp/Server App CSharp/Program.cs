using System;
using System.Threading;
using VMUV_TCP_CSharp;
using Comms_Protocol_CSharp;
using Trace_Logger_CSharp;

namespace Server_App_CSharp
{
    class Program
    {
        private static string _version = "1.0.1.0";
        private static SocketWrapper _tcpServer = new SocketWrapper(Configuration.server);
        private static DataQueue _queue = new DataQueue();

        static void Main(string[] args)
        {
            Mutex mutex = null;
            try
            {
                mutex = new Mutex(false, "3fb63999603824ebd0b416f74e96505023cfcd41");
                if (mutex.WaitOne(0, false))
                {
                    BTWorker bTWorker = new BTWorker();
                    MotusWorker motusWorker = new MotusWorker();

                    Initialize();
                    _tcpServer.StartServer();
                    motusWorker.Run();

                    while (true)
                    {
                        if (!bTWorker.IsRunning)
                            bTWorker.Run();
                        bTWorker.GetData(_queue);
                        HIDInterface.GetData(_queue);

                        // debug stuff
                        byte[] tmp = new byte[2048];
                        int numBytes = _queue.GetStreamable(tmp);
                        if (numBytes > 0)
                        {
                            Console.WriteLine("Got " + numBytes + " bytes!");
                            byte[] toSend = new byte[numBytes];
                            Buffer.BlockCopy(tmp, 0, toSend, 0, numBytes);
                            _tcpServer.ServerSetTxData(toSend);
                        }

                        ServiceLoggingRequests();
                        Thread.Sleep(2);
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
            string startTime = DateTime.Now.ToString("h:mm:ss tt");
            Logger.CreateLogFile();
            Logger.LogMessage("Motus-1 Pipe Server version: " + _version);
            Logger.LogMessage("Motus-1 Pipe Server started at " + startTime);
            Logger.LogMessage("VMUV_TCP version: " + SocketWrapper.version);
        }

        static void TakeDown()
        {
            string endTime = DateTime.Now.ToString("h:mm:ss tt");
            Logger.LogMessage("Motus-1 Pipe Server ended at " + endTime);
        }

        static void ServiceLoggingRequests()
        {
            if (HIDInterface.HasTraceMessages())
            {
                TraceLoggerMessage[] msgs = HIDInterface.GetTraceMessages();
                string[] strMsg = new string[msgs.Length];

                for (int i = 0; i < msgs.Length; i++)
                    strMsg[i] = TraceLogger.TraceLoggerMessageToString(msgs[i]);

                Logger.LogMessage(strMsg);
            }

            if (_tcpServer.HasTraceMessages())
            {
                TraceLoggerMessage[] msgs = _tcpServer.GetTraceMessages();
                string[] strMsg = new string[msgs.Length];

                for (int i = 0; i < msgs.Length; i++)
                    strMsg[i] = TraceLogger.TraceLoggerMessageToString(msgs[i]);

                Logger.LogMessage(strMsg);
            }
        }
    }
}
