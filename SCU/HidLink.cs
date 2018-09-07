using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace SCU
{
	internal class HidLink : SmartLink
	{
		private HidType hid = new HidType();

		private FileStream hidDevice = null;

		private byte[] inputBuff;

		private byte[] outputBuff;

		private bool isReadCompleted = false;

		private bool isWriteCompleted = false;

		private const byte DATA_PROCESS_MODE = 0;

		protected override bool Open(string dev_path)
		{
			Close();
			return hid.CT_CreateFile(dev_path);
		}

		public override void Close()
		{
			try
			{
				hid.close();
				if (hidDevice != null)
				{
					hidDevice.Close();
				}
			}
			finally
			{
				hidDevice = null;
			}
		}

		public new bool TestLinkerCmd()
		{
			WriteTxFrame(CommCmd.TestLinker);
			WaiteForDeviceAck(5000);
			if (base.RxBuffer[0] == 1)
			{
				return true;
			}
			return false;
		}

		public override bool SearchIsdtDevice()
		{
			List<string> deviceList = new List<string>();
			HidType.GetHidDeviceList(ref deviceList);
			foreach (string item in deviceList)
			{
				try
				{
					if (Open(item))
					{
						IntPtr intPtr = Marshal.AllocHGlobal(512);
						HidType.HidD_GetAttributes(hid.HidHandle, out HidType.HIDD_ATTRIBUTES attributes);
						HidType.HidD_GetSerialNumberString(hid.HidHandle, intPtr, 512);
						string serial = Marshal.PtrToStringAuto(intPtr);
						Marshal.FreeHGlobal(intPtr);
						hid.setHidAttr(attributes);
						hid.setSerial(serial);
						HidType.HidD_GetPreparsedData(hid.HidHandle, out IntPtr PreparsedData);
						HidType.HidP_GetCaps(PreparsedData, out HidType.HIDP_CAPS Capabilities);
						HidType.HidD_FreePreparsedData(ref PreparsedData);
						hid.setInputReportLength(Capabilities.InputReportByteLength);
						hid.setOutputReportLength(Capabilities.OutputReportByteLength);
						if (HidType.TEST_VID == hid.getHidAttr().VendorID && HidType.TEST_PID == hid.getHidAttr().ProductID)
						{
							hidDevice = new FileStream(new SafeFileHandle(hid.HidHandle, ownsHandle: false), FileAccess.ReadWrite, hid.getInputReportLength(), isAsync: true);
							inputBuff = new byte[hid.getInputReportLength()];
							outputBuff = new byte[hid.getOutputReportLength()];
							if (TestLinkerCmd())
							{
								return true;
							}
						}
					}
				}
				catch
				{
				}
			}
			return false;
		}

		protected override void WriteMsg()
		{
			SmLnkPkgMsg = false;
			commState = CommState.csStart;
			int i;
			for (i = 0; i < outputBuff.Length; i++)
			{
				outputBuff[i] = 0;
			}
			outputBuff[0] = 1;
			for (i = 2; i < outputBuff.Length; i++)
			{
				if (TxPos >= TxLen)
				{
					break;
				}
				outputBuff[i] = TxBuffer[TxPos++];
			}
			outputBuff[1] = (byte)(i - 2);
			try
			{
				hidDevice.Flush();
				hidDevice.BeginWrite(outputBuff, 0, outputBuff.Length, WriteCompleted, outputBuff);
			}
			catch
			{
				commState = CommState.csErr;
			}
		}

		private void WriteCompleted(IAsyncResult iResult)
		{
			isWriteCompleted = true;
			bool flag = true;
			if (isWriteCompleted)
			{
				isWriteCompleted = false;
				try
				{
					hidDevice.BeginRead(inputBuff, 0, hid.getInputReportLength(), ReadCompleted, inputBuff);
				}
				catch
				{
					commState = CommState.csErr;
				}
			}
		}

		private void ReadCompleted(IAsyncResult iResult)
		{
			isReadCompleted = true;
			bool flag = true;
			if (isReadCompleted && inputBuff[0] == 2)
			{
				isReadCompleted = false;
				for (int i = 2; i < inputBuff[1] + 2 && i < inputBuff.Length; i++)
				{
					RxDataProcess(inputBuff[i]);
				}
			}
		}

		protected override void WaiteForDeviceAck(int timeout)
		{
			byte b = 0;
			int num = 0;
			long ticks = DateTime.Now.Ticks;
			while (true)
			{
				num = (int)((DateTime.Now.Ticks - ticks) / 10000);
				if (timeout <= num)
				{
					if ((b = (byte)(b + 1)) > 1)
					{
						throw new Exception("Communication timeout.");
					}
					ticks = DateTime.Now.Ticks;
					SendDataFrame();
				}
				bool flag = false;
				Thread.Sleep(0);
				if (commState == CommState.csErr)
				{
					CommCmd curCmd = base.curCmd;
					Thread.Sleep(1000);
					if (!SearchIsdtDevice())
					{
						break;
					}
					WriteTxFrame(curCmd);
					ticks = DateTime.Now.Ticks;
				}
				if (SmLnkPkgMsg)
				{
					return;
				}
			}
			throw new Exception("Connect the device failed.");
		}
	}
}
