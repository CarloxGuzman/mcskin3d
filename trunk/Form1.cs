﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using DevCIL;
using Devcorp.Controls.Design;
using MB.Controls;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Security.Cryptography;
using Paril.Settings;
using Paril.Settings.Serializers;
using Paril.Components;
using Paril.Components.Shortcuts;
using Paril.Components.Update;
using System.Runtime.InteropServices;
using System.Collections;
using Paril.Compatibility;

namespace MCSkin3D
{
	public partial class Form1 : Form
	{
		// ===============================================
		// Private/Static variables
		// ===============================================
		#region Variables
		Updater _updater;

		ColorSliderRenderer redRenderer, greenRenderer, blueRenderer, alphaRenderer;
		HueSliderRenderer hueRenderer;
		SaturationSliderRenderer saturationRenderer;
		LuminanceSliderRenderer lightnessRenderer;

		static ShortcutEditor _shortcutEditor = new ShortcutEditor();
		int _grassTop;
		int _alphaTex, _backgroundTex;
		int _previewPaint;
		Dictionary<Size, int> _charPaintSizes = new Dictionary<Size, int>();

		float _animationTime = 0;
		float _3dZoom = -80;
		float _2dCamOffsetX = 0;
		float _2dCamOffsetY = 0;
		float _2dZoom = 8;
		float _3dRotationX = 0, _3dRotationY = 0;
		PixelsChangedUndoable _changedPixels = null;
		bool _mouseIsDown = false;
		Point _mousePoint;
		UndoBuffer _currentUndoBuffer = null;
		Skin _lastSkin = null;
		bool _skipListbox = false;
		internal PleaseWait _pleaseWaitForm;
		ToolStripButton[] _toolButtons = null;
		ToolStripMenuItem[] _toolMenus = null;
		Tools _currentTool = Tools.Camera;
		Color _currentColor = Color.FromArgb(255, 255, 255, 255);
		bool _skipColors = false;
		ViewMode _currentViewMode = ViewMode.Perspective;
		#endregion

		// ===============================================
		// Constructor
		// ===============================================
		#region Constructor
		public Form1()
		{
			InitializeComponent();

			System.Timers.Timer animTimer = new System.Timers.Timer();
			animTimer.Interval = 22;
			animTimer.Elapsed += new System.Timers.ElapsedEventHandler(animTimer_Elapsed);
			animTimer.Start();

			GlobalSettings.Load();

			glControl1.MouseWheel += new MouseEventHandler(glControl1_MouseWheel);

			animateToolStripMenuItem.Checked = GlobalSettings.Animate;
			followCursorToolStripMenuItem.Checked = GlobalSettings.FollowCursor;
			grassToolStripMenuItem.Checked = GlobalSettings.Grass;

			alphaCheckerboardToolStripMenuItem.Checked = GlobalSettings.AlphaCheckerboard;
			textureOverlayToolStripMenuItem.Checked = GlobalSettings.TextureOverlay;

			SetCheckbox(VisiblePartFlags.HeadFlag, headToolStripMenuItem);
			SetCheckbox(VisiblePartFlags.ChestFlag, chestToolStripMenuItem);
			SetCheckbox(VisiblePartFlags.LeftArmFlag, leftArmToolStripMenuItem);
			SetCheckbox(VisiblePartFlags.RightArmFlag, rightArmToolStripMenuItem);
			SetCheckbox(VisiblePartFlags.HelmetFlag, helmetToolStripMenuItem);
			SetCheckbox(VisiblePartFlags.LeftLegFlag, leftLegToolStripMenuItem);
			SetCheckbox(VisiblePartFlags.RightLegFlag, rightLegToolStripMenuItem);

			if (Screen.PrimaryScreen.BitsPerPixel != 32)
			{
				MessageBox.Show("Sorry, but apparently your video mode doesn't support a 32-bit pixel format - this is required, at the moment, for proper functionality of MCSkin3D. 16-bit support will be implemented at a later date, if it is asked for.", "Sorry", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Application.Exit();
			}

			mainMenuStrip.Renderer = new Szotar.WindowsForms.ToolStripAeroRenderer(Szotar.WindowsForms.ToolbarTheme.Toolbar);
			toolStrip1.Renderer = new Szotar.WindowsForms.ToolStripAeroRenderer(Szotar.WindowsForms.ToolbarTheme.Toolbar);

			redColorSlider.Renderer = redRenderer = new ColorSliderRenderer(redColorSlider);
			greenColorSlider.Renderer = greenRenderer = new ColorSliderRenderer(greenColorSlider);
			blueColorSlider.Renderer = blueRenderer = new ColorSliderRenderer(blueColorSlider);
			alphaColorSlider.Renderer = alphaRenderer = new ColorSliderRenderer(alphaColorSlider);

			hueColorSlider.Renderer = hueRenderer = new HueSliderRenderer(hueColorSlider);
			saturationColorSlider.Renderer = saturationRenderer = new SaturationSliderRenderer(saturationColorSlider);
			lightnessColorSlider.Renderer = lightnessRenderer = new LuminanceSliderRenderer(lightnessColorSlider);

			KeyPreview = true;
			Text = "MCSkin3D v" + ProductVersion[0] + '.' + ProductVersion[2];

			if (!Directory.Exists("Swatches") || !Directory.Exists("Skins"))
				MessageBox.Show("The swatches and/or skins directory was missing - usually this is because you didn't extract the program. While not a critical issue, you will be missing any included templates or swatches.");

			Directory.CreateDirectory("Swatches");
			Directory.CreateDirectory("Skins");
			swatchContainer.AddDirectory("Swatches");

			_updater = new Updater("http://alteredsoftworks.com/mcskin3d/update", "" + ProductVersion[0] + '.' + ProductVersion[2]);
			_updater.UpdateHandler = new AssemblyVersion();
			_updater.NewVersionAvailable += _updater_NewVersionAvailable;
			_updater.SameVersion += _updater_SameVersion;
			_updater.CheckForUpdate();

			automaticallyCheckForUpdatesToolStripMenuItem.Checked = GlobalSettings.AutoUpdate;
		}
		#endregion

		// =====================================================================
		// Updating
		// =====================================================================
		#region Update
		public void Invoke(Action action)
		{
			this.Invoke((Delegate)action);
		}

		void _updater_SameVersion(object sender, EventArgs e)
		{
			this.Invoke(() => MessageBox.Show("You have the latest and greatest."));
		}

		void _updater_NewVersionAvailable(object sender, EventArgs e)
		{
			this.Invoke(delegate()
			{
				if (MessageBox.Show("A new version is available! Would you like to go to the forum post?", "Woo!", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
					Process.Start("http://www.minecraftforum.net/topic/746941-mcskin3d-new-skinning-program/");
			});
		}
		#endregion

		// =====================================================================
		// Shortcuts
		// =====================================================================
		#region Shortcuts

		string CompileShortcutKeys()
		{
			string c = "";

			for (int i = 0; i < _shortcutEditor.ShortcutCount; ++i)
			{
				var shortcut = _shortcutEditor.ShortcutAt(i);

				if (i != 0)
					c += "|";

				Keys key = shortcut.Keys & ~Keys.Modifiers;
				Keys modifiers = (Keys)((int)shortcut.Keys - (int)key);

				if (modifiers != 0)
					c += shortcut.Name + "=" + key + "+" + modifiers;
				else
					c += shortcut.Name + "=" + key;
			}

			return c;
		}

		IShortcutImplementor FindShortcut(string name)
		{
			foreach (var s in _shortcutEditor.Shortcuts)
			{
				if (s.Name == name)
					return s;
			}

			return null;
		}

		void LoadShortcutKeys(string s)
		{
			if (string.IsNullOrEmpty(s))
				return; // leave defaults

			var shortcuts = s.Split('|');

			foreach (var shortcut in shortcuts)
			{
				var args = shortcut.Split('=');

				string name = args[0];
				string key;
				string modifiers = "0";

				if (args[1].Contains('+'))
				{
					var mods = args[1].Split('+');

					key = mods[0];
					modifiers = mods[1];
				}
				else
					key = args[1];

				var sh = FindShortcut(name);

				if (sh == null)
					continue;

				sh.Keys = (Keys)Enum.Parse(typeof(Keys), key) | (Keys)Enum.Parse(typeof(Keys), modifiers);
			}
		}

		void InitMenuShortcut(ToolStripMenuItem item, Action callback)
		{
			MenuStripShortcut shortcut = new MenuStripShortcut(item);
			shortcut.Pressed = callback;

			_shortcutEditor.AddShortcut(shortcut);
		}

		void InitUnlinkedShortcut(string name, Keys defaultKeys, Action callback)
		{
			ShortcutBase shortcut = new ShortcutBase(name, defaultKeys);
			shortcut.Pressed = callback;

			_shortcutEditor.AddShortcut(shortcut);
		}

		void InitShortcuts()
		{
			// shortcut menus
			InitMenuShortcut(undoToolStripMenuItem, PerformUndo);
			InitMenuShortcut(redoToolStripMenuItem, PerformRedo);
			InitMenuShortcut(cameraToolStripMenuItem, () => SetTool(Tools.Camera));
			InitMenuShortcut(pencilToolStripMenuItem, () => SetTool(Tools.Pencil));
			InitMenuShortcut(dropperToolStripMenuItem, () => SetTool(Tools.Dropper));
			InitMenuShortcut(eraserToolStripMenuItem, () => SetTool(Tools.Eraser));
			InitMenuShortcut(dodgeToolStripMenuItem, () => SetTool(Tools.Dodge));
			InitMenuShortcut(burnToolStripMenuItem, () => SetTool(Tools.Burn));
			InitMenuShortcut(addNewSkinToolStripMenuItem, PerformImportSkin);
			InitMenuShortcut(deleteSelectedSkinToolStripMenuItem, PerformDeleteSkin);
			InitMenuShortcut(cloneSkinToolStripMenuItem, PerformCloneSkin);
			InitMenuShortcut(perspectiveToolStripMenuItem, () => SetViewMode(ViewMode.Perspective));
			InitMenuShortcut(textureToolStripMenuItem, () => SetViewMode(ViewMode.Orthographic));
			InitMenuShortcut(animateToolStripMenuItem, ToggleAnimation);
			InitMenuShortcut(followCursorToolStripMenuItem, ToggleFollowCursor);
			InitMenuShortcut(grassToolStripMenuItem, ToggleGrass);
			InitMenuShortcut(alphaCheckerboardToolStripMenuItem, ToggleAlphaCheckerboard);
			InitMenuShortcut(textureOverlayToolStripMenuItem, ToggleOverlay);
			InitMenuShortcut(offToolStripMenuItem, () => SetTransparencyMode(TransparencyMode.Off));
			InitMenuShortcut(helmetOnlyToolStripMenuItem, () => SetTransparencyMode(TransparencyMode.Helmet));
			InitMenuShortcut(allToolStripMenuItem, () => SetTransparencyMode(TransparencyMode.All));
			InitMenuShortcut(headToolStripMenuItem, () => ToggleVisiblePart(VisiblePartFlags.HeadFlag));
			InitMenuShortcut(helmetToolStripMenuItem, () => ToggleVisiblePart(VisiblePartFlags.HelmetFlag));
			InitMenuShortcut(chestToolStripMenuItem, () => ToggleVisiblePart(VisiblePartFlags.ChestFlag));
			InitMenuShortcut(leftArmToolStripMenuItem, () => ToggleVisiblePart(VisiblePartFlags.LeftArmFlag));
			InitMenuShortcut(rightArmToolStripMenuItem, () => ToggleVisiblePart(VisiblePartFlags.RightArmFlag));
			InitMenuShortcut(leftLegToolStripMenuItem, () => ToggleVisiblePart(VisiblePartFlags.LeftLegFlag));
			InitMenuShortcut(rightLegToolStripMenuItem, () => ToggleVisiblePart(VisiblePartFlags.RightLegFlag));
			InitMenuShortcut(saveToolStripMenuItem, PerformSave);
			InitMenuShortcut(saveAsToolStripMenuItem, PerformSaveAs);
			InitMenuShortcut(saveAllToolStripMenuItem, PerformSaveAll);

			// not in the menu
			InitUnlinkedShortcut("Toggle transparency mode", Keys.Shift | Keys.U, ToggleTransparencyMode);
			InitUnlinkedShortcut("Upload skin", Keys.Control | Keys.U, PerformUpload);
			InitUnlinkedShortcut("Toggle view mode", Keys.Control | Keys.V, ToggleViewMode);
			InitUnlinkedShortcut("Screenshot (clipboard)", Keys.Control | Keys.H, TakeScreenshot);
			InitUnlinkedShortcut("Screenshot (save)", Keys.Control | Keys.Shift | Keys.H, SaveScreenshot);
		}

		bool PerformShortcut(Keys key, Keys modifiers)
		{
			foreach (var shortcut in _shortcutEditor.Shortcuts)
			{
				if ((shortcut.Keys & ~Keys.Modifiers) == key &&
					(shortcut.Keys & ~(shortcut.Keys & ~Keys.Modifiers)) == modifiers)
				{
					shortcut.Pressed();
					return true;
				}
			}

			return false;
		}
		#endregion

		// =====================================================================
		// Overrides
		// =====================================================================
		#region Overrides
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (PerformShortcut(e.KeyCode & ~Keys.Modifiers, e.Modifiers))
			{
				e.Handled = true;
				return;
			}

			base.OnKeyDown(e);
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			_updater.Abort();
			GlobalSettings.ShortcutKeys = CompileShortcutKeys();

			GlobalSettings.Save();
		}

		void RecurseAddDirectories(string path, TreeNodeCollection nodes)
		{
			var di = new DirectoryInfo(path);

			foreach (var file in di.GetFiles("*.png", SearchOption.TopDirectoryOnly))
			{
				var skin = new Skin(file);
				nodes.Add(skin);

				if (treeView1.SelectedNode == null)
					treeView1.SelectedNode = skin;
				else if (GlobalSettings.LastSkin == skin.Name)
					treeView1.SelectedNode = skin;
			}

			foreach (var dir in di.GetDirectories())
			{
				if ((dir.Attributes & FileAttributes.Hidden) != 0)
					continue;

				TreeNode folderNode = new TreeNode(dir.Name);
				RecurseAddDirectories(dir.FullName, folderNode.Nodes);
				nodes.Add(folderNode);
			}
		}

		// Summary:
		//     Exposes a method that compares two objects.
		[ComVisible(true)]
		public class SkinNodeSorter : IComparer
		{
			public int Compare(object x, object y)
			{
				TreeNode l = (TreeNode)x;
				TreeNode r = (TreeNode)y;

				string ls, lr;

				if (l is Skin)
					ls = ((Skin)l).Name;
				else
					ls = l.Text;

				if (r is Skin)
					lr = ((Skin)r).Name;
				else
					lr = r.Text;

				return ls.CompareTo(lr);
			}
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			InitShortcuts();
			LoadShortcutKeys(GlobalSettings.ShortcutKeys);

			SetTool(Tools.Camera);
			SetTransparencyMode(GlobalSettings.Transparency);
			SetViewMode(_currentViewMode);

			glControl1.MakeCurrent();

			/*foreach (var file in Directory.EnumerateFiles("./Skins/", "*.png"))
				treeView1.Items.Add(new Skin(file));

			treeView1.SelectedIndex = 0;
			foreach (var skin in treeView1.Items)
			{
				Skin s = (Skin)skin;

				if (s.FileName.Equals(GlobalSettings.LastSkin, StringComparison.CurrentCultureIgnoreCase))
				{
					treeView1.SelectedNode = s;
					break;
				}
			}*/

			RecurseAddDirectories("Skins", treeView1.Nodes);

			SetColor(Color.White);

			treeView1.DrawMode = TreeViewDrawMode.OwnerDrawAll;
			treeView1.ItemHeight = 23;
			treeView1.DrawNode += new DrawTreeNodeEventHandler(treeView1_DrawNode);
			treeView1.FullRowSelect = true;
			treeView1.HotTracking = true;
			treeView1.TreeViewNodeSorter = new SkinNodeSorter();

			SetVisibleParts();
		}

		class DoubleBufferedTreeView : TreeView
		{
			public DoubleBufferedTreeView()
			{
				SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
				DoubleBuffered = true;
			}

			[DllImport("user32.dll", CharSet = CharSet.Auto)]
			public static extern int GetScrollPos(int hWnd, int nBar);

			[DllImport("user32.dll")]
			static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);

			private const int SB_HORZ = 0x0;
			private const int SB_VERT = 0x1;

			Point ScrollPosition
			{
				get
				{
					return new Point(
						GetScrollPos((int)Handle, SB_HORZ),
						GetScrollPos((int)Handle, SB_VERT));
				}

				set
				{
					SetScrollPos((IntPtr)Handle, SB_HORZ, value.X, true);
					SetScrollPos((IntPtr)Handle, SB_VERT, value.Y, true);
				}
			}

			int _numVisible = 0;
			protected override void OnSizeChanged(EventArgs e)
			{
				_numVisible = (int)Math.Ceiling((float)Height / (float)ItemHeight);
				base.OnSizeChanged(e);
			}

			TreeNode GetSelectedNodeAt(int y, TreeNode node, ref int currentIndex)
			{
				if (currentIndex >= ScrollPosition.Y + _numVisible)
					return null;

				if (y <= node.Bounds.Y + ItemHeight)
					return node;

				currentIndex++;

				if (node.IsExpanded)
				foreach (TreeNode child in node.Nodes)
				{
					var tryNode = GetSelectedNodeAt(y, child, ref currentIndex);

					if (tryNode != null)
						return tryNode;
				}

				return null;
			}

			TreeNode lastClick = null;
			bool lastOpened = false;
			protected override void OnMouseDoubleClick(MouseEventArgs e)
			{
				base.OnMouseDoubleClick(e);

				if (SelectedNode == lastClick && lastClick.IsExpanded == lastOpened)
					lastClick.Toggle();
			}

			protected override void OnMouseDown(MouseEventArgs e)
			{
				base.OnMouseDown(e);

				var node = GetSelectedNodeAt(e.Location);

				if (node != null)
					SelectedNode = node;

				lastClick = SelectedNode;
				lastOpened = lastClick == null ? false : lastClick.IsExpanded;
			}

			protected override void OnMouseClick(MouseEventArgs e)
			{
				base.OnMouseClick(e);
			}

			TreeNode _hoverNode;
			Point _hoverPoint;
			protected override void OnMouseMove(MouseEventArgs e)
			{
				_hoverPoint = e.Location;
				var hover = GetSelectedNodeAt(e.Location);
				if (_hoverNode == null || _hoverNode != hover)
					_hoverNode = hover;

				Invalidate();

				base.OnMouseMove(e);
			}

			private TreeNode GetSelectedNodeAt(Point p)
			{
				int currentIndex = 0;

				TreeNode node = null;
				foreach (TreeNode child in Nodes)
				{
					node = GetSelectedNodeAt(p.Y, child, ref currentIndex);

					if (node != null)
						break;
				}

				return node;
			}

			void RecursiveDrawCheck(PaintEventArgs args, TreeNode node, ref int currentIndex)
			{
				TreeNodeStates state = 0;

				if (_hoverNode == node)
					state |= TreeNodeStates.Hot;

				OnDrawNode(new DrawTreeNodeEventArgs(args.Graphics, node, new Rectangle(0, node.Bounds.Y, Width, ItemHeight), state));
				currentIndex++;

				if (node.IsExpanded)
				foreach (TreeNode child in node.Nodes)
					RecursiveDrawCheck(args, child, ref currentIndex);
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				int currentIndex = 0;
				foreach (TreeNode n in Nodes)
					RecursiveDrawCheck(e, n, ref currentIndex);
			}
		}

		void treeView1_DrawNode(object sender, DrawTreeNodeEventArgs e)
		{
			if (e.Bounds.Width == 0 || e.Bounds.Height == 0)
				return;

			int realX = e.Bounds.X + ((e.Node.Level + 1) * 20);

			e.Graphics.FillRectangle(new SolidBrush(treeView1.BackColor), 0, e.Bounds.Y, treeView1.Width, e.Bounds.Height);
		
			if (e.Node.IsSelected)
				e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(127, SystemColors.Highlight)), realX, e.Bounds.Y, treeView1.Width, e.Bounds.Height);
		
