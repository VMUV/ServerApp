using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.HumanInterfaceDevice;
using Windows.Foundation;
using Windows.Storage.Streams;
using Comms_Protocol_CSharp;

namespace Server_App_CSharp
{
    public class HIDInterface : SensorInterface
    {
        private  bool _deviceIsEnumerated = false;
        private  bool _deviceIsPresent = false;
        private  HidDevice _device = null;

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
            _deviceIsPresent = false;

            try
            {
                var deviceInfo = await DeviceInformation.FindAllAsync(USBSelector.GetSelector());

                if (deviceInfo.Count > 0)
                {
                    _deviceIsPresent = true;
                    LogMessage("Motus-1 is present!");
                }
            }
            catch (Exception e0)
            {
                LogMessage(e0.Message + e0.StackTrace);
            }
        }

        public  async Task PollDevice()
        {
            try
            {
                var deviceInfo = await DeviceInformation.FindAllAsync(USBSelector.GetSelector());

                if (deviceInfo.Count == 0)
                {
                    _deviceIsPresent = false;
                    LogMessage("Motus-1 has been disconnected");
                }
            }
            catch (Exception e0)
            {
                LogMessage(e0.Message + e0.StackTrace);
            }
        }

        public  async Task EnumerateDevice()
        {
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
                    LogMessage(e0.Message + e0.StackTrace);
                }

                if (_device != null)
                {
                    _deviceIsEnumerated = true;
                    _device.InputReportReceived += new TypedEventHandler<HidDevice, 
                        HidInputReportReceivedEventArgs>(USBInterruptTransferHandler);
                    LogMessage("Motus-1 enumeration success!");
                }
                else
                {
                    LogMessage("Motus-1 enumeration failure.");
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
                LogMessage(e0.Message + e0.StackTrace);
            }
            catch (IndexOutOfRangeException e1)
            {
                LogMessage(e1.Message + e1.StackTrace);
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
    }
}
