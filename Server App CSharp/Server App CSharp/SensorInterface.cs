using System;
using System.Threading;
using Comms_Protocol_CSharp;

namespace Server_App_CSharp
{
    public class SensorInterface
    {
        private DataQueue _queue = new DataQueue();
        private Object _lock = new Object();
        private Thread _thread = null;

        public bool HasData()
        {
            bool rtn = false;
            lock (_lock) { rtn = !_queue.IsEmpty(); }
            return rtn;
        }

        public byte[] GetData()
        {
            byte[] data = new byte[2048];
            int len = 0;
            lock (_lock)
            {
                len = _queue.GetStreamable(data);
            }

            byte[] rtn = new byte[len];
            Buffer.BlockCopy(data, 0, rtn, 0, len);
            return rtn;
        }

        public bool IsAlive()
        {
            bool rtn = false;
            if (_thread != null)
                rtn = _thread.IsAlive;
            return rtn;
        }

        public void Run()
        {
            Thread _thread = new Thread(new ThreadStart(Worker));
            _thread.Start();
        }

        private void Worker()
        {
            Motus_1_RawDataPacket packet = new Motus_1_RawDataPacket();
            lock (_lock)
            {
                _queue.Add(packet);
                while (!_queue.IsEmpty())
                {
                    if (!_queue.Add(packet))
                        break;
                }
            }

            //Thread.Sleep(10000);
        }
    }
}