			Skin skin = e.Node is Skin ? (Skin)e.Node : null;

			if (skin == null)
			{
				if (e.Node.IsExpanded)
					e.Graphics.DrawImage(Properties.Resources.FolderOpen_32x32_72, realX, e.Bounds.Y, treeView1.ItemHeight, treeView1.ItemHeight);
				else
					e.Graphics.DrawImage(Properties.Resources.Folder_32x32, realX, e.Bounds.Y, treeView1.ItemHeight, treeView1.ItemHeight);
			}
			else
			{
				e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
				e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
				e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

				e.Graphics.DrawImage(skin.Head, realX, e.Bounds.Y, treeView1.ItemHeight, treeView1.ItemHeight);
			}

			if (skin == null)
			{
				if (e.Node.IsExpanded)
				{
					if ((e.State & TreeNodeStates.Hot) != 0)
						e.Graphics.DrawImage(Properties.Resources.arrow_state_blue_expanded, new Rectangle(realX - 13, e.Bounds.Y + (treeView1.ItemHeight / 2) - (16 / 2), 16, 16));
					else
						e.Graphics.DrawImage(Properties.Resources.arrow_state_grey_expanded, new Rectangle(realX - 13, e.Bounds.Y + (treeView1.ItemHeight / 2) - (16 / 2), 16, 16));
				}
				else
				{
					if ((e.State & TreeNodeStates.Hot) != 0)
						e.Graphics.DrawImage(Properties.Resources.arrow_state_blue_right, new Rectangle(realX - 13, e.Bounds.Y + (treeView1.ItemHeight / 2) - (16 / 2), 16, 16));
					else
						e.Graphics.DrawImage(Properties.Resources.arrow_state_grey_right, new Rectangle(realX - 13, e.Bounds.Y + (treeView1.ItemHeight / 2) - (16 / 2), 16, 16));
				}
			}

			string text = (skin == null) ? e.Node.Text : skin.ToString();

