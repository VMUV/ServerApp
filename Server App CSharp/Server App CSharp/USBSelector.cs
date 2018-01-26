using Windows.Devices.HumanInterfaceDevice;

namespace Server_App_CSharp
{
    static class USBSelector
    {
        private static ushort vendorId = 0x6969;
        private static ushort productId = 0x0002;
        private static ushort usagePage = 0x0003;
        private static ushort usage = 0x0002;

        public static string GetSelector()
        {
            return (HidDevice.GetDeviceSelector(usagePage, usage, vendorId, productId));
        }

        public static ushort GetUsagePage()
        {
            return usagePage;
        }

        public static ushort GetUsage()
        {
            return usage;
        }
    }
}
