using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace USBCheck.Common
{
    public class Win32Helper
    {
        #region WndProcでUSBの抜き差しを検出するAPI
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr NotificationFilter, uint Flags);

        public const int WM_DEVICECHANGE = 0x0219;
        public const int DBT_DEVICEARRIVAL = 0x8000; // system detected a new device
        public const int DBT_DEVICEREMOVECOMPLETE = 0x8004; //device was removed

        [StructLayout(LayoutKind.Sequential)]
        public struct DevBroadcastDeviceinterface
        {
            internal int Size;
            internal int DeviceType;
            internal int Reserved;
            internal Guid ClassGuid;
            internal short Name;
        }

        [DllImport("user32.dll")]
        public static extern bool UnregisterDeviceNotification(IntPtr handle);

        public const int DbtDevtypDeviceinterface = 5;
        public const string GUID_DEVINTERFACE_USB_DEVICE = "A5DCBF10-6530-11D2-901F-00C04FB951ED";
        public static Guid GuidDevinterfaceUSBDevice = new Guid(GUID_DEVINTERFACE_USB_DEVICE); // USB devices
        #endregion

        /// <summary>
        /// ISBデバイスのDescriptionの一覧を作成する
        /// </summary>
        public static IList<string> MakeDeviceList()
        {
            List<string> list = new List<string>();

            Guid guid = Guid.Empty;
            IntPtr DevInfoHandle = IntPtr.Zero;

            //DevInfoHandle = SetupDiGetClassDevs(
            //                         IntPtr.Zero, null, IntPtr.Zero,
            //                         DIGCF_ALLCLASSES | DIGCF_PRESENT);

            // USBデバイスだけ列挙
            DevInfoHandle = SetupDiGetClassDevs(
                            ref GuidDevinterfaceUSBDevice, IntPtr.Zero, IntPtr.Zero,
                            DIGCF_DEVICEINTERFACE | DIGCF_PRESENT);

            SP_DEVINFO_DATA DevInfoData = new SP_DEVINFO_DATA
            {
                classGuid = Guid.Empty,
                devInst = 0,
                reserved = IntPtr.Zero
            };

            DevInfoData.cbSize = (uint)Marshal.SizeOf(DevInfoData);// 32, // 28 When 32-Bit, 32 When 64-Bit,

            for (uint i = 0; SetupDiEnumDeviceInfo(DevInfoHandle, i, ref DevInfoData); i++)
            {
                string str = GetStringPropertyForDevice(
                    DevInfoHandle,
                    DevInfoData,
                    (uint)SetupDiGetDeviceRegistryPropertyEnum.SPDRP_DEVICEDESC);

                if (str != null)
                    list.Add(str.Replace("\0", ""));
                else
                    str = "(null)";

                // I-O DATA GV-USB2t.gvusb2.name%;I-O DATA GV-USB2
                Debug.WriteLine(str.Replace("\0", ""));
            }
            SetupDiDestroyDeviceInfoList(DevInfoHandle);

            return list;
        }

        #region SetupDiEnumDeviceInfoで列挙するAPI

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetupDiGetDeviceRegistryProperty(
            IntPtr deviceInfoSet,
            ref SP_DEVINFO_DATA deviceInfoData,
            uint property,
            out UInt32 propertyRegDataType,
            byte[] propertyBuffer,
            uint propertyBufferSize,
            out UInt32 requiredSize
            );

        [StructLayout(LayoutKind.Sequential)]
        struct SP_DEVINFO_DATA
        {
            public uint cbSize;
            public Guid classGuid;
            public uint devInst;
            public IntPtr reserved;
        }

        /// <summary>
        /// Flags for SetupDiGetDeviceRegistryProperty().
        /// </summary>
        public enum SetupDiGetDeviceRegistryPropertyEnum : uint
        {
            SPDRP_DEVICEDESC = 0x00000000, // DeviceDesc (R/W)
            SPDRP_HARDWAREID = 0x00000001, // HardwareID (R/W)
            SPDRP_COMPATIBLEIDS = 0x00000002, // CompatibleIDs (R/W)
            SPDRP_UNUSED0 = 0x00000003, // unused
            SPDRP_SERVICE = 0x00000004, // Service (R/W)
            SPDRP_UNUSED1 = 0x00000005, // unused
            SPDRP_UNUSED2 = 0x00000006, // unused
            SPDRP_CLASS = 0x00000007, // Class (R--tied to ClassGUID)
            SPDRP_CLASSGUID = 0x00000008, // ClassGUID (R/W)
            SPDRP_DRIVER = 0x00000009, // Driver (R/W)
            SPDRP_CONFIGFLAGS = 0x0000000A, // ConfigFlags (R/W)
            SPDRP_MFG = 0x0000000B, // Mfg (R/W)
            SPDRP_FRIENDLYNAME = 0x0000000C, // FriendlyName (R/W)
            SPDRP_LOCATION_INFORMATION = 0x0000000D, // LocationInformation (R/W)
            SPDRP_PHYSICAL_DEVICE_OBJECT_NAME = 0x0000000E, // PhysicalDeviceObjectName (R)
            SPDRP_CAPABILITIES = 0x0000000F, // Capabilities (R)
            SPDRP_UI_NUMBER = 0x00000010, // UiNumber (R)
            SPDRP_UPPERFILTERS = 0x00000011, // UpperFilters (R/W)
            SPDRP_LOWERFILTERS = 0x00000012, // LowerFilters (R/W)
            SPDRP_BUSTYPEGUID = 0x00000013, // BusTypeGUID (R)
            SPDRP_LEGACYBUSTYPE = 0x00000014, // LegacyBusType (R)
            SPDRP_BUSNUMBER = 0x00000015, // BusNumber (R)
            SPDRP_ENUMERATOR_NAME = 0x00000016, // Enumerator Name (R)
            SPDRP_SECURITY = 0x00000017, // Security (R/W, binary form)
            SPDRP_SECURITY_SDS = 0x00000018, // Security (W, SDS form)
            SPDRP_DEVTYPE = 0x00000019, // Device Type (R/W)
            SPDRP_EXCLUSIVE = 0x0000001A, // Device is exclusive-access (R/W)
            SPDRP_CHARACTERISTICS = 0x0000001B, // Device Characteristics (R/W)
            SPDRP_ADDRESS = 0x0000001C, // Device Address (R)
            SPDRP_UI_NUMBER_DESC_FORMAT = 0X0000001D, // UiNumberDescFormat (R/W)
            SPDRP_DEVICE_POWER_DATA = 0x0000001E, // Device Power Data (R)
            SPDRP_REMOVAL_POLICY = 0x0000001F, // Removal Policy (R)
            SPDRP_REMOVAL_POLICY_HW_DEFAULT = 0x00000020, // Hardware Removal Policy (R)
            SPDRP_REMOVAL_POLICY_OVERRIDE = 0x00000021, // Removal Policy Override (RW)
            SPDRP_INSTALL_STATE = 0x00000022, // Device Install State (R)
            SPDRP_LOCATION_PATHS = 0x00000023, // Device Location Paths (R)
            SPDRP_BASE_CONTAINERID = 0x00000024  // Base ContainerID (R)
        }

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SetupDiGetClassDevs(           // 1st form using a ClassGUID only, with null Enumerator
           ref Guid ClassGuid,
           IntPtr Enumerator,
           IntPtr hwndParent,
           int Flags
        );

        const int DIGCF_PRESENT = 0x2;
        const int DIGCF_ALLCLASSES = 0x4;
        const int DIGCF_DEVICEINTERFACE = 0x10;

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, uint MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiDestroyDeviceInfoList
        (
             IntPtr DeviceInfoSet
        );

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]     // 2nd form uses an Enumerator only, with null ClassGUID 
        static extern IntPtr SetupDiGetClassDevs(
           IntPtr ClassGuid,
           string Enumerator,
           IntPtr hwndParent,
           int Flags
        );

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiGetDeviceRegistryProperty(
            IntPtr deviceInfoSet,
            ref SP_DEVINFO_DATA deviceInfoData,
            uint property,
            out UInt32 propertyRegDataType,
            IntPtr propertyBuffer, // the difference between this signature and the one above.
            uint propertyBufferSize,
            out UInt32 requiredSize
            );

        public const int ERROR_INVALID_DATA = 13;

        private static string GetStringPropertyForDevice(IntPtr info, SP_DEVINFO_DATA devdata,
            uint propId)
        {
            string res = string.Empty;

            uint proptype, outsize;
            byte[] buffer = null;
            try
            {
                uint buflen = 512;
                buffer = new byte[buflen];
                outsize = 0;
                // CHANGE #2 - Use this instead of SetupDiGetDeviceProperty 
                SetupDiGetDeviceRegistryProperty(
                    info,
                    ref devdata,
                    propId,
                    out proptype,
                    buffer,
                    buflen,
                    out outsize);
                //byte[] lbuffer = new byte[outsize];
                //Marshal.Copy(buffer, lbuffer, 0, (int)outsize);
                int errcode = Marshal.GetLastWin32Error();
                if (errcode == ERROR_INVALID_DATA) return null;

                res = System.Text.Encoding.Unicode.GetString(buffer);
            }
            finally
            {
                //if (buffer != IntPtr.Zero)
                //    Marshal.FreeHGlobal(buffer);
            }

            return res;
        }
        #endregion

    }
}
