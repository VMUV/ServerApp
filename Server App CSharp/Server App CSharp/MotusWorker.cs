using System.Threading.Tasks;
using System.Threading;

namespace Server_App_CSharp
{
    class MotusWorker : SensorInterface
    {
        private HardwareStates _hwState = HardwareStates.find_device;
        private int _devicePollCounter = 0;
        private HIDInterface _hidInterface = new HIDInterface();

        private void FindDevice()
        {
            Task.Run(async () =>
            {
                await _hidInterface.FindDevice();
            }).GetAwaiter().GetResult();
        }

        private void PollDevice()
        {
            Task.Run(async () =>
            {
                await _hidInterface.PollDevice();
            }).GetAwaiter().GetResult();
        }

        private void EnumerateDevice()
        {
            Task.Run(async () =>
            {
                await _hidInterface.EnumerateDevice();
            }).GetAwaiter().GetResult();
        }

        private void WorkerThread()
        {
            while (true)
            {
                switch (_hwState)
                {
                    case HardwareStates.find_device:
                        Thread.Sleep(1000);
                        FindDevice();

                        if (_hidInterface.DeviceIsPresent())
                            _hwState = HardwareStates.enumerate_device;
                        break;
                    case HardwareStates.enumerate_device:
                        EnumerateDevice();

                        if (_hidInterface.DeviceIsEnumerated())
                            _hwState = HardwareStates.device_enumerated;
                        else
                            _hwState = HardwareStates.find_device;
                        break;
                    case HardwareStates.device_enumerated:
                        if (_devicePollCounter++ > 1000)
                        {
                            _devicePollCounter = 0;
                            PollDevice();
                        }

                        if (!_hidInterface.DeviceIsPresent())
                        {
                            _hidInterface.DisposeDevice();
                            _hwState = HardwareStates.find_device;
                        }
                        break;
                }

                if (_hidInterface.HasData())
                    SetData(_hidInterface.GetData());

                Thread.Sleep(2);
            }
        }

        public void Start()
        {
            Run(WorkerThread);
        }
    }

    public enum HardwareStates
    {
        find_device,
        enumerate_device,
        device_enumerated
    }
}
