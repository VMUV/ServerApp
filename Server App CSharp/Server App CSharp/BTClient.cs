using System;
using System.Net.Sockets;
using InTheHand.Net;
using InTheHand.Net.Sockets;
using InTheHand.Net.Bluetooth;
using Comms_Protocol_CSharp;

namespace Server_App_CSharp
{
    class BTClient
    {
        private BluetoothClient _client;
        private BluetoothDeviceInfo[] _devices;
        private NetworkStream _streamIn;
        private Guid _service = new Guid("{7A51FDC2-FDDF-4c9b-AFFC-98BCD91BF93B}");
        private BTStates _state = BTStates.start_radio;
        private int _deviceIndex = 0;
        private byte[] _streamData = new byte[2056];
        private int _timeOutInMs = 0;
        public DataQueue dataQueue = new DataQueue();

        private void TimeOutIncrement()
        {
            _timeOutInMs += 25;
        }

        private void LaunchRadio()
        {
            BluetoothRadio radio = BluetoothRadio.PrimaryRadio;
            if (radio == null)
            {
                // TODO: No radio found we need to report an error in the logs
                _state = BTStates.start_radio;
                return;
            }
            else if (radio.Mode == RadioMode.PowerOff)
                BluetoothRadio.PrimaryRadio.Mode = RadioMode.Connectable;

            _client = new BluetoothClient();
            _state = BTStates.find_connected_devices;
        }

        private void LookForConnectedDevices()
        {
            try
            {
                _devices = _client.DiscoverDevices();
                if (_deviceIndex >= _devices.Length)
                    _state = BTStates.disconnected;
                else
                    _state = BTStates.connect_to_service;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.GetType() + ": " + e.Message);
                _state = BTStates.disconnected;
            }
        }

        private void ConnectToService()
        {
            try
            {
                BluetoothDeviceInfo info = _devices[_deviceIndex++];
                _client.Connect(new BluetoothEndPoint((BluetoothAddress)info.DeviceAddress, _service));
                _state = BTStates.connected_to_service;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.GetType() + ": " + e.Message);
                _state = BTStates.find_connected_devices;
            }
        }

        private void InitStream()
        {
            try
            {
                _streamIn = _client.GetStream();
                _streamIn.ReadTimeout = 100;
                _streamIn.Flush();
                _state = BTStates.read_stream;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.GetType() + ": " + e.Message);
                _state = BTStates.disconnected;
            }
        }

        private void ReadStream()
        {
            try
            {
                if (_streamIn.DataAvailable)
                {
                    int numBytes = _streamIn.Read(_streamData, 0, _streamData.Length);

                    //Console.WriteLine("Got " + numBytes + " bytes:");
                    if (numBytes > 0)
                    {
                        _timeOutInMs = 0;
                        dataQueue.ParseStreamable(_streamData, numBytes);
                        //Console.WriteLine("Got " + dataQueue.Count + " valid packets");
                    }
                    else
                        TimeOutIncrement();
                }
                else
                    TimeOutIncrement();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.GetType() + ": " + e.Message);
                TimeOutIncrement();
            }

            // TODO: variable timeout period?
            if (_timeOutInMs > 5000)
                _state = BTStates.disconnected;
        }

        public BTStates RunBTStateMachine()
        {
            switch (_state)
            {
                case BTStates.start_radio:
                    {
                        LaunchRadio();
                    }
                    break;
                case BTStates.find_connected_devices:
                    {
                        Console.WriteLine("Searching for connected devices..");
                        LookForConnectedDevices();
                    }
                    break;
                case BTStates.connect_to_service:
                    {
                        Console.WriteLine("Found " + _devices.Length + " devices");
                        if (_devices.Length > 0)
                            Console.Write("Attempting to connect to " + _devices[_deviceIndex].DeviceName +
                                " with service " + _service.ToString());
                        ConnectToService();
                    }
                    break;
                case BTStates.connected_to_service:
                    {
                        Console.WriteLine("Connected to service " + _service.ToString());
                        InitStream();
                    }
                    break;
                case BTStates.read_stream:
                    {
                        ReadStream();
                    }
                    break;
                case BTStates.disconnected:
                    {
                        Console.WriteLine("Disconnecting");
                        if (_streamIn != null)
                            _streamIn.Dispose();
                        if (_client != null)
                            _client.Close();
                        if (_client != null)
                            _client.Dispose();
                        _devices = null;
                    }
                    break;
            }

            return _state;
        }
    }

    public enum BTStates
    {
        start_radio = 0,
        find_connected_devices,
        connect_to_service,
        connected_to_service,
        read_stream,
        disconnected
    }
}
