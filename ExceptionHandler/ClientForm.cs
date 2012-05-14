﻿using System;
using System.Drawing;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;

namespace MCSkin3D.ExceptionHandler
{
	public partial class ExceptionForm : Form
	{
		internal const UInt32 MF_ENABLED = 0x00000000;
		internal const UInt32 MF_GRAYED = 0x00000001;
		internal const UInt32 MF_DISABLED = 0x00000002;
		private uint SC_CLOSE = 61536;
		private Client _client;
		private bool _sendingReport;

		public ExceptionForm()
		{
			InitializeComponent();
		}

		public Exception Exception { get; set; }

		public string GetHardwareInfo()
		{
			try
			{
				string info = "Video Info:\r\n";

				var searcher =
					new ManagementObjectSearcher("Select * from Win32_VideoController");

				foreach (ManagementObject video in searcher.Get())
				{
					foreach (PropertyData prop in video.Properties)
					{
						if (prop.Value != null &&
						    prop.Name != "SystemName")
							info += prop.Name + ": " + prop.Value + "\r\n";
					}
				}

				info += "\r\nMemory Info:\r\n";

				searcher =
					new ManagementObjectSearcher("Select * from Win32_PhysicalMemoryArray");

				foreach (ManagementObject video in searcher.Get())
				{
					foreach (PropertyData prop in video.Properties)
					{
						if (prop.Value != null &&
						    prop.Name != "SystemName")
							info += prop.Name + ": " + prop.Value + "\r\n";
					}
				}

				info += "\r\nProcessor Info:\r\n";

				searcher =
					new ManagementObjectSearcher("Select * from Win32_Processor");

				foreach (ManagementObject video in searcher.Get())
				{
					foreach (PropertyData prop in video.Properties)
					{
						if (prop.Value != null &&
						    prop.Name != "SystemName")
							info += prop.Name + ": " + prop.Value + "\r\n";
					}
				}

				try
				{
					info += "\r\nGL Data:\r\n";
					info += "Vendor: " + GL.GetString(StringName.Vendor) + "\r\n";
					info += "Version: " + GL.GetString(StringName.Version) + "\r\n";
					info += "Renderer: " + GL.GetString(StringName.Renderer) + "\r\n";
					info += "Extensions: " + GL.GetString(StringName.Extensions) + "\r\n";
				}
				catch (Exception)
				{
					info += "Couldn't get GL data\r\n";
				}

				return info;
			}
			catch (Exception ex)
			{
				return "Couldn't get hardware info: " + ex;
			}
		}

		public void InitSizes()
		{
			int oldHeight = label3.Height;

			using (Graphics g = CreateGraphics())
			{
				SizeF size = g.MeasureString(label3.Text, label3.Font, label3.Width);
				label3.Size = new Size((int) size.Width, (int) size.Height + 12);
			}

			int offs = label3.Height - oldHeight;
			panel4.Location = new Point(panel4.Location.X, panel4.Location.Y + offs);

			Height += offs;

			linkLabel1.Location = new Point(label2.Location.X + label2.Width - 4, label2.Location.Y);
		}

		private void _client_SendFinished(object sender, SendEventArgs e)
		{
			Invoke((Action) delegate
			                {
			                	label8.Visible = false;
			                	progressBar1.Value = progressBar1.Maximum;
			                	progressBar1.Style = ProgressBarStyle.Continuous;
			                	button2.Text = Editor.GetLanguageString("M_EXPT_CLOSE");

			                	if (e.Failed)
			                		label9.Text = Editor.GetLanguageString("M_EXPT_FAILED") + "\r\n" + e.StatusString;
			                	else
			                		label9.Text = Editor.GetLanguageString("M_EXPT_SUCCEED");

			                	_sendingReport = false;
			                	EnableMenuItem(GetSystemMenu(Handle, false), SC_CLOSE, MF_ENABLED);
			                });
		}

		private void ExceptionForm_Load(object sender, EventArgs e)
		{
			InitSizes();
		}

		private void panel2_Paint_1(object sender, PaintEventArgs e)
		{
			e.Graphics.DrawLine(Pens.White, 0, 0, panel2.Width, 0);
		}

		private void panel3_Paint(object sender, PaintEventArgs e)
		{
			e.Graphics.DrawLine(SystemPens.ControlLight, 0, 0, panel3.Width, 0);
			e.Graphics.DrawLine(SystemPens.ControlLight, 0, panel3.Height - 1, panel3.Width, panel3.Height - 1);
		}

		private ErrorReport BuildErrorReport()
		{
			return ErrorReport.Construct(textBox1.Text, textBox2.Text, textBox3.Text, Exception, checkBox1.Checked);
		}

		[DllImport("user32.dll")]
		private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

		[DllImport("user32.dll")]
		private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem,
		                                          uint uEnable);

		private void button1_Click(object sender, EventArgs e)
		{
			EnableMenuItem(GetSystemMenu(Handle, false), SC_CLOSE, MF_GRAYED);

			panel6.Visible = false;
			panel5.Visible = true;
			button1.Enabled = false;
			_sendingReport = true;
			button2.Text = Editor.GetLanguageString("C_CANCEL");

			_client = new Client(BuildErrorReport(),
				
#if BETA
				Environment.UserName == "Paril" ? "192.168.0.102" : "exception.alteredsoftworks.com",
#else
				"exception.alteredsoftworks.com",
#endif
 8888);
			_client.SendFinished += _client_SendFinished;
			_client.SendToServer();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			if (!_sendingReport || _client == null)
				Close();
			else
			{
				_client.Abort();
				_client = null;
				_sendingReport = false;
			}
		}

		private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			SimpleReportViewer.ShowReportViewer(BuildErrorReport().ToString());
		}
	}
}