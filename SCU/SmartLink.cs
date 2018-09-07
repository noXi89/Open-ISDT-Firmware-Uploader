using System;
using System.Runtime.InteropServices;

namespace SCU
{
	internal class SmartLink
	{
		public struct TFlashInfoTable
		{
			public uint AppDataCheck;

			public byte DeviceID0;

			public byte DeviceID1;

			public byte DeviceID2;

			public byte DeviceID3;

			public byte DeviceID4;

			public byte DeviceID5;

			public byte DeviceID6;

			public byte DeviceID7;

			public byte HwMajorVersion;

			public byte HwMinorVersion;

			public byte HwRevisionVersion;

			public byte HwLayoutNumber;

			public byte SwMajorVersion;

			public byte SwMinorVersion;

			public byte SwRevisionVersion;

			public byte SwBuildNumber;

			public uint AppStartAddress;

			public uint AppSize;
		}

		public struct TFileHeadInfo
		{
			public uint EncryptionKey;

			public uint FileCheckSum;

			public uint AppStorageOffset;

			public uint DataStorageOffset;

			public uint AppSize;

			public uint DataSize;

			public uint OriginalBaudRate;

			public uint RapidBaudRate;
		}

		protected enum CommCmd
		{
			TestLinker,
			QueryDevice,
			AppUpdate,
			EraseApp,
			WriteApp,
			AppCheckSum,
			RebootToApp,
			WriteAppData,
			AppDataCheckSum,
			DeviceRename
		}

		private enum TRxBytePrsSta
		{
			rbpsWaitSync,
			rbpsWaitAddres,
			rbpsWaitLength,
			rbpsWaitData,
			rbpsWaitChkSum
		}

		public enum CommState
		{
			csStart,
			csErr
		}

		private const byte PC_ADDR = 1;

		private const byte SLAVER_ADDR = 2;

		private const byte TargetMcuId = 0;

		private const int BUFFER_SIZE = 512;

		public byte[] renameArray = new byte[8];

		protected byte[] TxBuffer = new byte[512];

		protected bool SmLnkPkgMsg = false;

		protected CommCmd curCmd = CommCmd.TestLinker;

		public CommState commState;

		public int TxLen = 0;

		public int TxPos = 0;

		private byte RxDataCount;

		private byte SyncByteCount = 0;

		private byte FrameAddress;

		private byte RxFrameChkSum;

		private byte RxFrameLength;

		private byte RxFrameDataP;

		private TRxBytePrsSta RxFrameStatus = TRxBytePrsSta.rbpsWaitSync;

		public const byte PAGE_SIZE = 128;

		public uint ExFlashUpdateWriteAddress;

		public uint AppUpdateWriteAddress;

		public byte[] UpdateFileBuf;

		public TFileHeadInfo FileHeadInfo;

		public TFlashInfoTable AppInfoTable;

		public byte[] RxBuffer
		{
			get;
		} = new byte[512];


		protected virtual bool Open(string dev_path)
		{
			return false;
		}

		public virtual void Close()
		{
		}

		protected virtual void WriteMsg()
		{
		}

		public virtual bool SearchIsdtDevice()
		{
			return false;
		}

		protected virtual void WaiteForDeviceAck(int timeout)
		{
		}

		public bool TestLinkerCmd()
		{
			WriteTxFrame(CommCmd.TestLinker);
			WaiteForDeviceAck(100);
			if (RxBuffer[0] == 1)
			{
				return true;
			}
			return false;
		}

		public bool QueryDeviceInfoCmd()
		{
			WriteTxFrame(CommCmd.QueryDevice);
			WaiteForDeviceAck(5000);
			if (RxBuffer[0] == 225)
			{
				return true;
			}
			return false;
		}

		public bool AppUpdateCmd()
		{
			WriteTxFrame(CommCmd.AppUpdate);
			WaiteForDeviceAck(5000);
			if (RxBuffer[0] == 241)
			{
				return true;
			}
			return false;
		}