			TextRenderer.DrawText(e.Graphics, text, treeView1.Font, new Rectangle(realX + treeView1.ItemHeight + 1, e.Bounds.Y, treeView1.Width, e.Bounds.Height), (e.Node.IsSelected) ? Color.White : Color.Black, TextFormatFlags.VerticalCenter);
		}
		#endregion

		// =====================================================================
		// Private functions
		// =====================================================================
		#region Private Functions
		// Utility function, sets a tool strip checkbox item's state if the flag is present
		void SetCheckbox(VisiblePartFlags flag, ToolStripMenuItem checkbox)
		{
			if ((GlobalSettings.ViewFlags & flag) != 0)
				checkbox.Checked = true;
			else
				checkbox.Checked = false;
		}

		int GetPaintTexture(int width, int height)
		{
			if (!_charPaintSizes.ContainsKey(new Size(width, height)))
			{
				int id = GL.GenTexture();

				byte[] arra = new byte[width * height * 4];
				unsafe
				{
					fixed (byte* texData = arra)
					{
						byte *d = texData;

						for (int y = 0; y < height; ++y)
							for (int x = 0; x < width; ++x)
							{
								*((int*)d) = (x << 0) | (y << 8) | (0 << 16) | (255 << 24);
								d += 4;
							}
					}
				}

				GL.BindTexture(TextureTarget.Texture2D, id);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, arra);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);

				_charPaintSizes.Add(new Size(width, height), id);

