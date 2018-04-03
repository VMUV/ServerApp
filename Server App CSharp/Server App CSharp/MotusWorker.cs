using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Server_App_CSharp
{
    class MotusWorker
    {
        private HardwareStates hwState = HardwareStates.find_device;
        private int devicePollCounter = 0;

        private void Motus1HardwareMain()
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

        private void FindDevice()
        {
            Task.Run(async () =>
            {
                await HIDInterface.FindDevice();
            }).GetAwaiter().GetResult();
        }

        private void PollDevice()
        {
            Task.Run(async () =>
            {
                await HIDInterface.PollDevice();
            }).GetAwaiter().GetResult();
        }

        private void EnumerateDevice()
        {
            Task.Run(async () =>
            {
                await HIDInterface.EnumerateDevice();
            }).GetAwaiter().GetResult();
        }

        private void WorkerThread()
        {
            while (true)
            {
                Motus1HardwareMain();
                Thread.Sleep(2);
            }
        }

        public void Run()
        {
            Thread thread = new Thread(new ThreadStart(WorkerThread));
            thread.Start();
        }
    }

    public enum HardwareStates
    {
        find_device,
        enumerate_device,
        device_enumerated
    }
}
