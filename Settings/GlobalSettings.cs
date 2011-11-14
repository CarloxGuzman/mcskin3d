﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Paril.Settings;
using System.ComponentModel;
using Paril.Settings.Serializers;
using System.Security.Cryptography;
using System.Drawing;
using System.Windows.Forms;

namespace MCSkin3D
{
	public static class GlobalSettings
	{
		[Savable]
		public static bool Animate { get; set; }

		[Savable]
		public static bool FollowCursor { get; set; }

		[Savable]
		[DefaultValue(true)]
		public static bool Grass { get; set; }

		[Savable]
		[DefaultValue(false)]
		public static bool Ghost { get; set; }

		[Savable]
		[DefaultValue("")]
		public static string LastSkin { get; set; }

		[Savable]
		public static bool RememberMe { get; set; }

		[Savable]
		public static bool AutoLogin { get; set; }

		[Savable]
		[DefaultValue("")]
		public static string LastUsername { get; set; }

		[Savable]
		[DefaultValue("")]
		[TypeSerializer(typeof(PasswordSerializer<AesManaged>), false)]
		public static string LastPassword { get; set; }

		[Savable]
		[DefaultValue(TransparencyMode.Helmet)]
		[TypeSerializer(typeof(EnumSerializer<TransparencyMode>), true)]
		public static TransparencyMode Transparency { get; set; }

		[Savable]
		[DefaultValue(VisiblePartFlags.Default)]
		[TypeSerializer(typeof(EnumSerializer<VisiblePartFlags>), true)]
		public static VisiblePartFlags ViewFlags { get; set; }

		[Savable]
		[DefaultValue(true)]
		public static bool AlphaCheckerboard { get; set; }

		[Savable]
		[DefaultValue(true)]
		public static bool TextureOverlay { get; set; }

		[Savable]
		[DefaultValue("None")]
		public static string LastBackground { get; set; }

		[Savable]
		[DefaultValue("")]
		public static string ShortcutKeys { get; set; }

		[Savable]
		[DefaultValue("135 206 235 255")]
		[TypeSerializer(typeof(ColorSerializer), true)]
		public static Color BackgroundColor { get; set; }

		[Savable]
		[DefaultValue(true)]
		public static bool AutoUpdate { get; set; }

		[Savable]
		[DefaultValue(0)]
		public static int Multisamples { get; set; }

		[Savable]
		public static bool PencilIncremental { get; set; }

		[Savable]
		public static bool DodgeBurnIncremental { get; set; }

		[Savable]
		[DefaultValue(0.70f)]
		public static float DodgeBurnExposure { get; set; }

		[Savable]
		public static bool DarkenLightenIncremental { get; set; }

		[Savable]
		[DefaultValue(0.25f)]
		public static float DarkenLightenExposure { get; set; }

		[Savable]
		[DefaultValue(0.20f)]
		public static float NoiseSaturation { get; set; }

        [Savable]
        public static string LanguageFile { get; set; }

		[Savable]
		[DefaultValue(23)]
		public static int TreeViewHeight { get; set; }

		[Savable]
		[DefaultValue(0)]
		public static float FloodFillThreshold { get; set; }

		static Settings Settings = null;

		public static bool Load()
		{
			try
			{
				Settings = new Settings();
				Settings.Structures.Add(typeof(GlobalSettings));
				Settings.Load("settings.ini");
				return true;
			}
			catch
			{
				Settings.LoadDefaults();
				return false;
			}
		}

		public static void Save()
		{
			Settings.Save("settings.ini");
		}
	}
}