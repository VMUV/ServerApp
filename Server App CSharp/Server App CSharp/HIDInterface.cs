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
    public class HIDInterface : SensorInterface
    {
        private  bool _deviceIsEnumerated = false;
        private  bool _deviceIsPresent = false;
        private  HidDevice _device = null;
        private  TraceLogger _hidLogger = new TraceLogger(128);
        private  string _moduleName = "HIDInterface.cs";

        public  bool DeviceIsEnumerated()
        {
            return _deviceIsEnumerated;
        }

        public  bool DeviceIsPresent()
        {
            return _deviceIsPresent;
        }

        public  async Task FindDevice()
        {
            string methodName = "FindDevice";

            _deviceIsPresent = false;

            try
            {
                var deviceInfo = await DeviceInformation.FindAllAsync(USBSelector.GetSelector());

                if (deviceInfo.Count > 0)
                {
                    _deviceIsPresent = true;
                    _hidLogger.QueueMessage(_hidLogger.BuildMessage(_moduleName, methodName, 
                        "Motus-1 is present!"));
                }
            }
            catch (Exception e0)
            {
                string message = "An exception of type " + e0.GetType().ToString() + " occurred." +
                    " Exception occurred at : " + Environment.StackTrace + ". Exception message is : " + 
                    e0.Message;
                _hidLogger.QueueMessage(_hidLogger.BuildMessage(_moduleName, methodName, message));
            }
        }

        public  async Task PollDevice()
        {
            string methodName = "PollDevice";
            try
            {
                var deviceInfo = await DeviceInformation.FindAllAsync(USBSelector.GetSelector());

                if (deviceInfo.Count == 0)
                {
                    _deviceIsPresent = false;
                    _hidLogger.QueueMessage(_hidLogger.BuildMessage(_moduleName, methodName, 
                        "Motus-1 has been disconnected"));
                }
            }
            catch (Exception e0)
            {
                string message = "An exception of type " + e0.GetType().ToString() + " occurred." +
                    " Exception occurred at : " + Environment.StackTrace + ". Exception message is : " + 
                    e0.Message;
                _hidLogger.QueueMessage(_hidLogger.BuildMessage(_moduleName, methodName, message));
            }
        }

        public  async Task EnumerateDevice()
        {
            string methodName = "EnumerateDevice";
            _deviceIsEnumerated = false;

            if (!DeviceIsEnumerated() && DeviceIsPresent())
            {
                try
                {
                    var deviceInfo = await DeviceInformation.FindAllAsync(USBSelector.GetSelector());
                    _device = await HidDevice.FromIdAsync(deviceInfo.ElementAt(0).Id, 
                        Windows.Storage.FileAccessMode.ReadWrite);
                }
                catch (Exception e0)
                {
                    string message = "An exception of type " + e0.GetType().ToString() + " occurred." +
                        " Exception occurred at : " + Environment.StackTrace + ". Exception message is : " +
                        e0.Message;
                    _hidLogger.QueueMessage(_hidLogger.BuildMessage(_moduleName, methodName, message));
                }

                if (_device != null)
                {
                    _deviceIsEnumerated = true;
                    _device.InputReportReceived += new TypedEventHandler<HidDevice, 
                        HidInputReportReceivedEventArgs>(USBInterruptTransferHandler);
                    _hidLogger.QueueMessage(_hidLogger.BuildMessage(_moduleName, methodName, 
                        "Motus-1 enumeration success!"));
                }
                else
                {
                    _hidLogger.QueueMessage(_hidLogger.BuildMessage(_moduleName, methodName, 
                        "Motus-1 enumeration failure."));
                }
            }
        }

        public  void DisposeDevice()
        {
            _deviceIsEnumerated = false;
            _device.Dispose();
        }

        private  void GetHidReport(HidInputReportReceivedEventArgs args)
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
                byte[] stream = new byte[packet.ExpectedLen + DataPacket.NumOverHeadBytes];
                packet.SerializeToStream(stream, 0);
                SetData(stream);
            }
            catch (ArgumentException e0)
            {
                string msg = e0.Message + e0.StackTrace;
                _hidLogger.QueueMessage(_hidLogger.BuildMessage(_moduleName, methodName,
                    msg));
            }
            catch (IndexOutOfRangeException e1)
            {
                string msg = e1.Message + e1.StackTrace;
                _hidLogger.QueueMessage(_hidLogger.BuildMessage(_moduleName, methodName,
                    msg));
            }
        }

        private  void USBInterruptTransferHandler(HidDevice sender, 
            HidInputReportReceivedEventArgs args)
        {
            GetHidReport(args);
            //if (Logger.IsLoggingRawData())
            //{
            //    lock (_lock)
            //    {
            //        DataPacket p = _dataQueue.Get();
            //        _dataQueue.Add(p);
            //        Motus_1_RawDataPacket packet = new Motus_1_RawDataPacket(p);
            //        Logger.LogRawData(packet.ToString());
            //    }
            //}
        }

        public  TraceLoggerMessage[] GetTraceMessages()
        {
            return _hidLogger.GetAllMessages();
        }

        public  bool HasTraceMessages()
        {
            return _hidLogger.HasMessages();
        }
    }
}
