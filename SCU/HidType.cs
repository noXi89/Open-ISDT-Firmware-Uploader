using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SCU
{
	public class HidType
	{
		public struct SP_DEVICE_INTERFACE_DATA
		{
			public int cbSize;

			public Guid interfaceClassGuid;

			public int flags;

			public int reserved;
		}

		[StructLayout(LayoutKind.Sequential)]
		public class SP_DEVINFO_DATA
		{
			public int cbSize = Marshal.SizeOf(typeof(SP_DEVINFO_DATA));

			public Guid classGuid = Guid.Empty;

			public int devInst = 0;

			public int reserved = 0;
		}

		public struct HIDD_ATTRIBUTES
		{
			public int Size;

			public ushort VendorID;

			public ushort ProductID;

			public ushort VersionNumber;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 2)]
		internal struct SP_DEVICE_INTERFACE_DETAIL_DATA
		{
			internal int cbSize;

			internal short devicePath;
		}

		public enum DIGCF
		{
			DIGCF_DEFAULT = 1,
			DIGCF_PRESENT = 2,
			DIGCF_ALLCLASSES = 4,
			DIGCF_PROFILE = 8,
			DIGCF_DEVICEINTERFACE = 0x10
		}

		public struct HIDP_CAPS
		{
			public ushort Usage;

			public ushort UsagePage;

			public ushort InputReportByteLength;

			public ushort OutputReportByteLength;

			public ushort FeatureReportByteLength;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
			public ushort[] Reserved;

			public ushort NumberLinkCollectionNodes;

			public ushort NumberInputButtonCaps;

			public ushort NumberInputValueCaps;

			public ushort NumberInputDataIndices;

			public ushort NumberOutputButtonCaps;

			public ushort NumberOutputValueCaps;

			public ushort NumberOutputDataIndices;

			public ushort NumberFeatureButtonCaps;

			public ushort NumberFeatureValueCaps;

			public ushort NumberFeatureDataIndices;
		}

		private const int MAX_USB_DEVICES = 64;

		public IntPtr INVALID_HANDLER = new IntPtr(-1);

		public static long TEST_VID = 10473L;

		public static long TEST_PID = 650L;

		public const int OUT_REPORT_ID = 1;

		public const int IN_REPORT_ID = 2;

		public IntPtr HidHandle = new IntPtr(-1);

		public const uint FILE_FLAG_WRITE_THROUGH = 2147483648u;

		public const uint FILE_FLAG_OVERLAPPED = 1073741824u;

		public const uint FILE_FLAG_NO_BUFFERING = 536870912u;

		public const uint FILE_FLAG_RANDOM_ACCESS = 268435456u;

		public const uint FILE_FLAG_SEQUENTIAL_SCAN = 134217728u;

		public const uint FILE_FLAG_DELETE_ON_CLOSE = 67108864u;

		public const uint FILE_FLAG_BACKUP_SEMANTICS = 33554432u;

		public const uint FILE_FLAG_POSIX_SEMANTICS = 16777216u;

		public const uint FILE_FLAG_OPEN_REPARSE_POINT = 2097152u;

		public const uint FILE_FLAG_OPEN_NO_RECALL = 1048576u;

		public const uint FILE_FLAG_FIRST_PIPE_INSTANCE = 524288u;

		public const uint GENERIC_READ = 2147483648u;

		public const uint GENERIC_WRITE = 1073741824u;

		public const uint FILE_SHARE_READ = 1u;

		public const uint FILE_SHARE_WRITE = 2u;

		public const uint MAXIMUM_ALLOWED = 33554432u;

		public const int OPEN_EXISTING = 3;

		private HIDD_ATTRIBUTES HidAttr;

		private int InputReportLength;

		private int OutputReportLength;

		private string Serial;

		[DllImport("hid.dll")]
		public static extern uint HidP_GetCaps(IntPtr PreparsedData, out HIDP_CAPS Capabilities);

		[DllImport("hid.dll")]
		public static extern bool HidD_GetInputReport(IntPtr handle, out byte[] buf, int len);

		[DllImport("hid.dll")]
		public static extern bool HidD_GetSerialNumberString(IntPtr hidDeviceObject, IntPtr buffer, int bufferLength);

		[DllImport("hid.dll")]
		public static extern void HidD_GetHidGuid(ref Guid HidGuid);

		[DllImport("setupapi.dll", SetLastError = true)]
		public static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, uint Enumerator, IntPtr HwndParent, DIGCF Flags);

		[DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern bool SetupDiEnumDeviceInterfaces(IntPtr hDevInfo, IntPtr devInfo, ref Guid interfaceClassGuid, uint memberIndex, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

		[DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr deviceInfoSet, ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData, IntPtr deviceInterfaceDetailData, int deviceInterfaceDetailDataSize, ref int requiredSize, SP_DEVINFO_DATA deviceInfoData);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, uint lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, uint hTemplateFile);

		[DllImport("Kernel32.dll", SetLastError = true)]
		public static extern bool ReadFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToRead, ref uint lpNumberOfBytesRead, IntPtr lpOverlapped);

		[DllImport("hid.dll")]
		public static extern bool HidD_FreePreparsedData(ref IntPtr PreparsedData);

		[DllImport("kernel32.dll")]
		private static extern int CloseHandle(IntPtr hObject);

		[DllImport("Kernel32.dll")]
		public static extern int FormatMessage(int flag, ref IntPtr source, int msgid, int langid, ref string buf, int size, ref IntPtr args);

		public static string GetSysErrMsg(int errCode)
		{
			IntPtr source = IntPtr.Zero;
			string buf = null;
			FormatMessage(4864, ref source, errCode, 0, ref buf, 255, ref source);
			return buf;
		}

		public bool CT_CreateFile(string DeviceName)
		{
			HidHandle = CreateFile(DeviceName, 3221225472u, 0u, 0u, 3u, 1073741824u, 0u);
			if (HidHandle == INVALID_HANDLER)
			{
				return false;
			}
			return true;
		}

		[DllImport("hid.dll", SetLastError = true)]
		public static extern bool HidD_GetPreparsedData(IntPtr hObject, out IntPtr PreparsedData);

		[DllImport("setupapi.dll", SetLastError = true)]
		internal static extern IntPtr SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

		[DllImport("hid.dll")]
		public static extern bool HidD_GetAttributes(IntPtr hidDeviceObject, out HIDD_ATTRIBUTES attributes);

		public static void GetHidDeviceList(ref List<string> deviceList)
		{
			Guid HidGuid = Guid.Empty;
			uint num = 0u;
			deviceList.Clear();
			HidD_GetHidGuid(ref HidGuid);
			IntPtr intPtr = SetupDiGetClassDevs(ref HidGuid, 0u, IntPtr.Zero, (DIGCF)18);
			if (intPtr != IntPtr.Zero)
			{
				SP_DEVICE_INTERFACE_DATA deviceInterfaceData = default(SP_DEVICE_INTERFACE_DATA);
				deviceInterfaceData.cbSize = Marshal.SizeOf(deviceInterfaceData);
				for (num = 0u; num < 64; num++)
				{
					if (SetupDiEnumDeviceInterfaces(intPtr, IntPtr.Zero, ref HidGuid, num, ref deviceInterfaceData))
					{
						int requiredSize = 0;
						SetupDiGetDeviceInterfaceDetail(intPtr, ref deviceInterfaceData, IntPtr.Zero, requiredSize, ref requiredSize, null);
						IntPtr intPtr2 = Marshal.AllocHGlobal(requiredSize);
						SP_DEVICE_INTERFACE_DETAIL_DATA structure = default(SP_DEVICE_INTERFACE_DETAIL_DATA);
						structure.cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DETAIL_DATA));
						Marshal.StructureToPtr(structure, intPtr2, fDeleteOld: false);
						if (SetupDiGetDeviceInterfaceDetail(intPtr, ref deviceInterfaceData, intPtr2, requiredSize, ref requiredSize, null))
						{
							deviceList.Add(Marshal.PtrToStringAuto((IntPtr)((int)intPtr2 + 4)));
						}
						Marshal.FreeHGlobal(intPtr2);
					}
				}
			}
			SetupDiDestroyDeviceInfoList(intPtr);
		}

		public void setHidAttr(HIDD_ATTRIBUTES hid_attr)
		{
			HidAttr = hid_attr;
		}

		public void setInputReportLength(int input_report_length)
		{
			InputReportLength = input_report_length;
		}

		public void setOutputReportLength(int output_report_length)
		{
			OutputReportLength = output_report_length;
		}

		public void setSerial(string serial)
		{
			Serial = serial;
		}

		public HIDD_ATTRIBUTES getHidAttr()
		{
			return HidAttr;
		}

		public int getInputReportLength()
		{
			return InputReportLength;
		}

		public int getOutputReportLength()
		{
			return OutputReportLength;
		}

		public string getSerial()
		{
			return Serial;
		}

		public void close()
		{
			if (HidHandle != INVALID_HANDLER)
			{
				CloseHandle(HidHandle);
				HidHandle = INVALID_HANDLER;
			}
		}
	}
}