				return id;
			}

			return _charPaintSizes[new Size(width, height)];
		}

		void DrawSkinnedRectangle
			(float x, float y, float z, float width, float length, float height,
			int frontSkinX, int frontSkinY, int frontSkinW, int frontSkinH,
			int backSkinX, int backSkinY, int backSkinW, int backSkinH,
			int topSkinX, int topSkinY, int topSkinW, int topSkinH,
			int bottomSkinX, int bottomSkinY, int bottomSkinW, int bottomSkinH,
			int leftSkinX, int leftSkinY, int leftSkinW, int leftSkinH,
			int rightSkinX, int rightSkinY, int rightSkinW, int rightSkinH,
			int texture, int skinW = 64, int skinH = 32)
		{
			GL.BindTexture(TextureTarget.Texture2D, texture);

			GL.Begin(BeginMode.Quads);

			width /= 2;
			length /= 2;
			height /= 2;

			float fsX = (float)frontSkinX / skinW;
			float fsY = (float)frontSkinY / skinH;
			float fsW = (float)frontSkinW / skinW;
			float fsH = (float)frontSkinH / skinH;

			float basX = (float)backSkinX / skinW;
			float basY = (float)backSkinY / skinH;
			float basW = (float)backSkinW / skinW;
			float basH = (float)backSkinH / skinH;

			float tsX = (float)topSkinX / skinW;
			float tsY = (float)topSkinY / skinH;
			float tsW = (float)topSkinW / skinW;
			float tsH = (float)topSkinH / skinH;

			float bsX = (float)bottomSkinX / skinW;
			float bsY = (float)bottomSkinY / skinH;
			float bsW = (float)bottomSkinW / skinW;
			float bsH = (float)bottomSkinH / skinH;

			float lsX = (float)leftSkinX / skinW;
			float lsY = (float)leftSkinY / skinH;
			float lsW = (float)leftSkinW / skinW;
			float lsH = (float)leftSkinH / skinH;

			float rsX = (float)rightSkinX / skinW;
			float rsY = (float)rightSkinY / skinH;
			float rsW = (float)rightSkinW / skinW;
			float rsH = (float)rightSkinH / skinH;

			// Front Face
			if (texture != _grassTop)
			{
				GL.TexCoord2(fsX, fsY); GL.Vertex3(x - width, y + length, z + height);  // Bottom Left Of The Texture and Quad
				GL.TexCoord2(fsX + fsW - 0.00005, fsY); GL.Vertex3(x + width, y + length, z + height);  // Bottom Right Of The Texture and Quad
				GL.TexCoord2(fsX + fsW - 0.00005, fsY + fsH - 0.00005); GL.Vertex3(x + width, y - length, z + height);  // Top Right Of The Texture and Quad
				GL.TexCoord2(fsX, fsY + fsH - 0.00005); GL.Vertex3(x - width, y - length, z + height);  // Top Left Of The Texture and Quad
			}
			GL.TexCoord2(tsX, tsY); GL.Vertex3(x - width, y + length, z - height);          // Top Right Of The Quad (Top)
			GL.TexCoord2(tsX + tsW - 0.00005, tsY); GL.Vertex3(x + width, y + length, z - height);          // Top Left Of The Quad (Top)
			GL.TexCoord2(tsX + tsW - 0.00005, tsY + tsH - 0.00005); GL.Vertex3(x + width, y + length, z + height);          // Bottom Left Of The Quad (Top)
			GL.TexCoord2(tsX, tsY + tsH - 0.00005); GL.Vertex3(x - width, y + length, z + height);          // Bottom Right Of The Quad (Top)

			if (texture != _grassTop)
			{
				GL.TexCoord2(bsX, bsY); GL.Vertex3(x - width, y - length, z + height);          // Top Right Of The Quad (Top)
				GL.TexCoord2(bsX + bsW - 0.00005, bsY); GL.Vertex3(x + width, y - length, z + height);          // Top Left Of The Quad (Top)
				GL.TexCoord2(bsX + bsW - 0.00005, bsY + bsH - 0.00005); GL.Vertex3(x + width, y - length, z - height);          // Bottom Left Of The Quad (Top)
				GL.TexCoord2(bsX, bsY + bsH - 0.00005); GL.Vertex3(x - width, y - length, z - height);          // Bottom Right Of The Quad (Top)

				GL.TexCoord2(lsX, lsY); GL.Vertex3(x - width, y + length, z - height);          // Top Right Of The Quad (Left)
				GL.TexCoord2(lsX + lsW - 0.00005, lsY); GL.Vertex3(x - width, y + length, z + height);          // Top Left Of The Quad (Left)
				GL.TexCoord2(lsX + lsW - 0.00005, lsY + lsH - 0.00005); GL.Vertex3(x - width, y - length, z + height);          // Bottom Left Of The Quad (Left)
				GL.TexCoord2(lsX, lsY + lsH - 0.00005); GL.Vertex3(x - width, y - length, z - height);          // Bottom Right Of The Quad (Left)

				GL.TexCoord2(rsX, rsY); GL.Vertex3(x + width, y + length, z + height);          // Top Right Of The Quad (Left)
				GL.TexCoord2(rsX + rsW - 0.00005, rsY); GL.Vertex3(x + width, y + length, z - height);          // Top Left Of The Quad (Left)
				GL.TexCoord2(rsX + rsW - 0.00005, rsY + rsH - 0.00005); GL.Vertex3(x + width, y - length, z - height);          // Bottom Left Of The Quad (Left)
				GL.TexCoord2(rsX, rsY + rsH - 0.00005); GL.Vertex3(x + width, y - length, z + height);          // Bottom Right Of The Quad (Left)

				GL.TexCoord2(basX, basY); GL.Vertex3(x + width, y + length, z - height);  // Bottom Left Of The Texture and Quad
				GL.TexCoord2(basX + basW - 0.00005, basY); GL.Vertex3(x - width, y + length, z - height);  // Bottom Right Of The Texture and Quad
				GL.TexCoord2(basX + basW - 0.00005, basY + basH - 0.00005); GL.Vertex3(x - width, y - length, z - height);  // Top Right Of The Texture and Quad
				GL.TexCoord2(basX, basY + basH - 0.00005); GL.Vertex3(x + width, y - length, z - height);  // Top Left Of The Texture and Quad		
			}

			GL.End();
		}

		void DrawPlayer(int tex, Skin skin, bool grass, bool pickView)
		{
			if (_currentViewMode == ViewMode.Orthographic)
			{
				if (!pickView && GlobalSettings.AlphaCheckerboard)
				{
					GL.BindTexture(TextureTarget.Texture2D, _alphaTex);

					GL.Begin(BeginMode.Quads);
					GL.TexCoord2(0, 0); GL.Vertex2(0, 0);
					GL.TexCoord2(glControl1.Width / 32.0f, 0); GL.Vertex2(glControl1.Width, 0);
					GL.TexCoord2(glControl1.Width / 32.0f, glControl1.Height / 32.0f); GL.Vertex2(glControl1.Width, glControl1.Height);
					GL.TexCoord2(0, glControl1.Height / 32.0f); GL.Vertex2(0, glControl1.Height);
					GL.End();
				}

				if (skin != null)
					GL.BindTexture(TextureTarget.Texture2D, tex);

				GL.PushMatrix();

				GL.Translate((glControl1.Width / 2) + -_2dCamOffsetX, (glControl1.Height / 2) + -_2dCamOffsetY, 0);
				GL.Scale(_2dZoom, _2dZoom, 1);

				GL.Enable(EnableCap.Blend);

				if (skin != null)
				{
					float w = skin.Width;
					float h = skin.Height;
					GL.PushMatrix();
					GL.Translate((_2dCamOffsetX), (_2dCamOffsetY), 0);
					GL.Begin(BeginMode.Quads);
					GL.TexCoord2(0, 0); GL.Vertex2(-(skin.Width / 2), -(skin.Height / 2));
					GL.TexCoord2(1, 0); GL.Vertex2((skin.Width / 2), -(skin.Height / 2));
					GL.TexCoord2(1, 1); GL.Vertex2((skin.Width / 2), (skin.Height / 2));
					GL.TexCoord2(0, 1); GL.Vertex2(-(skin.Width / 2), (skin.Height / 2));
					GL.End();
				}

				if (!pickView && GlobalSettings.TextureOverlay)
				{
					GL.BindTexture(TextureTarget.Texture2D, _backgroundTex);

					GL.Begin(BeginMode.Quads);
					GL.TexCoord2(0, 0); GL.Vertex2(-(skin.Width / 2), -(skin.Height / 2));
					GL.TexCoord2(1, 0); GL.Vertex2((skin.Width / 2), -(skin.Height / 2));
					GL.TexCoord2(1, 1); GL.Vertex2((skin.Width / 2), (skin.Height / 2));
					GL.TexCoord2(0, 1); GL.Vertex2(-(skin.Width / 2), (skin.Height / 2));
					GL.End();
				}
				GL.PopMatrix();

				GL.PopMatrix();

				GL.Disable(EnableCap.Blend);

				return;
			}

			Vector3 vec = new Vector3();
			int count = 0;

			if ((GlobalSettings.ViewFlags & VisiblePartFlags.HeadFlag) != 0)
			{
				vec = Vector3.Add(vec, new Vector3(0, 10, 0));
				count++;
			}

			if ((GlobalSettings.ViewFlags & VisiblePartFlags.ChestFlag) != 0)
			{
				vec = Vector3.Add(vec, new Vector3(0, 0, 0));
				count++;
			}

			if ((GlobalSettings.ViewFlags & VisiblePartFlags.RightLegFlag) != 0)
			{
				vec = Vector3.Add(vec, new Vector3(-2, -12, 0));
				count++;
			}

			if ((GlobalSettings.ViewFlags & VisiblePartFlags.LeftLegFlag) != 0)
			{
				vec = Vector3.Add(vec, new Vector3(2, -12, 0));
				count++;
			}

			if ((GlobalSettings.ViewFlags & VisiblePartFlags.RightArmFlag) != 0)
			{
				vec = Vector3.Add(vec, new Vector3(-6, 0, 0));
				count++;
			}

			if ((GlobalSettings.ViewFlags & VisiblePartFlags.LeftArmFlag) != 0)
			{
				vec = Vector3.Add(vec, new Vector3(6, 0, 0));
				count++;
			}

			if ((GlobalSettings.ViewFlags & VisiblePartFlags.HelmetFlag) != 0)
			{
				vec = Vector3.Add(vec, new Vector3(0, 10, 0));
				count++;
			}

			if (count != 0)
				vec = Vector3.Divide(vec, count);

			GL.Translate(0, 0, _3dZoom);
			GL.Rotate(_3dRotationX, 1, 0, 0);
			GL.Rotate(_3dRotationY, 0, 1, 0);

			GL.Translate(-vec.X, -vec.Y, 0);
			GL.PushMatrix();

			var clPt = glControl1.PointToClient(Cursor.Position);
			var x = clPt.X - (glControl1.Width / 2);
			var y = clPt.Y - (glControl1.Height / 2);

			if (!pickView && GlobalSettings.Transparency == TransparencyMode.All)
				GL.Enable(EnableCap.Blend);
			else
				GL.Disable(EnableCap.Blend);

			if (grass)
				DrawSkinnedRectangle(0, -20, 0, 1024, 4, 1024, 0, 0, 1024, 1024, 0, 0, 0, 0, 0, 0, 1024, 1024, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, _grassTop, 16, 16);
		
			GL.PushMatrix();

			if (skin != null)
			{
				if (followCursorToolStripMenuItem.Checked)
				{
					GL.Translate(0, 4, 0);
					GL.Rotate((float)x / 25, 0, 1, 0);
					GL.Rotate((float)y / 25, 1, 0, 0);
					GL.Translate(0, -4, 0);
				}

				if ((GlobalSettings.ViewFlags & VisiblePartFlags.HeadFlag) != 0)
					DrawSkinnedRectangle(0, 10, 0, 8, 8, 8,
					8, 8, 8, 8,
					24, 8, 8, 8,
					8, 0, 8, 8,
					16, 0, 8, 8,
					0, 8, 8, 8,
					16, 8, 8, 8,
					tex);
				GL.PopMatrix();

				if ((GlobalSettings.ViewFlags & VisiblePartFlags.ChestFlag) != 0)
					DrawSkinnedRectangle(0, 0, 0, 8, 12, 4,
					20, 20, 8, 12,
					32, 20, 8, 12,
					20, 16, 8, 4,
					28, 16, 8, 4,
					16, 20, 4, 12,
					28, 20, 4, 12,
					tex);

				// right
				GL.PushMatrix();
				if (animateToolStripMenuItem.Checked)
				{
					GL.Translate(0, -6, 0);
					GL.Rotate(Math.Sin(_animationTime) * 37, 1, 0, 0);
					GL.Translate(0, 6, 0);
				}
				if ((GlobalSettings.ViewFlags & VisiblePartFlags.RightLegFlag) != 0)
					DrawSkinnedRectangle(-2, -12, 0, 4, 12, 4,
					4, 20, 4, 12,
					12, 20, 4, 12,
					4, 16, 4, 4,
					8, 16, 4, 4,
					0, 20, 4, 12,
					8, 20, 4, 12,
					tex);
				GL.PopMatrix();

				// left
				GL.PushMatrix();
				if (animateToolStripMenuItem.Checked)
				{
					GL.Translate(0, -6, 0);
					GL.Rotate(Math.Sin(_animationTime) * -37, 1, 0, 0);
					GL.Translate(0, 6, 0);
				}
				if ((GlobalSettings.ViewFlags & VisiblePartFlags.LeftLegFlag) != 0)
					DrawSkinnedRectangle(2, -12, 0, 4, 12, 4,
					8, 20, -4, 12,
					16, 20, -4, 12,
					8, 16, -4, 4,
					12, 16, -4, 4,
					12, 20, -4, 12,
					4, 20, -4, 12,
					tex);
				GL.PopMatrix();

				// right arm
				GL.PushMatrix();
				if (animateToolStripMenuItem.Checked)
				{
					GL.Translate(0, 5, 0);
					GL.Rotate(Math.Sin(_animationTime) * -37, 1, 0, 0);
					GL.Translate(0, -5, 0);
				}
				if ((GlobalSettings.ViewFlags & VisiblePartFlags.RightArmFlag) != 0)
					DrawSkinnedRectangle(-6, 0, 0, 4, 12, 4,
					44, 20, 4, 12,
					52, 20, 4, 12,
					44, 16, 4, 4,
					48, 16, 4, 4,
					40, 20, 4, 12,
					48, 20, 4, 12,
					tex);
				GL.PopMatrix();

				GL.PushMatrix();
				if (animateToolStripMenuItem.Checked)
				{
					GL.Translate(0, 5, 0);
					GL.Rotate(Math.Sin(_animationTime) * 37, 1, 0, 0);
					GL.Translate(0, -5, 0);
				}
				// left arm
				if ((GlobalSettings.ViewFlags & VisiblePartFlags.LeftArmFlag) != 0)
					DrawSkinnedRectangle(6, 0, 0, 4, 12, 4,
					48, 20, -4, 12,
					56, 20, -4, 12,
					48, 16, -4, 4,
					52, 16, -4, 4,
					52, 20, -4, 12,
					44, 20, -4, 12,
					tex);
				GL.PopMatrix();

				if ((GlobalSettings.ViewFlags & VisiblePartFlags.HelmetFlag) != 0)
				{
					GL.PushMatrix();
					if (followCursorToolStripMenuItem.Checked)
					{
						GL.Translate(0, 4, 0);
						GL.Rotate((float)x / 25, 0, 1, 0);
						GL.Rotate((float)y / 25, 1, 0, 0);
						GL.Translate(0, -4, 0);
					}

					if (!pickView && GlobalSettings.Transparency != TransparencyMode.Off)
						GL.Enable(EnableCap.Blend);
					else
						GL.Disable(EnableCap.Blend);

					DrawSkinnedRectangle(0, 10, 0, 9, 9, 9,
										32 + 8, 8, 8, 8,
										32 + 24, 8, 8, 8,
										32 + 8, 0, 8, 8,
										32 + 16, 0, 8, 8,
										32 + 0, 8, 8, 8,
										32 + 16, 8, 8, 8,
										tex);
					GL.PopMatrix();
				}
			}

			GL.PopMatrix();
		}

		void SetPreview()
		{
			if (_lastSkin == null)
			{
				int[] array = new int[64 * 32];
				GL.BindTexture(TextureTarget.Texture2D, _previewPaint);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 64, 32, 0, PixelFormat.Rgba, PixelType.UnsignedByte, array);
			}
			else
			{
				Skin skin = _lastSkin;

				GL.BindTexture(TextureTarget.Texture2D, GlobalDirtiness.CurrentSkin);
				int[] array = new int[skin.Width * skin.Height];
				GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgba, PixelType.UnsignedByte, array);

				if (_currentTool == Tools.Pencil || _currentTool == Tools.Eraser || _currentTool == Tools.Burn || _currentTool == Tools.Dodge)
				{
					Point p = Point.Empty;

					if (GetPick(_mousePoint.X, _mousePoint.Y, ref p))
						UseToolOnPixel(array, skin, p.X, p.Y);
				}

				GL.BindTexture(TextureTarget.Texture2D, _previewPaint);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, skin.Width, skin.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, array);
			}
		}

		bool GetPick(int x, int y, ref Point hitPixel)
		{
			glControl1.MakeCurrent();
			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadIdentity();

			GL.ClearColor(Color.White);
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			GL.ClearColor(GlobalSettings.BackgroundColor);

			var skin = _lastSkin;

			DrawPlayer(GetPaintTexture(skin.Width, skin.Height), skin, false, true);

			int[] viewport = new int[4];
			byte[] pixel = new byte[3];

			GL.GetInteger(GetPName.Viewport, viewport);

			GL.ReadPixels(x, viewport[3] - y, 1, 1,
				PixelFormat.Rgb, PixelType.UnsignedByte, pixel);

			if (pixel[2] == 0)
			{
				hitPixel = new Point(pixel[0], pixel[1]);
				return true;
			}

			return false;
		}

		Color UseToolOnPixel(int[] pixels, Skin skin, int x, int y)
		{
			int pixNum = x + (skin.Width * y);
			var c = pixels[pixNum];
			var oldColor = Color.FromArgb((c >> 24) & 0xFF, (c >> 0) & 0xFF, (c >> 8) & 0xFF, (c >> 16) & 0xFF);
			Color newColor = Color.White;

			// blend
			if (_currentTool == Tools.Pencil)
				newColor = Color.FromArgb(ColorBlending.AlphaBlend(_currentColor, oldColor).ToArgb());
			else if (_currentTool == Tools.Burn)
				newColor = Color.FromArgb(ColorBlending.Burn(oldColor, 0.75f).ToArgb());
			else if (_currentTool == Tools.Dodge)
				newColor = Color.FromArgb(ColorBlending.Dodge(oldColor, 0.25f).ToArgb());
			else if (_currentTool == Tools.Eraser)
				newColor = Color.FromArgb(0);

			pixels[pixNum] = newColor.R | (newColor.G << 8) | (newColor.B << 16) | (newColor.A << 24);
			return newColor;
		}

		void UseToolOnViewport(int x, int y)
		{
			if (_lastSkin == null)
				return;

			Point p = Point.Empty;

			if (GetPick(x, y, ref p))
			{
				Skin skin = _lastSkin;

				GL.BindTexture(TextureTarget.Texture2D, GlobalDirtiness.CurrentSkin);
				int[] array = new int[skin.Width * skin.Height];
				GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgba, PixelType.UnsignedByte, array);

				if (_currentTool == Tools.Pencil || _currentTool == Tools.Eraser || _currentTool == Tools.Burn || _currentTool == Tools.Dodge)
				{
					if (_changedPixels == null)
					{
						_changedPixels = new PixelsChangedUndoable();
					}

					if (!_changedPixels.Points.ContainsKey(new Point(p.X, p.Y)))
					{
						var c = array[p.X + (skin.Width * p.Y)];
						var oldColor = Color.FromArgb((c >> 24) & 0xFF, (c >> 0) & 0xFF, (c >> 8) & 0xFF, (c >> 16) & 0xFF);
						_changedPixels.Points[new Point(p.X, p.Y)] = Tuple.MakeTuple
							(oldColor,
							UseToolOnPixel(array, skin, p.X, p.Y));

						SetCanSave(true);
						skin.Dirty = true;
						GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, skin.Width, skin.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, array);
					}
				}
				else if (_currentTool == Tools.Dropper)
				{
					var c = array[p.X + (skin.Width * p.Y)];
					SetColor(Color.FromArgb((c >> 24) & 0xFF, (c >> 0) & 0xFF, (c >> 8) & 0xFF, (c >> 16) & 0xFF));
				}
			}

			glControl1.Invalidate();
		}

		#region File uploading (FIXME: REMOVE)
		public static Exception HttpUploadFile(string url, string file, string paramName, string contentType, Dictionary<string, byte[]> nvc, CookieContainer cookies)
		{
			//log.Debug(string.Format("Uploading {0} to {1}", file, url));
			string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
			byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

			HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
			wr.ContentType = "multipart/form-data; boundary=" + boundary;
			wr.Method = "POST";
			wr.KeepAlive = true;
			wr.CookieContainer = cookies;
			wr.Credentials = System.Net.CredentialCache.DefaultCredentials;
			wr.Timeout = 10000;

			Stream rs = wr.GetRequestStream();

			string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
			foreach (var kvp in nvc)
			{
				rs.Write(boundarybytes, 0, boundarybytes.Length);
				string formitem = string.Format(formdataTemplate, kvp.Key, Encoding.ASCII.GetString(kvp.Value));
				byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
				rs.Write(formitembytes, 0, formitembytes.Length);
			}
			rs.Write(boundarybytes, 0, boundarybytes.Length);

			string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
			string header = string.Format(headerTemplate, paramName, Path.GetFileName(file), contentType);
			byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
			rs.Write(headerbytes, 0, headerbytes.Length);

			FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
			byte[] buffer = new byte[4096];
			int bytesRead = 0;
			while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
			{
				rs.Write(buffer, 0, bytesRead);
			}
			fileStream.Close();

			byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
			rs.Write(trailer, 0, trailer.Length);
			rs.Close();

			WebResponse wresp = null;
			Exception ret = null;
			try
			{
				wresp = wr.GetResponse();
				Stream stream2 = wresp.GetResponseStream();
				StreamReader reader2 = new StreamReader(stream2);
				//log.Debug(string.Format("File uploaded, server response is: {0}", reader2.ReadToEnd()));
			}
			catch (Exception ex)
			{
				//log.Error("Error uploading file", ex);
				if (wresp != null)
				{
					wresp.Close();
					wresp = null;
				}

				ret = ex;
			}
			finally
			{
				wr = null;
			}

			return ret;
		}

		public enum ErrorCodes
		{
			Succeeded,
			TimeOut,
			WrongCredentials,
			Unknown
		}

		class ErrorReturn
		{
			public ErrorCodes Code;
			public Exception Exception;
			public string ReportedError;
		}

		void UploadThread(object param)
		{
			var parms = (object[])param;
			ErrorReturn error = (ErrorReturn)parms[3];

			error.Code = ErrorCodes.Succeeded;
			error.Exception = null;
			error.ReportedError = null;

			try
			{
				CookieContainer cookies = new CookieContainer();
				var request = (HttpWebRequest)HttpWebRequest.Create("http://www.minecraft.net/login");
				request.CookieContainer = cookies;
				request.Timeout = 10000;
				var response = request.GetResponse();
				StreamReader sr = new StreamReader(response.GetResponseStream());
				var text = sr.ReadToEnd();

				var match = Regex.Match(text, @"<input type=""hidden"" name=""authenticityToken"" value=""(.*?)"">");
				string authToken = null;
				if (match.Success)
					authToken = match.Groups[1].Value;

				if (authToken == null)
					return;

				sr.Dispose();

				response.Close();

				string requestTemplate = @"authenticityToken={0}&redirect=http%3A%2F%2Fwww.minecraft.net%2Fprofile&username={1}&password={2}";
				string requestContent = string.Format(requestTemplate, authToken, parms[0].ToString(), parms[1].ToString());
				var inBytes = Encoding.UTF8.GetBytes(requestContent);

				// craft the login request
				request = (HttpWebRequest)HttpWebRequest.Create("https://www.minecraft.net/login");
				request.Method = "POST";
				request.ContentType = "application/x-www-form-urlencoded";
				request.CookieContainer = cookies;
				request.ContentLength = inBytes.Length;
				request.Timeout = 10000;

				using (Stream dataStream = request.GetRequestStream())
					dataStream.Write(inBytes, 0, inBytes.Length);

				response = request.GetResponse();
				sr = new StreamReader(response.GetResponseStream());
				text = sr.ReadToEnd();

				match = Regex.Match(text, @"<p class=""error"">([\w\W]*?)</p>");

				sr.Dispose();
				response.Close();

				if (match.Success)
				{
					error.ReportedError = match.Groups[1].Value.Trim();
					error.Code = ErrorCodes.WrongCredentials;
				}
				else
				{
					var dict = new Dictionary<string, byte[]>();
					dict.Add("authenticityToken", Encoding.ASCII.GetBytes(authToken));
					if ((error.Exception = HttpUploadFile("http://www.minecraft.net/profile/skin", parms[2].ToString(), "skin", "image/png", dict, cookies)) != null)
						error.Code = ErrorCodes.Unknown;
				}
			}
			catch (Exception ex)
			{
				error.Exception = ex;
			}
			finally
			{
				Invoke((Action)delegate() { _pleaseWaitForm.Close(); });
			}
		}

		void PerformUpload()
		{
			if (_lastSkin == null)
				return;

			using (Login login = new Login())
			{
				login.Username = GlobalSettings.LastUsername;
				login.Password = GlobalSettings.LastPassword;

				bool dialogRes = true;
				bool didShowDialog = false;

				if ((ModifierKeys & Keys.Shift) != 0 || !GlobalSettings.RememberMe || !GlobalSettings.AutoLogin)
				{
					login.Remember = GlobalSettings.RememberMe;
					login.AutoLogin = GlobalSettings.AutoLogin;
					dialogRes = login.ShowDialog() == System.Windows.Forms.DialogResult.OK;
					didShowDialog = true;
				}

				if (!dialogRes)
					return;

				_pleaseWaitForm = new PleaseWait();

				Thread thread = new Thread(UploadThread);
				ErrorReturn ret = new ErrorReturn();
				thread.Start(new object[] { login.Username, login.Password, _lastSkin.File.FullName, ret });

				_pleaseWaitForm.ShowDialog();
				_pleaseWaitForm.Dispose();

				if (ret.ReportedError != null)
					MessageBox.Show("Error uploading skin:\r\n" + ret.ReportedError, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				else if (ret.Exception != null)
					MessageBox.Show("Error uploading skin:\r\n" + ret.Exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				else
				{
					MessageBox.Show("Skin upload success! Enjoy!", "Woo!", MessageBoxButtons.OK, MessageBoxIcon.Information);
					GlobalSettings.LastSkin = _lastSkin.File.FullName;
					treeView1.Invalidate();
				}

				if (didShowDialog)
				{
					GlobalSettings.RememberMe = login.Remember;
					GlobalSettings.AutoLogin = login.AutoLogin;

					if (GlobalSettings.RememberMe == false)
						GlobalSettings.LastUsername = GlobalSettings.LastPassword = null;
					else
					{
						GlobalSettings.LastUsername = login.Username;
						GlobalSettings.LastPassword = login.Password;
					}
				}
			}
		}
		#endregion

		void ToggleAnimation()
		{
			animateToolStripMenuItem.Checked = !animateToolStripMenuItem.Checked;
			GlobalSettings.Animate = animateToolStripMenuItem.Checked;

			Invalidate();
		}

		void ToggleFollowCursor()
		{
			followCursorToolStripMenuItem.Checked = !followCursorToolStripMenuItem.Checked;
			GlobalSettings.FollowCursor = followCursorToolStripMenuItem.Checked;

			Invalidate();
		}

		void ToggleGrass()
		{
			grassToolStripMenuItem.Checked = !grassToolStripMenuItem.Checked;
			GlobalSettings.Grass = grassToolStripMenuItem.Checked;

			glControl1.Invalidate();
		}

		#region Skin Management
		void PerformImportSkin()
		{
			using (var ofd = new OpenFileDialog())
			{
				ofd.Filter = "Minecraft Skins|*.png";
				ofd.Multiselect = true;

				if (ofd.ShowDialog() == DialogResult.OK)
				{
					foreach (var f in ofd.FileNames)
					{
						var name = Path.GetFileNameWithoutExtension(f);

						while (File.Exists("./Skins/" + name + ".png"))
							name += " (New)";

						File.Copy(f, "./Skins/" + name + ".png");

						Skin skin = new Skin("./Skins/" + name + ".png");
						//treeView1.Items.Add(skin);
						//treeView1.SelectedNode = skin;
					}
				}

				//treeView1.Sorted = false;
				//treeView1.Sorted = true;
			}
		}

		void PerformDeleteSkin()
		{
			if (_lastSkin == null)
				return;

			if (MessageBox.Show("Delete this skin perminently?\r\nThis will delete the skin from the Skins directory!", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
			{
				Skin skin = _lastSkin;
				/*if (treeView1.Items.Count != 1)
				{
					if (treeView1.SelectedIndex == treeView1.Items.Count - 1)
						treeView1.SelectedIndex--;
					else
						treeView1.SelectedIndex++;
				}
				treeView1.Items.Remove(skin);*/

				_lastSkin.File.Delete();

				Invalidate();
			}
		}

		void PerformCloneSkin()
		{
			if (_lastSkin == null)
				return;

			Skin skin = _lastSkin;
			string newName = skin.Name;
			string newFileName;

			do
			{
				newName += " - Copy";
				newFileName = skin.Directory.FullName + '\\' + newName + ".png";
			} while (File.Exists(newFileName));

			File.Copy(skin.File.FullName, newFileName);
			Skin newSkin = new Skin(newFileName);
			//treeView1.Items.Add(newSkin);
		}

		TreeNode _currentlyEditing = null;
		void PerformNameChange()
		{
			if (treeView1.SelectedNode != null)
			{
				_currentlyEditing = treeView1.SelectedNode;

				if (_currentlyEditing is Skin)
					labelEditTextBox.Text = ((Skin)_currentlyEditing).Name;

				labelEditTextBox.Location = new Point(treeView1.SelectedNode.Bounds.Location.X + 26, treeView1.SelectedNode.Bounds.Location.Y + 4);
				labelEditTextBox.BringToFront();
				labelEditTextBox.Show();
				labelEditTextBox.Focus();
			}

/*			if (_lastSkin == null)
				return;

			var skin = _lastSkin;

			using (NameChange nc = new NameChange())
			{
				while (true)
				{
					nc.SkinName = skin.Name;

					if (nc.ShowDialog() == System.Windows.Forms.DialogResult.OK)
					{
						string newName = Path.GetDirectoryName(skin.FileName) + '/' + nc.SkinName + ".png";

						if (skin.FileName == newName)
							return;

						if (File.Exists(newName))
						{
							MessageBox.Show("Skin name already exists");
							continue;
						}

						File.Copy(skin.FileName, newName);
						File.Delete(skin.FileName);
						skin.FileName = newName;
						skin.Name = nc.SkinName;

						treeView1.Sorted = false;
						treeView1.Sorted = true;

						break;
					}

					break;
				}
			}*/
		}
		#endregion

		private void DoneEditingNode(string newName, TreeNode _currentlyEditing)
		{
			labelEditTextBox.Hide();

			if (_currentlyEditing is Skin)
			{
				Skin skin = (Skin)_currentlyEditing;

				if (skin.Name == newName)
					return;

				if (skin.ChangeName(newName) == false)
					System.Media.SystemSounds.Question.Play();
			}
		}

		void SetTool(Tools tool)
		{
			_currentTool = tool;

			if (_toolButtons == null)
				_toolButtons = new ToolStripButton[] { cameraToolStripButton, pencilToolStripButton, pipetteToolStripButton, eraserToolStripButton, dodgeToolStripButton, burnToolStripButton };
			if (_toolMenus == null)
				_toolMenus = new ToolStripMenuItem[] { cameraToolStripMenuItem, pencilToolStripMenuItem, dropperToolStripMenuItem, eraserToolStripMenuItem, dodgeToolStripMenuItem, burnToolStripMenuItem };

			for (int i = 0; i < _toolButtons.Length; ++i)
			{
				if (i == (int)tool)
				{
					_toolButtons[i].Checked = true;
					_toolMenus[i].Checked = true;
				}
				else
				{
					_toolButtons[i].Checked = false;
					_toolMenus[i].Checked = false;
				}
			}
		}

		#region Saving
		void SetCanSave(bool value)
		{
			saveToolStripButton.Enabled = saveToolStripMenuItem.Enabled = value;
		}

		void PerformSaveAs()
		{
			var skin = _lastSkin;

			GL.BindTexture(TextureTarget.Texture2D, GlobalDirtiness.CurrentSkin);
			int[] pixels = new int[skin.Width * skin.Height];
			GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

			Bitmap b = new Bitmap(skin.Width, skin.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			var locked = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			unsafe
			{
				fixed (void *inPixels = pixels)
				{
					void *outPixels = locked.Scan0.ToPointer();

					int *inInt = (int*)inPixels;
					int *outInt = (int*)outPixels;

					for (int y = 0; y < b.Height; ++y)
						for (int x = 0; x < b.Width; ++x)
						{
							var color = Color.FromArgb((*inInt >> 24) & 0xFF, (*inInt >> 0) & 0xFF, (*inInt >> 8) & 0xFF, (*inInt >> 16) & 0xFF);
							*outInt = color.ToArgb();

							inInt++;
							outInt++;
						}
				}
			}

			b.UnlockBits(locked);

			using (SaveFileDialog sfd = new SaveFileDialog())
			{
				sfd.Filter = "Skin Image|*.png";

				if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
					b.Save(sfd.FileName);
			}

			b.Dispose();
		}

		void PerformSaveSkin(Skin s)
		{
			glControl1.MakeCurrent();

			s.CommitChanges((s == _lastSkin) ? GlobalDirtiness.CurrentSkin : s.GLImage, true);
		}

		void RecursiveNodeSave(TreeNodeCollection nodes)
		{
			foreach (TreeNode node in nodes)
			{
				if (node is Skin)
				{
					Skin skin = (Skin)node;

					if (skin.Dirty)
						PerformSaveSkin(skin);
				}
				else
					RecursiveNodeSave(node.Nodes);
			}
		}

		void PerformSaveAll()
		{
			RecursiveNodeSave(treeView1.Nodes);
			treeView1.Invalidate();
		}

		void PerformSave()
		{
			Skin skin = _lastSkin;

			if (!skin.Dirty)
				return;

			SetCanSave(false);
			PerformSaveSkin(skin);
			treeView1.Invalidate();
		}
		#endregion

		void PerformUndo()
		{
			if (!_currentUndoBuffer.CanUndo)
				return;

			glControl1.MakeCurrent();

			_currentUndoBuffer.Undo();

			undoToolStripMenuItem.Enabled = undoToolStripButton.Enabled = _currentUndoBuffer.CanUndo;
			redoToolStripMenuItem.Enabled = redoToolStripButton.Enabled = _currentUndoBuffer.CanRedo;

			Skin current = _lastSkin;
			SetCanSave(current.Dirty = true);

			glControl1.Invalidate();
		}

		void PerformRedo()
		{
			if (!_currentUndoBuffer.CanRedo)
				return;

			glControl1.MakeCurrent();

			_currentUndoBuffer.Redo();

			Skin current = _lastSkin;
			SetCanSave(current.Dirty = true);

			undoToolStripMenuItem.Enabled = undoToolStripButton.Enabled = _currentUndoBuffer.CanUndo;
			redoToolStripMenuItem.Enabled = redoToolStripButton.Enabled = _currentUndoBuffer.CanRedo;

			glControl1.Invalidate();
		}

		void SetColor(Color c)
		{
			_currentColor = c;
			colorPreview1.ForeColor = _currentColor;

			var hsl = Devcorp.Controls.Design.ColorSpaceHelper.RGBtoHSL(c);

			_skipColors = true;
			redNumericUpDown.Value = c.R;
			greenNumericUpDown.Value = c.G;
			blueNumericUpDown.Value = c.B;
			alphaNumericUpDown.Value = c.A;

			colorSquare.CurrentHue = (int)hsl.Hue;
			colorSquare.CurrentSat = (int)(hsl.Saturation * 240);
			saturationSlider.CurrentLum = (int)(hsl.Luminance * 240);

			hueNumericUpDown.Value = colorSquare.CurrentHue;
			saturationNumericUpDown.Value = colorSquare.CurrentSat;
			luminanceNumericUpDown.Value = saturationSlider.CurrentLum;

			redRenderer.StartColor =
				greenRenderer.StartColor =
				blueRenderer.StartColor = _currentColor;

			redRenderer.EndColor = Color.FromArgb(255, 255, _currentColor.G, _currentColor.B);
			greenRenderer.EndColor = Color.FromArgb(255, _currentColor.R, 255, _currentColor.B);
			blueRenderer.EndColor = Color.FromArgb(255, _currentColor.R, _currentColor.G, 255);

			hueRenderer.Saturation = colorSquare.CurrentSat;
			hueRenderer.Luminance = saturationSlider.CurrentLum;

			saturationRenderer.Luminance = saturationSlider.CurrentLum;
			saturationRenderer.Hue = colorSquare.CurrentHue;

			lightnessRenderer.Hue = colorSquare.CurrentHue;
			lightnessRenderer.Saturation = colorSquare.CurrentSat;

			redColorSlider.Value = _currentColor.R;
			greenColorSlider.Value = _currentColor.G;
			blueColorSlider.Value = _currentColor.B;
			alphaColorSlider.Value = _currentColor.A;

			hueColorSlider.Value = colorSquare.CurrentHue;
			saturationColorSlider.Value = colorSquare.CurrentSat;
			lightnessColorSlider.Value = saturationSlider.CurrentLum;

			if (!_editingHex)
			{
				textBox1.Text = string.Format("{0:X2}{1:X2}{2:X2}{3:X2}", c.R, c.G, c.B, c.A);
			}

			_skipColors = false;
		}

		void SetViewMode(ViewMode newMode)
		{
			perspectiveToolStripButton.Checked = orthographicToolStripButton.Checked = false;
			perspectiveToolStripMenuItem.Checked = textureToolStripMenuItem.Checked = false;
			_currentViewMode = newMode;

			switch (_currentViewMode)
			{
			case ViewMode.Orthographic:
				orthographicToolStripButton.Checked = true;
				textureToolStripMenuItem.Checked = true;
				break;
			case ViewMode.Perspective:
				perspectiveToolStripButton.Checked = true;
				perspectiveToolStripMenuItem.Checked = true;
				break;
			}

			glControl1_Resize(glControl1, null);
		}

		void SetTransparencyMode(TransparencyMode trans)
		{
			offToolStripMenuItem.Checked = helmetOnlyToolStripMenuItem.Checked = allToolStripMenuItem.Checked = false;
			GlobalSettings.Transparency = trans;

			switch (GlobalSettings.Transparency)
			{
			case TransparencyMode.Off:
				offToolStripMenuItem.Checked = true;
				break;
			case TransparencyMode.Helmet:
				helmetOnlyToolStripMenuItem.Checked = true;
				break;
			case TransparencyMode.All:
				allToolStripMenuItem.Checked = true;
				break;
			}

			glControl1.Invalidate();
		}

		ToolStripMenuItem[] _toggleMenuItems;
		ToolStripButton[] _toggleButtons;
		void SetVisibleParts()
		{
			if (_toggleMenuItems == null)
			{
				_toggleMenuItems = new ToolStripMenuItem[] { headToolStripMenuItem, helmetToolStripMenuItem, chestToolStripMenuItem, leftArmToolStripMenuItem, rightArmToolStripMenuItem, leftLegToolStripMenuItem, rightLegToolStripMenuItem };
				_toggleButtons = new ToolStripButton[] { toggleHeadToolStripButton, toggleHelmetToolStripButton, toggleChestToolStripButton, toggleLeftArmToolStripButton, toggleRightArmToolStripButton, toggleLeftLegToolStripButton, toggleRightLegToolStripButton };
			}

			for (int i = 0; i < _toggleButtons.Length; ++i)
				_toggleMenuItems[i].Checked = _toggleButtons[i].Checked = ((GlobalSettings.ViewFlags & (VisiblePartFlags)(1 << i)) != 0);
		}

		void ToggleVisiblePart(VisiblePartFlags flag)
		{
			GlobalSettings.ViewFlags ^= flag;

			bool hasNow = (GlobalSettings.ViewFlags & flag) != 0;

			ToolStripMenuItem item = null;
			ToolStripButton itemButton = null;

			// TODO: ugly
			switch (flag)
			{
			case VisiblePartFlags.HeadFlag:
				item = headToolStripMenuItem;
				itemButton = toggleHeadToolStripButton;
				break;
			case VisiblePartFlags.HelmetFlag:
				item = helmetToolStripMenuItem;
				itemButton = toggleHelmetToolStripButton;
				break;
			case VisiblePartFlags.ChestFlag:
				item = chestToolStripMenuItem;
				itemButton = toggleChestToolStripButton;
				break;
			case VisiblePartFlags.LeftArmFlag:
				item = leftArmToolStripMenuItem;
				itemButton = toggleLeftArmToolStripButton;
				break;
			case VisiblePartFlags.RightArmFlag:
				item = rightArmToolStripMenuItem;
				itemButton = toggleRightArmToolStripButton;
				break;
			case VisiblePartFlags.LeftLegFlag:
				item = leftLegToolStripMenuItem;
				itemButton = toggleLeftLegToolStripButton;
				break;
			case VisiblePartFlags.RightLegFlag:
				item = rightLegToolStripMenuItem;
				itemButton = toggleRightLegToolStripButton;
				break;
			}

			item.Checked = hasNow;
			itemButton.Checked = hasNow;

			glControl1.Invalidate();
		}

		void ToggleAlphaCheckerboard()
		{
			GlobalSettings.AlphaCheckerboard = !GlobalSettings.AlphaCheckerboard;
			alphaCheckerboardToolStripMenuItem.Checked = GlobalSettings.AlphaCheckerboard;
			glControl1.Invalidate();
		}

		void ToggleOverlay()
		{
			GlobalSettings.TextureOverlay = !GlobalSettings.TextureOverlay;
			textureOverlayToolStripMenuItem.Checked = GlobalSettings.TextureOverlay;
			glControl1.Invalidate();
		}

		void ToggleTransparencyMode()
		{
			switch (GlobalSettings.Transparency)
			{
			case TransparencyMode.Off:
				SetTransparencyMode(TransparencyMode.Helmet);
				break;
			case TransparencyMode.Helmet:
				SetTransparencyMode(TransparencyMode.All);
				break;
			case TransparencyMode.All:
				SetTransparencyMode(TransparencyMode.Off);
				break;
			}
		}

		void ToggleViewMode()
		{
			switch (_currentViewMode)
			{
			case ViewMode.Orthographic:
				SetViewMode(ViewMode.Perspective);
				break;
			case ViewMode.Perspective:
				SetViewMode(ViewMode.Orthographic);
				break;
			}
		}

		#region Screenshots
		Bitmap CopyScreenToBitmap()
		{
			glControl1.MakeCurrent();
			Bitmap b = new Bitmap(glControl1.Width, glControl1.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			int[] pixels = new int[glControl1.Width * glControl1.Height];
			GL.ReadPixels(0, 0, glControl1.Width, glControl1.Height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

			var locked = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			unsafe
			{
				fixed (void *inPixels = pixels)
				{
					void *outPixels = locked.Scan0.ToPointer();

					int *inInt = (int*)inPixels;
					int *outInt = (int*)outPixels;

					for (int y = 0; y < b.Height; ++y)
						for (int x = 0; x < b.Width; ++x)
						{
							var color = Color.FromArgb((*inInt >> 24) & 0xFF, (*inInt >> 0) & 0xFF, (*inInt >> 8) & 0xFF, (*inInt >> 16) & 0xFF);
							*outInt = color.ToArgb();

							inInt++;
							outInt++;
						}
				}
			}

			b.UnlockBits(locked);
			b.RotateFlip(RotateFlipType.RotateNoneFlipY);

			return b;
		}

		void TakeScreenshot()
		{
			Clipboard.SetImage(CopyScreenToBitmap());
		}

		void SaveScreenshot()
		{
			using (SaveFileDialog sfd = new SaveFileDialog())
			{
				sfd.Filter = "PNG Image|*.png";

				if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					using (var bmp = CopyScreenToBitmap())
						bmp.Save(sfd.FileName);
				}
			}
		}
		#endregion
		#endregion

		void glControl1_Load(object sender, EventArgs e)
		{
			glControl1_Resize(this, EventArgs.Empty);   // Ensure the Viewport is set up correctly
			GL.ClearColor(GlobalSettings.BackgroundColor);

			GL.Enable(EnableCap.Texture2D);
			GL.ShadeModel(ShadingModel.Smooth);                        // Enable Smooth Shading
			GL.ClearDepth(1.0f);                         // Depth Buffer Setup
			GL.Enable(EnableCap.DepthTest);                        // Enables Depth Testing
			GL.DepthFunc(DepthFunction.Lequal);                         // The Type Of Depth Testing To Do
			GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);          // Really Nice Perspective Calculations
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvMode.Modulate);
			GL.Enable(EnableCap.CullFace);
			GL.CullFace(CullFaceMode.Front);

			IL.ilInit();

			_grassTop = ImageUtilities.LoadImage("grass.png");
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

			_backgroundTex = ImageUtilities.LoadImage("inverted.png");
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);

			_previewPaint = GL.GenTexture();
			GlobalDirtiness.CurrentSkin = GL.GenTexture();
			_alphaTex = GL.GenTexture();

			unsafe
			{
				byte[] arra = new byte[64 * 32];
				GL.BindTexture(TextureTarget.Texture2D, _previewPaint);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 64, 32, 0, PixelFormat.Rgba, PixelType.UnsignedByte, arra);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);

				GL.BindTexture(TextureTarget.Texture2D, GlobalDirtiness.CurrentSkin);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 64, 32, 0, PixelFormat.Rgba, PixelType.UnsignedByte, arra);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);

				arra = new byte[4 * 4 * 4];
				fixed (byte* texData = arra)
				{
					byte *d = texData;

					for (int y = 0; y < 4; ++y)
						for (int x = 0; x < 4; ++x)
						{
							bool dark = ((x + (y & 1)) & 1) == 1;

							if (dark)
								*((int*)d) = (80 << 0) | (80 << 8) | (80 << 16) | (255 << 24);
							else
								*((int*)d) = (127 << 0) | (127 << 8) | (127 << 16) | (255 << 24);
							d += 4;
						}
				}

				GL.BindTexture(TextureTarget.Texture2D, _alphaTex);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 4, 4, 0, PixelFormat.Rgba, PixelType.UnsignedByte, arra);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
			}
		}

		void animTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			_animationTime += 0.24f;
			glControl1.Invalidate();
		}

		void glControl1_MouseWheel(object sender, MouseEventArgs e)
		{
			if (_currentViewMode == ViewMode.Perspective)
				_3dZoom += e.Delta / 50;
			else
				_2dZoom += e.Delta / 50;

			glControl1.Invalidate();
		}

		void glControl1_Paint(object sender, PaintEventArgs e)
		{
			glControl1.MakeCurrent();
			SetPreview();

			GL.ClearColor(GlobalSettings.BackgroundColor);

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadIdentity();

			var skin = (Skin)_lastSkin;

			DrawPlayer(_previewPaint, skin, grassToolStripMenuItem.Checked, false);

			glControl1.SwapBuffers();
		}

		void glControl1_Resize(object sender, EventArgs e)
		{
			glControl1.MakeCurrent();

			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadIdentity();

			GL.Viewport(0, 0, glControl1.Width, glControl1.Height);

			if (_currentViewMode == ViewMode.Perspective)
			{
				var mat = OpenTK.Matrix4d.Perspective(45, (double)glControl1.Width / (double)glControl1.Height, 0.01, 100000);
				GL.MultMatrix(ref mat);
			}
			else
				GL.Ortho(0, glControl1.Width, glControl1.Height, 0, -1, 1);

			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadIdentity();

			glControl1.Invalidate();
		}

		void glControl1_MouseDown(object sender, MouseEventArgs e)
		{
			_mouseIsDown = true;
			_mousePoint = e.Location;

			if (e.Button == MouseButtons.Left)
				UseToolOnViewport(e.X, e.Y);
		}

		void glControl1_MouseMove(object sender, MouseEventArgs e)
		{
			if (_mouseIsDown)
			{
				var delta = new Point(e.X - _mousePoint.X, e.Y - _mousePoint.Y);

				if ((_currentTool == Tools.Camera && e.Button == MouseButtons.Left) ||
					((_currentTool != Tools.Camera) && e.Button == MouseButtons.Right))
				{
					if (_currentViewMode == ViewMode.Perspective)
					{
						_3dRotationY += (float)delta.X;
						_3dRotationX += (float)delta.Y;
					}
					else
					{
						_2dCamOffsetX += delta.X / _2dZoom;
						_2dCamOffsetY += delta.Y / _2dZoom;
					}
				}
				else if ((_currentTool == Tools.Camera && e.Button == MouseButtons.Right) ||
					((_currentTool != Tools.Camera) && e.Button == MouseButtons.Middle))
				{
					if (_currentViewMode == ViewMode.Perspective)
						_3dZoom += (float)-delta.Y;
					else
						_2dZoom += -delta.Y / 25.0f;
				}

				if ((_currentTool != Tools.Camera) && e.Button == MouseButtons.Left)
					UseToolOnViewport(e.X, e.Y);

				glControl1.Invalidate();
			}

			_mousePoint = e.Location;
		}

		void glControl1_MouseUp(object sender, MouseEventArgs e)
		{
			if (_currentUndoBuffer != null && _changedPixels != null)
			{
				_currentUndoBuffer.AddBuffer(_changedPixels);
				_changedPixels = null;

				undoToolStripMenuItem.Enabled = undoToolStripButton.Enabled = _currentUndoBuffer.CanUndo;
				redoToolStripMenuItem.Enabled = redoToolStripButton.Enabled = _currentUndoBuffer.CanRedo;
			}

			_mouseIsDown = false;
		}

		private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if (_skipListbox || treeView1.SelectedNode == _lastSkin ||
				!(e.Node is Skin))
				return;

			if (_lastSkin != null && treeView1.SelectedNode != _lastSkin)
			{
				// Copy over the current changes to the tex stored in the skin.
				// This allows us to pick up where we left off later, without undoing any work.
				_lastSkin.CommitChanges(GlobalDirtiness.CurrentSkin, false);
			}

			//if (_lastSkin != null)
			//	_lastSkin.Undo.Clear();

			glControl1.MakeCurrent();

			Skin skin = (Skin)treeView1.SelectedNode;
			SetCanSave(skin.Dirty);

			if (skin == null)
			{
				_currentUndoBuffer = null;
				GL.BindTexture(TextureTarget.Texture2D, 0);
				int[] array = new int[64 * 32];
				GL.BindTexture(TextureTarget.Texture2D, GlobalDirtiness.CurrentSkin);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 64, 32, 0, PixelFormat.Rgba, PixelType.UnsignedByte, array);
				undoToolStripMenuItem.Enabled = undoToolStripButton.Enabled = false;
				redoToolStripMenuItem.Enabled = redoToolStripButton.Enabled = false;
			}
			else
			{
				GL.BindTexture(TextureTarget.Texture2D, skin.GLImage);
				int[] array = new int[skin.Width * skin.Height];
				GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgba, PixelType.UnsignedByte, array);
				GL.BindTexture(TextureTarget.Texture2D, GlobalDirtiness.CurrentSkin);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, skin.Width, skin.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, array);
				GL.BindTexture(TextureTarget.Texture2D, _previewPaint);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, skin.Width, skin.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, array);

				_currentUndoBuffer = skin.Undo;
				undoToolStripMenuItem.Enabled = undoToolStripButton.Enabled = _currentUndoBuffer.CanUndo;
				redoToolStripMenuItem.Enabled = redoToolStripButton.Enabled = _currentUndoBuffer.CanRedo;
			}

			glControl1.Invalidate();
			_lastSkin = (Skin)treeView1.SelectedNode;
		}

		void uploadButton_Click(object sender, EventArgs e)
		{
			if (_lastSkin.Width != 64 || _lastSkin.Height != 32)
			{
				MessageBox.Show("While you can edit high resolution textures with MCSkin3D, you cannot upload them to your Minecraft profile.");
				return;
			}

			PerformUpload();
		}

		void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		void animateToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleAnimation();
		}

		void followCursorToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleFollowCursor();
		}

		void grassToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleGrass();
		}

		void addNewSkinToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PerformImportSkin();
		}

		void deleteSelectedSkinToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PerformDeleteSkin();
		}

		void cloneSkinToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PerformCloneSkin();
		}

		private void treeView1_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			//PerformNameChange();
		}

		private void treeView1_MouseUp(object sender, MouseEventArgs e)
		{
			if (_lastSkin != null && e.Button == MouseButtons.Right)
				contextMenuStrip1.Show(Cursor.Position);
		}

		void cameraToolStripButton_Click(object sender, EventArgs e)
		{
			SetTool(Tools.Camera);
		}

		void pencilToolStripButton_Click(object sender, EventArgs e)
		{
			SetTool(Tools.Pencil);
		}

		void pipetteToolStripButton_Click(object sender, EventArgs e)
		{
			SetTool(Tools.Dropper);
		}

		void eraserToolStripButton_Click(object sender, EventArgs e)
		{
			SetTool(Tools.Eraser);
		}

		void undoToolStripButton_Click(object sender, EventArgs e)
		{
			PerformUndo();
		}

		void redoToolStripButton_Click(object sender, EventArgs e)
		{
			PerformRedo();
		}

		void redNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			if (_skipColors)
				return;

			SetColor(Color.FromArgb(_currentColor.A, (byte)redNumericUpDown.Value, _currentColor.G, _currentColor.B));
		}

		void greenNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			if (_skipColors)
				return;

			SetColor(Color.FromArgb(_currentColor.A, _currentColor.R, (byte)greenNumericUpDown.Value, _currentColor.B));
		}

		void blueNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			if (_skipColors)
				return;

			SetColor(Color.FromArgb(_currentColor.A, _currentColor.R, _currentColor.G, (byte)blueNumericUpDown.Value));
		}

		void alphaNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			if (_skipColors)
				return;

			SetColor(Color.FromArgb((byte)alphaNumericUpDown.Value, _currentColor.R, _currentColor.G, _currentColor.B));
		}

		const float oneDivTwoFourty = 1.0f / 240.0f;

		void colorSquare_HueChanged(object sender, EventArgs e)
		{
			if (_skipColors)
				return;

			var c = new HSL(colorSquare.CurrentHue, (float)colorSquare.CurrentSat * oneDivTwoFourty, (float)saturationSlider.CurrentLum * oneDivTwoFourty);
			SetColor(Devcorp.Controls.Design.ColorSpaceHelper.HSLtoColor(c));
		}

		void colorSquare_SatChanged(object sender, EventArgs e)
		{
			if (_skipColors)
				return;

			var c = new HSL(colorSquare.CurrentHue, (float)colorSquare.CurrentSat * oneDivTwoFourty, (float)saturationSlider.CurrentLum * oneDivTwoFourty);
			SetColor(Devcorp.Controls.Design.ColorSpaceHelper.HSLtoColor(c));
		}

		void saturationSlider_LumChanged(object sender, EventArgs e)
		{
			if (_skipColors)
				return;

			var c = new HSL(colorSquare.CurrentHue, (float)colorSquare.CurrentSat * oneDivTwoFourty, (float)saturationSlider.CurrentLum * oneDivTwoFourty);
			SetColor(Devcorp.Controls.Design.ColorSpaceHelper.HSLtoColor(c));
		}

		void hueColorSlider_Scroll(object sender, ScrollEventArgs e)
		{
			if (_skipColors)
				return;

			var c = new HSL(e.NewValue, (float)saturationColorSlider.Value * oneDivTwoFourty, (float)lightnessColorSlider.Value * oneDivTwoFourty);
			SetColor(Devcorp.Controls.Design.ColorSpaceHelper.HSLtoColor(c));
		}

		void saturationColorSlider_Scroll(object sender, ScrollEventArgs e)
		{
			if (_skipColors)
				return;

			var c = new HSL(hueColorSlider.Value, (float)e.NewValue * oneDivTwoFourty, (float)lightnessColorSlider.Value * oneDivTwoFourty);
			SetColor(Devcorp.Controls.Design.ColorSpaceHelper.HSLtoColor(c));
		}

		void lightnessColorSlider_Scroll(object sender, ScrollEventArgs e)
		{
			if (_skipColors)
				return;

			var c = new HSL(hueColorSlider.Value, (float)saturationColorSlider.Value * oneDivTwoFourty, (float)e.NewValue * oneDivTwoFourty);
			SetColor(Devcorp.Controls.Design.ColorSpaceHelper.HSLtoColor(c));
		}

		void hueNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			if (_skipColors)
				return;

			var c = new HSL((double)hueNumericUpDown.Value, (float)saturationNumericUpDown.Value * oneDivTwoFourty, (float)luminanceNumericUpDown.Value * oneDivTwoFourty);
			SetColor(Devcorp.Controls.Design.ColorSpaceHelper.HSLtoColor(c));
		}

		void saturationNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			if (_skipColors)
				return;

			var c = new HSL((double)hueNumericUpDown.Value, (float)saturationNumericUpDown.Value * oneDivTwoFourty, (float)luminanceNumericUpDown.Value * oneDivTwoFourty);
			SetColor(Devcorp.Controls.Design.ColorSpaceHelper.HSLtoColor(c));
		}

		void luminanceNumericUpDown_ValueChanged(object sender, EventArgs e)
		{
			if (_skipColors)
				return;

			var c = new HSL((double)hueNumericUpDown.Value, (float)saturationNumericUpDown.Value * oneDivTwoFourty, (float)luminanceNumericUpDown.Value * oneDivTwoFourty);
			SetColor(Devcorp.Controls.Design.ColorSpaceHelper.HSLtoColor(c));
		}

		void perspectiveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetViewMode(ViewMode.Perspective);
		}

		void textureToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetViewMode(ViewMode.Orthographic);
		}

		void perspectiveToolStripButton_Click(object sender, EventArgs e)
		{
			SetViewMode(ViewMode.Perspective);
		}

		void orthographicToolStripButton_Click(object sender, EventArgs e)
		{
			SetViewMode(ViewMode.Orthographic);
		}

		void offToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetTransparencyMode(TransparencyMode.Off);
		}

		void helmetOnlyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetTransparencyMode(TransparencyMode.Helmet);
		}

		void allToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetTransparencyMode(TransparencyMode.All);
		}

		void headToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.HeadFlag);
		}

		void helmetToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.HelmetFlag);
		}

		void chestToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.ChestFlag);
		}

		void leftArmToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.LeftArmFlag);
		}

		void rightArmToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.RightArmFlag);
		}

		void leftLegToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.LeftLegFlag);
		}

		void rightLegToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.RightLegFlag);
		}

		void alphaCheckerboardToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleAlphaCheckerboard();
		}

		void textureOverlayToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleOverlay();
		}

		void checkForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (_updater.Checking)
				return;

			_updater.PrintOnEqual = true;
			_updater.CheckForUpdate();
		}

		void undoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PerformUndo();
		}

		void redoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PerformRedo();
		}

		void cameraToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetTool(Tools.Camera);
		}

		void pencilToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetTool(Tools.Pencil);
		}

		void dropperToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetTool(Tools.Dropper);
		}

		void redColorSlider_Scroll(object sender, ScrollEventArgs e)
		{
			if (_skipColors)
				return;

			SetColor(Color.FromArgb(_currentColor.A, e.NewValue, _currentColor.G, _currentColor.B));
		}

		void greenColorSlider_Scroll(object sender, ScrollEventArgs e)
		{
			if (_skipColors)
				return;

			SetColor(Color.FromArgb(_currentColor.A, _currentColor.R, e.NewValue, _currentColor.B));
		}

		void blueColorSlider_Scroll(object sender, ScrollEventArgs e)
		{
			if (_skipColors)
				return;

			SetColor(Color.FromArgb(_currentColor.A, _currentColor.R, _currentColor.G, e.NewValue));
		}

		void swatchContainer_SwatchChanged(object sender, SwatchChangedEventArgs e)
		{
			SetColor(e.Swatch);
		}

		void alphaColorSlider_Scroll(object sender, ScrollEventArgs e)
		{
			if (_skipColors)
				return;

			SetColor(Color.FromArgb(e.NewValue, _currentColor.R, _currentColor.G, _currentColor.B));
		}

		void dodgeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetTool(Tools.Dodge);
		}

		void burnToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetTool(Tools.Burn);
		}

		void dodgeToolStripButton_Click(object sender, EventArgs e)
		{
			SetTool(Tools.Dodge);
		}

		void burnToolStripButton_Click(object sender, EventArgs e)
		{
			SetTool(Tools.Burn);
		}

		void keyboardShortcutsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			_shortcutEditor.ShowDialog();
		}

		void backgroundColorToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (MultiPainter.ColorPicker picker = new MultiPainter.ColorPicker())
			{
				picker.CurrentColor = GlobalSettings.BackgroundColor;

				if (picker.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					GlobalSettings.BackgroundColor = picker.CurrentColor;

					glControl1.Invalidate();
				}
			}
		}

		void screenshotToolStripButton_Click(object sender, EventArgs e)
		{
			if ((ModifierKeys & Keys.Shift) != 0)
				SaveScreenshot();
			else
				TakeScreenshot();
		}

		void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PerformSaveAs();
		}

		void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PerformSave();
		}

		void saveAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PerformSaveAll();
		}

		void saveToolStripButton_Click(object sender, EventArgs e)
		{
			PerformSave();
		}

		void saveAlltoolStripButton_Click(object sender, EventArgs e)
		{
			PerformSaveAll();
		}

		void changeNameToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PerformNameChange();
		}

		void deleteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PerformDeleteSkin();
		}

		void cloneToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PerformCloneSkin();
		}

		void colorTabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (colorTabControl.SelectedIndex == 1 || colorTabControl.SelectedIndex == 2)
			{
				colorTabControl.SelectedTab.Controls.Add(colorSquare);
				colorTabControl.SelectedTab.Controls.Add(saturationSlider);
				colorTabControl.SelectedTab.Controls.Add(colorPreview1);
				colorTabControl.SelectedTab.Controls.Add(label5);
				colorTabControl.SelectedTab.Controls.Add(alphaColorSlider);
				colorTabControl.SelectedTab.Controls.Add(alphaNumericUpDown);
			}
		}

		void automaticallyCheckForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			GlobalSettings.AutoUpdate = automaticallyCheckForUpdatesToolStripMenuItem.Checked = !automaticallyCheckForUpdatesToolStripMenuItem.Checked;
		}

		private void Form1_Load(object sender, EventArgs e)
		{

		}

		private void toggleHeadToolStripButton_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.HeadFlag);
		}

		private void toggleHelmetToolStripButton_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.HelmetFlag);
		}

		private void toggleChestToolStripButton_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.ChestFlag);
		}

		private void toggleLeftArmToolStripButton_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.LeftArmFlag);
		}

		private void toggleRightArmToolStripButton_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.RightArmFlag);
		}

		private void toggleLeftLegToolStripButton_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.LeftLegFlag);
		}

		private void toggleRightLegToolStripButton_Click(object sender, EventArgs e)
		{
			ToggleVisiblePart(VisiblePartFlags.RightLegFlag);
		}

		private void labelEditTextBox_Leave(object sender, EventArgs e)
		{
			DoneEditingNode(labelEditTextBox.Text, _currentlyEditing);
		}

		bool _editingHex = false;
		private void textBox1_TextChanged(object sender, EventArgs e)
		{
			if (textBox1.Text.Contains('#'))
				textBox1.Text = textBox1.Text.Replace("#", "");

			string realHex = textBox1.Text;

			while (realHex.Length != 8)
				realHex += 'F';

			byte r = byte.Parse(realHex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
			byte g = byte.Parse(realHex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
			byte b = byte.Parse(realHex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
			byte a = byte.Parse(realHex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);

			_editingHex = true;
			SetColor(Color.FromArgb(a, r, g, b));
			_editingHex = false;
		}

		private void labelEditTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Return || e.KeyCode == Keys.Enter)
			{
				treeView1.Focus();
				e.Handled = true;
			}
		}

		private void labelEditTextBox_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
		{
			if (e.KeyChar == '\r' || e.KeyChar == '\n')
				e.Handled = true;
		}

		private void labelEditTextBox_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return)
				e.Handled = true;
		}
	}
}
