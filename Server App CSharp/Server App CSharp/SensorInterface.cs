using System;
using System.Threading;
using Comms_Protocol_CSharp;

namespace Server_App_CSharp
{
    public class SensorInterface : Loggable
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
            lock (_lock) { len = _queue.GetStreamable(data); }

            byte[] rtn = new byte[len];
            Buffer.BlockCopy(data, 0, rtn, 0, len);
            return rtn;
        }

        public void GetData(DataQueue queue)
        {
            lock (_lock)
            {
                queue.TransferAll(_queue);
            }
        }

        public bool IsAlive()
        {
            bool rtn = false;
            if (_thread != null)
                rtn = _thread.IsAlive;
            return rtn;
        }

        public ThreadState GetState()
        {
            ThreadState rtn = ThreadState.Stopped;
            if (_thread != null)
                rtn = _thread.ThreadState;
            return rtn;
        }

        protected void Run(Action worker)
        {
            _thread = new Thread(new ThreadStart(worker));
            _thread.Start();
        }

        protected void SetData(byte[] data)
        {
            lock (_lock)
            {
                _queue.ParseStreamable(data, data.Length);
            }
        }
    }
}
