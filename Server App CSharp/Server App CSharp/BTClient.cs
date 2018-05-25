using System;
using System.Net.Sockets;
using InTheHand.Net;
using InTheHand.Net.Sockets;
using InTheHand.Net.Bluetooth;

namespace Server_App_CSharp
{
    class BTClient : Loggable
    {
        private BluetoothClient _client;
        private BluetoothDeviceInfo[] _devices;
        private NetworkStream _streamIn;
        private Guid _service = new Guid("{7A51FDC2-FDDF-4c9b-AFFC-98BCD91BF93B}");
        private BTStates _state = BTStates.start_radio;
        private int _deviceIndex = 0;
        private int _timeOutInMs = 0;
        private byte[] _streamData = new byte[2056];
        private int _streamDataLen = 0;

        public bool HasData { get; set; }

        private void TimeOutIncrement()
        {
            _timeOutInMs += 5;
        }

        private void LaunchRadio()
        {
            BluetoothRadio radio = BluetoothRadio.PrimaryRadio;
            if (radio == null)
            {
                LogMessage("No radio present, cannot launch Bluetooth client.");
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
                LogMessage(e.GetType() + ": " + e.Message);
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
                LogMessage(e.GetType() + ": " + e.Message);
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
                LogMessage(e.GetType() + ": " + e.Message);
                _state = BTStates.disconnected;
            }
        }

        private void ReadStream()
        {
            try
            {
                if (_streamIn.DataAvailable)
                {
                    _streamDataLen = _streamIn.Read(_streamData, 0, _streamData.Length);
                    if (_streamDataLen > 0)
                    {
                        HasData = true;
                        _timeOutInMs = 0;
                    }
                    else
                        TimeOutIncrement();
                }
                else
                    TimeOutIncrement();
            }
            catch (Exception e)
            {
                LogMessage(e.GetType() + ": " + e.Message);
                TimeOutIncrement();
            }

            // TODO: variable timeout period?
            if (_timeOutInMs > 10000)
                _state = BTStates.disconnected;
        }

        public byte[] GetData()
        {
            byte[] rtn = new byte[_streamDataLen];
            Buffer.BlockCopy(_streamData, 0, rtn, 0, _streamDataLen);
            _streamDataLen = 0;
            HasData = false;
            return rtn;
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
                        LogMessage("Searching for connected devices..");
                        LookForConnectedDevices();
                    }
                    break;
                case BTStates.connect_to_service:
                    {
                        LogMessage("Found " + _devices.Length + " devices");
                        if (_devices.Length > 0)
                            LogMessage("Attempting to connect to " + _devices[_deviceIndex].DeviceName +
                                " with service " + _service.ToString());
                        ConnectToService();
                    }
                    break;
                case BTStates.connected_to_service:
                    {
                        LogMessage("Connected to service " + _service.ToString());
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
                        LogMessage("Disconnecting");
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
