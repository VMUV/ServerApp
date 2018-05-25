using System.Threading;

namespace Server_App_CSharp
{
    public class BTWorker : SensorInterface
    {
        public void Start()
        {
            Run(WorkerThread);
        }

        private void WorkerThread()
        {
            BTClient client = new BTClient();
            BTStates state;
            while (true)
            {
                state = client.RunBTStateMachine();

                if (state == BTStates.disconnected)
                {
                    client.RunBTStateMachine();
                    break;
                }

                if (client.HasData)
                    SetData(client.GetData());

                Thread.Sleep(4);
            }
        }
    }
}
