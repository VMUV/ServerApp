using Comms_Protocol_CSharp;

namespace Server_App_CSharp
{
    class DataStorageTable
    {
        private static Motus_1_RawDataPacket _currentMotus1RawDataPing 
            = new Motus_1_RawDataPacket();
        private static Motus_1_RawDataPacket _currentMotus1RawDataPong 
            = new Motus_1_RawDataPacket();
        private static bool _useMotus1RawDataPing = true;

        public static void SetCurrentMotus1RawData(Motus_1_RawDataPacket data)
        {
            if (_useMotus1RawDataPing)
            {
                _currentMotus1RawDataPing = data;
                _useMotus1RawDataPing = false;
            }
            else
            {
                _currentMotus1RawDataPong = data;
                _useMotus1RawDataPing = true;
            }
        }

        public static Motus_1_RawDataPacket GetCurrentMotus1RawData()
        {
            if (_useMotus1RawDataPing)
                return _currentMotus1RawDataPong;
            else
                return _currentMotus1RawDataPing;
        }
    }
}