		public bool EraseAppCmd()
		{
			WriteTxFrame(CommCmd.EraseApp);
			WaiteForDeviceAck(5000);
			if (RxBuffer[0] == 243)
			{
				return true;
			}
			return false;
		}

		public bool WriteAppCmd()
		{
			WriteTxFrame(CommCmd.WriteApp);
			WaiteForDeviceAck(5000);
			while (RxBuffer[0] == 245)
			{
				if (RxBuffer[6] == 0)
				{
					return true;
				}
				if (RxBuffer[6] != 1)
				{
					return false;
				}
				if (TxPos < TxLen)
				{
					WriteMsg();
				}
				else
				{
					WriteTxFrame(CommCmd.WriteApp);
				}
				WaiteForDeviceAck(5000);
			}
			return false;
		}

		public bool AppCheckSumCmd()
		{
			SmLnkPkgMsg = false;
			WriteTxFrame(CommCmd.AppCheckSum);
			WaiteForDeviceAck(5000);
			if (RxBuffer[0] == 247)
			{
				return true;
			}
			return false;
		}

		public bool WriteAppDataCmd()
		{
			WriteTxFrame(CommCmd.WriteAppData);
			WaiteForDeviceAck(5000);
			while (RxBuffer[0] == 32)
			{
				if (RxBuffer[1] == 5)
				{
					return true;
				}
				if (RxBuffer[1] != 4)
				{
					return false;
				}
				if (TxPos < TxLen)
				{
					WriteMsg();
				}
				else
				{
					WriteTxFrame(CommCmd.WriteAppData);
				}
				WaiteForDeviceAck(5000);
			}
			return false;
		}

		public bool AppDataCheckSumCmd()
		{
			WriteTxFrame(CommCmd.AppDataCheckSum);
			WaiteForDeviceAck(5000);
			if (RxBuffer[0] == 32)
			{
				return true;
			}
			return false;
		}

		public bool RebootToAppCmd()
		{
			WriteTxFrame(CommCmd.RebootToApp);
			WaiteForDeviceAck(5000);
			if (RxBuffer[0] == 253)
			{
				return true;
			}
			return false;
		}

		public bool RenameDeviceCmd()
		{
			WriteTxFrame(CommCmd.DeviceRename);
			WaiteForDeviceAck(5000);
			if (RxBuffer[0] == 193)
			{
				return true;
			}
			return false;
		}

