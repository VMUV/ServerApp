﻿using System;
using System.Threading;
using Comms_Protocol_CSharp;
using Trace_Logger_CSharp;

namespace Server_App_CSharp
{
    public class SensorInterface
    {
        private DataQueue _queue = new DataQueue();
        private TraceLogger _logger = new TraceLogger();
        private Object _lock = new Object();
        private Thread _thread = null;

        public bool HasData()
        {
            bool rtn = false;
            lock (_lock) { rtn = !_queue.IsEmpty(); }
            return rtn;
        }

        public bool HasLogs()
        {
            bool rtn = false;
            lock (_lock) { rtn = _logger.HasMessages() };
            return rtn;
        }

        public TraceLoggerMessage[] GetLogs()
        {
            TraceLoggerMessage[] rtn = new TraceLoggerMessage[0];
            lock (_lock) { rtn = _logger.GetAllMessages(); }
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

        protected void SetLog(TraceLoggerMessage msg)
        {
            lock (_lock)
            {
                _logger.QueueMessage(msg);
            }
        }
    }
}
