﻿//
//    MCSkin3D, a 3d skin management studio for Minecraft
//    Copyright (C) 2011-2012 Altered Softworks & MCSkin3D Team
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using Paril.Components;
using System.Drawing;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using Paril.Compatibility;
using Paril.OpenGL;

namespace MCSkin3D
{
	public struct ColorAlpha
	{
		public Color Color;
		public float TotalAlpha;

		public ColorAlpha(Color c, float alpha) :
			this()
		{
			Color = c;
			TotalAlpha = alpha;
		}
	}

	public class PixelsChangedUndoable : IUndoable
	{
		public Dictionary<Point, Tuple<Color, ColorAlpha>> Points = new Dictionary<Point, Tuple<Color, ColorAlpha>>();
		public string Action { get; private set; }

		public PixelsChangedUndoable(string action)
		{
			Action = action + " [" + DateTime.Now.ToString("h:mm:ss") + "]";
		}

		public PixelsChangedUndoable(string action, string tool) :
			this(action + " (" + tool + ")")
		{
		}

		public void Undo(object obj)
		{
			Skin skin = (Skin)obj;

			ColorGrabber grabber = new ColorGrabber(GlobalDirtiness.CurrentSkin, skin.Width, skin.Height);
			grabber.Load();

			foreach (var kvp in Points)
			{
				var p = kvp.Key;
				var color = kvp.Value;
				grabber[p.X, p.Y] = new ColorPixel(color.Item1.R | (color.Item1.G << 8) | (color.Item1.B << 16) | (color.Item1.A << 24));
			}

			grabber.Save();
		}

		public void Redo(object obj)
		{
			Skin skin = (Skin)obj;

			ColorGrabber grabber = new ColorGrabber(GlobalDirtiness.CurrentSkin, skin.Width, skin.Height);
			grabber.Load();

			foreach (var kvp in Points)
			{
				var p = kvp.Key;
				var color = kvp.Value;
				grabber[p.X, p.Y] = new ColorPixel(color.Item2.Color.R | (color.Item2.Color.G << 8) | (color.Item2.Color.B << 16) | (color.Item2.Color.A << 24));
			}

			grabber.Save();
		}
	}
}
