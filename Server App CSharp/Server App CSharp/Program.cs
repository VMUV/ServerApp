﻿using System;
using System.Threading;
using VMUV_TCP_CSharp;
using Comms_Protocol_CSharp;
using Trace_Logger_CSharp;

namespace Server_App_CSharp
{
    class Program
    {
        private static string _version = "1.0.2.1";
        private static DataQueue _queue = new DataQueue();

        static void Main(string[] args)
        {
            Mutex mutex = null;
            try
            {
                mutex = new Mutex(false, "3fb63999603824ebd0b416f74e96505023cfcd41");
                if (mutex.WaitOne(0, false))
                {
                    Initialize();

                    BTWorker btWorker = new BTWorker();
                    MotusWorker motusWorker = new MotusWorker();
                    SocketWrapper tcpServer = new SocketWrapper(Configuration.server);

                    tcpServer.StartServer();

                    while (true)
                    {
                        if (btWorker.GetState() == ThreadState.Stopped)
                            btWorker.Start();
                        else if (btWorker.HasData())
                            btWorker.GetData(_queue);

                        if (motusWorker.GetState() == ThreadState.Stopped)
                            motusWorker.Start();
                        else if (motusWorker.HasData())
                            motusWorker.GetData(_queue);

                        if (!_queue.IsEmpty())
                            tcpServer.ServerSetTxData(_queue);

                        Thread.Sleep(4);
                    }
                }       
            }
            catch (Exception)
            { }
            finally
            {
                if (mutex != null)
                {
                    mutex.Close();
                    mutex = null;
                }
            }
        }

        static void Initialize()
        {
            Logger.PrintToConsole = true;
            string startTime = DateTime.Now.ToString("h:mm:ss tt");
            Logger.CreateLogFile();
            Logger.LogMessage("Motus-1 Pipe Server version: " + _version);
            Logger.LogMessage("Motus-1 Pipe Server started at " + startTime);
            Logger.LogMessage("VMUV_TCP version: " + SocketWrapper.version);
        }

        static void TakeDown()
        {
            string endTime = DateTime.Now.ToString("h:mm:ss tt");
            Logger.LogMessage("Motus-1 Pipe Server ended at " + endTime);
        }
    }
}
