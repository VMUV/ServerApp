using System;
using System.Threading.Tasks;
using System.Threading;
using VMUV_TCP_CSharp;
using Comms_Protocol_CSharp;
using Trace_Logger_CSharp;

namespace Server_App_CSharp
{
    class Program
    {
        private static string version = "1.0.1.0";
        private static SocketWrapper tcpServer = new SocketWrapper(Configuration.server);

        static void Main(string[] args)
        {
            Mutex mutex = null;
            try
            {
                mutex = new Mutex(false, "3fb63999603824ebd0b416f74e96505023cfcd41");
                if (mutex.WaitOne(0, false))
                {
                    Initialize();
                    tcpServer.StartServer();

                    BTWorker bTWorker = new BTWorker();

                    MotusWorker motusWorker = new MotusWorker();
                    motusWorker.Run();

                    while (true)
                    {
                        if (!bTWorker.IsRunning)
                            bTWorker.Run();
                        if (!bTWorker.btQueue.IsEmpty())
                        {
                            byte[] tmp = new byte[2046];
                            int len = bTWorker.btQueue.GetStreamable(tmp);
                            Console.WriteLine("Got " + len + " bytes!\n" + tmp.ToString());
                        }

                        Motus_1_RawDataPacket packet = DataStorageTable.GetCurrentMotus1RawData();
                        tcpServer.ServerSetTxData(packet.Payload, (byte)packet.Type);
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
            Logger.LogMessage("Motus-1 Pipe Server version: " + version);
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

            if (tcpServer.HasTraceMessages())
            {
                TraceLoggerMessage[] msgs = tcpServer.GetTraceMessages();
                string[] strMsg = new string[msgs.Length];

                for (int i = 0; i < msgs.Length; i++)
                    strMsg[i] = TraceLogger.TraceLoggerMessageToString(msgs[i]);

                Logger.LogMessage(strMsg);
            }
        }
    }
}