		protected void WriteTxFrame(CommCmd cmd)
		{
			byte[] array = new byte[512];
			curCmd = cmd;
			switch (cmd)
			{
			case CommCmd.TestLinker:
				array[0] = 0;
				TxLen = ConstructTxDataFrame(array, 1);
				break;
			case CommCmd.QueryDevice:
				array[0] = 224;
				TxLen = ConstructTxDataFrame(array, 1);
				break;
			case CommCmd.AppUpdate:
				array[0] = 240;
				array[1] = 172;
				TxLen = ConstructTxDataFrame(array, 2);
				break;
			case CommCmd.EraseApp:
				array[0] = 242;
				array[1] = 0;
				array[2] = (byte)(AppInfoTable.AppStartAddress & 0xFF);
				array[3] = (byte)((AppInfoTable.AppStartAddress >> 8) & 0xFF);
				array[4] = (byte)((AppInfoTable.AppStartAddress >> 16) & 0xFF);
				array[5] = (byte)((AppInfoTable.AppStartAddress >> 24) & 0xFF);
				array[6] = (byte)(AppInfoTable.AppSize & 0xFF);
				array[7] = (byte)((AppInfoTable.AppSize >> 8) & 0xFF);
				array[8] = (byte)((AppInfoTable.AppSize >> 16) & 0xFF);
				array[9] = (byte)((AppInfoTable.AppSize >> 24) & 0xFF);
				TxLen = ConstructTxDataFrame(array, 10);
				break;
			case CommCmd.WriteApp:
				array[0] = 244;
				array[1] = 0;
				array[2] = (byte)(AppUpdateWriteAddress & 0xFF);
				array[3] = (byte)((AppUpdateWriteAddress >> 8) & 0xFF);
				array[4] = (byte)((AppUpdateWriteAddress >> 16) & 0xFF);
				array[5] = (byte)((AppUpdateWriteAddress >> 24) & 0xFF);
				for (int n = 0; n < 128; n++)
				{
					array[6 + n] = UpdateFileBuf[(uint)((int)(AppUpdateWriteAddress - FileHeadInfo.AppStorageOffset) + Marshal.SizeOf(typeof(TFileHeadInfo))) + n];
				}
				TxLen = ConstructTxDataFrame(array, 134);
				break;
			case CommCmd.WriteAppData:
				array[0] = 32;
				array[1] = 5;
				array[2] = 0;
				array[3] = 0;
				array[4] = 0;
				array[5] = (byte)(ExFlashUpdateWriteAddress & 0xFF);
				array[6] = (byte)((ExFlashUpdateWriteAddress >> 8) & 0xFF);
				array[7] = (byte)((ExFlashUpdateWriteAddress >> 16) & 0xFF);
				array[8] = (byte)((ExFlashUpdateWriteAddress >> 24) & 0xFF);
				array[9] = 128;
				array[10] = 0;
				array[11] = 0;
				array[12] = 0;
				for (int j = 0; j < 16; j++)
				{
					array[13 + j] = 0;
				}
				for (int k = 0; k < 128; k++)
				{
					array[29 + k] = UpdateFileBuf[ExFlashUpdateWriteAddress - FileHeadInfo.DataStorageOffset + k + FileHeadInfo.AppSize + (uint)Marshal.SizeOf(typeof(TFileHeadInfo))];
				}
				TxLen = ConstructTxDataFrame(array, 157);
				break;
			case CommCmd.AppDataCheckSum:
			{
				uint num2 = 0u;
				array[0] = 32;
				array[1] = 6;
				array[2] = 0;
				array[3] = 0;
				array[4] = 0;
				array[5] = (byte)(FileHeadInfo.DataStorageOffset & 0xFF);
				array[6] = (byte)((FileHeadInfo.DataStorageOffset >> 8) & 0xFF);
				array[7] = (byte)((FileHeadInfo.DataStorageOffset >> 16) & 0xFF);
				array[8] = (byte)((FileHeadInfo.DataStorageOffset >> 24) & 0xFF);
				array[9] = (byte)(FileHeadInfo.DataSize & 0xFF);
				array[10] = (byte)((FileHeadInfo.DataSize >> 8) & 0xFF);
				array[11] = (byte)((FileHeadInfo.DataSize >> 16) & 0xFF);
				array[12] = (byte)((FileHeadInfo.DataSize >> 24) & 0xFF);
				for (int m = (int)FileHeadInfo.AppSize + Marshal.SizeOf(typeof(TFileHeadInfo)); m < (int)FileHeadInfo.AppSize + Marshal.SizeOf(typeof(TFileHeadInfo)) + FileHeadInfo.DataSize; m++)
				{
					num2 += UpdateFileBuf[m];
				}
				array[13] = (byte)(num2 & 0xFF);
				array[14] = (byte)((num2 >> 8) & 0xFF);
				array[15] = (byte)((num2 >> 16) & 0xFF);
				array[16] = (byte)((num2 >> 24) & 0xFF);
				TxLen = ConstructTxDataFrame(array, 17);
				break;
			}
			case CommCmd.AppCheckSum:
			{
				uint num = 0u;
				array[0] = 246;
				array[1] = 53;
				array[2] = 0;
				array[3] = (byte)(FileHeadInfo.AppStorageOffset & 0xFF);
				array[4] = (byte)((FileHeadInfo.AppStorageOffset >> 8) & 0xFF);
				array[5] = (byte)((FileHeadInfo.AppStorageOffset >> 16) & 0xFF);
				array[6] = (byte)((FileHeadInfo.AppStorageOffset >> 24) & 0xFF);
				array[7] = (byte)(FileHeadInfo.AppSize & 0xFF);
				array[8] = (byte)((FileHeadInfo.AppSize >> 8) & 0xFF);
				array[9] = (byte)((FileHeadInfo.AppSize >> 16) & 0xFF);
				array[10] = (byte)((FileHeadInfo.AppSize >> 24) & 0xFF);
				for (int l = Marshal.SizeOf(typeof(TFileHeadInfo)); l < (int)FileHeadInfo.AppSize + Marshal.SizeOf(typeof(TFileHeadInfo)); l += 4)
				{
					num += BitConverter.ToUInt32(UpdateFileBuf, l);
				}
				array[11] = (byte)(num & 0xFF);
				array[12] = (byte)((num >> 8) & 0xFF);
				array[13] = (byte)((num >> 16) & 0xFF);
				array[14] = (byte)((num >> 24) & 0xFF);
				TxLen = ConstructTxDataFrame(array, 15);
				break;
			}
			case CommCmd.RebootToApp:
				array[0] = 252;
				array[1] = 202;
				TxLen = ConstructTxDataFrame(array, 2);
				break;
			case CommCmd.DeviceRename:
				array[0] = 192;
				for (int i = 0; i < renameArray.Length; i++)
				{
					array[i + 1] = renameArray[i];
				}
				TxLen = ConstructTxDataFrame(array, 9);
				break;
			default:
				TxLen = 0;
				break;
			}
			if (TxLen != 0)
			{
				SendDataFrame();
			}
		}

