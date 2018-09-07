using System;
using System.IO.Ports;

namespace SCU
{
	internal class UartLink : SmartLink
	{
		private SerialPort mySerialPort = new SerialPort();

		protected override bool Open(string port)
		{
			Close();
			mySerialPort.PortName = port;
			mySerialPort.BaudRate = 115200;
			mySerialPort.DataBits = 8;
			mySerialPort.StopBits = StopBits.One;
			mySerialPort.Parity = Parity.None;
			mySerialPort.ReadTimeout = 100;
			mySerialPort.WriteTimeout = 100;
			mySerialPort.ReadBufferSize = 4096;
			mySerialPort.WriteBufferSize = 4096;
			mySerialPort.Open();
			return true;
		}

		public override void Close()
		{
			if (mySerialPort.IsOpen)
			{
				mySerialPort.Close();
			}
		}

		public override bool SearchIsdtDevice()
		{
			string[] portNames = SerialPort.GetPortNames();
			if (portNames != null && portNames.Length != 0)
			{
				Array.Sort(portNames);
				string[] array = portNames;
				foreach (string dev_path in array)
				{
					try
					{
						if (Open(dev_path) && TestLinkerCmd())
						{
							return true;
						}
					}
					catch
					{
					}
				}
			}
			return false;
		}

		protected override void WriteMsg()
		{
			if (mySerialPort != null && mySerialPort.IsOpen)
			{
				SmLnkPkgMsg = false;
				commState = CommState.csStart;
				mySerialPort.Write(TxBuffer, 0, TxLen);
			}
		}

		protected override void WaiteForDeviceAck(int timeout)
		{
			byte b = 0;
			int num = 0;
			while (true)
			{
				try
				{
					int num2 = mySerialPort.ReadByte();
					if (num2 != -1)
					{
						RxDataProcess((byte)num2);
					}
				}
				catch (TimeoutException)
				{
					num += mySerialPort.ReadTimeout;
					if (timeout <= num)
					{
						if ((b = (byte)(b + 1)) > 1)
						{
							throw new Exception("Communication timeout.");
						}
						num = 0;
						SendDataFrame();
					}
				}
				if (commState == CommState.csErr)
				{
					break;
				}
				if (SmLnkPkgMsg)
				{
					return;
				}
			}
			throw new Exception("Communication error.");
		}
	}
}
