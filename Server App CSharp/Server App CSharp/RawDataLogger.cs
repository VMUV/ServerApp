using System;
using Comms_Protocol_CSharp;

namespace Server_App_CSharp
{
    public static class RawDataLogger
    {
        private static string _currentDir = System.IO.Directory.GetCurrentDirectory();

        // File for each sensor
        private static string _accelFile = System.IO.Path.Combine(_currentDir, "accel.csv");
        private static string _gyroFile = System.IO.Path.Combine(_currentDir, "gyro.csv");
        private static string _linearAccelFile = System.IO.Path.Combine(_currentDir, "linearAccel.csv");
        private static string _rotationVectorFile = System.IO.Path.Combine(_currentDir, "rotation.csv");
        private static string _motusFile = System.IO.Path.Combine(_currentDir, "motus.csv");
        private static string _pose6DOFFile = System.IO.Path.Combine(_currentDir, "pose6DOF.csv");
        private static string _stepFile = System.IO.Path.Combine(_currentDir, "step.csv");

        public static bool PrintToConsole { get; set; }
        
        public static void CreateRawDataFiles()
        {
            try
            {
                string tStamp = "time stamp,";
                string xyzHeader = "x,y,z";
                string quatHeader = "x,y,z,w";
                string motusHeader = "sample,pad0,pad1,pad2,pad3,pad4,pad5,pad6,pad7,pad8";

                System.IO.File.WriteAllText(_accelFile, tStamp + xyzHeader);
                System.IO.File.WriteAllText(_gyroFile, tStamp + xyzHeader);
                System.IO.File.WriteAllText(_linearAccelFile, tStamp + xyzHeader);
                System.IO.File.WriteAllText(_rotationVectorFile, tStamp + quatHeader);
                System.IO.File.WriteAllText(_motusFile, motusHeader);
                System.IO.File.WriteAllText(_pose6DOFFile, tStamp + quatHeader + "," + xyzHeader + "," + quatHeader + "," + xyzHeader + ",sample");
                System.IO.File.WriteAllText(_stepFile, tStamp + "steps");
            }
            catch (System.IO.IOException) { }
            catch (Exception) { }
        }
        
        public static void SetLogData(byte[] rawData)
        {
            DataQueue queue = new DataQueue(256);
            queue.ParseStreamable(rawData, rawData.Length);

            while (!queue.IsEmpty())
            {
                DataPacket packet = queue.Get();
                if (packet.Type != ValidPacketTypes.motus_1_raw_data_packet)
                {
                    int numFloats = packet.ExpectedLen - AndroidSensor.NUM_BYTES_PER_LONG;
                    int numVals = numFloats / AndroidSensor.NUM_BYTES_PER_FLOAT;
                    AndroidDataPacket aPacket = new AndroidDataPacket(packet.Type, numVals, packet.Payload);
                    switch (aPacket.Type)
                    {
                        case ValidPacketTypes.accelerometer_raw_data_packet:
                            LogValues(aPacket.ToString(), _accelFile);
                            break;
                        case ValidPacketTypes.gyro_raw_data_packet:
                            LogValues(aPacket.ToString(), _gyroFile);
                            break;
                        case ValidPacketTypes.linear_acceleration_raw_data_packet:
                            LogValues(aPacket.ToString(), _linearAccelFile);
                            break;
                        case ValidPacketTypes.pose_6DOF_raw_data_packet:
                            LogValues(aPacket.ToString(), _pose6DOFFile);
                            break;
                        case ValidPacketTypes.rotation_vector_raw_data_packet:
                            LogValues(aPacket.ToString(), _rotationVectorFile);
                            break;
                        case ValidPacketTypes.step_detector_raw_data_packet:
                            LogValues(aPacket.ToString(), _stepFile);
                            break;
                    }
                }
            }
        }

        private static void LogValues(string msg, string path)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(path, true))
            {
                try
                {
                    file.WriteLine(msg);
                    if (PrintToConsole)
                        Console.WriteLine(path + ":" + msg);
                }
                catch (Exception) { }
            }
        }
    }
}