		protected void SendDataFrame()
		{
			TxPos = 0;
			WriteMsg();
		}

		private int ConstructTxDataFrame(byte[] Buffer, byte size)
		{
			int num = 0;
			int num2 = 0;
			byte b = 0;
			TxBuffer[num2++] = 170;
			TxBuffer[num2++] = 18;
			TxBuffer[num2++] = size;
			if (size == 170)
			{
				TxBuffer[num2++] = 170;
			}
			b = (byte)(b + 18);
			b = (byte)(b + size);
			for (num = 0; num < size; num++)
			{
				TxBuffer[num2++] = Buffer[num];
				b = (byte)(b + Buffer[num]);
				if (Buffer[num] == 170)
				{
					TxBuffer[num2++] = 170;
				}
			}
			TxBuffer[num2++] = b;
			if (b == 170)
			{
				TxBuffer[num2++] = 170;
			}
			return num2;
		}

		protected void RxDataProcess(byte RxByte)
		{
			if (RxByte == 170)
			{
				SyncByteCount++;
				if ((SyncByteCount & 1) == 1)
				{
					return;
				}
			}
			else
			{
				if ((SyncByteCount & 1) == 1)
				{
					RxFrameStatus = TRxBytePrsSta.rbpsWaitAddres;
				}
				SyncByteCount = 0;
			}
			switch (RxFrameStatus)
			{
			case TRxBytePrsSta.rbpsWaitAddres:
				FrameAddress = RxByte;
				if (33 == FrameAddress)
				{
					RxFrameStatus = TRxBytePrsSta.rbpsWaitLength;
					RxFrameChkSum = RxByte;
				}
				else
				{
					RxFrameStatus = TRxBytePrsSta.rbpsWaitSync;
				}
				break;
			case TRxBytePrsSta.rbpsWaitLength:
				RxFrameDataP = 0;
				RxFrameLength = RxByte;
				RxDataCount = RxByte;
				RxFrameChkSum += RxByte;
				RxFrameStatus = TRxBytePrsSta.rbpsWaitData;
				break;
			case TRxBytePrsSta.rbpsWaitData:
				RxBuffer[RxFrameDataP++] = RxByte;
				RxFrameChkSum += RxByte;
				if (--RxDataCount == 0)
				{
					RxFrameStatus = TRxBytePrsSta.rbpsWaitChkSum;
				}
				break;
			case TRxBytePrsSta.rbpsWaitChkSum:
				if (RxByte == RxFrameChkSum)
				{
					SmLnkPkgMsg = true;
				}
				RxFrameStatus = TRxBytePrsSta.rbpsWaitSync;
				break;
			}
		}
	}
}
