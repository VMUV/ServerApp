using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.HumanInterfaceDevice;
using Windows.Foundation;
using Windows.Storage.Streams;
using Comms_Protocol_CSharp;
using Trace_Logger_CSharp;

namespace Server_App_CSharp
{
    static class HIDInterface
    {
        private static bool deviceIsEnumerated = false;
        private static bool deviceIsPresent = false;
        private static HidDevice device = null;
        private static TraceLogger hidLogger = new TraceLogger(128);
        private static string moduleName = "HIDInterface.cs";

        public static bool DeviceIsEnumerated()
        {
            return deviceIsEnumerated;
        }

        public static bool DeviceIsPresent()
        {
            return deviceIsPresent;
        }

        public static async Task FindDevice()
        {
            string methodName = "FindDevice";

            deviceIsPresent = false;

            try
            {
                var deviceInfo = await DeviceInformation.FindAllAsync(USBSelector.GetSelector());

                if (deviceInfo.Count > 0)
                {
                    deviceIsPresent = true;
                    hidLogger.QueueMessage(hidLogger.BuildMessage(moduleName, methodName, "Motus-1 is present!"));
                }
            }
            catch (Exception e0)
            {
                string message = "An exception of type " + e0.GetType().ToString() + " occurred." +
                    " Exception occurred at : " + Environment.StackTrace + ". Exception message is : " + e0.Message;
                hidLogger.QueueMessage(hidLogger.BuildMessage(moduleName, methodName, message));
            }
        }

        public static async Task PollDevice()
        {
            string methodName = "PollDevice";
            try
            {
                var deviceInfo = await DeviceInformation.FindAllAsync(USBSelector.GetSelector());

                if (deviceInfo.Count == 0)
                {
                    deviceIsPresent = false;
                    hidLogger.QueueMessage(hidLogger.BuildMessage(moduleName, methodName, "Motus-1 has been disconnected"));
                }
            }
            catch (Exception e0)
            {
                string message = "An exception of type " + e0.GetType().ToString() + " occurred." +
                    " Exception occurred at : " + Environment.StackTrace + ". Exception message is : " + e0.Message;
                hidLogger.QueueMessage(hidLogger.BuildMessage(moduleName, methodName, message));
            }
        }

        public static async Task EnumerateDevice()
        {
            string methodName = "EnumerateDevice";
            deviceIsEnumerated = false;

            if (!DeviceIsEnumerated() && DeviceIsPresent())
            {
                try
                {
                    var deviceInfo = await DeviceInformation.FindAllAsync(USBSelector.GetSelector());
                    device = await HidDevice.FromIdAsync(deviceInfo.ElementAt(0).Id, Windows.Storage.FileAccessMode.ReadWrite);
                }
                catch (Exception e0)
                {
                    string message = "An exception of type " + e0.GetType().ToString() + " occurred." +
                        " Exception occurred at : " + Environment.StackTrace + ". Exception message is : " + e0.Message;
                    hidLogger.QueueMessage(hidLogger.BuildMessage(moduleName, methodName, message));
                }

                if (device != null)
                {
                    deviceIsEnumerated = true;
                    device.InputReportReceived += new TypedEventHandler<HidDevice, HidInputReportReceivedEventArgs>(USBInterruptTransferHandler);
                    hidLogger.QueueMessage(hidLogger.BuildMessage(moduleName, methodName, "Motus-1 enumeration success!"));
                }
                else
                {
                    hidLogger.QueueMessage(hidLogger.BuildMessage(moduleName, methodName, "Motus-1 enumeration failure."));
                }
            }
        }

        public static void DisposeDevice()
        {
            deviceIsEnumerated = false;
            device.Dispose();
        }

        private static void GetHidReport(HidInputReportReceivedEventArgs args)
        {
            // For now there is only one data type
            string methodName = "GetHidReport";
            HidInputReport rpt = args.Report;
            IBuffer buff = rpt.Data;
            DataReader dr = DataReader.FromBuffer(buff);
            byte[] bytes = new byte[rpt.Data.Length];
            dr.ReadBytes(bytes);
            Motus_1_RawDataPacket packet = new Motus_1_RawDataPacket();
            try
            {
                // Have to remove a bonus byte on the payload
                byte[] parsed = new byte[bytes.Length - 1];
                for (int i = 0; i < parsed.Length; i++)
                    parsed[i] = bytes[i + 1];
                packet.Serialize(parsed);
                DataStorageTable.SetCurrentMotus1RawData(packet);
            }
            catch (ArgumentException e0)
            {
                string msg = e0.Message + e0.StackTrace;
                hidLogger.QueueMessage(hidLogger.BuildMessage(moduleName, methodName,
                    msg));
            }
            catch (IndexOutOfRangeException e1)
            {
                string msg = e1.Message + e1.StackTrace;
                hidLogger.QueueMessage(hidLogger.BuildMessage(moduleName, methodName,
                    msg));
            }
        }

        private static void USBInterruptTransferHandler(HidDevice sender, 
            HidInputReportReceivedEventArgs args)
        {
            GetHidReport(args);
            if (Logger.IsLoggingRawData())
            {
                Motus_1_RawDataPacket packet = DataStorageTable.GetCurrentMotus1RawData();
                Logger.LogRawData(packet.ToString());
            }
        }

        public static TraceLoggerMessage[] GetTraceMessages()
        {
            return hidLogger.GetAllMessages();
        }

        public static bool HasTraceMessages()
        {
            return hidLogger.HasMessages();
        }
    }
}
