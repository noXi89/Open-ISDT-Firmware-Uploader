using SCU.Properties;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace SCU
{
	public class Form_Update : Form
	{
		private enum TaskType
		{
			tyCheck,
			tyUpdate,
			tyRename,
            tyDownload
        }

		private delegate void MyDelegate(TaskType type);

		private delegate void InvokeCallback_Msg(string msg, Color color);

		private delegate void InvokeCallback_Val(int val);

		private delegate void InvokeCallback_Progess(int val, int max);

		private TaskType taskType = TaskType.tyCheck;

		private const string DRIVE_FILE_PATH = "Driver\\\\ScLinkerDrv.exe";

		private const string FIMWARE_FILE_PATH = "Firmware\\\\Firmware.fwd";

		private const string FIMWARE_README_PATH = "Firmware\\\\Release Notes.rtf";

		private const uint TARGET_INFO_TAB_VECTOR_ADD = 40u;

		private const uint APP_DATA_CHECK = 2857749555u;

		private const uint APP_DATA_CHECK_NULL = uint.MaxValue;

		private bool isFirstInput = true;

		private byte languageIndex = 0;

		private Color CORRECT_COLOR = Color.Green;

		private Color ERR_COLOR = Color.Red;

		private MyDelegate myDelegate = null;

		private IAsyncResult asyncResult = null;

		private int reHeight = 0;

		private HidLink hidLink = new HidLink();

		private UartLink uartLink = new UartLink();

		private SmartLink smartLink;

		private Stopwatch sw = new Stopwatch();

		private IContainer components = null;

		private PictureBox pictureBox1;

		private Panel panel1;

		private Button button_update;

        private Button button_dl;

        private TableLayoutPanel tableLayoutPanel_updateInfo;

		private Panel panel_progress;

		private Label label_progress;

		private ProgressBar progressBar_update;

		private System.Windows.Forms.Timer timer1;

		private Panel panel_space;

		private Label label_progressInfo;

		private System.Windows.Forms.Timer timer2;

		private Panel panel_rename;

		private TableLayoutPanel tableLayoutPanel_rename;

		private Panel panel_rename_edit;

		private TextBox textBox_rename;

		private Label label1;

		private Button button_rename;

		private System.Windows.Forms.Timer timer3;

		private System.Windows.Forms.Timer timer4;

		private ContextMenuStrip contextMenuStrip_language;

		private ToolStripMenuItem ChinaToolStripMenuItem;

		private ToolStripMenuItem englishToolStripMenuItem;

		private Button button_language;

		private ToolStripMenuItem FchinaToolStripMenuItem;

		private Button button_drive;

		private Label label_updateTime;

		private System.Windows.Forms.Timer timer6;

		private Panel panel2;

		private RichTextBox richTextBox_updateInfo;

		private Label label_deviceInfo;

		public Form_Update()
		{
			InitializeComponent();
		}

		private void ResizeUpdateInfoLayout(int height)
		{
			tableLayoutPanel_updateInfo.Height -= height;
			tableLayoutPanel_updateInfo.RowStyles[2].Height -= (float)height;
			richTextBox_updateInfo.Height -= height;
			tableLayoutPanel_updateInfo.Location = new Point(tableLayoutPanel_updateInfo.Location.X, tableLayoutPanel_updateInfo.Location.Y + height);
		}

		private void ResizeRenameLayout(int height)
		{
			tableLayoutPanel_rename.Location = new Point(tableLayoutPanel_rename.Location.X, tableLayoutPanel_rename.Location.Y + height);
		}

		private void SearchIsdtDevice()
		{
			m_progressInfo_MessageEvent("Connecting the device...", CORRECT_COLOR);
			smartLink = hidLink;
			if (!smartLink.SearchIsdtDevice())
			{
				uartLink.UpdateFileBuf = hidLink.UpdateFileBuf;
				uartLink.FileHeadInfo = hidLink.FileHeadInfo;
				uartLink.AppInfoTable = hidLink.AppInfoTable;
				smartLink = uartLink;
				if (!smartLink.SearchIsdtDevice())
				{
					throw new Exception("Connect the device failed.");
				}
			}
		}

        private void DownloadFirmwareCmd()
        {
            m_progressInfo_MessageEvent("waiting for firmware share...", CORRECT_COLOR);
            smartLink.RxBuffer

        }


        private void QueryDeviceInfoCmd()
		{
			m_progressInfo_MessageEvent("Querying device information...", CORRECT_COLOR);
			if (!smartLink.QueryDeviceInfoCmd())
			{
				throw new Exception("Query device information failed.");
			}
			bool flag = false;
			if (smartLink.RxBuffer[1] != smartLink.AppInfoTable.DeviceID0)
			{
				flag = true;
			}
			if (smartLink.RxBuffer[2] != smartLink.AppInfoTable.DeviceID1)
			{
				flag = true;
			}
			if (smartLink.RxBuffer[3] != smartLink.AppInfoTable.DeviceID2)
			{
				flag = true;
			}
			if (smartLink.RxBuffer[4] != smartLink.AppInfoTable.DeviceID3)
			{
				flag = true;
			}
			if (smartLink.RxBuffer[5] != smartLink.AppInfoTable.DeviceID4)
			{
				flag = true;
			}
			if (smartLink.RxBuffer[6] != smartLink.AppInfoTable.DeviceID5)
			{
				flag = true;
			}
			if (smartLink.RxBuffer[7] != smartLink.AppInfoTable.DeviceID6)
			{
				flag = true;
			}
			if (smartLink.RxBuffer[8] != smartLink.AppInfoTable.DeviceID7)
			{
				flag = true;
			}
			if (smartLink.RxBuffer[9] != smartLink.AppInfoTable.HwMajorVersion)
			{
				flag = true;
			}
			if (smartLink.RxBuffer[10] != smartLink.AppInfoTable.HwMinorVersion)
			{
				flag = true;
			}
			string text = "Device: ";
			for (int i = 0; i < 10 && smartLink.RxBuffer[21 + i] != 0; i++)
			{
				string str = text;
				char c = (char)smartLink.RxBuffer[21 + i];
				text = str + c.ToString();
			}
			text += "  App Version: ";
			text += smartLink.RxBuffer[17];
			text += ".";
			text += smartLink.RxBuffer[18];
			text += ".";
			text += smartLink.RxBuffer[19];
			text += ".";
			text += smartLink.RxBuffer[20];
			if (false)
			{
				if (true)
				{
					uartLink.UpdateFileBuf = hidLink.UpdateFileBuf;
					uartLink.FileHeadInfo = hidLink.FileHeadInfo;
					uartLink.AppInfoTable = hidLink.AppInfoTable;
					smartLink = uartLink;
					if (smartLink.SearchIsdtDevice())
					{
						QueryDeviceInfoCmd();
						return;
					}
				}
				throw new Exception(text + " Firmware and device information does not match.");
			}
			m_comm_MessageEvent(text, CORRECT_COLOR);
		}

		private void AppUpdateCmd()
		{
			m_progressInfo_MessageEvent("Reboot to the bootloader...", CORRECT_COLOR);
			if (!smartLink.AppUpdateCmd())
			{
				throw new Exception("Reboot to the bootloader failed.");
			}
			switch (smartLink.RxBuffer[1])
			{
			default:
				Thread.Sleep(1000);
				if (!smartLink.SearchIsdtDevice())
				{
					throw new Exception("Connected the device failed.");
				}
				break;
			case 1:
				throw new Exception("Device is busy, stop current task and try again.");
			case 2:
			case 3:
				throw new Exception("Remove battery and try again.");
			case 4:
				throw new Exception("Wait for self-test completed and try again.");
			}
		}

		private void WriteAppCmd()
		{
			if (smartLink.FileHeadInfo.AppSize != 0)
			{
				m_progressInfo_MessageEvent("Writing the application...", CORRECT_COLOR);
				smartLink.AppUpdateWriteAddress = smartLink.FileHeadInfo.AppStorageOffset;
				while (smartLink.AppUpdateWriteAddress - smartLink.FileHeadInfo.AppStorageOffset < smartLink.FileHeadInfo.AppSize)
				{
					if (!smartLink.WriteAppCmd())
					{
						throw new Exception("Write application failed.");
					}
					smartLink.AppUpdateWriteAddress += 128u;
					m_comm_ProgressEvent((int)(smartLink.AppUpdateWriteAddress - smartLink.FileHeadInfo.AppStorageOffset), (int)(smartLink.FileHeadInfo.AppSize + smartLink.FileHeadInfo.DataSize));
				}
				AppCheckSumCmd();
			}
		}

		private void AppCheckSumCmd()
		{
			m_progressInfo_MessageEvent("Checking the application...", CORRECT_COLOR);
			if (!smartLink.AppCheckSumCmd() || smartLink.RxBuffer[2] != 0)
			{
				throw new Exception("Application verification is failed.");
			}
		}

		private void WriteAppDataCmd()
		{
			if (smartLink.FileHeadInfo.DataSize != 0)
			{
				m_progressInfo_MessageEvent("Updating the application data...", CORRECT_COLOR);
				smartLink.ExFlashUpdateWriteAddress = smartLink.FileHeadInfo.DataStorageOffset;
				while (smartLink.ExFlashUpdateWriteAddress - smartLink.FileHeadInfo.DataStorageOffset < smartLink.FileHeadInfo.DataSize)
				{
					if (!smartLink.WriteAppDataCmd())
					{
						throw new Exception("Update the application data failed.");
					}
					smartLink.ExFlashUpdateWriteAddress += 128u;
					m_comm_ProgressEvent((int)(smartLink.ExFlashUpdateWriteAddress - smartLink.FileHeadInfo.DataStorageOffset + smartLink.FileHeadInfo.AppSize), (int)(smartLink.FileHeadInfo.AppSize + smartLink.FileHeadInfo.DataSize));
				}
				AppDataCheckSumCmd();
			}
		}

		private void AppDataCheckSumCmd()
		{
			m_progressInfo_MessageEvent("Checking the application data ...", CORRECT_COLOR);
			if (!smartLink.AppDataCheckSumCmd() || smartLink.RxBuffer[1] != 6)
			{
				throw new Exception("Application data verification is failed.");
			}
		}

		private void EraseAppCmd()
		{
			m_progressInfo_MessageEvent("Erasing the application...", CORRECT_COLOR);
			if (!smartLink.EraseAppCmd() || smartLink.RxBuffer[2] != 0)
			{
				throw new Exception("Erase the application failed.");
			}
		}

		private void RebootToAppCmd()
		{
			m_progressInfo_MessageEvent("Reboot and run the new application...", CORRECT_COLOR);
			if (!smartLink.RebootToAppCmd())
			{
				throw new Exception("Reboot to Application failed.");
			}
		}

		private void RenameDeviceCmd()
		{
			for (int i = 0; i < 8; i++)
			{
				smartLink.renameArray[i] = 0;
			}
			for (int j = 0; j < textBox_rename.Text.Length; j++)
			{
				smartLink.renameArray[j] = (byte)textBox_rename.Text.ToCharArray()[j];
			}
			if (!smartLink.RenameDeviceCmd())
			{
				throw new Exception("Rename failed.");
			}
			m_progressInfo_MessageEvent("Rename succeed.", CORRECT_COLOR);
		}

		private void AsyncThread(TaskType type)
		{
			try
			{
				sw.Restart();
				switch (type)
				{
				case TaskType.tyUpdate:
					LoadFirmWareFile();
					SearchIsdtDevice();
					QueryDeviceInfoCmd();
					AppUpdateCmd();
					EraseAppCmd();
					WriteAppCmd();
					WriteAppDataCmd();
					RebootToAppCmd();
					m_progressInfo_MessageEvent("Update succeed.", CORRECT_COLOR);
					break;
				case TaskType.tyRename:
					SearchIsdtDevice();
					RenameDeviceCmd();
					break;
				case TaskType.tyCheck:
					LoadFirmWareFile();
					SearchIsdtDevice();
					QueryDeviceInfoCmd();
					break;
                case TaskType.tyDownload:
                    LoadFirmWareFile();
                    SearchIsdtDevice();
                    QueryDeviceInfoCmd();
                    break;
                }
			}
			catch (Exception ex)
			{
				m_comm_MessageEvent(ex.Message, ERR_COLOR);
			}
			finally
			{
				sw.Stop();
				if (smartLink != null)
				{
					smartLink.Close();
				}
			}
		}

		private void Completed(IAsyncResult result)
		{
		}

		private void m_progressInfo_MessageEvent(string msg, Color color)
		{
			if (taskType != 0)
			{
				if (label_progressInfo.InvokeRequired)
				{
					InvokeCallback_Msg method = m_progressInfo_MessageEvent;
					label_progressInfo.Invoke(method, msg, color);
				}
				else
				{
					label_progressInfo.ForeColor = color;
					label_progressInfo.Text = msg;
				}
			}
		}

		private void m_comm_MessageEvent(string msg, Color color)
		{
			if (taskType == TaskType.tyCheck)
			{
				m_deviceInfo_MessageEvent(msg, color);
			}
			else
			{
				m_progressInfo_MessageEvent(msg, color);
			}
		}

		private void m_comm_ProgressEvent(int val, int max)
		{
			if (progressBar_update.InvokeRequired)
			{
				InvokeCallback_Progess method = m_comm_ProgressEvent;
				progressBar_update.Invoke(method, val, max);
			}
			else
			{
				progressBar_update.Maximum = max;
				progressBar_update.Value = val;
				label_progress.Text = val * 100 / progressBar_update.Maximum + "%";
				int num = val * progressBar_update.Width / progressBar_update.Maximum - label_progress.Width;
				int y = label_progress.Location.Y;
				label_progress.Location = new Point((num >= 0) ? num : 0, y);
			}
		}

		private void LoadFirmWareFile()
		{
			if (!File.Exists("Firmware\\\\Firmware.fwd"))
			{
				throw new Exception("The firmware file not exist.");
			}
			hidLink.UpdateFileBuf = File.ReadAllBytes("Firmware\\\\Firmware.fwd");
			hidLink.FileHeadInfo = (SmartLink.TFileHeadInfo)ByteToStruct(hidLink.UpdateFileBuf, 0, typeof(SmartLink.TFileHeadInfo));
			DecryptedFile(hidLink.UpdateFileBuf, (uint)((int)(hidLink.FileHeadInfo.AppSize + hidLink.FileHeadInfo.DataSize) + Marshal.SizeOf(typeof(SmartLink.TFileHeadInfo))), hidLink.FileHeadInfo.EncryptionKey, hidLink.FileHeadInfo.FileCheckSum);
			if (!FimwareFileCheck(hidLink.UpdateFileBuf, (uint)((int)(hidLink.FileHeadInfo.AppSize + hidLink.FileHeadInfo.DataSize) + Marshal.SizeOf(typeof(SmartLink.TFileHeadInfo)))))
			{
				throw new Exception("The firmware checksum is error.");
			}
			if (!GetFlashHdInfoTab())
			{
				throw new Exception("The firmware file information is incorrect.");
			}
			m_progressInfo_MessageEvent("The firmware file was loaded successfully.", CORRECT_COLOR);
			m_comm_ProgressEvent(0, (int)(hidLink.FileHeadInfo.AppSize + hidLink.FileHeadInfo.DataSize));
		}

		private void button_update_Click(object sender, EventArgs e)
		{
			if (!timer1.Enabled)
			{
				timer1.Start();
			}
		}

        private void button_download_Click(object Sender, EventArgs e)
        {

        }


        private void button_update_MouseEnter(object sender, EventArgs e)
		{
			if (languageIndex == 0)
			{
				button_update.Image = Resources.U2;
			}
			else if (languageIndex == 1)
			{
				button_update.Image = Resources.FU2;
			}
			else if (languageIndex == 2)
			{
				button_update.Image = Resources.CU2;
			}
			else if (languageIndex == 3)
			{
				button_update.Image = Resources.DU2;
			}
		}

		private void button_update_MouseLeave(object sender, EventArgs e)
		{
			if (languageIndex == 0)
			{
				button_update.Image = Resources.U1;
			}
			else if (languageIndex == 1)
			{
				button_update.Image = Resources.FU1;
			}
			else if (languageIndex == 2)
			{
				button_update.Image = Resources.CU1;
			}
			else if (languageIndex == 3)
			{
				button_update.Image = Resources.DU1;
			}
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			int num = 10;
			if (timer2.Enabled)
			{
				timer2.Stop();
			}
			if (tableLayoutPanel_updateInfo.Location.Y < -num / 2)
			{
				ResizeUpdateInfoLayout(num);
			}
			else if (tableLayoutPanel_updateInfo.Location.Y > num / 2)
			{
				ResizeUpdateInfoLayout(-num);
			}
			else
			{
				timer1.Stop();
				timer2.Start();
				if (asyncResult == null || asyncResult.IsCompleted)
				{
					label_updateTime.Visible = true;
					asyncResult = myDelegate.BeginInvoke(taskType = TaskType.tyUpdate, Completed, null);
				}
			}
		}

		private void button_rename_MouseEnter(object sender, EventArgs e)
		{
			if (reHeight != 0)
			{
				reHeight = 0;
				if (languageIndex == 0)
				{
					button_rename.Image = Resources.R2;
				}
				else if (languageIndex == 1)
				{
					button_rename.Image = Resources.FR2;
				}
				else if (languageIndex == 2)
				{
					button_rename.Image = Resources.CR2;
				}
				else if (languageIndex == 3)
				{
					button_rename.Image = Resources.DR2;
				}
				if (!timer3.Enabled)
				{
					timer3.Start();
				}
			}
		}

		private void Form_Update_Load(object sender, EventArgs e)
		{
			if (File.Exists("Firmware\\\\Release Notes.rtf"))
			{
				try
				{
					richTextBox_updateInfo.LoadFile("Firmware\\\\Release Notes.rtf", RichTextBoxStreamType.RichText);
				}
				catch (ArgumentException)
				{
					richTextBox_updateInfo.LoadFile("Firmware\\\\Release Notes.rtf", RichTextBoxStreamType.PlainText);
				}
			}
			ResizeUpdateInfoLayout(-(int)tableLayoutPanel_updateInfo.RowStyles[0].Height);
			ResizeRenameLayout(reHeight = panel_rename_edit.Location.Y);
			Text = Text + " " + FileVersionInfo.GetVersionInfo(Application.ExecutablePath).FileVersion;
			myDelegate = AsyncThread;
			if (asyncResult == null || asyncResult.IsCompleted)
			{
				asyncResult = myDelegate.BeginInvoke(taskType = TaskType.tyCheck, Completed, null);
			}
		}

		private void timer2_Tick(object sender, EventArgs e)
		{
			int num = 10;
			int num2 = -(int)tableLayoutPanel_updateInfo.RowStyles[0].Height;
			TimeSpan elapsed = sw.Elapsed;
			label_updateTime.Text = string.Format("{0}: {1}: {2}", elapsed.Minutes.ToString("D2"), elapsed.Seconds.ToString("D2"), elapsed.Milliseconds.ToString("D3"));
			if (timer1.Enabled)
			{
				timer1.Stop();
			}
			if (asyncResult == null || asyncResult.IsCompleted)
			{
				if (tableLayoutPanel_updateInfo.Location.Y < num2 + -num / 2)
				{
					ResizeUpdateInfoLayout(num);
				}
				else if (tableLayoutPanel_updateInfo.Location.Y > num2 + num / 2)
				{
					ResizeUpdateInfoLayout(-num);
				}
				else
				{
					label_updateTime.Visible = false;
					timer2.Stop();
				}
			}
		}

		private void button_rename_Click(object sender, EventArgs e)
		{
			if (asyncResult == null || asyncResult.IsCompleted)
			{
				asyncResult = myDelegate.BeginInvoke(taskType = TaskType.tyRename, Completed, null);
			}
		}

		private void timer3_Tick(object sender, EventArgs e)
		{
			int num = 2;
			if (tableLayoutPanel_rename.Location.Y > reHeight + num / 2)
			{
				ResizeRenameLayout(-num);
			}
			else if (tableLayoutPanel_rename.Location.Y < reHeight + -num / 2)
			{
				ResizeRenameLayout(num);
			}
			else
			{
				if (textBox_rename.Text == "" && reHeight == panel_rename_edit.Location.Y)
				{
					isFirstInput = true;
					textBox_rename.Text = "ISDT";
					textBox_rename.ForeColor = Color.DarkGray;
				}
				timer3.Stop();
			}
		}

		[DllImport("user32.dll")]
		internal static extern bool GetCursorPos(out Point lpPoint);

		private void timer4_Tick(object sender, EventArgs e)
		{
			GetCursorPos(out Point lpPoint);
			lpPoint = PointToClient(lpPoint);
			if ((lpPoint.X <= panel_rename.Location.X || lpPoint.X >= panel_rename.Location.X + panel_rename.Width || lpPoint.Y <= panel_rename.Location.Y || lpPoint.Y >= panel_rename.Location.Y + panel_rename.Height) && reHeight != panel_rename_edit.Location.Y)
			{
				reHeight = panel_rename_edit.Location.Y;
				if (languageIndex == 0)
				{
					button_rename.Image = Resources.R1;
				}
				else if (languageIndex == 1)
				{
					button_rename.Image = Resources.FR1;
				}
				else if (languageIndex == 2)
				{
					button_rename.Image = Resources.CR1;
				}
				else if (languageIndex == 3)
				{
					button_rename.Image = Resources.DR1;
				}
				if (!timer3.Enabled)
				{
					timer3.Start();
				}
			}
		}

		private void textBox_rename_MouseClick(object sender, MouseEventArgs e)
		{
			if (isFirstInput)
			{
				isFirstInput = false;
				textBox_rename.Clear();
				textBox_rename.ForeColor = Color.Teal;
			}
		}

		private void button_language_Click(object sender, EventArgs e)
		{
			contextMenuStrip_language.Show();
			contextMenuStrip_language.Show(button_language, new Point((button_language.Width - contextMenuStrip_language.Size.Width) / 2, button_language.Size.Height));
		}

		private void button_language_MouseEnter(object sender, EventArgs e)
		{
			if (languageIndex == 0)
			{
				button_language.Image = Resources.L2;
			}
			else if (languageIndex == 1)
			{
				button_language.Image = Resources.FL2;
			}
			else if (languageIndex == 2)
			{
				button_language.Image = Resources.CL2;
			}
			else if (languageIndex == 3)
			{
				button_language.Image = Resources.DL2;
			}
		}

		private void button_language_MouseLeave(object sender, EventArgs e)
		{
			if (languageIndex == 0)
			{
				button_language.Image = Resources.L1;
			}
			else if (languageIndex == 1)
			{
				button_language.Image = Resources.FL1;
			}
			else if (languageIndex == 2)
			{
				button_language.Image = Resources.CL1;
			}
			else if (languageIndex == 3)
			{
				button_language.Image = Resources.DL1;
			}
		}

		private void button_drive_MouseEnter(object sender, EventArgs e)
		{
			if (languageIndex == 0)
			{
				button_drive.Image = Resources.D2;
			}
			else if (languageIndex == 1)
			{
				button_drive.Image = Resources.FD2;
			}
			else if (languageIndex == 2)
			{
				button_drive.Image = Resources.CD2;
			}
		}

		private void button_drive_MouseLeave(object sender, EventArgs e)
		{
			if (languageIndex == 0)
			{
				button_drive.Image = Resources.D1;
			}
			else if (languageIndex == 1)
			{
				button_drive.Image = Resources.FD1;
			}
			else if (languageIndex == 2)
			{
				button_drive.Image = Resources.CD1;
			}
		}

		private void button_drive_Click(object sender, EventArgs e)
		{
			if (File.Exists("Driver\\\\ScLinkerDrv.exe"))
			{
				Process process = Process.Start("Driver\\\\ScLinkerDrv.exe");
				process.WaitForExit();
			}
		}

		private void richTextBox_updateInfo_Enter(object sender, EventArgs e)
		{
			if (textBox_rename.CanFocus)
			{
				textBox_rename.Focus();
			}
		}

		private void contextMenuStrip_language_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			switch (e.ClickedItem.Text)
			{
			case "Deutsch":
				languageIndex = 3;
				break;
			case "简体中文":
				languageIndex = 2;
				break;
			case "繁體中文":
				languageIndex = 1;
				break;
			default:
				languageIndex = 0;
				break;
			}
			button_language_MouseLeave(sender, e);
			button_update_MouseLeave(sender, e);
			button_drive_MouseLeave(sender, e);
			if (languageIndex == 0)
			{
				button_rename.Image = Resources.R1;
			}
			else if (languageIndex == 1)
			{
				button_rename.Image = Resources.FR1;
			}
			else if (languageIndex == 2)
			{
				button_rename.Image = Resources.CR1;
			}
			else if (languageIndex == 3)
			{
				button_rename.Image = Resources.DR1;
			}
		}

		private bool GetFlashHdInfoTab()
		{
			uint num = BitConverter.ToUInt32(hidLink.UpdateFileBuf, Marshal.SizeOf(typeof(SmartLink.TFileHeadInfo)) + 40);
			switch (num)
			{
			case 0u:
				return false;
			case uint.MaxValue:
				return false;
			default:
				num -= hidLink.FileHeadInfo.AppStorageOffset;
				if (num > hidLink.FileHeadInfo.AppSize)
				{
					return false;
				}
				hidLink.AppInfoTable = (SmartLink.TFlashInfoTable)ByteToStruct(hidLink.UpdateFileBuf, Marshal.SizeOf(typeof(SmartLink.TFileHeadInfo)) + (int)num, typeof(SmartLink.TFlashInfoTable));
				if (hidLink.FileHeadInfo.AppSize > hidLink.AppInfoTable.AppSize)
				{
					return false;
				}
				if (hidLink.AppInfoTable.AppDataCheck == 2857749555u)
				{
					for (int i = 0; i < 4; i++)
					{
						hidLink.UpdateFileBuf[Marshal.SizeOf(typeof(SmartLink.TFileHeadInfo)) + (int)num + i] = byte.MaxValue;
					}
				}
				else if (hidLink.AppInfoTable.AppDataCheck != uint.MaxValue)
				{
					return false;
				}
				return true;
			}
		}

		private bool FimwareFileCheck(byte[] FileBuf, uint Size)
		{
			uint num = 0u;
			for (int i = Marshal.SizeOf(typeof(SmartLink.TFileHeadInfo)); i < Size; i += 4)
			{
				num += BitConverter.ToUInt32(FileBuf, i);
			}
			if (num != hidLink.FileHeadInfo.FileCheckSum)
			{
				return false;
			}
			return true;
		}

		private void DecryptedFile(byte[] FileBuf, uint Size, uint Key1, uint Key2)
		{
			for (int i = Marshal.SizeOf(typeof(SmartLink.TFileHeadInfo)); i < Size; i += 4)
			{
				uint num = BitConverter.ToUInt32(FileBuf, i);
				num ^= Key2;
				Key2 += Key1;
				Key2 ^= Key1;
				byte[] bytes = BitConverter.GetBytes(num);
				for (int j = 0; j < 4; j++)
				{
					FileBuf[i + j] = bytes[j];
				}
			}
		}

		public static object ByteToStruct(byte[] bytes, int offset, Type type)
		{
			int num = Marshal.SizeOf(type);
			if (num + offset > bytes.Length)
			{
				return null;
			}
			byte[] array = new byte[num];
			Array.Copy(bytes, offset, array, 0, num);
			IntPtr intPtr = Marshal.AllocHGlobal(num);
			Marshal.Copy(array, 0, intPtr, num);
			object result = Marshal.PtrToStructure(intPtr, type);
			Marshal.FreeHGlobal(intPtr);
			return result;
		}

		private void m_deviceInfo_MessageEvent(string msg, Color color)
		{
			if (label_deviceInfo.InvokeRequired)
			{
				InvokeCallback_Msg method = m_deviceInfo_MessageEvent;
				label_deviceInfo.Invoke(method, msg, color);
			}
			else
			{
				label_deviceInfo.ForeColor = color;
				label_deviceInfo.Text = msg;
			}
		}

		private void timer6_Tick(object sender, EventArgs e)
		{
			if (asyncResult == null || asyncResult.IsCompleted)
			{
				asyncResult = myDelegate.BeginInvoke(taskType = TaskType.tyCheck, Completed, null);
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager componentResourceManager = new System.ComponentModel.ComponentResourceManager(typeof(SCU.Form_Update));
			panel1 = new System.Windows.Forms.Panel();
			tableLayoutPanel_updateInfo = new System.Windows.Forms.TableLayoutPanel();
			panel_progress = new System.Windows.Forms.Panel();
			label_progress = new System.Windows.Forms.Label();
			progressBar_update = new System.Windows.Forms.ProgressBar();
			panel_space = new System.Windows.Forms.Panel();
			label_updateTime = new System.Windows.Forms.Label();
			label_progressInfo = new System.Windows.Forms.Label();
			timer1 = new System.Windows.Forms.Timer(components);
			timer2 = new System.Windows.Forms.Timer(components);
			panel_rename = new System.Windows.Forms.Panel();
			tableLayoutPanel_rename = new System.Windows.Forms.TableLayoutPanel();
			panel_rename_edit = new System.Windows.Forms.Panel();
			textBox_rename = new System.Windows.Forms.TextBox();
			label1 = new System.Windows.Forms.Label();
			button_rename = new System.Windows.Forms.Button();
			timer3 = new System.Windows.Forms.Timer(components);
			timer4 = new System.Windows.Forms.Timer(components);
			contextMenuStrip_language = new System.Windows.Forms.ContextMenuStrip(components);
			ChinaToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			FchinaToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			englishToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			button_drive = new System.Windows.Forms.Button();
			button_language = new System.Windows.Forms.Button();
			button_update = new System.Windows.Forms.Button();
            button_dl = new System.Windows.Forms.Button();
            pictureBox1 = new System.Windows.Forms.PictureBox();
			timer6 = new System.Windows.Forms.Timer(components);
			panel2 = new System.Windows.Forms.Panel();
			richTextBox_updateInfo = new System.Windows.Forms.RichTextBox();
			label_deviceInfo = new System.Windows.Forms.Label();
			panel1.SuspendLayout();
			tableLayoutPanel_updateInfo.SuspendLayout();
			panel_progress.SuspendLayout();
			panel_space.SuspendLayout();
			panel_rename.SuspendLayout();
			tableLayoutPanel_rename.SuspendLayout();
			panel_rename_edit.SuspendLayout();
			contextMenuStrip_language.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
			panel2.SuspendLayout();
			SuspendLayout();
			panel1.BackColor = System.Drawing.Color.White;
			panel1.Controls.Add(tableLayoutPanel_updateInfo);
			panel1.Dock = System.Windows.Forms.DockStyle.Right;
			panel1.Location = new System.Drawing.Point(289, 0);
			panel1.Name = "panel1";
			panel1.Size = new System.Drawing.Size(484, 556);
			panel1.TabIndex = 3;
			tableLayoutPanel_updateInfo.ColumnCount = 2;
			tableLayoutPanel_updateInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 91.73553f));
			tableLayoutPanel_updateInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8.264462f));
			tableLayoutPanel_updateInfo.Controls.Add(richTextBox_updateInfo, 0, 2);
			tableLayoutPanel_updateInfo.Controls.Add(panel_progress, 0, 0);
			tableLayoutPanel_updateInfo.Controls.Add(panel_space, 0, 1);
			tableLayoutPanel_updateInfo.Location = new System.Drawing.Point(0, 0);
			tableLayoutPanel_updateInfo.Name = "tableLayoutPanel_updateInfo";
			tableLayoutPanel_updateInfo.RowCount = 3;
			tableLayoutPanel_updateInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 85f));
			tableLayoutPanel_updateInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 45f));
			tableLayoutPanel_updateInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 423f));
			tableLayoutPanel_updateInfo.Size = new System.Drawing.Size(484, 518);
			tableLayoutPanel_updateInfo.TabIndex = 11;
			panel_progress.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			panel_progress.BackColor = System.Drawing.Color.White;
			panel_progress.Controls.Add(label_progress);
			panel_progress.Controls.Add(progressBar_update);
			panel_progress.Dock = System.Windows.Forms.DockStyle.Fill;
			panel_progress.Location = new System.Drawing.Point(3, 3);
			panel_progress.Name = "panel_progress";
			panel_progress.Size = new System.Drawing.Size(437, 79);
			panel_progress.TabIndex = 9;
			label_progress.AutoSize = true;
			label_progress.Font = new System.Drawing.Font("Segoe UI Symbol", 24f);
			label_progress.ForeColor = System.Drawing.Color.Green;
			label_progress.Location = new System.Drawing.Point(0, 19);
			label_progress.Name = "label_progress";
			label_progress.Size = new System.Drawing.Size(63, 45);
			label_progress.TabIndex = 3;
			label_progress.Text = "0%";
			progressBar_update.BackColor = System.Drawing.Color.LightSlateGray;
			progressBar_update.Dock = System.Windows.Forms.DockStyle.Bottom;
			progressBar_update.ForeColor = System.Drawing.Color.Red;
			progressBar_update.Location = new System.Drawing.Point(0, 69);
			progressBar_update.Name = "progressBar_update";
			progressBar_update.Size = new System.Drawing.Size(437, 10);
			progressBar_update.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
			progressBar_update.TabIndex = 2;
			panel_space.Controls.Add(label_updateTime);
			panel_space.Controls.Add(label_progressInfo);
			panel_space.Dock = System.Windows.Forms.DockStyle.Fill;
			panel_space.Location = new System.Drawing.Point(3, 88);
			panel_space.Name = "panel_space";
			panel_space.Size = new System.Drawing.Size(437, 39);
			panel_space.TabIndex = 12;
			label_updateTime.Dock = System.Windows.Forms.DockStyle.Fill;
			label_updateTime.Font = new System.Drawing.Font("微软雅黑", 10.5f, System.Drawing.FontStyle.Bold);
			label_updateTime.ForeColor = System.Drawing.Color.Green;
			label_updateTime.Location = new System.Drawing.Point(346, 0);
			label_updateTime.Name = "label_updateTime";
			label_updateTime.Size = new System.Drawing.Size(91, 39);
			label_updateTime.TabIndex = 16;
			label_updateTime.TextAlign = System.Drawing.ContentAlignment.TopRight;
			label_updateTime.Visible = false;
			label_progressInfo.Dock = System.Windows.Forms.DockStyle.Left;
			label_progressInfo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			label_progressInfo.Font = new System.Drawing.Font("微软雅黑", 10.5f, System.Drawing.FontStyle.Bold);
			label_progressInfo.ForeColor = System.Drawing.Color.Green;
			label_progressInfo.Location = new System.Drawing.Point(0, 0);
			label_progressInfo.Name = "label_progressInfo";
			label_progressInfo.Size = new System.Drawing.Size(346, 39);
			label_progressInfo.TabIndex = 0;
			timer1.Interval = 20;
			timer1.Tick += new System.EventHandler(timer1_Tick);
			timer2.Interval = 20;
			timer2.Tick += new System.EventHandler(timer2_Tick);
			panel_rename.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			panel_rename.Controls.Add(tableLayoutPanel_rename);
			panel_rename.Location = new System.Drawing.Point(47, 346);
			panel_rename.Margin = new System.Windows.Forms.Padding(0);
			panel_rename.Name = "panel_rename";
			panel_rename.Size = new System.Drawing.Size(166, 90);
			panel_rename.TabIndex = 12;
			tableLayoutPanel_rename.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			tableLayoutPanel_rename.ColumnCount = 1;
			tableLayoutPanel_rename.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50f));
			tableLayoutPanel_rename.Controls.Add(panel_rename_edit, 0, 1);
			tableLayoutPanel_rename.Controls.Add(button_rename, 0, 0);
			tableLayoutPanel_rename.Location = new System.Drawing.Point(14, 0);
			tableLayoutPanel_rename.Margin = new System.Windows.Forms.Padding(0);
			tableLayoutPanel_rename.Name = "tableLayoutPanel_rename";
			tableLayoutPanel_rename.RowCount = 2;
			tableLayoutPanel_rename.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 51.72414f));
			tableLayoutPanel_rename.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 48.27586f));
			tableLayoutPanel_rename.Size = new System.Drawing.Size(140, 87);
			tableLayoutPanel_rename.TabIndex = 12;
			panel_rename_edit.AutoSize = true;
			panel_rename_edit.Controls.Add(textBox_rename);
			panel_rename_edit.Controls.Add(label1);
			panel_rename_edit.Location = new System.Drawing.Point(3, 48);
			panel_rename_edit.Name = "panel_rename_edit";
			panel_rename_edit.Size = new System.Drawing.Size(134, 36);
			panel_rename_edit.TabIndex = 13;
			textBox_rename.BorderStyle = System.Windows.Forms.BorderStyle.None;
			textBox_rename.Font = new System.Drawing.Font("微软雅黑", 16f);
			textBox_rename.ForeColor = System.Drawing.Color.DarkGray;
			textBox_rename.ImeMode = System.Windows.Forms.ImeMode.Disable;
			textBox_rename.Location = new System.Drawing.Point(1, 1);
			textBox_rename.Margin = new System.Windows.Forms.Padding(0);
			textBox_rename.MaxLength = 8;
			textBox_rename.Name = "textBox_rename";
			textBox_rename.RightToLeft = System.Windows.Forms.RightToLeft.No;
			textBox_rename.ShortcutsEnabled = false;
			textBox_rename.Size = new System.Drawing.Size(128, 29);
			textBox_rename.TabIndex = 12;
			textBox_rename.Text = "ISDT";
			textBox_rename.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			textBox_rename.MouseClick += new System.Windows.Forms.MouseEventHandler(textBox_rename_MouseClick);
			label1.AutoSize = true;
			label1.ForeColor = System.Drawing.Color.FromArgb(179, 179, 179);
			label1.Location = new System.Drawing.Point(5, 20);
			label1.Name = "label1";
			label1.Size = new System.Drawing.Size(129, 21);
			label1.TabIndex = 11;
			label1.Text = "———————";
			button_rename.AutoSize = true;
			button_rename.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			button_rename.BackColor = System.Drawing.Color.White;
			button_rename.Cursor = System.Windows.Forms.Cursors.Hand;
			button_rename.FlatAppearance.BorderColor = System.Drawing.Color.White;
			button_rename.FlatAppearance.BorderSize = 0;
			button_rename.FlatAppearance.MouseDownBackColor = System.Drawing.Color.White;
			button_rename.FlatAppearance.MouseOverBackColor = System.Drawing.Color.White;
			button_rename.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			button_rename.Font = new System.Drawing.Font("Segoe UI Symbol", 20.25f, System.Drawing.FontStyle.Bold);
			button_rename.ForeColor = System.Drawing.Color.White;
			button_rename.Image = SCU.Properties.Resources.R1;
			button_rename.Location = new System.Drawing.Point(3, 3);
			button_rename.Name = "button_rename";
			button_rename.Size = new System.Drawing.Size(131, 39);
			button_rename.TabIndex = 11;
			button_rename.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			button_rename.UseVisualStyleBackColor = false;
			button_rename.Click += new System.EventHandler(button_rename_Click);
			button_rename.MouseEnter += new System.EventHandler(button_rename_MouseEnter);

            button_dl.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            button_dl.BackColor = System.Drawing.Color.White;
            button_dl.Cursor = System.Windows.Forms.Cursors.Hand;
            button_dl.FlatAppearance.BorderColor = System.Drawing.Color.White;
            button_dl.FlatAppearance.BorderSize = 0;
            button_dl.FlatAppearance.MouseDownBackColor = System.Drawing.Color.White;
            button_dl.FlatAppearance.MouseOverBackColor = System.Drawing.Color.White;
            button_dl.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            button_dl.Font = new System.Drawing.Font("Segoe UI Symbol", 15f, System.Drawing.FontStyle.Bold);
            button_dl.Text = "Download";
            button_dl.ForeColor = System.Drawing.Color.Black;
            //button_dl.Image = (System.Drawing.Image)componentResourceManager.GetObject("button_update.Image");
            button_dl.Location = new System.Drawing.Point(62, 281);
            button_dl.Name = "button_dl";
            button_dl.Size = new System.Drawing.Size(131, 40);
            button_dl.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            button_dl.UseVisualStyleBackColor = false;
            button_dl.Click += new System.EventHandler(button_download_Click);
            //button_dl.MouseEnter += new System.EventHandler(button_update_MouseEnter);
            //button_dl.MouseLeave += new System.EventHandler(button_update_MouseLeave);

            timer3.Interval = 10;
			timer3.Tick += new System.EventHandler(timer3_Tick);
			timer4.Enabled = true;
			timer4.Tick += new System.EventHandler(timer4_Tick);
			contextMenuStrip_language.BackColor = System.Drawing.Color.White;
			contextMenuStrip_language.Items.AddRange(new System.Windows.Forms.ToolStripItem[3]
			{
				ChinaToolStripMenuItem,
				FchinaToolStripMenuItem,
				englishToolStripMenuItem
			});
			contextMenuStrip_language.Name = "contextMenuStrip_language";
			contextMenuStrip_language.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			contextMenuStrip_language.Size = new System.Drawing.Size(125, 70);
			contextMenuStrip_language.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(contextMenuStrip_language_ItemClicked);
			ChinaToolStripMenuItem.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
			ChinaToolStripMenuItem.Name = "ChinaToolStripMenuItem";
			ChinaToolStripMenuItem.RightToLeft = System.Windows.Forms.RightToLeft.No;
			ChinaToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
			ChinaToolStripMenuItem.Text = "简体中文";
			FchinaToolStripMenuItem.AutoSize = false;
			FchinaToolStripMenuItem.Name = "FchinaToolStripMenuItem";
			FchinaToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			FchinaToolStripMenuItem.Text = "繁體中文";
			FchinaToolStripMenuItem.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			englishToolStripMenuItem.Name = "englishToolStripMenuItem";
			englishToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
			englishToolStripMenuItem.Text = "English";
			button_drive.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			button_drive.BackColor = System.Drawing.Color.White;
			button_drive.Cursor = System.Windows.Forms.Cursors.Hand;
			button_drive.FlatAppearance.BorderColor = System.Drawing.Color.White;
			button_drive.FlatAppearance.BorderSize = 0;
			button_drive.FlatAppearance.MouseDownBackColor = System.Drawing.Color.White;
			button_drive.FlatAppearance.MouseOverBackColor = System.Drawing.Color.White;
			button_drive.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			button_drive.Font = new System.Drawing.Font("Segoe UI Symbol", 20.25f, System.Drawing.FontStyle.Bold);
			button_drive.ForeColor = System.Drawing.Color.White;
			button_drive.Image = SCU.Properties.Resources.D1;
			button_drive.Location = new System.Drawing.Point(62, 214);
			button_drive.Name = "button_drive";
			button_drive.Size = new System.Drawing.Size(131, 40);
			button_drive.TabIndex = 15;
			button_drive.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			button_drive.UseVisualStyleBackColor = false;
			button_drive.Click += new System.EventHandler(button_drive_Click);
			button_drive.MouseEnter += new System.EventHandler(button_drive_MouseEnter);
			button_drive.MouseLeave += new System.EventHandler(button_drive_MouseLeave);
			button_language.AutoSize = true;
			button_language.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			button_language.Cursor = System.Windows.Forms.Cursors.Hand;
			button_language.FlatAppearance.BorderColor = System.Drawing.Color.White;
			button_language.FlatAppearance.MouseDownBackColor = System.Drawing.Color.White;
			button_language.FlatAppearance.MouseOverBackColor = System.Drawing.Color.White;
			button_language.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			button_language.ForeColor = System.Drawing.Color.White;
			button_language.Image = SCU.Properties.Resources.L1;
			button_language.Location = new System.Drawing.Point(65, 445);
			button_language.Name = "button_language";
			button_language.Size = new System.Drawing.Size(132, 41);
			button_language.TabIndex = 14;
			button_language.UseVisualStyleBackColor = true;
			button_language.Click += new System.EventHandler(button_language_Click);
			button_language.MouseEnter += new System.EventHandler(button_language_MouseEnter);
			button_language.MouseLeave += new System.EventHandler(button_language_MouseLeave);
			button_update.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			button_update.BackColor = System.Drawing.Color.White;
			button_update.Cursor = System.Windows.Forms.Cursors.Hand;
			button_update.FlatAppearance.BorderColor = System.Drawing.Color.White;
			button_update.FlatAppearance.BorderSize = 0;
			button_update.FlatAppearance.MouseDownBackColor = System.Drawing.Color.White;
			button_update.FlatAppearance.MouseOverBackColor = System.Drawing.Color.White;
			button_update.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			button_update.Font = new System.Drawing.Font("Segoe UI Symbol", 20.25f, System.Drawing.FontStyle.Bold);
			button_update.ForeColor = System.Drawing.Color.White;
			button_update.Image = (System.Drawing.Image)componentResourceManager.GetObject("button_update.Image");
			button_update.Location = new System.Drawing.Point(62, 161);
			button_update.Name = "button_update";
			button_update.Size = new System.Drawing.Size(131, 40);
			button_update.TabIndex = 4;
			button_update.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			button_update.UseVisualStyleBackColor = false;
			button_update.Click += new System.EventHandler(button_update_Click);
			button_update.MouseEnter += new System.EventHandler(button_update_MouseEnter);
			button_update.MouseLeave += new System.EventHandler(button_update_MouseLeave);
			pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			pictureBox1.Image = SCU.Properties.Resources.updata_soft_1_;
			pictureBox1.Location = new System.Drawing.Point(0, 0);
			pictureBox1.Name = "pictureBox1";
			pictureBox1.Size = new System.Drawing.Size(773, 556);
			pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
			pictureBox1.TabIndex = 0;
			pictureBox1.TabStop = false;
			timer6.Enabled = true;
			timer6.Interval = 5000;
			timer6.Tick += new System.EventHandler(timer6_Tick);
			panel2.Controls.Add(label_deviceInfo);
			panel2.Location = new System.Drawing.Point(0, 519);
			panel2.Name = "panel2";
			panel2.Size = new System.Drawing.Size(773, 37);
			panel2.TabIndex = 16;
			richTextBox_updateInfo.BackColor = System.Drawing.Color.White;
			richTextBox_updateInfo.BorderStyle = System.Windows.Forms.BorderStyle.None;
			richTextBox_updateInfo.Cursor = System.Windows.Forms.Cursors.Hand;
			richTextBox_updateInfo.Dock = System.Windows.Forms.DockStyle.Top;
			richTextBox_updateInfo.Font = new System.Drawing.Font("微软雅黑", 10.5f);
			richTextBox_updateInfo.Location = new System.Drawing.Point(3, 133);
			richTextBox_updateInfo.Name = "richTextBox_updateInfo";
			richTextBox_updateInfo.ReadOnly = true;
			richTextBox_updateInfo.Size = new System.Drawing.Size(437, 380);
			richTextBox_updateInfo.TabIndex = 14;
			richTextBox_updateInfo.Text = "";
			richTextBox_updateInfo.Enter += new System.EventHandler(richTextBox_updateInfo_Enter);
			label_deviceInfo.Dock = System.Windows.Forms.DockStyle.Fill;
			label_deviceInfo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			label_deviceInfo.Font = new System.Drawing.Font("微软雅黑", 10.6f, System.Drawing.FontStyle.Bold);
			label_deviceInfo.ForeColor = System.Drawing.Color.Red;
			label_deviceInfo.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			label_deviceInfo.Location = new System.Drawing.Point(0, 0);
			label_deviceInfo.Name = "label_deviceInfo";
			label_deviceInfo.Size = new System.Drawing.Size(773, 37);
			label_deviceInfo.TabIndex = 13;
			label_deviceInfo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			base.AutoScaleDimensions = new System.Drawing.SizeF(10f, 21f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			BackColor = System.Drawing.Color.White;
			base.ClientSize = new System.Drawing.Size(773, 556);
			base.Controls.Add(panel2);
			base.Controls.Add(button_drive);
			base.Controls.Add(button_language);
			base.Controls.Add(panel_rename);
			base.Controls.Add(button_update);
            base.Controls.Add(button_dl);
            base.Controls.Add(panel1);
			base.Controls.Add(pictureBox1);
			DoubleBuffered = true;
			Font = new System.Drawing.Font("微软雅黑", 12f);
			base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			base.Icon = (System.Drawing.Icon)componentResourceManager.GetObject("$this.Icon");
			base.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			base.MaximizeBox = false;
			base.Name = "Form_Update";
			base.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			Text = "ISDT Updater";
			base.Load += new System.EventHandler(Form_Update_Load);
			panel1.ResumeLayout(performLayout: false);
			tableLayoutPanel_updateInfo.ResumeLayout(performLayout: false);
			panel_progress.ResumeLayout(performLayout: false);
			panel_progress.PerformLayout();
			panel_space.ResumeLayout(performLayout: false);
			panel_rename.ResumeLayout(performLayout: false);
			tableLayoutPanel_rename.ResumeLayout(performLayout: false);
			tableLayoutPanel_rename.PerformLayout();
			panel_rename_edit.ResumeLayout(performLayout: false);
			panel_rename_edit.PerformLayout();
			contextMenuStrip_language.ResumeLayout(performLayout: false);
			((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
			panel2.ResumeLayout(performLayout: false);
			ResumeLayout(performLayout: false);
			PerformLayout();
		}
	}
}
