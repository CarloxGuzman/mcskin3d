﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Paril.Settings;
using System.ComponentModel;
using Paril.Settings.Serializers;
using System.Security.Cryptography;
using System.Drawing;

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
		[DefaultValue(true)]
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
		[DefaultValue("")]
		public static string ShortcutKeys { get; set; }

		[Savable]
		[DefaultValue("135 206 235 255")]
		[TypeSerializer(typeof(ColorSerializer), true)]
		public static Color BackgroundColor { get; set; }

		[Savable]
		[DefaultValue(true)]
		public static bool AutoUpdate { get; set; }

		static Settings Settings = null;
		
		public static void Load()
		{
			Settings = new Settings();
			Settings.Structures.Add(typeof(GlobalSettings));
			Settings.Load("settings.ini");
		}

		public static void Save()
		{
			Settings.Save("settings.ini");
		}
	}
}
