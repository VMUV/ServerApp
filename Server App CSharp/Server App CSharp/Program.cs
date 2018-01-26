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
        private static string version = "1.0.7";
        private static HardwareStates hwState = HardwareStates.find_device;
        private static SocketWrapper tcpServer = new SocketWrapper(Configuration.server);
        private static int devicePollCounter = 0;

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

                    while (true)
                    {
                        Motus1HardwareMain();
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

        static void Motus1HardwareMain()
        {
            switch (hwState)
            {
                case HardwareStates.find_device:
                    Thread.Sleep(1000);
                    FindDevice();

                    if (HIDInterface.DeviceIsPresent())
                        hwState = HardwareStates.enumerate_device;
                    break;
                case HardwareStates.enumerate_device:
                    EnumerateDevice();

                    if (HIDInterface.DeviceIsEnumerated())
                        hwState = HardwareStates.device_enumerated;
                    else
                        hwState = HardwareStates.find_device;
                    break;
                case HardwareStates.device_enumerated:
                    if (devicePollCounter++ > 1000)
                    {
                        devicePollCounter = 0;
                        PollDevice();
                    }

                    if (!HIDInterface.DeviceIsPresent())
                    {
                        HIDInterface.DisposeDevice();
                        hwState = HardwareStates.find_device;
                    }
                    break;
            }
        }

        static void FindDevice()
        {
            Task.Run(async () =>
            {
                await HIDInterface.FindDevice();
            }).GetAwaiter().GetResult();
        }

        static void PollDevice()
        {
            Task.Run(async () =>
            {
                await HIDInterface.PollDevice();
            }).GetAwaiter().GetResult();
        }

        static void EnumerateDevice()
        {
            Task.Run(async () =>
            {
                await HIDInterface.EnumerateDevice();
            }).GetAwaiter().GetResult();
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

        public enum HardwareStates
        {
            find_device,
            enumerate_device,
            device_enumerated
        }
    }
}
