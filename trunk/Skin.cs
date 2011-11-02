﻿using System;
using System.Drawing;
using Paril.Components;
using System.IO;
using OpenTK.Graphics.OpenGL;
using DevCIL;
using System.Windows.Forms;
using Paril.Extensions;

namespace MCSkin3D
{
	public class Skin : TreeNode
	{
		public new string Name;
		public Bitmap Image;
		public Bitmap Head;
		public int GLImage;
		public UndoBuffer Undo;
		public bool Dirty;
		public Size Size;

		public int Width { get { return Size.Width; } }
		public int Height { get { return Size.Height; } }

		public FileInfo File;
		public DirectoryInfo Directory;

		public Skin(string fileName)
		{
			Undo = new UndoBuffer(this);
	
			File = new FileInfo(fileName);
			Directory = File.Directory;

			Name = Path.GetFileNameWithoutExtension(File.Name);

			SetImages();
		}

		public Skin(FileInfo file) :
			this(file.FullName)
		{
		}

		void SetImages()
		{
			if (Head != null)
			{
				Head.Dispose();
				GL.DeleteTexture(GLImage);
			}

			Image = new Bitmap(File.FullName);

			Size = Image.Size;

			float scale = Size.Width / 64.0f;
			int headSize = (int)(8.0f * scale);

			Head = new Bitmap(headSize, headSize);
			using (Graphics g = Graphics.FromImage(Head))
				g.DrawImage(Image, new Rectangle(0, 0, headSize, headSize), new Rectangle(headSize, headSize, headSize, headSize), GraphicsUnit.Pixel);

			Image.Dispose();
			Image = null;
			GLImage = ImageUtilities.LoadImage(File.FullName);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
		}

		public override string ToString()
		{
			if (Dirty)
				return Name + " *";
			return Name;
		}

		public void CommitChanges(int currentSkin, bool save)
		{
			byte[] data = new byte[Width * Height * 4];
			GL.BindTexture(TextureTarget.Texture2D, currentSkin);
			GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);

			GL.BindTexture(TextureTarget.Texture2D, GLImage);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Width, Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);

			if (save)
			{
				uint ilim = IL.ilGenImage();
				IL.ilBindImage(ilim);
				IL.ilLoadDataL(data, (uint)data.Length, (uint)Width, (uint)Height, 1, 4);
				File.Delete();
				IL.ilSave(IL.ImageType.PNG, File.FullName);

				SetImages();

				IL.ilDeleteImage(ilim);
				Dirty = false;
			}
		}

		public bool ChangeName(string newName)
		{
			if (Directory.GetFiles(newName + ".png", SearchOption.TopDirectoryOnly).Length != 0)
				return false;

			Name = newName;
			var oldFile = File;
			File = File.CopyToParent(newName + ".png");
			oldFile.Delete();

			return true;
		}
	}
}
