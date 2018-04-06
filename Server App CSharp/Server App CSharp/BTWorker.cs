using System;
using System.Collections.Generic;
using System.Threading;
using Comms_Protocol_CSharp;

namespace Server_App_CSharp
{
    class BTWorker
    {
        private DataQueue _btQueue = new DataQueue();
        private bool _isRunning = false;
        private Object _lock = new Object();

        public int GetData(DataQueue queue)
        {
            int numPacketsQueued = 0;

            lock (_lock)
            {
                while (!_btQueue.IsEmpty())
                {
                    if (queue.Add(_btQueue.Get()))
                        numPacketsQueued++;
                    else
                        break;
                }
            }

            return numPacketsQueued;
        }

        public bool IsRunning
        {
            get { return _isRunning; }
        }

        public void Run()
        {
            Thread thread = new Thread(new ThreadStart(WorkerThread));
            thread.Start();
        }

        private void WorkerThread()
        {
            _isRunning = true;

            BTClient client = new BTClient();
            BTStates state;
            while (true)
            {
                state = client.RunBTStateMachine();
                if (state == BTStates.disconnected)
                {
                    client.RunBTStateMachine();
                    Thread.Sleep(5000);
                    break;
                }
                else
                {
                    if (client.streamDataLen > 0)
                    {
                        lock (_lock)
                        {
                            _btQueue.ParseStreamable(client.streamData, client.streamDataLen);
                            client.streamDataLen = 0;
                        }
                    }

                    Thread.Sleep(10);
                }
            }

            _isRunning = false;
        }
    }
}
