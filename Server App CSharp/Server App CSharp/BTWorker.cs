using System;
using System.Threading;
using Comms_Protocol_CSharp;

namespace Server_App_CSharp
{
    class BTWorker
    {
        public DataQueue btQueue = new DataQueue();
        private bool _isRunning = false;

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
                    while (!client.dataQueue.IsEmpty())
                        btQueue.Add(client.dataQueue.Get());
                    Thread.Sleep(10);
                }
            }

            _isRunning = false;
        }
    }
}
