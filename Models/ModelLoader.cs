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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Paril.OpenGL;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using MCSkin3D.Models;
using System.Xml;

namespace MCSkin3D
{
	public static class ModelLoader
	{
		public static Dictionary<string, Model> Models = new Dictionary<string, Model>();

		public static void InvertBottomFaces()
		{
			foreach (var m in Models.Values)
				foreach (var mesh in m.Meshes)
					foreach (var face in mesh.Faces)
					{
						if (face.Downface)
						{
							float minY = 1, maxY = 0;

							for (int i = 0; i < 4; ++i)
							{
								if (face.TexCoords[i].Y < minY)
									minY = face.TexCoords[i].Y;
								if (face.TexCoords[i].Y > maxY)
									maxY = face.TexCoords[i].Y;
							}

							for (int i = 0; i < 4; ++i)
							{
								if (face.TexCoords[i].Y == minY)
									face.TexCoords[i].Y = maxY;
								else
									face.TexCoords[i].Y = minY;
							}
						}
					}
		}

		/*
		 * Magic from Java
		 */
		public class PositionTextureVertex
		{
			public Vector3 vector3D;
			public float texturePositionX;
			public float texturePositionY;

			public PositionTextureVertex(float f, float f1, float f2, float f3, float f4) :
				this(new Vector3(f, f1, f2), f3, f4)
			{
			}

			public PositionTextureVertex setTexturePosition(float f, float f1)
			{
				return new PositionTextureVertex(this, f, f1);
			}

			public PositionTextureVertex(PositionTextureVertex positiontexturevertex, float f, float f1)
			{
				vector3D = positiontexturevertex.vector3D;
				texturePositionX = f;
				texturePositionY = f1;
			}

			public PositionTextureVertex(Vector3 vec3d, float f, float f1)
			{
				vector3D = vec3d;
				texturePositionX = f;
				texturePositionY = f1;
			}
		}

		public class TexturedQuad
		{
			public PositionTextureVertex[] vertexPositions;
			public int nVertices;

			public TexturedQuad(PositionTextureVertex[] apositiontexturevertex)
			{
				nVertices = 0;
				vertexPositions = apositiontexturevertex;
				nVertices = apositiontexturevertex.Length;
			}

			public TexturedQuad(PositionTextureVertex[] apositiontexturevertex, int i, int j, int k, int l, float f, float f1) :
				this(apositiontexturevertex)
			{
				float f2 = 0.0F / f;
				float f3 = 0.0F / f1;
				apositiontexturevertex[0] = apositiontexturevertex[0].setTexturePosition((float)k / f - f2, (float)j / f1 + f3);
				apositiontexturevertex[1] = apositiontexturevertex[1].setTexturePosition((float)i / f + f2, (float)j / f1 + f3);
				apositiontexturevertex[2] = apositiontexturevertex[2].setTexturePosition((float)i / f + f2, (float)l / f1 - f3);
				apositiontexturevertex[3] = apositiontexturevertex[3].setTexturePosition((float)k / f - f2, (float)l / f1 - f3);
			}

			public void flipFace()
			{
				PositionTextureVertex[] apositiontexturevertex = new PositionTextureVertex[vertexPositions.Length];
				for (int i = 0; i < vertexPositions.Length; i++)
					apositiontexturevertex[i] = vertexPositions[vertexPositions.Length - i - 1];

				vertexPositions = apositiontexturevertex;
			}
		}

		public class TextureOffset
		{

			public int field_40734_a;
			public int field_40733_b;

			public TextureOffset(int i, int j)
			{
				field_40734_a = i;
				field_40733_b = j;
			}
		}

		public class ModelRenderer
		{
			public float textureWidth;
			public float textureHeight;
			private int textureOffsetX;
			private int textureOffsetY;
			public float rotationPointX;
			public float rotationPointY;
			public float rotationPointZ;
			public float rotateAngleX;
			public float rotateAngleY;
			public float rotateAngleZ;
			public bool mirror;
			public bool showModel;
			public bool isHidden;
			public List<ModelBox> cubeList;
			public List<ModelRenderer> childModels;
			public string boxName;
			private ModelBase baseModel;

			public VisiblePartFlags Flags;

			public ModelRenderer(ModelBase b, VisiblePartFlags flags, bool helmet, bool animate) :
				this(b, null, flags, helmet, animate)
			{
			}

			public ModelRenderer(ModelBase modelbase, string s, VisiblePartFlags flags, bool helmet, bool animate)
			{
				textureWidth = 64F;
				textureHeight = 32F;
				mirror = false;
				showModel = true;
				isHidden = false;
				cubeList = new List<ModelBox>();
				baseModel = modelbase;
				modelbase.boxList.Add(this);
				boxName = s;
				setTextureSize(modelbase.textureWidth, modelbase.textureHeight);

				Flags = flags;
			}

			public ModelRenderer(ModelBase modelbase, int i, int j, VisiblePartFlags flags, bool helmet, bool animate) :
				this(modelbase, null, flags, helmet, animate)
			{
				setTextureOffset(i, j);
			}

			public void addChild(ModelRenderer modelrenderer)
			{
				if (childModels == null)
				{
					childModels = new List<ModelRenderer>();
				}
				childModels.Add(modelrenderer);
			}

			public ModelRenderer setTextureOffset(int i, int j)
			{
				textureOffsetX = i;
				textureOffsetY = j;
				return this;
			}

			public ModelRenderer addBox(string name, String s, float f, float f1, float f2, int i, int j, int k)
			{
				s = (new StringBuilder()).Append(boxName).Append(".").Append(s).ToString();
				TextureOffset textureoffset = baseModel.func_40297_a(s);
				setTextureOffset(textureoffset.field_40734_a, textureoffset.field_40733_b);
				cubeList.Add((new ModelBox(this, name, textureOffsetX, textureOffsetY, f, f1, f2, i, j, k, 0.0F)).func_40671_a(s));
				return this;
			}

			public ModelRenderer addBox(string name, float f, float f1, float f2, int i, int j, int k)
			{
				cubeList.Add(new ModelBox(this, name, textureOffsetX, textureOffsetY, f, f1, f2, i, j, k, 0.0F));
				return this;
			}

			public void addBox(string name, float f, float f1, float f2, int i, int j, int k, float f3)
			{
				cubeList.Add(new ModelBox(this, name, textureOffsetX, textureOffsetY, f, f1, f2, i, j, k, f3));
			}

			public void setRotationPoint(float f, float f1, float f2)
			{
				rotationPointX = f;
				rotationPointY = f1;
				rotationPointZ = f2;
			}

			public ModelRenderer setTextureSize(int i, int j)
			{
				textureWidth = i;
				textureHeight = j;
				return this;
			}
		}

		public class PlaneRenderer
		{
			public float textureWidth;
			public float textureHeight;
			public PositionTextureVertex[] corners;
			public TexturedQuad[] faces;
			public int textureOffsetX;
			public int textureOffsetY;
			public float rotationPointX;
			public float rotationPointY;
			public float rotationPointZ;
			public float rotateAngleX;
			public float rotateAngleY;
			public float rotateAngleZ;
			public float x;
			public float y;
			public float z;
			public float xMax;
			public float yMax;
			public float zMax;
			public bool compiled;
			public int displayList;
			public bool mirror;
			public bool showModel;
			public bool isHidden;
			public string Name;
			public float Offset;
			public int Side;

			public VisiblePartFlags Flags;
			public bool Helmet;
			public bool Animate, AlsoReverse;

			public PlaneRenderer(ModelBase modelbase, string name, int i, int j, VisiblePartFlags flags, bool helmet, bool animate, bool alsoReverse = true)
			{
				Name = name;
				this.textureWidth = 64.0F;
				this.textureHeight = 32.0F;
				this.compiled = false;
				this.displayList = 0;
				this.mirror = false;
				this.showModel = true;
				this.isHidden = false;
				this.textureOffsetX = i;
				this.textureOffsetY = j;
				modelbase.boxList.Add(this);

				Flags = flags;
				Helmet = helmet;
				AlsoReverse = alsoReverse;
				Animate = animate;
			}

			public void addBackPlane(float f, float f1, float f2, int i, int j, int k)
			{
				addBackPlane(f, f1, f2, i, j, k, 0.0F);
			}

			public void addSidePlane(float f, float f1, float f2, int i, int j, int k)
			{
				addSidePlane(f, f1, f2, i, j, k, 0.0F);
			}

			public void addTopPlane(float f, float f1, float f2, int i, int j, int k)
			{
				addTopPlane(f, f1, f2, i, j, k, 0.0F);
			}

			public void addBackPlane(float f, float f1, float f2, int i, int j, int k, float f3)
			{
				Side = 0;
				Offset = f3;

				this.x = f;
				this.y = f1;
				this.z = f2;
				this.xMax = (f + i);
				this.yMax = (f1 + j);
				this.zMax = (f2 + k);
				this.corners = new PositionTextureVertex[8];
				this.faces = new TexturedQuad[1];
				float f4 = f + i;
				float f5 = f1 + j;
				float f6 = f2 + k;
				f -= f3;
				f1 -= f3;
				f2 -= f3;
				f4 += f3;
				f5 += f3;
				f6 += f3;
				if (this.mirror)
				{
					float f7 = f4;
					f4 = f;
					f = f7;
				}
				PositionTextureVertex positiontexturevertex = new PositionTextureVertex(f, f1, f2, 0.0F, 0.0F);
				PositionTextureVertex positiontexturevertex1 = new PositionTextureVertex(f4, f1, f2, 0.0F, 8.0F);
				PositionTextureVertex positiontexturevertex2 = new PositionTextureVertex(f4, f5, f2, 8.0F, 8.0F);
				PositionTextureVertex positiontexturevertex3 = new PositionTextureVertex(f, f5, f2, 8.0F, 0.0F);
				PositionTextureVertex positiontexturevertex4 = new PositionTextureVertex(f, f1, f6, 0.0F, 0.0F);
				PositionTextureVertex positiontexturevertex5 = new PositionTextureVertex(f4, f1, f6, 0.0F, 8.0F);
				PositionTextureVertex positiontexturevertex6 = new PositionTextureVertex(f4, f5, f6, 8.0F, 8.0F);
				PositionTextureVertex positiontexturevertex7 = new PositionTextureVertex(f, f5, f6, 8.0F, 0.0F);
				this.corners[0] = positiontexturevertex;
				this.corners[1] = positiontexturevertex1;
				this.corners[2] = positiontexturevertex2;
				this.corners[3] = positiontexturevertex3;
				this.corners[4] = positiontexturevertex4;
				this.corners[5] = positiontexturevertex5;
				this.corners[6] = positiontexturevertex6;
				this.corners[7] = positiontexturevertex7;
				this.faces[0] = new TexturedQuad(new PositionTextureVertex[] { positiontexturevertex1, positiontexturevertex, positiontexturevertex3, positiontexturevertex2 }, this.textureOffsetX, this.textureOffsetY, this.textureOffsetX + i, this.textureOffsetY + j, this.textureWidth, this.textureHeight);

				if (this.mirror)
					this.faces[0].flipFace();
			}

			public void addSidePlane(float f, float f1, float f2, int i, int j, int k, float f3)
			{
				Side = 1;
				Offset = f3;
				
				this.x = f;
				this.y = f1;
				this.z = f2;
				this.xMax = (f + i);
				this.yMax = (f1 + j);
				this.zMax = (f2 + k);
				this.corners = new PositionTextureVertex[8];
				this.faces = new TexturedQuad[1];
				float f4 = f + i;
				float f5 = f1 + j;
				float f6 = f2 + k;
				f -= f3;
				f1 -= f3;
				f2 -= f3;
				f4 += f3;
				f5 += f3;
				f6 += f3;
				if (this.mirror)
				{
					float f7 = f4;
					f4 = f;
					f = f7;
				}
				PositionTextureVertex positiontexturevertex = new PositionTextureVertex(f, f1, f2, 0.0F, 0.0F);
				PositionTextureVertex positiontexturevertex1 = new PositionTextureVertex(f4, f1, f2, 0.0F, 8.0F);
				PositionTextureVertex positiontexturevertex2 = new PositionTextureVertex(f4, f5, f2, 8.0F, 8.0F);
				PositionTextureVertex positiontexturevertex3 = new PositionTextureVertex(f, f5, f2, 8.0F, 0.0F);
				PositionTextureVertex positiontexturevertex4 = new PositionTextureVertex(f, f1, f6, 0.0F, 0.0F);
				PositionTextureVertex positiontexturevertex5 = new PositionTextureVertex(f4, f1, f6, 0.0F, 8.0F);
				PositionTextureVertex positiontexturevertex6 = new PositionTextureVertex(f4, f5, f6, 8.0F, 8.0F);
				PositionTextureVertex positiontexturevertex7 = new PositionTextureVertex(f, f5, f6, 8.0F, 0.0F);
				this.corners[0] = positiontexturevertex;
				this.corners[1] = positiontexturevertex1;
				this.corners[2] = positiontexturevertex2;
				this.corners[3] = positiontexturevertex3;
				this.corners[4] = positiontexturevertex4;
				this.corners[5] = positiontexturevertex5;
				this.corners[6] = positiontexturevertex6;
				this.corners[7] = positiontexturevertex7;
				this.faces[0] = new TexturedQuad(new PositionTextureVertex[] { positiontexturevertex5, positiontexturevertex1, positiontexturevertex2, positiontexturevertex6 }, this.textureOffsetX, this.textureOffsetY, this.textureOffsetX + k, this.textureOffsetY + j, this.textureWidth, this.textureHeight);

				if (this.mirror)
					this.faces[0].flipFace();
			}

			public void addTopPlane(float f, float f1, float f2, int i, int j, int k, float f3)
			{
				Side = 2;
				Offset = f3;

				this.x = f;
				this.y = f1;
				this.z = f2;
				this.xMax = (f + i);
				this.yMax = (f1 + j);
				this.zMax = (f2 + k);
				this.corners = new PositionTextureVertex[8];
				this.faces = new TexturedQuad[1];
				float f4 = f + i;
				float f5 = f1 + j;
				float f6 = f2 + k;
				f -= f3;
				f1 -= f3;
				f2 -= f3;
				f4 += f3;
				f5 += f3;
				f6 += f3;
				if (this.mirror)
				{
					float f7 = f4;
					f4 = f;
					f = f7;
				}
				PositionTextureVertex positiontexturevertex = new PositionTextureVertex(f, f1, f2, 0.0F, 0.0F);
				PositionTextureVertex positiontexturevertex1 = new PositionTextureVertex(f4, f1, f2, 0.0F, 8.0F);
				PositionTextureVertex positiontexturevertex2 = new PositionTextureVertex(f4, f5, f2, 8.0F, 8.0F);
				PositionTextureVertex positiontexturevertex3 = new PositionTextureVertex(f, f5, f2, 8.0F, 0.0F);
				PositionTextureVertex positiontexturevertex4 = new PositionTextureVertex(f, f1, f6, 0.0F, 0.0F);
				PositionTextureVertex positiontexturevertex5 = new PositionTextureVertex(f4, f1, f6, 0.0F, 8.0F);
				PositionTextureVertex positiontexturevertex6 = new PositionTextureVertex(f4, f5, f6, 8.0F, 8.0F);
				PositionTextureVertex positiontexturevertex7 = new PositionTextureVertex(f, f5, f6, 8.0F, 0.0F);
				this.corners[0] = positiontexturevertex;
				this.corners[1] = positiontexturevertex1;
				this.corners[2] = positiontexturevertex2;
				this.corners[3] = positiontexturevertex3;
				this.corners[4] = positiontexturevertex4;
				this.corners[5] = positiontexturevertex5;
				this.corners[6] = positiontexturevertex6;
				this.corners[7] = positiontexturevertex7;
				this.faces[0] = new TexturedQuad(new PositionTextureVertex[] { positiontexturevertex5, positiontexturevertex4, positiontexturevertex, positiontexturevertex1 }, this.textureOffsetX, this.textureOffsetY, this.textureOffsetX + i, this.textureOffsetY + k, this.textureWidth, this.textureHeight);

				if (this.mirror)
					this.faces[0].flipFace();
			}

			public void setRotationPoint(float f, float f1, float f2)
			{
				this.rotationPointX = f;
				this.rotationPointY = f1;
				this.rotationPointZ = f2;
			}

			public PlaneRenderer setTextureSize(int i, int j)
			{
				this.textureWidth = i;
				this.textureHeight = j;
				return this;
			}
		}

		public class ModelBox
		{

			public PositionTextureVertex[] field_40679_h;
			public TexturedQuad[] field_40680_i;
			public float x;
			public float y;
			public float z;
			public float xMax;
			public float yMax;
			public float zMax;
			public String field_40673_g;
			public string Name;
			public int texX, texY;
			public float sizeOfs;

			public ModelBox(ModelRenderer modelrenderer, string name, int i, int j, float f, float f1, float f2, int k,
					int l, int i1, float f3)
			{
				sizeOfs = f3;
				texX = i;
				texY = j;
				Name = name;
				x = f;
				y = f1;
				z = f2;
				xMax = f + (float)k;
				yMax = f1 + (float)l;
				zMax = f2 + (float)i1;
				field_40679_h = new PositionTextureVertex[8];
				field_40680_i = new TexturedQuad[6];
				float f4 = f + (float)k;
				float f5 = f1 + (float)l;
				float f6 = f2 + (float)i1;
				f -= f3;
				f1 -= f3;
				f2 -= f3;
				f4 += f3;
				f5 += f3;
				f6 += f3;
				if (modelrenderer.mirror)
				{
					float f7 = f4;
					f4 = f;
					f = f7;
				}
				PositionTextureVertex positiontexturevertex = new PositionTextureVertex(f, f1, f2, 0.0F, 0.0F);
				PositionTextureVertex positiontexturevertex1 = new PositionTextureVertex(f4, f1, f2, 0.0F, 8F);
				PositionTextureVertex positiontexturevertex2 = new PositionTextureVertex(f4, f5, f2, 8F, 8F);
				PositionTextureVertex positiontexturevertex3 = new PositionTextureVertex(f, f5, f2, 8F, 0.0F);
				PositionTextureVertex positiontexturevertex4 = new PositionTextureVertex(f, f1, f6, 0.0F, 0.0F);
				PositionTextureVertex positiontexturevertex5 = new PositionTextureVertex(f4, f1, f6, 0.0F, 8F);
				PositionTextureVertex positiontexturevertex6 = new PositionTextureVertex(f4, f5, f6, 8F, 8F);
				PositionTextureVertex positiontexturevertex7 = new PositionTextureVertex(f, f5, f6, 8F, 0.0F);
				field_40679_h[0] = positiontexturevertex;
				field_40679_h[1] = positiontexturevertex1;
				field_40679_h[2] = positiontexturevertex2;
				field_40679_h[3] = positiontexturevertex3;
				field_40679_h[4] = positiontexturevertex4;
				field_40679_h[5] = positiontexturevertex5;
				field_40679_h[6] = positiontexturevertex6;
				field_40679_h[7] = positiontexturevertex7;
				field_40680_i[0] = new TexturedQuad(new PositionTextureVertex[] {
					positiontexturevertex5, positiontexturevertex1, positiontexturevertex2, positiontexturevertex6
				}, i + i1 + k, j + i1, i + i1 + k + i1, j + i1 + l, modelrenderer.textureWidth, modelrenderer.textureHeight);
				field_40680_i[1] = new TexturedQuad(new PositionTextureVertex[] {
					positiontexturevertex, positiontexturevertex4, positiontexturevertex7, positiontexturevertex3
				}, i + 0, j + i1, i + i1, j + i1 + l, modelrenderer.textureWidth, modelrenderer.textureHeight);
				field_40680_i[2] = new TexturedQuad(new PositionTextureVertex[] {
					positiontexturevertex5, positiontexturevertex4, positiontexturevertex, positiontexturevertex1
				}, i + i1, j + 0, i + i1 + k, j + i1, modelrenderer.textureWidth, modelrenderer.textureHeight);
				field_40680_i[3] = new TexturedQuad(new PositionTextureVertex[] {
					positiontexturevertex2, positiontexturevertex3, positiontexturevertex7, positiontexturevertex6
				}, i + i1 + k, j + i1, i + i1 + k + k, j + 0, modelrenderer.textureWidth, modelrenderer.textureHeight);
				field_40680_i[4] = new TexturedQuad(new PositionTextureVertex[] {
					positiontexturevertex1, positiontexturevertex, positiontexturevertex3, positiontexturevertex2
				}, i + i1, j + i1, i + i1 + k, j + i1 + l, modelrenderer.textureWidth, modelrenderer.textureHeight);
				field_40680_i[5] = new TexturedQuad(new PositionTextureVertex[] {
					positiontexturevertex4, positiontexturevertex5, positiontexturevertex6, positiontexturevertex7
				}, i + i1 + k + i1, j + i1, i + i1 + k + i1 + k, j + i1 + l, modelrenderer.textureWidth, modelrenderer.textureHeight);
				if (modelrenderer.mirror)
				{
					for (int j1 = 0; j1 < field_40680_i.Length; j1++)
					{
						field_40680_i[j1].flipFace();
					}

				}
			}

			public ModelBox func_40671_a(String s)
			{
				field_40673_g = s;
				return this;
			}
		}

		public class Entity { }
		public class EntityLiving { }
		public class Map { }

		public class ModelBase
		{
			public float onGround;
			public bool isRiding;
			public List<object> boxList;
			public bool field_40301_k;
			private Dictionary<string, TextureOffset> field_39000_a;
			public int textureWidth;
			public int textureHeight;

			public ModelBase()
			{
				isRiding = false;
				boxList = new List<object>();
				field_40301_k = true;
				field_39000_a = new Dictionary<string, TextureOffset>();
				textureWidth = 64;
				textureHeight = 32;
			}

			protected void setTextureOffset(String s, int i, int j)
			{
				field_39000_a.Add(s, new TextureOffset(i, j));
			}

			public TextureOffset func_40297_a(String s)
			{
				return (TextureOffset)field_39000_a[s];
			}

			public Model Compile(string name, float scale, float defaultWidth, float defaultHeight)
			{
				var model = new Model();
				model.Name = name;
				model.DefaultWidth = defaultWidth;
				model.DefaultHeight = defaultHeight;

				foreach (var boxObj in boxList)
				{
					if (boxObj is ModelRenderer)
					{
						ModelRenderer box = (ModelRenderer)boxObj;
						foreach (var face in box.cubeList)
						{
							Mesh mesh = new Mesh(face.Name);
							mesh.Faces = new List<Face>();
							mesh.Translate = new Vector3(box.rotationPointX, box.rotationPointY, box.rotationPointZ);
							mesh.Part = box.Flags;
							mesh.Rotate = new Vector3(MathHelper.RadiansToDegrees(box.rotateAngleX), MathHelper.RadiansToDegrees(box.rotateAngleY), MathHelper.RadiansToDegrees(box.rotateAngleZ));
							mesh.Pivot = mesh.Translate;

							mesh.Mode = BeginMode.Quads;

							int[] cwIndices = new int[] { 0, 1, 2, 3 };
							int[] cwwIndices = new int[] { 3, 2, 1, 0 };
							Color4[] colors = new Color4[] { Color4.White, Color4.White, Color4.White, Color4.White };

							foreach (var quad in face.field_40680_i)
							{
								List<Vector3> vertices = new List<Vector3>();
								List<Vector2> texcoords = new List<Vector2>();

								foreach (var x in quad.vertexPositions)
								{
									vertices.Add(x.vector3D * scale);
									texcoords.Add(new Vector2(x.texturePositionX, x.texturePositionY));
								}

								Face newFace = new Face(vertices.ToArray(), texcoords.ToArray(), colors, cwwIndices);

								var zero = Model.TranslateVertex(mesh.Translate, mesh.Rotate, mesh.Pivot, newFace.Vertices[0]);
								var one = Model.TranslateVertex(mesh.Translate, mesh.Rotate, mesh.Pivot, newFace.Vertices[1]);
								var two = Model.TranslateVertex(mesh.Translate, mesh.Rotate, mesh.Pivot, newFace.Vertices[2]);

								var dir = Vector3.Cross(one - zero, two - zero);
								newFace.Normal = Vector3.Normalize(dir);

								dir = Vector3.Cross(newFace.Vertices[1] - newFace.Vertices[0], newFace.Vertices[2] - newFace.Vertices[0]);
								var realNormal = Vector3.Normalize(dir);

								if (realNormal == new Vector3(0, 1, 0))
									newFace.Downface = true;

								mesh.Faces.Add(newFace);
							}

							mesh.CalculateCenter();

							model.Meshes.Add(mesh);
						}
					}
					else if (boxObj is PlaneRenderer)
					{
						PlaneRenderer box = (PlaneRenderer)boxObj;

						Mesh mesh = new Mesh(box.Name);
						mesh.Faces = new List<Face>();
						mesh.Translate = new Vector3(box.rotationPointX, box.rotationPointY, box.rotationPointZ);
						mesh.HasTransparency = mesh.AllowTransparency = box.Helmet;
						mesh.Part = box.Flags;
						mesh.Rotate = new Vector3(MathHelper.RadiansToDegrees(box.rotateAngleX), MathHelper.RadiansToDegrees(box.rotateAngleY), MathHelper.RadiansToDegrees(box.rotateAngleZ));
						mesh.Pivot = mesh.Translate;

						mesh.Mode = BeginMode.Quads;

						int[] cwIndices = new int[] { 0, 1, 2, 3 };
						int[] cwwIndices = new int[] { 3, 2, 1, 0 };
						Color4[] colors = new Color4[] { Color4.White, Color4.White, Color4.White, Color4.White };

						foreach (var quad in box.faces)
						{
							List<Vector3> vertices = new List<Vector3>();
							List<Vector2> texcoords = new List<Vector2>();

							foreach (var x in quad.vertexPositions)
							{
								vertices.Add(x.vector3D * scale);
								texcoords.Add(new Vector2(x.texturePositionX, x.texturePositionY));
							}

							Face newFace = new Face(vertices.ToArray(), texcoords.ToArray(), colors, cwIndices);

							var zero = Model.TranslateVertex(mesh.Translate, mesh.Rotate, mesh.Pivot, newFace.Vertices[0]);
							var one = Model.TranslateVertex(mesh.Translate, mesh.Rotate, mesh.Pivot, newFace.Vertices[1]);
							var two = Model.TranslateVertex(mesh.Translate, mesh.Rotate, mesh.Pivot, newFace.Vertices[2]);

							var dir = Vector3.Cross(one - zero, two - zero);
							newFace.Normal = Vector3.Normalize(dir);

							dir = Vector3.Cross(newFace.Vertices[1] - newFace.Vertices[0], newFace.Vertices[2] - newFace.Vertices[0]);
							var realNormal = Vector3.Normalize(dir);

							if (realNormal == new Vector3(0, 1, 0))
								newFace.Downface = true;

							mesh.Faces.Add(newFace);
						}

						mesh.CalculateCenter();

						model.Meshes.Add(mesh);
					}
				}

				model.PartsEnabled = new bool[model.Meshes.Count];

				for (int i = 0; i < model.Meshes.Count; ++i)
					model.PartsEnabled[i] = true;

				return model;
			}

			public void Save(string name, float scale, int defaultWidth, int defaultHeight, string fileName)
			{
				XmlWriterSettings settings = new XmlWriterSettings();
				settings.ConformanceLevel = ConformanceLevel.Fragment;
				settings.Indent = true;
				settings.NewLineOnAttributes = false;
				settings.IndentChars = "\t";

				using (XmlWriter writer = XmlWriter.Create(fileName, settings))
				{
					writer.WriteStartElement("Techne");
					writer.WriteAttributeString("Version", "2.2");
					writer.WriteElementString("Author", "");
					writer.WriteElementString("DateCreated", "");
					writer.WriteElementString("Description", "");

					writer.WriteStartElement("Models");
					writer.WriteStartElement("Model");
					writer.WriteAttributeString("texture", "none.png");

					writer.WriteElementString("BaseClass", "ModelBase");

					writer.WriteStartElement("Geometry");

					writer.WriteStartElement("Folder");
					writer.WriteAttributeString("type", "f8bf7d5b-37bf-455b-93f9-b6f9e81620e1");
					writer.WriteAttributeString("Name", "Model");

					foreach (var mesh in boxList)
					{
						if (mesh is ModelRenderer)
						{
							ModelRenderer renderer = (ModelRenderer)mesh;

							foreach (var x in renderer.cubeList)
							{
								writer.WriteStartElement("Shape");
								writer.WriteAttributeString("type", "d9e621f7-957f-4b77-b1ae-20dcd0da7751");
								writer.WriteAttributeString("name", x.Name);

								writer.WriteStartElement("Animation");
								writer.WriteElementString("AnimationAngles", "0,0,0");
								writer.WriteElementString("AnimationDuration", "0,0,0");
								writer.WriteElementString("AnimationType", "0,0,0");
								writer.WriteEndElement();

								writer.WriteElementString("IsDecorative", "False");
								writer.WriteElementString("IsFixed", "False");
								writer.WriteElementString("IsMirrored", renderer.mirror.ToString());
								writer.WriteElementString("Offset", renderer.rotationPointX + "," + renderer.rotationPointY + "," + renderer.rotationPointZ);
								writer.WriteElementString("Position", x.x + "," + x.y + "," + x.z);
								writer.WriteElementString("Rotation", MathHelper.RadiansToDegrees(renderer.rotateAngleX) + "," + MathHelper.RadiansToDegrees(renderer.rotateAngleY) + "," + MathHelper.RadiansToDegrees(renderer.rotateAngleZ));
								writer.WriteElementString("Size", (x.xMax - x.x) + "," + (x.yMax - x.y) + "," + (x.zMax - x.z));
								writer.WriteElementString("TextureOffset", x.texX + "," + x.texY);
								
								// Paril: MCSkin3D additions
								writer.WriteElementString("Scale", x.sizeOfs.ToString());

								writer.WriteEndElement();
							}
						}
						else if (mesh is PlaneRenderer)
						{
							PlaneRenderer renderer = (PlaneRenderer)mesh;

							writer.WriteStartElement("Shape");
							writer.WriteAttributeString("type", "ab894c83-e399-4236-808b-25a78d56f5e1");
							writer.WriteAttributeString("name", renderer.Name);

							writer.WriteElementString("IsMirrored", renderer.mirror.ToString());
							writer.WriteElementString("Offset", renderer.rotationPointX + "," + renderer.rotationPointY + "," + renderer.rotationPointZ);
							writer.WriteElementString("Position", renderer.x + "," + renderer.y + "," + renderer.z);
							writer.WriteElementString("Rotation", MathHelper.RadiansToDegrees(renderer.rotateAngleX) + "," + MathHelper.RadiansToDegrees(renderer.rotateAngleY) + "," + MathHelper.RadiansToDegrees(renderer.rotateAngleZ));
							writer.WriteElementString("Size", (renderer.xMax - renderer.x) + "," + (renderer.yMax - renderer.y) + "," + (renderer.zMax - renderer.z));
							writer.WriteElementString("TextureOffset", renderer.textureOffsetX + "," + renderer.textureOffsetY);
		
							writer.WriteElementString("Scale", renderer.Offset.ToString());
							
							writer.WriteElementString("Side", renderer.Side.ToString());

							writer.WriteEndElement();
						}
					}

					writer.WriteEndElement();

					writer.WriteEndElement();

					writer.WriteElementString("GlScale", "1,1,1");
					writer.WriteElementString("Name", name);
					writer.WriteElementString("TextureSize", defaultWidth + "," + defaultHeight);

					writer.WriteEndElement();
					writer.WriteEndElement();

					writer.WriteElementString("Name", name);

					writer.WriteEndElement();
				}
			}
		}

		public class ModelQuadruped : ModelBase
		{
			public ModelRenderer head;
			public ModelRenderer body;
			public ModelRenderer leg1;
			public ModelRenderer leg2;
			public ModelRenderer leg3;
			public ModelRenderer leg4;
			protected float field_40331_g;
			protected float field_40332_n;

			public ModelQuadruped(int i, float f)
			{
				field_40331_g = 8F;
				field_40332_n = 4F;
				head = new ModelRenderer(this, 0, 0, VisiblePartFlags.HeadFlag, false, false);
				head.addBox("Head", -4F, -4F, -8F, 8, 8, 8, f);
				head.setRotationPoint(0.0F, 18 - i, -6F);
				body = new ModelRenderer(this, 28, 8, VisiblePartFlags.ChestFlag, false, false);
				body.addBox("Body", -5F, -10F, -7F, 10, 16, 8, f);
				body.setRotationPoint(0.0F, 17 - i, 2.0F);
				body.rotateAngleX = 1.570796F;
				leg1 = new ModelRenderer(this, 0, 16, VisiblePartFlags.LeftLegFlag, false, true);
				leg1.addBox("Left Leg", -2F, 0.0F, -2F, 4, i, 4, f);
				leg1.setRotationPoint(-3F, 24 - i, 7F);
				leg2 = new ModelRenderer(this, 0, 16, VisiblePartFlags.RightLegFlag, false, true);
				leg2.mirror = true;
				leg2.addBox("Right Leg", -2F, 0.0F, -2F, 4, i, 4, f);
				leg2.setRotationPoint(3F, 24 - i, 7F);
				leg3 = new ModelRenderer(this, 0, 16, VisiblePartFlags.LeftArmFlag, false, true);
				leg3.addBox("Left Arm", -2F, 0.0F, -2F, 4, i, 4, f);
				leg3.setRotationPoint(-3F, 24 - i, -5F);
				leg4 = new ModelRenderer(this, 0, 16, VisiblePartFlags.RightArmFlag, false, true);
				leg4.addBox("Right Arm", -2F, 0.0F, -2F, 4, i, 4, f);
				leg4.setRotationPoint(3F, 24 - i, -5F);
				leg4.mirror = true;
			}
		}

		public class ModelPig : ModelQuadruped
		{
			public ModelPig() :
				this(0)
			{
			}

			public ModelPig(float f) :
				base(6, f)
			{
				head.setTextureOffset(16, 16).addBox("Snout", -2F, 0.0F, -9F, 4, 3, 1, f);
				field_40331_g = 4F;
			}
		}


		public class ModelBiped : ModelBase
		{
			public ModelRenderer bipedHead;
			public ModelRenderer bipedHeadwear;
			public ModelRenderer bipedBody;
			public ModelRenderer bipedRightArm;
			public ModelRenderer bipedLeftArm;
			public ModelRenderer bipedRightLeg;
			public ModelRenderer bipedLeftLeg;
			public ModelRenderer bipedEars;
			public ModelRenderer bipedCloak;
			public int heldItemLeft;
			public int heldItemRight;
			public bool isSneak;
			public bool aimedBow;

			public ModelBiped() :
				this(0.0F)
			{
			}

			public ModelBiped(float f) :
				this(f, 0.0F)
			{
			}

			public ModelBiped(float f, float f1)
			{
				heldItemLeft = 0;
				heldItemRight = 0;
				isSneak = false;
				aimedBow = false;
				/*bipedCloak = new ModelRenderer(this, 0, 0);
				bipedCloak.addBox(-5F, 0.0F, -1F, 10, 16, 1, f);
				bipedEars = new ModelRenderer(this, 24, 0);
				bipedEars.addBox(-3F, -6F, -1F, 6, 6, 1, f);*/
				bipedHead = new ModelRenderer(this, 0, 0, VisiblePartFlags.HeadFlag, false, false);
				bipedHead.addBox("Head", -4F, -8F, -4F, 8, 8, 8, f);
				bipedHead.setRotationPoint(0.0F, 0.0F + f1, 0.0F);
				bipedHeadwear = new ModelRenderer(this, 32, 0, VisiblePartFlags.HelmetFlag, true, false);
				bipedHeadwear.addBox("Headwear", -4F, -8F, -4F, 8, 8, 8, f + 0.5F);
				bipedHeadwear.setRotationPoint(0.0F, 0.0F + f1, 0.0F);
				bipedBody = new ModelRenderer(this, 16, 16, VisiblePartFlags.ChestFlag, false, false);
				bipedBody.addBox("Body", -4F, 0.0F, -2F, 8, 12, 4, f);
				bipedBody.setRotationPoint(0.0F, 0.0F + f1, 0.0F);
				bipedRightArm = new ModelRenderer(this, 40, 16, VisiblePartFlags.RightArmFlag, false, true);
				bipedRightArm.addBox("Right Arm", -3F, -2F, -2F, 4, 12, 4, f);
				bipedRightArm.setRotationPoint(-5F, 2.0F + f1, 0.0F);
				bipedLeftArm = new ModelRenderer(this, 40, 16, VisiblePartFlags.LeftArmFlag, false, true);
				bipedLeftArm.mirror = true;
				bipedLeftArm.addBox("Left Arm", -1F, -2F, -2F, 4, 12, 4, f);
				bipedLeftArm.setRotationPoint(5F, 2.0F + f1, 0.0F);
				bipedRightLeg = new ModelRenderer(this, 0, 16, VisiblePartFlags.RightLegFlag, false, true);
				bipedRightLeg.addBox("Right Leg", -2F, 0.0F, -2F, 4, 12, 4, f);
				bipedRightLeg.setRotationPoint(-2F, 12F + f1, 0.0F);
				bipedLeftLeg = new ModelRenderer(this, 0, 16, VisiblePartFlags.LeftLegFlag, false, true);
				bipedLeftLeg.mirror = true;
				bipedLeftLeg.addBox("Left Leg", -2F, 0.0F, -2F, 4, 12, 4, f);
				bipedLeftLeg.setRotationPoint(2.0F, 12F + f1, 0.0F);
			}
		}

		public class ModelArmor : ModelBiped
		{
			public ModelArmor() :
				base(0.5f)
			{
				boxList.Remove(bipedHeadwear);
			}
		}

		public class ModelCloak : ModelBase
		{
			public ModelRenderer bipedCloak;

			public ModelCloak()
			{
				bipedCloak = new ModelRenderer(this, 0, 0, VisiblePartFlags.LeftLegFlag, false, false);
				bipedCloak.addBox("Cloak", -5F, 0.0F, -1F, 10, 16, 1, 0);
			}
		}

		public class ModelVillager : ModelBase
		{
			public ModelRenderer head;
			public ModelRenderer body, bodyOverwear;
			public ModelRenderer arms;
			public ModelRenderer field_40336_d;
			public ModelRenderer field_40337_e;
			public int field_40335_g;
			public bool field_40341_n;
			public bool field_40342_o;

			public int field_40334_f;
			public ModelVillager() :
				this(0)
			{
			}

			public ModelVillager(float f) :
				this(f, 0)
			{
			}

			public ModelVillager(float f, float f1)
			{
				field_40334_f = 0;
				field_40335_g = 0;
				field_40341_n = false;
				field_40342_o = false;
				byte byte0 = 64;
				byte byte1 = 64;
				head = (new ModelRenderer(this, VisiblePartFlags.HeadFlag, false, false)).setTextureSize(byte0, byte1);
				head.setRotationPoint(0.0F, 0.0F + f1, 0.0F);
				head.setTextureOffset(0, 0).addBox("Head", -4F, -10F, -4F, 8, 10, 8, f);
				head.setTextureOffset(24, 0).addBox("Nose", -1F, -3F, -6F, 2, 4, 2, f);
				arms = (new ModelRenderer(this, VisiblePartFlags.LeftArmFlag, false, false)).setTextureSize(byte0, byte1);
				arms.rotationPointY = 3F;
				arms.rotationPointZ = -1F;
				arms.rotateAngleX = -0.75F;
				arms.setTextureOffset(44, 22).addBox("Arms", -8F, -2F, -2F, 4, 8, 4, f);
				arms.setTextureOffset(44, 22).addBox("Arms", 4F, -2F, -2F, 4, 8, 4, f);
				arms.setTextureOffset(40, 38).addBox("Arms", -4F, 2.0F, -2F, 8, 4, 4, f);
				field_40336_d = (new ModelRenderer(this, 0, 22, VisiblePartFlags.LeftLegFlag, false, true)).setTextureSize(byte0, byte1);
				field_40336_d.setRotationPoint(-2F, 12F + f1, 0.0F);
				field_40336_d.addBox("Left Leg", -2F, 0.0F, -2F, 4, 12, 4, f);
				field_40337_e = (new ModelRenderer(this, 0, 22, VisiblePartFlags.RightLegFlag, false, true)).setTextureSize(byte0, byte1);
				field_40337_e.mirror = true;
				field_40337_e.setRotationPoint(2.0F, 12F + f1, 0.0F);
				field_40337_e.addBox("Left Leg", -2F, 0.0F, -2F, 4, 12, 4, f);
				body = (new ModelRenderer(this, VisiblePartFlags.ChestFlag, false, false)).setTextureSize(byte0, byte1);
				body.setRotationPoint(0.0F, 0.0F + f1, 0.0F);
				body.setTextureOffset(16, 20).addBox("Body", -4F, 0.0F, -3F, 8, 12, 6, f);
				bodyOverwear = (new ModelRenderer(this, VisiblePartFlags.ChestFlag, true, false)).setTextureSize(byte0, byte1);
				bodyOverwear.setRotationPoint(0.0F, 0.0F + f1, 0.0F);
				bodyOverwear.setTextureOffset(0, 38).addBox("Overwear", -4F, 0.0F, -3F, 8, 18, 6, f + 0.5F);
			}
		}

		public class ModelCreeper : ModelBase
		{
			public ModelRenderer head;
			public ModelRenderer unusedCreeperHeadwear;
			public ModelRenderer body;
			public ModelRenderer leg1;
			public ModelRenderer leg2;
			public ModelRenderer leg3;
			public ModelRenderer leg4;

			public ModelCreeper() :
				this(0)
			{
			}

			public ModelCreeper(float f)
			{
				int i = 4;
				head = new ModelRenderer(this, 0, 0, VisiblePartFlags.HeadFlag, false, false);
				head.addBox("Head", -4F, -8F, -4F, 8, 8, 8, f);
				head.setRotationPoint(0.0F, i, 0.0F);
				//unusedCreeperHeadwear = new ModelRenderer(this, 32, 0);
				//unusedCreeperHeadwear.addBox(-4F, -8F, -4F, 8, 8, 8, f + 0.5F);
				//unusedCreeperHeadwear.setRotationPoint(0.0F, i, 0.0F);
				body = new ModelRenderer(this, 16, 16, VisiblePartFlags.ChestFlag, false, false);
				body.addBox("Body", -4F, 0.0F, -2F, 8, 12, 4, f);
				body.setRotationPoint(0.0F, i, 0.0F);
				leg1 = new ModelRenderer(this, 0, 16, VisiblePartFlags.LeftLegFlag, false, true);
				leg1.addBox("Left Leg", -2F, 0.0F, -2F, 4, 6, 4, f);
				leg1.setRotationPoint(-2F, 12 + i, 4F);
				leg2 = new ModelRenderer(this, 0, 16, VisiblePartFlags.RightLegFlag, false, true);
				leg2.addBox("Right Leg", -2F, 0.0F, -2F, 4, 6, 4, f);
				leg2.setRotationPoint(2.0F, 12 + i, 4F);
				leg2.mirror = true;
				leg3 = new ModelRenderer(this, 0, 16, VisiblePartFlags.LeftArmFlag, false, true);
				leg3.addBox("Left Arm", -2F, 0.0F, -2F, 4, 6, 4, f);
				leg3.setRotationPoint(-2F, 12 + i, -4F);
				leg4 = new ModelRenderer(this, 0, 16, VisiblePartFlags.RightArmFlag, false, true);
				leg4.addBox("Right Arm", -2F, 0.0F, -2F, 4, 6, 4, f);
				leg4.setRotationPoint(2.0F, 12 + i, -4F);
				leg4.mirror = true;
			}
		}

		public class ModelCow : ModelQuadruped
		{
			public ModelCow() :
				base(12, 0)
			{
				boxList.Remove(head);
				head = new ModelRenderer(this, 0, 0, VisiblePartFlags.HeadFlag, false, false);
				head.addBox("Head", -4F, -4F, -6F, 8, 8, 6, 0.0F);
				head.setRotationPoint(0.0F, 4F, -8F);
				head.setTextureOffset(22, 0).addBox("Ear", -5F, -5F, -4F, 1, 3, 1, 0.0F);
				head.setTextureOffset(22, 0).addBox("Ear", 4F, -5F, -4F, 1, 3, 1, 0.0F);
				boxList.Remove(body);
				body = new ModelRenderer(this, 18, 4, VisiblePartFlags.ChestFlag, false, false);
				body.addBox("Body", -6F, -10F, -7F, 12, 18, 10, 0.0F);
				body.setRotationPoint(0.0F, 5F, 2.0F);
				body.setTextureOffset(52, 0).addBox("Udder", -2F, 2.0F, -8F, 4, 6, 1);
				body.rotateAngleX = 1.570796F;
				leg1.rotationPointX--;
				leg2.rotationPointX++;
				leg1.rotationPointZ += 0.0F;
				leg2.rotationPointZ += 0.0F;
				leg3.rotationPointX--;
				leg4.rotationPointX++;
				leg3.rotationPointZ--;
				leg4.rotationPointZ--;
				field_40332_n += 2.0F;
			}
		}

		public class ModelChicken : ModelBase
		{
			public ModelRenderer head;
			public ModelRenderer body;
			public ModelRenderer rightLeg;
			public ModelRenderer leftLeg;
			public ModelRenderer rightWing;
			public ModelRenderer leftWing;
			public ModelRenderer bill;
			public ModelRenderer chin;

			public ModelChicken()
			{
				byte byte0 = 16;
				head = new ModelRenderer(this, 0, 0, VisiblePartFlags.HeadFlag, true, false);
				head.addBox("Head", -2F, -6F, -2F, 4, 6, 3, 0.0F);
				head.setRotationPoint(0.0F, -1 + byte0, -4F);
				bill = new ModelRenderer(this, 14, 0, VisiblePartFlags.HeadFlag, false, false);
				bill.addBox("Bill", -2F, -4F, -4F, 4, 2, 2, 0.0F);
				bill.setRotationPoint(0.0F, -1 + byte0, -4F);
				chin = new ModelRenderer(this, 14, 4, VisiblePartFlags.HeadFlag, false, false);
				chin.addBox("Chin", -1F, -2F, -3F, 2, 2, 2, 0.0F);
				chin.setRotationPoint(0.0F, -1 + byte0, -4F);
				body = new ModelRenderer(this, 0, 9, VisiblePartFlags.ChestFlag, false, false);
				body.addBox("Body", -3F, -4F, -3F, 6, 8, 6, 0.0F);
				body.setRotationPoint(0.0F, 0 + byte0, 0.0F);
				rightLeg = new ModelRenderer(this, 26, 0, VisiblePartFlags.RightLegFlag, true, true);
				rightLeg.addBox("Right Leg", -1F, 0.0F, -3F, 3, 5, 3);
				rightLeg.setRotationPoint(-2F, 3 + byte0, 1.0F);
				leftLeg = new ModelRenderer(this, 26, 0, VisiblePartFlags.LeftLegFlag, true, true);
				leftLeg.addBox("Left Leg", -1F, 0.0F, -3F, 3, 5, 3);
				leftLeg.setRotationPoint(1.0F, 3 + byte0, 1.0F);
				leftLeg.mirror = true;
				rightWing = new ModelRenderer(this, 24, 13, VisiblePartFlags.RightArmFlag, false, true);
				rightWing.addBox("Right Wing", 0.0F, 0.0F, -3F, 1, 4, 6);
				rightWing.setRotationPoint(-4F, -3 + byte0, 0.0F);
				leftWing = new ModelRenderer(this, 24, 13, VisiblePartFlags.LeftArmFlag, false, true);
				leftWing.addBox("Left Leg", -1F, 0.0F, -3F, 1, 4, 6);
				leftWing.setRotationPoint(4F, -3 + byte0, 0.0F);
			}
		}

		public class ModelSlime : ModelBase
		{
			ModelRenderer slimeBodies, slimeBodies2;
			ModelRenderer slimeRightEye;
			ModelRenderer slimeLeftEye;
			ModelRenderer slimeMouth;

			public ModelSlime(int i)
			{
				slimeBodies = new ModelRenderer(this, 0, 0, VisiblePartFlags.HeadFlag, true, false);
				slimeBodies.addBox("Slime", -4F, 16F, -4F, 8, 8, 8);
				if (i > 0)
				{
					slimeBodies2 = new ModelRenderer(this, 0, i, VisiblePartFlags.HelmetFlag, true, false);
					slimeBodies2.addBox("Slime", -3F, 17F, -3F, 6, 6, 6);
					slimeRightEye = new ModelRenderer(this, 32, 0, VisiblePartFlags.RightLegFlag, false, false);
					slimeRightEye.addBox("Eye", -3.25F, 18F, -3.5F, 2, 2, 2);
					slimeLeftEye = new ModelRenderer(this, 32, 4, VisiblePartFlags.LeftLegFlag, false, false);
					slimeLeftEye.addBox("Eye", 1.25F, 18F, -3.5F, 2, 2, 2);
					slimeMouth = new ModelRenderer(this, 32, 8, VisiblePartFlags.ChestFlag, false, false);
					slimeMouth.addBox("Mouth", 0.0F, 21F, -3.5F, 1, 1, 1);
				}
			}
		}

		public class ModelSquid : ModelBase
		{
			ModelRenderer squidBody;
			ModelRenderer[] squidTentacles;

			public ModelSquid()
			{
				squidTentacles = new ModelRenderer[8];
				var byte0 = -16;
				squidBody = new ModelRenderer(this, 0, 0, VisiblePartFlags.ChestFlag, false, false);
				squidBody.addBox("Body", -6F, -8F, -6F, 12, 16, 12);
				squidBody.rotationPointY += 24 + byte0;
				for (int i = 0; i < squidTentacles.Length; i++)
				{
					squidTentacles[i] = new ModelRenderer(this, 48, 0, VisiblePartFlags.LeftArmFlag, false, true);
					double d = ((double)i * 3.1415926535897931D * 2D) / (double)squidTentacles.Length;
					float f = (float)Math.Cos(d) * 5F;
					float f1 = (float)Math.Sin(d) * 5F;
					squidTentacles[i].addBox("Tentacle", -1F, 0.0F, -1F, 2, 18, 2);
					squidTentacles[i].rotationPointX = f;
					squidTentacles[i].rotationPointZ = f1;
					squidTentacles[i].rotationPointY = 31 + byte0;
					d = ((double)i * 3.1415926535897931D * -2D) / (double)squidTentacles.Length + 1.5707963267948966D;
					squidTentacles[i].rotateAngleY = (float)d;
				}
			}
		}

		public class ModelMagmaCube : ModelBase
		{
			ModelRenderer[] field_40345_a;
			ModelRenderer field_40344_b;

			public ModelMagmaCube()
			{
				field_40345_a = new ModelRenderer[8];
				for (int i = 0; i < field_40345_a.Length; i++)
				{
					byte byte0 = 0;
					int j = i;
					if (i == 2)
					{
						byte0 = 24;
						j = 10;
					}
					else
						if (i == 3)
						{
							byte0 = 24;
							j = 19;
						}
					field_40345_a[i] = new ModelRenderer(this, byte0, j, VisiblePartFlags.HeadFlag, false, false);
					field_40345_a[i].addBox("Head", -4F, 16 + i, -4F, 8, 1, 8);
				}

				field_40344_b = new ModelRenderer(this, 0, 16, VisiblePartFlags.ChestFlag, false, false);
				field_40344_b.addBox("Core", -2F, 18F, -2F, 4, 4, 4);
			}
		}

		public class ModelBlaze : ModelBase
		{
			private ModelRenderer[] field_40323_a;
			private ModelRenderer field_40322_b;

			public ModelBlaze()
			{
				field_40323_a = new ModelRenderer[12];
				for (int i = 0; i < field_40323_a.Length; i++)
				{
					field_40323_a[i] = new ModelRenderer(this, 0, 16, VisiblePartFlags.LeftArmFlag, false, false);
					field_40323_a[i].addBox("Rod", 0.0F, 0.0F, 0.0F, 2, 8, 2);
				}

				field_40322_b = new ModelRenderer(this, 0, 0, VisiblePartFlags.HeadFlag, false, false);
				field_40322_b.addBox("Head", -4F, -4F, -4F, 8, 8, 8);

				setRotationAngles(0, 0, 0, 0, 0, 0);
			}

			public void setRotationAngles(float f, float f1, float f2, float f3, float f4, float f5)
			{
				float f6 = f2 * 3.141593F * -0.1F;
				for (int i = 0; i < 4; i++)
				{
					field_40323_a[i].rotationPointY = -2F + (float)Math.Cos(((float)(i * 2) + f2) * 0.25F);
					field_40323_a[i].rotationPointX = (float)Math.Cos(f6) * 9F;
					field_40323_a[i].rotationPointZ = (float)Math.Sin(f6) * 9F;
					f6 += 1.570796F;
				}

				f6 = 0.7853982F + f2 * 3.141593F * 0.03F;
				for (int j = 4; j < 8; j++)
				{
					field_40323_a[j].rotationPointY = 2.0F + (float)Math.Cos(((float)(j * 2) + f2) * 0.25F);
					field_40323_a[j].rotationPointX = (float)Math.Cos(f6) * 7F;
					field_40323_a[j].rotationPointZ = (float)Math.Sin(f6) * 7F;
					f6 += 1.570796F;
				}

				f6 = 0.4712389F + f2 * 3.141593F * -0.05F;
				for (int k = 8; k < 12; k++)
				{
					field_40323_a[k].rotationPointY = 11F + (float)Math.Cos(((float)k * 1.5F + f2) * 0.5F);
					field_40323_a[k].rotationPointX = (float)Math.Cos(f6) * 5F;
					field_40323_a[k].rotationPointZ = (float)Math.Sin(f6) * 5F;
					f6 += 1.570796F;
				}

				field_40322_b.rotateAngleY = f3 / 57.29578F;
				field_40322_b.rotateAngleX = f4 / 57.29578F;
			}
		}

		public class ModelSilverfish : ModelBase
		{

			private ModelRenderer[] silverfishBodyParts;
			private ModelRenderer[] silverfishWings;
			private float[] field_35399_c;
			private static int[,] silverfishBoxLength = {
				{
					3, 2, 2
				}, {
					4, 3, 2
				}, {
					6, 4, 3
				}, {
					3, 3, 3
				}, {
					2, 2, 3
				}, {
					2, 1, 2
				}, {
					1, 1, 2
				}
			};
			private static int[,] silverfishTexturePositions = {
				{
					0, 0
				}, {
					0, 4
				}, {
					0, 9
				}, {
					0, 16
				}, {
					0, 22
				}, {
					11, 0
				}, {
					13, 4
				}
			};

			public ModelSilverfish()
			{
				field_35399_c = new float[7];
				silverfishBodyParts = new ModelRenderer[7];
				float f = -3.5F;
				for (int i = 0; i < silverfishBodyParts.Length; i++)
				{
					silverfishBodyParts[i] = new ModelRenderer(this, silverfishTexturePositions[i, 0], silverfishTexturePositions[i, 1], VisiblePartFlags.ChestFlag, false, false);
					silverfishBodyParts[i].addBox("Body", (float)silverfishBoxLength[i, 0] * -0.5F, 0.0F, (float)silverfishBoxLength[i, 2] * -0.5F, silverfishBoxLength[i, 0], silverfishBoxLength[i, 1], silverfishBoxLength[i, 2]);
					silverfishBodyParts[i].setRotationPoint(0.0F, 24 - silverfishBoxLength[i, 1], f);
					field_35399_c[i] = f;
					if (i < silverfishBodyParts.Length - 1)
					{
						f += (float)(silverfishBoxLength[i, 2] + silverfishBoxLength[i + 1, 2]) * 0.5F;
					}
				}

				silverfishWings = new ModelRenderer[3];
				silverfishWings[0] = new ModelRenderer(this, 20, 0, VisiblePartFlags.HeadFlag, true, false);
				silverfishWings[0].addBox("Wings", -5F, 0.0F, (float)silverfishBoxLength[2, 2] * -0.5F, 10, 8, silverfishBoxLength[2, 2]);
				silverfishWings[0].setRotationPoint(0.0F, 16F, field_35399_c[2]);
				silverfishWings[1] = new ModelRenderer(this, 20, 11, VisiblePartFlags.HeadFlag, true, false);
				silverfishWings[1].addBox("Wings", -3F, 0.0F, (float)silverfishBoxLength[4, 2] * -0.5F, 6, 4, silverfishBoxLength[4, 2]);
				silverfishWings[1].setRotationPoint(0.0F, 20F, field_35399_c[4]);
				silverfishWings[2] = new ModelRenderer(this, 20, 18, VisiblePartFlags.HeadFlag, true, false);
				silverfishWings[2].addBox("Wings", -3F, 0.0F, (float)silverfishBoxLength[4, 2] * -0.5F, 6, 5, silverfishBoxLength[1, 2]);
				silverfishWings[2].setRotationPoint(0.0F, 19F, field_35399_c[1]);
			}
		}

		public class ModelEnderman : ModelBiped
		{
			public bool isCarrying;
			public bool isAttacking;

			public ModelEnderman() :
				base(0.0F, -14F)
			{
				isCarrying = false;
				isAttacking = false;
				float f = -14F;
				float f1 = 0.0F;
				boxList.Remove(bipedHeadwear);
				bipedHeadwear = new ModelRenderer(this, 0, 16, VisiblePartFlags.HelmetFlag, true, false);
				bipedHeadwear.addBox("Jaw", -4F, -8F, -4F, 8, 8, 8, f1 - 0.5F);
				bipedHeadwear.setRotationPoint(0.0F, 0.0F + f, 0.0F);
				boxList.Remove(bipedBody);
				bipedBody = new ModelRenderer(this, 32, 16, VisiblePartFlags.ChestFlag, false, false);
				bipedBody.addBox("Body", -4F, 0.0F, -2F, 8, 12, 4, f1);
				bipedBody.setRotationPoint(0.0F, 0.0F + f, 0.0F);
				boxList.Remove(bipedRightArm);
				bipedRightArm = new ModelRenderer(this, 56, 0, VisiblePartFlags.RightArmFlag, false, true);
				bipedRightArm.addBox("Right Arm", -1F, -2F, -1F, 2, 30, 2, f1);
				bipedRightArm.setRotationPoint(-5F, 2.0F + f, 0.0F);
				boxList.Remove(bipedLeftArm);
				bipedLeftArm = new ModelRenderer(this, 56, 0, VisiblePartFlags.LeftArmFlag, false, true);
				bipedLeftArm.mirror = true;
				bipedLeftArm.addBox("Left Arm", -1F, -2F, -1F, 2, 30, 2, f1);
				bipedLeftArm.setRotationPoint(5F, 2.0F + f, 0.0F);
				boxList.Remove(bipedRightLeg);
				bipedRightLeg = new ModelRenderer(this, 56, 0, VisiblePartFlags.RightLegFlag, false, true);
				bipedRightLeg.addBox("Right Leg", -1F, 0.0F, -1F, 2, 30, 2, f1);
				bipedRightLeg.setRotationPoint(-2F, 12F + f, 0.0F);
				boxList.Remove(bipedLeftLeg);
				bipedLeftLeg = new ModelRenderer(this, 56, 0, VisiblePartFlags.LeftLegFlag, false, true);
				bipedLeftLeg.mirror = true;
				bipedLeftLeg.addBox("Left Leg", -1F, 0.0F, -1F, 2, 30, 2, f1);
				bipedLeftLeg.setRotationPoint(2.0F, 12F + f, 0.0F);
			}
		}

		public class ModelWolf : ModelBase
		{
			public ModelRenderer wolfHeadMain;
			public ModelRenderer wolfBody;
			public ModelRenderer wolfLeg1;
			public ModelRenderer wolfLeg2;
			public ModelRenderer wolfLeg3;
			public ModelRenderer wolfLeg4;
			ModelRenderer wolfTail;
			ModelRenderer wolfMane;

			public ModelWolf()
			{
				float f = 0.0F;
				float f1 = 13.5F;
				wolfHeadMain = new ModelRenderer(this, 0, 0, VisiblePartFlags.HeadFlag, false, false);
				wolfHeadMain.addBox("Head", -3F, -3F, -2F, 6, 6, 4, f);
				wolfHeadMain.setRotationPoint(-1F, f1, -7F);
				wolfBody = new ModelRenderer(this, 18, 14, VisiblePartFlags.ChestFlag, false, false);
				wolfBody.addBox("Body", -4F, -2F, -3F, 6, 9, 6, f);
				wolfBody.setRotationPoint(0.0F, 14F, 2.0F);
				wolfMane = new ModelRenderer(this, 21, 0, VisiblePartFlags.HelmetFlag, false, false);
				wolfMane.addBox("Mane", -4F, -3F, -3F, 8, 6, 7, f);
				wolfMane.setRotationPoint(-1F, 14F, 2.0F);
				wolfLeg1 = new ModelRenderer(this, 0, 18, VisiblePartFlags.LeftLegFlag, false, true);
				wolfLeg1.addBox("Left Leg", -1F, 0.0F, -1F, 2, 8, 2, f);
				wolfLeg1.setRotationPoint(-2.5F, 16F, 7F);
				wolfLeg2 = new ModelRenderer(this, 0, 18, VisiblePartFlags.RightLegFlag, false, true);
				wolfLeg2.addBox("Right Leg", -1F, 0.0F, -1F, 2, 8, 2, f);
				wolfLeg2.setRotationPoint(0.5F, 16F, 7F);
				wolfLeg3 = new ModelRenderer(this, 0, 18, VisiblePartFlags.LeftArmFlag, false, true);
				wolfLeg3.addBox("Left Arm", -1F, 0.0F, -1F, 2, 8, 2, f);
				wolfLeg3.setRotationPoint(-2.5F, 16F, -4F);
				wolfLeg4 = new ModelRenderer(this, 0, 18, VisiblePartFlags.RightArmFlag, false, true);
				wolfLeg4.addBox("Right Arm", -1F, 0.0F, -1F, 2, 8, 2, f);
				wolfLeg4.setRotationPoint(0.5F, 16F, -4F);
				wolfTail = new ModelRenderer(this, 9, 18, VisiblePartFlags.ChestFlag, false, false);
				wolfTail.addBox("Chest", -1F, 0.0F, -1F, 2, 8, 2, f);
				wolfTail.setRotationPoint(-1F, 12F, 8F);
				wolfHeadMain.setTextureOffset(16, 14).addBox("Ear", -3F, -5F, 0.0F, 2, 2, 1, f);
				wolfHeadMain.setTextureOffset(16, 14).addBox("Ear", 1.0F, -5F, 0.0F, 2, 2, 1, f);
				wolfHeadMain.setTextureOffset(0, 10).addBox("Nose", -1.5F, 0.0F, -5F, 3, 3, 4, f);

				setLivingAnimations(0, 0, 0);
			}

			public void setLivingAnimations(float f, float f1, float f2)
			{
				wolfBody.setRotationPoint(0.0F, 14F, 2.0F);
				wolfBody.rotateAngleX = 1.570796F;
				wolfMane.setRotationPoint(-1F, 14F, -3F);
				wolfMane.rotateAngleX = wolfBody.rotateAngleX;
				wolfTail.setRotationPoint(-1F, 12F, 8F);
				wolfTail.rotateAngleX = wolfBody.rotateAngleX;
				wolfLeg1.setRotationPoint(-2.5F, 16F, 7F);
				wolfLeg2.setRotationPoint(0.5F, 16F, 7F);
				wolfLeg3.setRotationPoint(-2.5F, 16F, -4F);
				wolfLeg4.setRotationPoint(0.5F, 16F, -4F);
				wolfLeg1.rotateAngleX = (float)Math.Cos(f * 0.6662F) * 1.4F * f1;
				wolfLeg2.rotateAngleX = (float)Math.Cos(f * 0.6662F + 3.141593F) * 1.4F * f1;
				wolfLeg3.rotateAngleX = (float)Math.Cos(f * 0.6662F + 3.141593F) * 1.4F * f1;
				wolfLeg4.rotateAngleX = (float)Math.Cos(f * 0.6662F) * 1.4F * f1;
			}
		}

		public class ModelGhast : ModelBase
		{
			ModelRenderer body;
			ModelRenderer[] tentacles;

			public ModelGhast()
			{
				tentacles = new ModelRenderer[9];
				int byte0 = -16;
				body = new ModelRenderer(this, 0, 0, VisiblePartFlags.ChestFlag, false, false);
				body.addBox("Body", -8F, -8F, -8F, 16, 16, 16);
				body.rotationPointY += 24 + byte0;
				Random random = new Random(1660);
				for (int i = 0; i < tentacles.Length; i++)
				{
					tentacles[i] = new ModelRenderer(this, 0, 0, VisiblePartFlags.LeftLegFlag, false, false);
					float f = (((((float)(i % 3) - (float)((i / 3) % 2) * 0.5F) + 0.25F) / 2.0F) * 2.0F - 1.0F) * 5F;
					float f1 = (((float)(i / 3) / 2.0F) * 2.0F - 1.0F) * 5F;
					int j = random.Next(7) + 8;
					tentacles[i].addBox("Tentacle", -1F, 0.0F, -1F, 2, j, 2);
					tentacles[i].rotationPointX = f;
					tentacles[i].rotationPointZ = f1;
					tentacles[i].rotationPointY = 31 + byte0;
				}

				setRotationAngles(0, 0, 123456, 0, 0, 0);
			}

			public void setRotationAngles(float f, float f1, float f2, float f3, float f4, float f5)
			{
				for (int i = 0; i < tentacles.Length; i++)
				{
					tentacles[i].rotateAngleX = 0.2F * (float)Math.Sin(f2 * 0.3F + (float)i) + 0.4F;
				}
			}
		}

		public class ModelSpider : ModelBase
		{
			public ModelRenderer spiderHead;
			public ModelRenderer spiderNeck;
			public ModelRenderer spiderBody;
			public ModelRenderer spiderLeg1;
			public ModelRenderer spiderLeg2;
			public ModelRenderer spiderLeg3;
			public ModelRenderer spiderLeg4;
			public ModelRenderer spiderLeg5;
			public ModelRenderer spiderLeg6;
			public ModelRenderer spiderLeg7;
			public ModelRenderer spiderLeg8;

			public ModelSpider()
			{
				float f = 0.0F;
				int i = 15;
				spiderHead = new ModelRenderer(this, 32, 4, VisiblePartFlags.HeadFlag, false, false);
				spiderHead.addBox("Head", -4F, -4F, -8F, 8, 8, 8, f);
				spiderHead.setRotationPoint(0.0F, 0 + i, -3F);
				spiderNeck = new ModelRenderer(this, 0, 0, VisiblePartFlags.HelmetFlag, false, false);
				spiderNeck.addBox("Neck", -3F, -3F, -3F, 6, 6, 6, f);
				spiderNeck.setRotationPoint(0.0F, i, 0.0F);
				spiderBody = new ModelRenderer(this, 0, 12, VisiblePartFlags.ChestFlag, false, false);
				spiderBody.addBox("Body", -5F, -4F, -6F, 10, 8, 12, f);
				spiderBody.setRotationPoint(0.0F, 0 + i, 9F);
				spiderLeg1 = new ModelRenderer(this, 18, 0, VisiblePartFlags.LeftArmFlag, false, false);
				spiderLeg1.addBox("Leg", -15F, -1F, -1F, 16, 2, 2, f);
				spiderLeg1.setRotationPoint(-4F, 0 + i, 2.0F);
				spiderLeg2 = new ModelRenderer(this, 18, 0, VisiblePartFlags.LeftArmFlag, false, true);
				spiderLeg2.addBox("Leg", -1F, -1F, -1F, 16, 2, 2, f);
				spiderLeg2.setRotationPoint(4F, 0 + i, 2.0F);
				spiderLeg3 = new ModelRenderer(this, 18, 0, VisiblePartFlags.LeftArmFlag, false, true);
				spiderLeg3.addBox("Leg", -15F, -1F, -1F, 16, 2, 2, f);
				spiderLeg3.setRotationPoint(-4F, 0 + i, 1.0F);
				spiderLeg4 = new ModelRenderer(this, 18, 0, VisiblePartFlags.LeftArmFlag, false, true);
				spiderLeg4.addBox("Leg", -1F, -1F, -1F, 16, 2, 2, f);
				spiderLeg4.setRotationPoint(4F, 0 + i, 1.0F);
				spiderLeg5 = new ModelRenderer(this, 18, 0, VisiblePartFlags.LeftArmFlag, false, true);
				spiderLeg5.addBox("Leg", -15F, -1F, -1F, 16, 2, 2, f);
				spiderLeg5.setRotationPoint(-4F, 0 + i, 0.0F);
				spiderLeg6 = new ModelRenderer(this, 18, 0, VisiblePartFlags.LeftArmFlag, false, true);
				spiderLeg6.addBox("Leg", -1F, -1F, -1F, 16, 2, 2, f);
				spiderLeg6.setRotationPoint(4F, 0 + i, 0.0F);
				spiderLeg7 = new ModelRenderer(this, 18, 0, VisiblePartFlags.LeftArmFlag, false, true);
				spiderLeg7.addBox("Leg", -15F, -1F, -1F, 16, 2, 2, f);
				spiderLeg7.setRotationPoint(-4F, 0 + i, -1F);
				spiderLeg8 = new ModelRenderer(this, 18, 0, VisiblePartFlags.LeftArmFlag, false, true);
				spiderLeg8.addBox("Leg", -1F, -1F, -1F, 16, 2, 2, f);
				spiderLeg8.setRotationPoint(4F, 0 + i, -1F);

				setRotationAngles(0, 0, 0, 0, 0, 0);
			}

			public void setRotationAngles(float f, float f1, float f2, float f3, float f4, float f5)
			{
				spiderHead.rotateAngleY = f3 / 57.29578F;
				spiderHead.rotateAngleX = f4 / 57.29578F;
				float f6 = 0.7853982F;
				spiderLeg1.rotateAngleZ = -f6;
				spiderLeg2.rotateAngleZ = f6;
				spiderLeg3.rotateAngleZ = -f6 * 0.74F;
				spiderLeg4.rotateAngleZ = f6 * 0.74F;
				spiderLeg5.rotateAngleZ = -f6 * 0.74F;
				spiderLeg6.rotateAngleZ = f6 * 0.74F;
				spiderLeg7.rotateAngleZ = -f6;
				spiderLeg8.rotateAngleZ = f6;
				float f7 = -0F;
				float f8 = 0.3926991F;
				spiderLeg1.rotateAngleY = f8 * 2.0F + f7;
				spiderLeg2.rotateAngleY = -f8 * 2.0F - f7;
				spiderLeg3.rotateAngleY = f8 * 1.0F + f7;
				spiderLeg4.rotateAngleY = -f8 * 1.0F - f7;
				spiderLeg5.rotateAngleY = -f8 * 1.0F + f7;
				spiderLeg6.rotateAngleY = f8 * 1.0F - f7;
				spiderLeg7.rotateAngleY = -f8 * 2.0F + f7;
				spiderLeg8.rotateAngleY = f8 * 2.0F - f7;
				float f9 = (float)-(Math.Cos(f * 0.6662F * 2.0F + 0.0F) * 0.4F) * f1;
				float f10 =(float)(-Math.Cos(f * 0.6662F * 2.0F + 3.141593F) * 0.4F) * f1;
				float f11 =(float)(-Math.Cos(f * 0.6662F * 2.0F + 1.570796F) * 0.4F) * f1;
				float f12 =(float)(-Math.Cos(f * 0.6662F * 2.0F + 4.712389F) * 0.4F) * f1;
				float f13 =(float)Math.Abs(Math.Sin(f * 0.6662F + 0.0F) * 0.4F) * f1;
				float f14 =(float)Math.Abs(Math.Sin(f * 0.6662F + 3.141593F) * 0.4F) * f1;
				float f15 =(float)Math.Abs(Math.Sin(f * 0.6662F + 1.570796F) * 0.4F) * f1;
				float f16 =(float)Math.Abs(Math.Sin(f * 0.6662F + 4.712389F) * 0.4F) * f1;
				spiderLeg1.rotateAngleY += f9;
				spiderLeg2.rotateAngleY += -f9;
				spiderLeg3.rotateAngleY += f10;
				spiderLeg4.rotateAngleY += -f10;
				spiderLeg5.rotateAngleY += f11;
				spiderLeg6.rotateAngleY += -f11;
				spiderLeg7.rotateAngleY += f12;
				spiderLeg8.rotateAngleY += -f12;
				spiderLeg1.rotateAngleZ += f13;
				spiderLeg2.rotateAngleZ += -f13;
				spiderLeg3.rotateAngleZ += f14;
				spiderLeg4.rotateAngleZ += -f14;
				spiderLeg5.rotateAngleZ += f15;
				spiderLeg6.rotateAngleZ += -f15;
				spiderLeg7.rotateAngleZ += f16;
				spiderLeg8.rotateAngleZ += -f16;
			}
		}

		public class ModelSheep1 : ModelQuadruped
		{
			public ModelSheep1() :
				base(12, 0.0F)
			{
				boxList.Remove(head);
				head = new ModelRenderer(this, 0, 0, VisiblePartFlags.HeadFlag, false, false);
				head.addBox("Head", -3F, -4F, -4F, 6, 6, 6, 0.6F);
				head.setRotationPoint(0.0F, 6F, -8F);
				boxList.Remove(body);
				body = new ModelRenderer(this, 28, 8, VisiblePartFlags.ChestFlag, false, false);
				body.addBox("Body", -4F, -10F, -7F, 8, 16, 6, 1.75F);
				body.setRotationPoint(0.0F, 5F, 2.0F);
				body.rotateAngleX = 1.570796F;
				float f = 0.5F;
				boxList.Remove(leg1);
				leg1 = new ModelRenderer(this, 0, 16, VisiblePartFlags.LeftLegFlag, false, true);
				leg1.addBox("Left Leg", -2F, 0.0F, -2F, 4, 6, 4, f);
				leg1.setRotationPoint(-3F, 12F, 7F);
				boxList.Remove(leg2);
				leg2 = new ModelRenderer(this, 0, 16, VisiblePartFlags.RightLegFlag, false, true);
				leg2.addBox("Right Leg", -2F, 0.0F, -2F, 4, 6, 4, f);
				leg2.setRotationPoint(3F, 12F, 7F);
				boxList.Remove(leg3);
				leg3 = new ModelRenderer(this, 0, 16, VisiblePartFlags.LeftArmFlag, false, true);
				leg3.addBox("Left Arm", -2F, 0.0F, -2F, 4, 6, 4, f);
				leg3.setRotationPoint(-3F, 12F, -5F);
				boxList.Remove(leg4);
				leg4 = new ModelRenderer(this, 0, 16, VisiblePartFlags.RightArmFlag, false, true);
				leg4.addBox("Right Arm", -2F, 0.0F, -2F, 4, 6, 4, f);
				leg4.setRotationPoint(3F, 12F, -5F);
			}
		}

		public class ModelSheep2 : ModelQuadruped
		{
			public ModelSheep2() :
				base(12, 0.0F)
			{
				boxList.Remove(head);
				head = new ModelRenderer(this, 0, 0, VisiblePartFlags.HeadFlag, false, false);
				head.addBox("Head", -3F, -4F, -6F, 6, 6, 8, 0.0F);
				head.setRotationPoint(0.0F, 6F, -8F);
				boxList.Remove(body);
				body = new ModelRenderer(this, 28, 8, VisiblePartFlags.ChestFlag, false, false);
				body.addBox("Body", -4F, -10F, -7F, 8, 16, 6, 0.0F);
				body.setRotationPoint(0.0F, 5F, 2.0F);
				body.rotateAngleX = 1.570796F;
			}
		}

		public class ModelChest : ModelBase
		{
			public ModelRenderer chestLid;
			public ModelRenderer chestBelow;
			public ModelRenderer chestKnob;

			public ModelChest()
			{
				chestLid = (new ModelRenderer(this, 0, 0, VisiblePartFlags.HeadFlag, false, true)).setTextureSize(64, 64);
				chestLid.addBox("Lid", 0.0F, -5F, -14F, 14, 5, 14, 0.0F);
				chestLid.rotationPointX = 1.0F;
				chestLid.rotationPointY = 7F;
				chestLid.rotationPointZ = 15F;
				chestKnob = (new ModelRenderer(this, 0, 0, VisiblePartFlags.HelmetFlag, false, true)).setTextureSize(64, 64);
				chestKnob.addBox("Knob", -1F, -2F, -15F, 2, 4, 1, 0.0F);
				chestKnob.rotationPointX = 8F;
				chestKnob.rotationPointY = 7F;
				chestKnob.rotationPointZ = 15F;
				chestBelow = (new ModelRenderer(this, 0, 19, VisiblePartFlags.ChestFlag, false, false)).setTextureSize(64, 64);
				chestBelow.addBox("Base", 0.0F, 0.0F, 0.0F, 14, 10, 14, 0.0F);
				chestBelow.rotationPointX = 1.0F;
				chestBelow.rotationPointY = 6F;
				chestBelow.rotationPointZ = 1.0F;
			}
		}

		public class ModelLargeChest : ModelBase
		{
			public ModelRenderer chestLid;
			public ModelRenderer chestBelow;
			public ModelRenderer chestKnob;

			public ModelLargeChest()
			{
				chestLid = (new ModelRenderer(this, 0, 0, VisiblePartFlags.HeadFlag, false, true)).setTextureSize(128, 64);
				chestLid.addBox("Lid", 0.0F, -5F, -14F, 30, 5, 14, 0.0F);
				chestLid.rotationPointX = 1.0F;
				chestLid.rotationPointY = 7F;
				chestLid.rotationPointZ = 15F;
				chestKnob = (new ModelRenderer(this, 0, 0, VisiblePartFlags.HelmetFlag, false, true)).setTextureSize(128, 64);
				chestKnob.addBox("Knob", -1F, -2F, -15F, 2, 4, 1, 0.0F);
				chestKnob.rotationPointX = 16F;
				chestKnob.rotationPointY = 7F;
				chestKnob.rotationPointZ = 15F;
				chestBelow = (new ModelRenderer(this, 0, 19, VisiblePartFlags.ChestFlag, false, false)).setTextureSize(128, 64);
				chestBelow.addBox("Base", 0.0F, 0.0F, 0.0F, 30, 10, 14, 0.0F);
				chestBelow.rotationPointX = 1.0F;
				chestBelow.rotationPointY = 6F;
				chestBelow.rotationPointZ = 1.0F;
			}
		}

		public class ModelBoat : ModelBase
		{
			public ModelRenderer[] boatSides;

			public ModelBoat()
			{
				boatSides = new ModelRenderer[5];
				boatSides[0] = new ModelRenderer(this, 0, 8, VisiblePartFlags.HeadFlag, false, false);
				boatSides[1] = new ModelRenderer(this, 0, 0, VisiblePartFlags.HelmetFlag, false, false);
				boatSides[2] = new ModelRenderer(this, 0, 0, VisiblePartFlags.ChestFlag, false, false);
				boatSides[3] = new ModelRenderer(this, 0, 0, VisiblePartFlags.LeftArmFlag, false, false);
				boatSides[4] = new ModelRenderer(this, 0, 0, VisiblePartFlags.RightArmFlag, false, false);
				byte byte0 = 24;
				byte byte1 = 6;
				byte byte2 = 20;
				byte byte3 = 4;
				boatSides[0].addBox("Base", -byte0 / 2, -byte2 / 2 + 2, -3F, byte0, byte2 - 4, 4, 0.0F);
				boatSides[0].setRotationPoint(0.0F, 0 + byte3, 0.0F);
				boatSides[1].addBox("Side", -byte0 / 2 + 2, -byte1 - 1, -1F, byte0 - 4, byte1, 2, 0.0F);
				boatSides[1].setRotationPoint(-byte0 / 2 + 1, 0 + byte3, 0.0F);
				boatSides[2].addBox("Side", -byte0 / 2 + 2, -byte1 - 1, -1F, byte0 - 4, byte1, 2, 0.0F);
				boatSides[2].setRotationPoint(byte0 / 2 - 1, 0 + byte3, 0.0F);
				boatSides[3].addBox("Side", -byte0 / 2 + 2, -byte1 - 1, -1F, byte0 - 4, byte1, 2, 0.0F);
				boatSides[3].setRotationPoint(0.0F, 0 + byte3, -byte2 / 2 + 1);
				boatSides[4].addBox("Side", -byte0 / 2 + 2, -byte1 - 1, -1F, byte0 - 4, byte1, 2, 0.0F);
				boatSides[4].setRotationPoint(0.0F, 0 + byte3, byte2 / 2 - 1);
				boatSides[0].rotateAngleX = 1.570796F;
				boatSides[1].rotateAngleY = 4.712389F;
				boatSides[2].rotateAngleY = 1.570796F;
				boatSides[3].rotateAngleY = 3.141593F;
			}
		}

		public class SignModel : ModelBase
		{
			public ModelRenderer signBoard;
			public ModelRenderer signStick;

			public SignModel()
			{
				signBoard = new ModelRenderer(this, 0, 0, VisiblePartFlags.HelmetFlag, false, false);
				signBoard.addBox("Board", -12F, -14F, -1F, 24, 12, 2, 0.0F);
				signStick = new ModelRenderer(this, 0, 14, VisiblePartFlags.ChestFlag, false, false);
				signStick.addBox("Stick", -1F, -2F, -1F, 2, 14, 2, 0.0F);
			}
		}

		public class ModelBook : ModelBase
		{
			public ModelRenderer field_40330_a;
			public ModelRenderer field_40328_b;
			public ModelRenderer field_40329_c;
			public ModelRenderer field_40326_d;
			public ModelRenderer field_40327_e;
			public ModelRenderer field_40324_f;
			public ModelRenderer field_40325_g;

			public ModelBook()
			{
				field_40330_a = (new ModelRenderer(this, VisiblePartFlags.HeadFlag, false, false)).setTextureOffset(0, 0).addBox("Left Cover", -6F, -5F, 0.0F, 6, 10, 0);
				field_40328_b = (new ModelRenderer(this, VisiblePartFlags.ChestFlag, false, false)).setTextureOffset(16, 0).addBox("Right Cover", 0.0F, -5F, 0.0F, 6, 10, 0);
				field_40325_g = (new ModelRenderer(this, VisiblePartFlags.HelmetFlag, false, false)).setTextureOffset(12, 0).addBox("Spine", -1F, -5F, 0.0F, 2, 10, 0);
				field_40329_c = (new ModelRenderer(this, VisiblePartFlags.LeftArmFlag, false, false)).setTextureOffset(0, 10).addBox("Page", 0.0F, -4F, -0.99F, 5, 8, 1);
				field_40326_d = (new ModelRenderer(this, VisiblePartFlags.LeftLegFlag, false, false)).setTextureOffset(12, 10).addBox("Page", 0.0F, -4F, -0.01F, 5, 8, 1);
				field_40327_e = (new ModelRenderer(this, VisiblePartFlags.RightArmFlag, false, false)).setTextureOffset(24, 10).addBox("Page", 0.0F, -4F, 0.0F, 5, 8, 0);
				field_40324_f = (new ModelRenderer(this, VisiblePartFlags.RightLegFlag, false, false)).setTextureOffset(24, 10).addBox("Page", 0.0F, -4F, 0.0F, 5, 8, 0);
				field_40330_a.setRotationPoint(0.0F, 0.0F, -1F);
				field_40328_b.setRotationPoint(0.0F, 0.0F, 1.0F);
				field_40325_g.rotateAngleY = 1.570796F;
			}

			public void setRotationAngles(float f, float f1, float f2, float f3, float f4, float f5)
			{
				float f6 = (float)(Math.Sin(f * 0.02F) * 0.1F + 1.25F) * f3;
				field_40330_a.rotateAngleY = 3.141593F + f6;
				field_40328_b.rotateAngleY = -f6;
				field_40329_c.rotateAngleY = f6;
				field_40326_d.rotateAngleY = -f6;
				field_40327_e.rotateAngleY = f6 - f6 * 2.0F * f1;
				field_40324_f.rotateAngleY = f6 - f6 * 2.0F * f2;
				field_40329_c.rotationPointX = (float)Math.Sin(f6);
				field_40326_d.rotationPointX = (float)Math.Sin(f6);
				field_40327_e.rotationPointX = (float)Math.Sin(f6);
				field_40324_f.rotationPointX = (float)Math.Sin(f6);
			}
		}

		public class ModelMinecart : ModelBase
		{
			public ModelRenderer[] sideModels;

			public ModelMinecart()
			{
				sideModels = new ModelRenderer[7];
				sideModels[0] = new ModelRenderer(this, 0, 10, VisiblePartFlags.HeadFlag, false, false);
				sideModels[1] = new ModelRenderer(this, 0, 0, VisiblePartFlags.LeftArmFlag, false, false);
				sideModels[2] = new ModelRenderer(this, 0, 0, VisiblePartFlags.LeftLegFlag, false, false);
				sideModels[3] = new ModelRenderer(this, 0, 0, VisiblePartFlags.RightArmFlag, false, false);
				sideModels[4] = new ModelRenderer(this, 0, 0, VisiblePartFlags.RightLegFlag, false, false);
				sideModels[5] = new ModelRenderer(this, 44, 10, VisiblePartFlags.ChestFlag, false, false);
				byte byte0 = 20;
				byte byte1 = 8;
				byte byte2 = 16;
				byte byte3 = 4;
				sideModels[0].addBox("Base", -byte0 / 2, -byte2 / 2, -1F, byte0, byte2, 2, 0.0F);
				sideModels[0].setRotationPoint(0.0F, 0 + byte3, 0.0F);
				sideModels[5].addBox("Leather", -byte0 / 2 + 1, -byte2 / 2 + 1, -1F, byte0 - 2, byte2 - 2, 1, 0.0F);
				sideModels[5].setRotationPoint(0.0F, 0 + byte3, 0.0F);
				sideModels[1].addBox("Side", -byte0 / 2 + 2, -byte1 - 1, -1F, byte0 - 4, byte1, 2, 0.0F);
				sideModels[1].setRotationPoint(-byte0 / 2 + 1, 0 + byte3, 0.0F);
				sideModels[2].addBox("Side", -byte0 / 2 + 2, -byte1 - 1, -1F, byte0 - 4, byte1, 2, 0.0F);
				sideModels[2].setRotationPoint(byte0 / 2 - 1, 0 + byte3, 0.0F);
				sideModels[3].addBox("Side", -byte0 / 2 + 2, -byte1 - 1, -1F, byte0 - 4, byte1, 2, 0.0F);
				sideModels[3].setRotationPoint(0.0F, 0 + byte3, -byte2 / 2 + 1);
				sideModels[4].addBox("Side", -byte0 / 2 + 2, -byte1 - 1, -1F, byte0 - 4, byte1, 2, 0.0F);
				sideModels[4].setRotationPoint(0.0F, 0 + byte3, byte2 / 2 - 1);
				sideModels[0].rotateAngleX = 1.570796F;
				sideModels[1].rotateAngleY = 4.712389F;
				sideModels[2].rotateAngleY = 1.570796F;
				sideModels[3].rotateAngleY = 3.141593F;
				sideModels[5].rotateAngleX = -1.570796F;
			}
		}

		public class ModelEnderCrystal : ModelBase
		{
			private ModelRenderer field_41057_g;
			private ModelRenderer field_41058_h;
			private ModelRenderer field_41059_i;

			public ModelEnderCrystal()
			{
				field_41058_h = new ModelRenderer(this, "glass", VisiblePartFlags.HeadFlag, true, false);
				field_41058_h.setTextureOffset(0, 0).addBox("Glass", -4F, -4F, -4F, 8, 8, 8);
				field_41057_g = new ModelRenderer(this, "cube", VisiblePartFlags.HelmetFlag, false, false);
				field_41057_g.setTextureOffset(32, 0).addBox("Cube", -4F, -4F, -4F, 8, 8, 8);
				field_41059_i = new ModelRenderer(this, "base", VisiblePartFlags.ChestFlag, false, false);
				field_41059_i.setTextureOffset(0, 16).addBox("Base", -6F, 0.0F, -6F, 12, 4, 12);
			}
		}

		public class ModelSnowMan : ModelBase
		{
			public ModelRenderer field_40306_a;
			public ModelRenderer field_40304_b;
			public ModelRenderer field_40305_c;
			public ModelRenderer field_40302_d;
			public ModelRenderer field_40303_e;

			public ModelSnowMan()
			{
				float f = 4F;
				float f1 = 0.0F;
				field_40305_c = (new ModelRenderer(this, 0, 0, VisiblePartFlags.HeadFlag, false, false)).setTextureSize(64, 64);
				field_40305_c.addBox("Head", -4F, -8F, -4F, 8, 8, 8, f1 - 0.5F);
				field_40305_c.setRotationPoint(0.0F, 0.0F + f, 0.0F);
				field_40302_d = (new ModelRenderer(this, 32, 0, VisiblePartFlags.HelmetFlag, false, false)).setTextureSize(64, 64);
				field_40302_d.addBox("Top", -1F, 0.0F, -1F, 12, 2, 2, f1 - 0.5F);
				field_40302_d.setRotationPoint(0.0F, (0.0F + f + 9F) - 7F, 0.0F);
				field_40303_e = (new ModelRenderer(this, 32, 0, VisiblePartFlags.ChestFlag, false, false)).setTextureSize(64, 64);
				field_40303_e.addBox("Bottom", -1F, 0.0F, -1F, 12, 2, 2, f1 - 0.5F);
				field_40303_e.setRotationPoint(0.0F, (0.0F + f + 9F) - 7F, 0.0F);
				field_40306_a = (new ModelRenderer(this, 0, 16, VisiblePartFlags.LeftArmFlag, false, false)).setTextureSize(64, 64);
				field_40306_a.addBox("Left Arm", -5F, -10F, -5F, 10, 10, 10, f1 - 0.5F);
				field_40306_a.setRotationPoint(0.0F, 0.0F + f + 9F, 0.0F);
				field_40304_b = (new ModelRenderer(this, 0, 36, VisiblePartFlags.RightArmFlag, false, false)).setTextureSize(64, 64);
				field_40304_b.addBox("Right Arm", -6F, -12F, -6F, 12, 12, 12, f1 - 0.5F);
				field_40304_b.setRotationPoint(0.0F, 0.0F + f + 20F, 0.0F);

				setRotationAngles(0, 0, 0, (float)Math.PI / 2, 0, 0);
			}

			public void setRotationAngles(float f, float f1, float f2, float f3, float f4, float f5)
			{
				field_40305_c.rotateAngleY = f3 / 57.29578F;
				field_40305_c.rotateAngleX = f4 / 57.29578F;
				field_40306_a.rotateAngleY = (f3 / 57.29578F) * 0.25F;
				float f6 = (float)Math.Sin(field_40306_a.rotateAngleY);
				float f7 = (float)Math.Cos(field_40306_a.rotateAngleY);
				field_40302_d.rotateAngleZ = 1.0F;
				field_40303_e.rotateAngleZ = -1F;
				field_40302_d.rotateAngleY = 0.0F + field_40306_a.rotateAngleY;
				field_40303_e.rotateAngleY = 3.141593F + field_40306_a.rotateAngleY;
				field_40302_d.rotationPointX = f7 * 5F;
				field_40302_d.rotationPointZ = -f6 * 5F;
				field_40303_e.rotationPointX = -f7 * 5F;
				field_40303_e.rotationPointZ = f6 * 5F;
			}
		}

		public class ModelZombie : ModelBiped
		{
			public ModelZombie()
			{
				//boxList.Remove(bipedHeadwear);
				setRotationAngles(0, 0, 0, 0, 0, 0);
			}

			public void setRotationAngles(float f, float f1, float f2, float f3, float f4, float f5)
			{
				float f6 = (float)Math.Sin(onGround * 3.141593F);
				float f7 = (float)Math.Sin((1.0F - (1.0F - onGround) * (1.0F - onGround)) * 3.141593F);
				bipedRightArm.rotateAngleZ = 0.0F;
				bipedLeftArm.rotateAngleZ = 0.0F;
				bipedRightArm.rotateAngleY = -(0.1F - f6 * 0.6F);
				bipedLeftArm.rotateAngleY = 0.1F - f6 * 0.6F;
				bipedRightArm.rotateAngleX = -1.570796F;
				bipedLeftArm.rotateAngleX = -1.570796F;
				bipedRightArm.rotateAngleX -= f6 * 1.2F - f7 * 0.4F;
				bipedLeftArm.rotateAngleX -= f6 * 1.2F - f7 * 0.4F;
				bipedRightArm.rotateAngleZ += (float)Math.Cos(f2 * 0.09F) * 0.05F + 0.05F;
				bipedLeftArm.rotateAngleZ -= (float)Math.Cos(f2 * 0.09F) * 0.05F + 0.05F;
				bipedRightArm.rotateAngleX += (float)Math.Sin(f2 * 0.067F) * 0.05F;
				bipedLeftArm.rotateAngleX -= (float)Math.Sin(f2 * 0.067F) * 0.05F;
			}
		}

		public class ModelSkeleton : ModelZombie
		{
			public ModelSkeleton()
			{
				float f = 0.0F;
				boxList.Remove(bipedRightArm);
				bipedRightArm = new ModelRenderer(this, 40, 16, VisiblePartFlags.RightArmFlag, false, false);
				bipedRightArm.addBox("Right Arm", -1F, -2F, -1F, 2, 12, 2, f);
				bipedRightArm.setRotationPoint(-5F, 2.0F, 0.0F);
				boxList.Remove(bipedLeftArm);
				bipedLeftArm = new ModelRenderer(this, 40, 16, VisiblePartFlags.LeftArmFlag, false, false);
				bipedLeftArm.mirror = true;
				bipedLeftArm.addBox("Right Arm", -1F, -2F, -1F, 2, 12, 2, f);
				bipedLeftArm.setRotationPoint(5F, 2.0F, 0.0F);
				boxList.Remove(bipedRightLeg);
				bipedRightLeg = new ModelRenderer(this, 0, 16, VisiblePartFlags.RightLegFlag, false, false);
				bipedRightLeg.addBox("Right Leg", -1F, 0.0F, -1F, 2, 12, 2, f);
				bipedRightLeg.setRotationPoint(-2F, 12F, 0.0F);
				boxList.Remove(bipedLeftLeg);
				bipedLeftLeg = new ModelRenderer(this, 0, 16, VisiblePartFlags.LeftLegFlag, false, false);
				bipedLeftLeg.mirror = true;
				bipedLeftLeg.addBox("Left Leg", -1F, 0.0F, -1F, 2, 12, 2, f);
				bipedLeftLeg.setRotationPoint(2.0F, 12F, 0.0F);

				setRotationAngles(0, 0, 0, 0, 0, 0);
			}

			public new void setRotationAngles(float f, float f1, float f2, float f3, float f4, float f5)
			{
				aimedBow = true;
				base.setRotationAngles(f, f1, f2, f3, f4, f5);
			}
		}

		public class pm_Pony : ModelBase
		{
			private bool rainboom;
			private float WingRotateAngleX;
			private float WingRotateAngleY;
			private float WingRotateAngleZ;
			private float TailRotateAngleY;
			public ModelRenderer head;
			public ModelRenderer[] headpiece;
			public ModelRenderer helmet;
			public ModelRenderer Body;
			public PlaneRenderer[] Bodypiece;
			public ModelRenderer rightarm;
			public ModelRenderer LeftArm;
			public ModelRenderer RightLeg;
			public ModelRenderer LeftLeg;
			public ModelRenderer SteveArm;
			public ModelRenderer unicornarm;
			public PlaneRenderer[] Tail;
			public ModelRenderer[] LeftWing;
			public ModelRenderer[] RightWing;
			public ModelRenderer[] LeftWingExt;
			public ModelRenderer[] RightWingExt;
			public float strech;

			public bool isFlying, isUnicorn, isPegasus;

			public pm_Pony()
			{
			}

			public pm_Pony init(bool unicorn, bool pegasis)
			{
				isUnicorn = unicorn;
				isPegasus = pegasis;
				init(0.0F);
				return this;
			}
			public void init(float yoffset)
			{
				init(yoffset, 0.0F);
			}

			public void init(float yoffset, float init_strech)
			{
				this.strech = init_strech;

				float headR1 = 0.0F;
				float headR2 = 0.0F;
				float headR3 = 0.0F;

				this.head = new ModelRenderer(this, 0, 0, VisiblePartFlags.HeadFlag, false, false);
				this.head.addBox("Head", -4.0F, -4.0F, -6.0F, 8, 8, 8, this.strech);
				this.head.setRotationPoint(headR1, headR2 + yoffset, headR3);

				this.headpiece = new ModelRenderer[3];

				this.headpiece[0] = new ModelRenderer(this, 12, 16, VisiblePartFlags.HelmetFlag, false, false);
				this.headpiece[0].addBox("Ear", -4.0F, -6.0F, -1.0F, 2, 2, 2, this.strech);
				this.headpiece[0].setRotationPoint(headR1, headR2 + yoffset, headR3);

				this.headpiece[1] = new ModelRenderer(this, 12, 16, VisiblePartFlags.HelmetFlag, false, false);
				this.headpiece[1].addBox("Ear", 2.0F, -6.0F, -1.0F, 2, 2, 2, this.strech);
				this.headpiece[1].setRotationPoint(headR1, headR2 + yoffset, headR3);

				this.headpiece[2] = new ModelRenderer(this, 56, 0, VisiblePartFlags.HelmetFlag, true, false);
				this.headpiece[2].addBox("Horn", -0.5F, -10.0F, -4.0F, 1, 4, 1, this.strech);
				this.headpiece[2].setRotationPoint(headR1, headR2 + yoffset, headR3);

				this.helmet = new ModelRenderer(this, 32, 0, VisiblePartFlags.HelmetFlag, true, false);
				this.helmet.addBox("Hair", -4.0F, -4.0F, -6.0F, 8, 8, 8, this.strech + 0.5F);
				this.helmet.setRotationPoint(headR1, headR2 + yoffset, headR3);

				float BodyR1 = 0.0F;
				float BodyR2 = 0.0F;
				float BodyR3 = 0.0F;

				this.Body = new ModelRenderer(this, 16, 16, VisiblePartFlags.ChestFlag, false, false);
				this.Body.addBox("Body|Body", -4.0F, 4.0F, -2.0F, 8, 8, 4, this.strech);
				this.Body.setRotationPoint(BodyR1, BodyR2 + yoffset, BodyR3);

				this.Bodypiece = new PlaneRenderer[13];

				this.Bodypiece[0] = new PlaneRenderer(this, "Body|Body", 24, 0, VisiblePartFlags.RightLegFlag, false, false);
				this.Bodypiece[0].addSidePlane(-4.0F, 4.0F, 2.0F, 0, 8, 8, this.strech);
				this.Bodypiece[0].setRotationPoint(BodyR1, BodyR2 + yoffset, BodyR3);

				this.Bodypiece[1] = new PlaneRenderer(this, "Body|Body", 24, 0, VisiblePartFlags.RightLegFlag, false, false);
				this.Bodypiece[1].addSidePlane(4.0F, 4.0F, 2.0F, 0, 8, 8, this.strech);
				this.Bodypiece[1].setRotationPoint(BodyR1, BodyR2 + yoffset, BodyR3);

				this.Bodypiece[2] = new PlaneRenderer(this, "Body|Body", 24, 0, VisiblePartFlags.ChestFlag, false, false);
				this.Bodypiece[2].addTopPlane(-4.0F, 4.0F, 2.0F, 8, 0, 8, this.strech);
				this.Bodypiece[2].setRotationPoint(headR1, headR2 + yoffset, headR3);

				this.Bodypiece[3] = new PlaneRenderer(this, "Body|Body", 24, 0, VisiblePartFlags.ChestFlag, false, false);
				this.Bodypiece[3].addTopPlane(-4.0F, 12.0F, 2.0F, 8, 0, 8, this.strech);
				this.Bodypiece[3].setRotationPoint(headR1, headR2 + yoffset, headR3);

				this.Bodypiece[4] = new PlaneRenderer(this, "Cutie Mark|Body", 0, 20, VisiblePartFlags.HeadFlag, false, false);
				this.Bodypiece[4].addSidePlane(-4.0F, 4.0F, 10.0F, 0, 8, 4, this.strech);
				this.Bodypiece[4].setRotationPoint(BodyR1, BodyR2 + yoffset, BodyR3);

				this.Bodypiece[5] = new PlaneRenderer(this, "Body|Body", 0, 20, VisiblePartFlags.HeadFlag, false, false);
				this.Bodypiece[5].addSidePlane(4.0F, 4.0F, 10.0F, 0, 8, 4, this.strech);
				this.Bodypiece[5].setRotationPoint(BodyR1, BodyR2 + yoffset, BodyR3);

				this.Bodypiece[6] = new PlaneRenderer(this, "Body|Body", 24, 0, VisiblePartFlags.ChestFlag, false, false);
				this.Bodypiece[6].addTopPlane(-4.0F, 4.0F, 10.0F, 8, 0, 4, this.strech);
				this.Bodypiece[6].setRotationPoint(headR1, headR2 + yoffset, headR3);

				this.Bodypiece[7] = new PlaneRenderer(this, "Body|Body", 24, 0, VisiblePartFlags.ChestFlag, false, false);
				this.Bodypiece[7].addTopPlane(-4.0F, 12.0F, 10.0F, 8, 0, 4, this.strech);
				this.Bodypiece[7].setRotationPoint(headR1, headR2 + yoffset, headR3);

				this.Bodypiece[8] = new PlaneRenderer(this, "Body|Body", 24, 0, VisiblePartFlags.ChestFlag, false, false);
				this.Bodypiece[8].addBackPlane(-4.0F, 4.0F, 14.0F, 8, 8, 0, this.strech);
				this.Bodypiece[8].setRotationPoint(headR1, headR2 + yoffset, headR3);

				this.Bodypiece[9] = new PlaneRenderer(this, "Body|Body", 32, 0, VisiblePartFlags.ChestFlag, false, false);
				this.Bodypiece[9].addTopPlane(-1.0F, 10.0F, 8.0F, 2, 0, 6, this.strech);
				this.Bodypiece[9].setRotationPoint(headR1, headR2 + yoffset, headR3);

				this.Bodypiece[10] = new PlaneRenderer(this, "Body|Body", 32, 0, VisiblePartFlags.ChestFlag, false, false);
				this.Bodypiece[10].addTopPlane(-1.0F, 12.0F, 8.0F, 2, 0, 6, this.strech);
				this.Bodypiece[10].setRotationPoint(headR1, headR2 + yoffset, headR3);

				this.Bodypiece[11] = new PlaneRenderer(this, "Body|Body", 32, 0, VisiblePartFlags.HeadFlag, false, false);
				this.Bodypiece[11].mirror = true;
				this.Bodypiece[11].addSidePlane(-1.0F, 10.0F, 8.0F, 0, 2, 6, this.strech);
				this.Bodypiece[11].setRotationPoint(headR1, headR2 + yoffset, headR3);

				this.Bodypiece[12] = new PlaneRenderer(this, "Body|Body", 32, 0, VisiblePartFlags.HeadFlag, false, false);
				this.Bodypiece[12].addSidePlane(1.0F, 10.0F, 8.0F, 0, 2, 6, this.strech);
				this.Bodypiece[12].setRotationPoint(headR1, headR2 + yoffset, headR3);

				this.rightarm = new ModelRenderer(this, 40, 16, VisiblePartFlags.RightArmFlag, false, false);
				this.rightarm.addBox("Right Arm", -2.0F, 4.0F, -2.0F, 4, 12, 4, this.strech);
				this.rightarm.setRotationPoint(-3.0F, 8.0F + yoffset, 0.0F);

				this.LeftArm = new ModelRenderer(this, 40, 16, VisiblePartFlags.LeftArmFlag, false, false);
				this.LeftArm.mirror = true;
				this.LeftArm.addBox("Left Arm", -2.0F, 4.0F, -2.0F, 4, 12, 4, this.strech);
				this.LeftArm.setRotationPoint(3.0F, 8.0F + yoffset, 0.0F);

				this.RightLeg = new ModelRenderer(this, 40, 16, VisiblePartFlags.LeftLegFlag, false, false);
				this.RightLeg.addBox("Right Leg", -2.0F, 4.0F, -2.0F, 4, 12, 4, this.strech);
				this.RightLeg.setRotationPoint(-3.0F, 0.0F + yoffset, 0.0F);

				this.LeftLeg = new ModelRenderer(this, 40, 16, VisiblePartFlags.LeftLegFlag, false, false);
				this.LeftLeg.mirror = true;
				this.LeftLeg.addBox("Left Leg", -2.0F, 4.0F, -2.0F, 4, 12, 4, this.strech);
				this.LeftLeg.setRotationPoint(3.0F, 0.0F + yoffset, 0.0F);

				this.SteveArm = new ModelRenderer(this, 40, 16, VisiblePartFlags.HeadFlag, false, false);
				this.SteveArm.addBox("Steve Arm?", -3.0F, -2.0F, -2.0F, 4, 12, 4, this.strech);
				this.SteveArm.setRotationPoint(-5.0F, 2.0F + yoffset, 0.0F);
				boxList.Remove(SteveArm);

				this.unicornarm = new ModelRenderer(this, 40, 16, VisiblePartFlags.HeadFlag, false, false);
				this.unicornarm.addBox("Unicorn Arm?", -3.0F, -2.0F, -2.0F, 4, 12, 4, this.strech);
				this.unicornarm.setRotationPoint(-5.0F, 2.0F + yoffset, 0.0F);
				boxList.Remove(unicornarm);

				float txf = 0.0F;
				float tyf = 8.0F;
				float tzf = -14.0F;
				float TailR1 = 0.0F - txf;
				float TailR2 = 10.0F - tyf;
				float TailR3 = 0.0F;
				this.Tail = new PlaneRenderer[10];

				this.Tail[0] = new PlaneRenderer(this, "Tail|Tail", 32, 0, VisiblePartFlags.ChestFlag, false, false);
				this.Tail[0].addTopPlane(-2.0F + txf, -7.0F + tyf, 16.0F + tzf, 4, 0, 4, this.strech);
				this.Tail[0].setRotationPoint(TailR1, TailR2 + yoffset, TailR3);

				this.Tail[1] = new PlaneRenderer(this, "Tail|Tail", 32, 0, VisiblePartFlags.ChestFlag, false, false);
				this.Tail[1].addTopPlane(-2.0F + txf, 9.0F + tyf, 16.0F + tzf, 4, 0, 4, this.strech);
				this.Tail[1].setRotationPoint(TailR1, TailR2 + yoffset, TailR3);

				this.Tail[2] = new PlaneRenderer(this, "Tail|Tail", 32, 0, VisiblePartFlags.ChestFlag, false, false);
				this.Tail[2].addBackPlane(-2.0F + txf, -7.0F + tyf, 16.0F + tzf, 4, 8, 0, this.strech);
				this.Tail[2].setRotationPoint(TailR1, TailR2 + yoffset, TailR3);

				this.Tail[3] = new PlaneRenderer(this, "Tail|Tail", 32, 0, VisiblePartFlags.ChestFlag, false, false);
				this.Tail[3].addBackPlane(-2.0F + txf, -7.0F + tyf, 20.0F + tzf, 4, 8, 0, this.strech);
				this.Tail[3].setRotationPoint(TailR1, TailR2 + yoffset, TailR3);

				this.Tail[4] = new PlaneRenderer(this, "Tail|Tail", 32, 0, VisiblePartFlags.ChestFlag, false, false);
				this.Tail[4].addBackPlane(-2.0F + txf, 1.0F + tyf, 16.0F + tzf, 4, 8, 0, this.strech);
				this.Tail[4].setRotationPoint(TailR1, TailR2 + yoffset, TailR3);

				this.Tail[5] = new PlaneRenderer(this, "Tail|Tail", 32, 0, VisiblePartFlags.ChestFlag, false, false);
				this.Tail[5].addBackPlane(-2.0F + txf, 1.0F + tyf, 20.0F + tzf, 4, 8, 0, this.strech);
				this.Tail[5].setRotationPoint(TailR1, TailR2 + yoffset, TailR3);

				this.Tail[6] = new PlaneRenderer(this, "Tail|Tail", 36, 0, VisiblePartFlags.ChestFlag, false, false);
				this.Tail[6].mirror = true;
				this.Tail[6].addSidePlane(2.0F + txf, -7.0F + tyf, 16.0F + tzf, 0, 8, 4, this.strech);
				this.Tail[6].setRotationPoint(TailR1, TailR2 + yoffset, TailR3);

				this.Tail[7] = new PlaneRenderer(this, "Tail|Tail", 36, 0, VisiblePartFlags.ChestFlag, false, false);
				this.Tail[7].addSidePlane(-2.0F + txf, -7.0F + tyf, 16.0F + tzf, 0, 8, 4, this.strech);
				this.Tail[7].setRotationPoint(TailR1, TailR2 + yoffset, TailR3);

				this.Tail[8] = new PlaneRenderer(this, "Tail|Tail", 36, 0, VisiblePartFlags.ChestFlag, false, false);
				this.Tail[8].mirror = true;
				this.Tail[8].addSidePlane(2.0F + txf, 1.0F + tyf, 16.0F + tzf, 0, 8, 4, this.strech);
				this.Tail[8].setRotationPoint(TailR1, TailR2 + yoffset, TailR3);

				this.Tail[9] = new PlaneRenderer(this, "Tail|Tail", 36, 0, VisiblePartFlags.ChestFlag, false, false);
				this.Tail[9].addSidePlane(-2.0F + txf, 1.0F + tyf, 16.0F + tzf, 0, 8, 4, this.strech);
				this.Tail[9].setRotationPoint(TailR1, TailR2 + yoffset, TailR3);

				this.TailRotateAngleY = this.Tail[0].rotateAngleY;
				this.TailRotateAngleY = this.Tail[0].rotateAngleY;

				float WingR1 = 0.0F;

				float WingR2 = 0.0F;
				float WingR3 = 0.0F;

				this.LeftWing = new ModelRenderer[3];

				this.LeftWing[0] = new ModelRenderer(this, 56, 16, VisiblePartFlags.RightLegFlag, true, true);
				this.LeftWing[0].mirror = true;
				this.LeftWing[0].addBox("Left Wing|Left Wing", 4.0F, 5.0F, 2.0F, 2, 6, 2, this.strech);
				this.LeftWing[0].setRotationPoint(WingR1, WingR2 + yoffset, WingR3);

				this.LeftWing[1] = new ModelRenderer(this, 56, 16, VisiblePartFlags.RightLegFlag, true, true);
				this.LeftWing[1].mirror = true;
				this.LeftWing[1].addBox("Left Wing|Left Wing", 4.0F, 5.0F, 4.0F, 2, 8, 2, this.strech);
				this.LeftWing[1].setRotationPoint(WingR1, WingR2 + yoffset, WingR3);

				this.LeftWing[2] = new ModelRenderer(this, 56, 16, VisiblePartFlags.RightLegFlag, true, true);
				this.LeftWing[2].mirror = true;
				this.LeftWing[2].addBox("Left Wing|Left Wing", 4.0F, 5.0F, 6.0F, 2, 6, 2, this.strech);
				this.LeftWing[2].setRotationPoint(WingR1, WingR2 + yoffset, WingR3);
				
				this.RightWing = new ModelRenderer[3];

				this.RightWing[0] = new ModelRenderer(this, 56, 16, VisiblePartFlags.RightLegFlag, true, true);
				this.RightWing[0].addBox("Right Wing|Right Wing", -6.0F, 5.0F, 2.0F, 2, 6, 2, this.strech);
				this.RightWing[0].setRotationPoint(WingR1, WingR2 + yoffset, WingR3);

				this.RightWing[1] = new ModelRenderer(this, 56, 16, VisiblePartFlags.RightLegFlag, true, true);
				this.RightWing[1].addBox("Right Wing|Right Wing", -6.0F, 5.0F, 4.0F, 2, 8, 2, this.strech);
				this.RightWing[1].setRotationPoint(WingR1, WingR2 + yoffset, WingR3);

				this.RightWing[2] = new ModelRenderer(this, 56, 16, VisiblePartFlags.RightLegFlag, true, true);
				this.RightWing[2].addBox("Right Wing|Right Wing", -6.0F, 5.0F, 6.0F, 2, 6, 2, this.strech);
				this.RightWing[2].setRotationPoint(WingR1, WingR2 + yoffset, WingR3);
				
				float LeftWingExtR1 = headR1 + 4.5F;
				float LeftWingExtR2 = headR2 + 5.0F;
				float LeftWingExtR3 = headR3 + 6.0F;

				this.LeftWingExt = new ModelRenderer[7];

				this.LeftWingExt[0] = new ModelRenderer(this, 56, 19, VisiblePartFlags.RightLegFlag, true, true);
				this.LeftWingExt[0].mirror = true;

				this.LeftWingExt[0].addBox("Left Wing Ext|Left Wing Extended", 0.0F, 0.0F, 0.0F, 1, 8, 2, this.strech + 0.1F);
				this.LeftWingExt[0].setRotationPoint(LeftWingExtR1, LeftWingExtR2 + yoffset, LeftWingExtR3);

				this.LeftWingExt[1] = new ModelRenderer(this, 56, 19, VisiblePartFlags.RightLegFlag, true, true);
				this.LeftWingExt[1].mirror = true;

				this.LeftWingExt[1].addBox("Left Wing Ext|Left Wing Extended", 0.0F, 8.0F, 0.0F, 1, 6, 2, this.strech + 0.1F);
				this.LeftWingExt[1].setRotationPoint(LeftWingExtR1, LeftWingExtR2 + yoffset, LeftWingExtR3);

				this.LeftWingExt[2] = new ModelRenderer(this, 56, 19, VisiblePartFlags.RightLegFlag, true, true);
				this.LeftWingExt[2].mirror = true;
				this.LeftWingExt[2].addBox("Left Wing Ext|Left Wing Extended", 0.0F, -1.2F, -0.2F, 1, 8, 2, this.strech - 0.2F);
				this.LeftWingExt[2].setRotationPoint(LeftWingExtR1, LeftWingExtR2 + yoffset, LeftWingExtR3);

				this.LeftWingExt[3] = new ModelRenderer(this, 56, 19, VisiblePartFlags.RightLegFlag, true, true);
				this.LeftWingExt[3].mirror = true;
				this.LeftWingExt[3].addBox("Left Wing Ext|Left Wing Extended", 0.0F, 1.8F, 1.3F, 1, 8, 2, this.strech - 0.1F);
				this.LeftWingExt[3].setRotationPoint(LeftWingExtR1, LeftWingExtR2 + yoffset, LeftWingExtR3);

				this.LeftWingExt[4] = new ModelRenderer(this, 56, 19, VisiblePartFlags.RightLegFlag, true, true);
				this.LeftWingExt[4].mirror = true;
				this.LeftWingExt[4].addBox("Left Wing Ext|Left Wing Extended", 0.0F, 5.0F, 2.0F, 1, 8, 2, this.strech);
				this.LeftWingExt[4].setRotationPoint(LeftWingExtR1, LeftWingExtR2 + yoffset, LeftWingExtR3);

				this.LeftWingExt[5] = new ModelRenderer(this, 56, 19, VisiblePartFlags.RightLegFlag, true, true);
				this.LeftWingExt[5].mirror = true;

				this.LeftWingExt[5].addBox("Left Wing Ext|Left Wing Extended", 0.0F, 0.0F, -0.2F, 1, 6, 2, this.strech + 0.3F);
				this.LeftWingExt[5].setRotationPoint(LeftWingExtR1, LeftWingExtR2 + yoffset, LeftWingExtR3);

				this.LeftWingExt[6] = new ModelRenderer(this, 56, 19, VisiblePartFlags.RightLegFlag, true, true);
				this.LeftWingExt[6].mirror = true;

				this.LeftWingExt[6].addBox("Left Wing Ext|Left Wing Extended", 0.0F, 0.0F, 0.2F, 1, 3, 2, this.strech + 0.2F);
				this.LeftWingExt[6].setRotationPoint(LeftWingExtR1, LeftWingExtR2 + yoffset, LeftWingExtR3);

				float RightWingExtR1 = headR1 - 5.5F;
				float RightWingExtR2 = headR2 + 5.0F;
				float RightWingExtR3 = headR3 + 6.0F;

				this.RightWingExt = new ModelRenderer[7];

				this.RightWingExt[0] = new ModelRenderer(this, 56, 19, VisiblePartFlags.RightLegFlag, true, true);
				this.RightWingExt[0].mirror = true;

				this.RightWingExt[0].addBox("Right Wing Ext|Right Wing Extended", 0.0F, 0.0F, 0.0F, 1, 8, 2, this.strech + 0.1F);
				this.RightWingExt[0].setRotationPoint(RightWingExtR1, RightWingExtR2 + yoffset, RightWingExtR3);

				this.RightWingExt[1] = new ModelRenderer(this, 56, 19, VisiblePartFlags.RightLegFlag, true, true);
				this.RightWingExt[1].mirror = true;

				this.RightWingExt[1].addBox("Right Wing Ext|Right Wing Extended", 0.0F, 8.0F, 0.0F, 1, 6, 2, this.strech + 0.1F);
				this.RightWingExt[1].setRotationPoint(RightWingExtR1, RightWingExtR2 + yoffset, RightWingExtR3);

				this.RightWingExt[2] = new ModelRenderer(this, 56, 19, VisiblePartFlags.RightLegFlag, true, true);
				this.RightWingExt[2].mirror = true;
				this.RightWingExt[2].addBox("Right Wing Ext|Right Wing Extended", 0.0F, -1.2F, -0.2F, 1, 8, 2, this.strech - 0.2F);
				this.RightWingExt[2].setRotationPoint(RightWingExtR1, RightWingExtR2 + yoffset, RightWingExtR3);

				this.RightWingExt[3] = new ModelRenderer(this, 56, 19, VisiblePartFlags.RightLegFlag, true, true);
				this.RightWingExt[3].mirror = true;
				this.RightWingExt[3].addBox("Right Wing Ext|Right Wing Extended", 0.0F, 1.8F, 1.3F, 1, 8, 2, this.strech - 0.1F);
				this.RightWingExt[3].setRotationPoint(RightWingExtR1, RightWingExtR2 + yoffset, RightWingExtR3);

				this.RightWingExt[4] = new ModelRenderer(this, 56, 19, VisiblePartFlags.RightLegFlag, true, true);
				this.RightWingExt[4].mirror = true;
				this.RightWingExt[4].addBox("Right Wing Ext|Right Wing Extended", 0.0F, 5.0F, 2.0F, 1, 8, 2, this.strech);
				this.RightWingExt[4].setRotationPoint(RightWingExtR1, RightWingExtR2 + yoffset, RightWingExtR3);

				this.RightWingExt[5] = new ModelRenderer(this, 56, 19, VisiblePartFlags.RightLegFlag, true, true);
				this.RightWingExt[5].mirror = true;

				this.RightWingExt[5].addBox("Right Wing Ext|Right Wing Extended", 0.0F, 0.0F, -0.2F, 1, 6, 2, this.strech + 0.3F);
				this.RightWingExt[5].setRotationPoint(RightWingExtR1, RightWingExtR2 + yoffset, RightWingExtR3);

				this.RightWingExt[6] = new ModelRenderer(this, 56, 19, VisiblePartFlags.RightLegFlag, true, true);
				this.RightWingExt[6].mirror = true;

				this.RightWingExt[6].addBox("Right Wing Ext|Right Wing Extended", 0.0F, 0.0F, 0.2F, 1, 3, 2, this.strech + 0.2F);
				this.RightWingExt[6].setRotationPoint(RightWingExtR1, RightWingExtR2 + yoffset, RightWingExtR3);

				this.WingRotateAngleX = this.LeftWingExt[0].rotateAngleX;
				this.WingRotateAngleY = this.LeftWingExt[0].rotateAngleY;
				this.WingRotateAngleZ = this.LeftWingExt[0].rotateAngleZ;

				animate(0, 0, 0, 0, 0);
			}

			public void animate(float Move, float Moveswing, float Loop, float Right, float Down)
			{
				int heldItemRight = 0;
				bool isSleeping = false, issneak = false, aimedBow = false;

				//Naming Error by MCP! Renamed for you...
				float SwingProgress = onGround;
				//
				rainboom = false;

				float headRotateAngleY;
				float headRotateAngleX;
				if (isSleeping)
				{
					// the full 90 degrees looks a bit off
					//headRotateAngleY = 1.571F;
					headRotateAngleY = 1.4F;
					headRotateAngleX = .1F;
				}
				else
				{
					headRotateAngleY = Right / 57.29578F;
					headRotateAngleX = Down / 57.29578F;
				}
				head.rotateAngleY = headRotateAngleY;
				head.rotateAngleX = headRotateAngleX;
				headpiece[0].rotateAngleY = headRotateAngleY;
				headpiece[0].rotateAngleX = headRotateAngleX;
				headpiece[1].rotateAngleY = headRotateAngleY;
				headpiece[1].rotateAngleX = headRotateAngleX;
				headpiece[2].rotateAngleY = headRotateAngleY;
				headpiece[2].rotateAngleX = headRotateAngleX;
				helmet.rotateAngleY = headRotateAngleY;
				helmet.rotateAngleX = headRotateAngleX;
				// finally, whatever else we did, tilt the horn forward a bit
				headpiece[2].rotateAngleX = headRotateAngleX + 0.5F;

				float rightarmRotateAngleX;
				float LeftArmRotateAngleX;
				float RightLegRotateAngleX;
				float LeftLegRotateAngleX;

				if (!isFlying || !isPegasus)
				{
					// swing the legs a bit
					rightarmRotateAngleX = (float)Math.Cos(Move * 0.6662F + (float)Math.PI) * 0.6F * Moveswing;
					LeftArmRotateAngleX = (float)Math.Cos(Move * 0.6662F) * 0.6F * Moveswing;
					RightLegRotateAngleX = (float)Math.Cos(Move * 0.6662F) * 0.3F * Moveswing;
					LeftLegRotateAngleX = (float)Math.Cos(Move * 0.6662F + (float)Math.PI) * 0.3F * Moveswing;
					rightarm.rotateAngleY = 0F;
					SteveArm.rotateAngleY = 0F;
					unicornarm.rotateAngleY = 0F;
					LeftArm.rotateAngleY = 0F;
					RightLeg.rotateAngleY = 0F;
					LeftLeg.rotateAngleY = 0F;
				}
				else
				{
					if (Moveswing < 0.9999F)
					{
						// just sweep the legs back slightly
						rainboom = false;
						rightarmRotateAngleX = (float)Math.Sin(0F - (Moveswing * 0.5F));
						LeftArmRotateAngleX = (float)Math.Sin(0F - (Moveswing * 0.5F));
						RightLegRotateAngleX = (float)Math.Sin(Moveswing * 0.5F);
						LeftLegRotateAngleX = (float)Math.Sin(Moveswing * 0.5F);

					}
					else
					{
						//sonic rainboom pose
						rainboom = true;
						rightarmRotateAngleX = 4.712F;
						LeftArmRotateAngleX = 4.712F;
						RightLegRotateAngleX = 1.571F;
						LeftLegRotateAngleX = 1.571F;
					}
					rightarm.rotateAngleY = .2F;
					SteveArm.rotateAngleY = .2F;
					LeftArm.rotateAngleY = -.2F;
					RightLeg.rotateAngleY = -.2F;
					LeftLeg.rotateAngleY = .2F;
				}

				if (isSleeping)
				{
					rightarmRotateAngleX = 4.712F;
					LeftArmRotateAngleX = 4.712F;
					RightLegRotateAngleX = 1.571F;
					LeftLegRotateAngleX = 1.571F;
				}

				rightarm.rotateAngleX = rightarmRotateAngleX;
				SteveArm.rotateAngleX = rightarmRotateAngleX;
				// unicorn arm stays still, because magic
				//unicornarm.rotateAngleX = rightarmRotateAngleX;
				unicornarm.rotateAngleX = 0F;
				LeftArm.rotateAngleX = LeftArmRotateAngleX;
				RightLeg.rotateAngleX = RightLegRotateAngleX;
				LeftLeg.rotateAngleX = LeftLegRotateAngleX;
				rightarm.rotateAngleZ = 0F;
				SteveArm.rotateAngleZ = 0F;
				unicornarm.rotateAngleZ = 0F;
				LeftArm.rotateAngleZ = 0F;



				for (int i = 0; i < Tail.Length; i++)
				{
					if (rainboom)
					{
						Tail[i].rotateAngleZ = 0F;
					}
					else
					{
						Tail[i].rotateAngleZ = (float)Math.Cos(Move * 0.8F) * 0.2F * Moveswing;
					}
				}

				if ((heldItemRight != 0) && !rainboom)
				{
					if (isUnicorn)
					{
						// unicorn arm stays still, because magic
						//unicornarm.rotateAngleX = rightarm.rotateAngleX * 0.5F - (float)Math.PI * 0.1F;
					}
					else
					{
						rightarm.rotateAngleX = rightarm.rotateAngleX * 0.5F - (float)Math.PI * 0.1F;
						SteveArm.rotateAngleX = SteveArm.rotateAngleX * 0.5F - (float)Math.PI * 0.1F;
					}
				}

				float BodyRotateAngleY = 0F;

				if ((SwingProgress > -9990F) && !isUnicorn)
				{
					// swing the body and tail if we are smashing stuff
					BodyRotateAngleY = (float)Math.Sin((float)Math.Sqrt(SwingProgress) * (float)Math.PI * 2F) * 0.2F;
				}

				Body.rotateAngleY = (float)(BodyRotateAngleY * 0.2);
				for (int i = 0; i < Bodypiece.Length; i++)
				{
					Bodypiece[i].rotateAngleY = (float)(BodyRotateAngleY * 0.2);
				}
				for (int i = 0; i < LeftWing.Length; i++)
				{
					LeftWing[i].rotateAngleY = (float)(BodyRotateAngleY * 0.2);
				}
				for (int i = 0; i < RightWing.Length; i++)
				{
					RightWing[i].rotateAngleY = (float)(BodyRotateAngleY * 0.2);
				}

				for (int i = 0; i < Tail.Length; i++)
				{
					Tail[i].rotateAngleY = BodyRotateAngleY;
				}

				float ArmRotationPointZ = (float)Math.Sin(Body.rotateAngleY) * 5F;
				float ArmRotationPointX = (float)Math.Cos(Body.rotateAngleY) * 5F;
				float LegSplay = 4F;
				if (issneak && !isFlying)
				{
					LegSplay = 0F;
				}
				if (isSleeping)
				{
					LegSplay = 2.6F;
				}
				if (rainboom)
				{
					rightarm.rotationPointZ = ArmRotationPointZ + 2F;
					SteveArm.rotationPointZ = ArmRotationPointZ + 2F;
					LeftArm.rotationPointZ = 0 - ArmRotationPointZ + 2F;
				}
				else
				{
					rightarm.rotationPointZ = ArmRotationPointZ + 1F;
					SteveArm.rotationPointZ = ArmRotationPointZ + 1F;
					LeftArm.rotationPointZ = 0 - ArmRotationPointZ + 1F;
				}
				rightarm.rotationPointX = 0 - ArmRotationPointX - 1F + LegSplay;
				SteveArm.rotationPointX = 0 - ArmRotationPointX;
				LeftArm.rotationPointX = ArmRotationPointX + 1F - LegSplay;
				RightLeg.rotationPointX = 0 - ArmRotationPointX - 1F + LegSplay;
				LeftLeg.rotationPointX = ArmRotationPointX + 1F - LegSplay;


				rightarm.rotateAngleY += Body.rotateAngleY;
				LeftArm.rotateAngleY += Body.rotateAngleY;
				LeftArm.rotateAngleX += Body.rotateAngleY;



				// set rotation point Y as well, to reset leg position after riding
				rightarm.rotationPointY = 8F;
				LeftArm.rotationPointY = 8F;
				RightLeg.rotationPointY = 4F;
				LeftLeg.rotationPointY = 4F;

				if (SwingProgress > -9990F)
				{
					float f = SwingProgress;
					f = 1F - SwingProgress;
					f *= f * f; //f^3
					f = 1F - f;
					float f1 = (float)Math.Sin(f * (float)Math.PI);
					float SwingProgressPi = (float)Math.Sin(SwingProgress * (float)Math.PI);
					float f2 = SwingProgressPi * -(head.rotateAngleX - 0.7F) * 0.75F;

					if (isUnicorn)
					{
						// swingprogress is used when we hit stuff with a pick or whatever      
						unicornarm.rotateAngleX -= (float)((double)f1 * 1.2D + (double)f2);
						unicornarm.rotateAngleY += Body.rotateAngleY * 2F;
						unicornarm.rotateAngleZ = SwingProgressPi * -0.4F;
					}
					else
					{
						rightarm.rotateAngleX -= (float)((double)f1 * 1.2D + (double)f2);
						rightarm.rotateAngleY += Body.rotateAngleY * 2F;
						rightarm.rotateAngleZ = SwingProgressPi * -0.4F;

						SteveArm.rotateAngleX -= (float)((double)f1 * 1.2D + (double)f2);
						SteveArm.rotateAngleY += Body.rotateAngleY * 2F;
						SteveArm.rotateAngleZ = SwingProgressPi * -0.4F;
					}
				}

				if (issneak && !isFlying)
				{
					// face down, plot up, that's the way we like to buck

					// body stuff
					float BodyRotateAngleX = 0.4F;
					float BodyRotationPointY = 7F;
					float BodyRotationPointZ = -4F;
					Body.rotateAngleX = BodyRotateAngleX;
					Body.rotationPointY = BodyRotationPointY;
					Body.rotationPointZ = BodyRotationPointZ;
					for (int i = 0; i < Bodypiece.Length; i++)
					{
						Bodypiece[i].rotateAngleX = BodyRotateAngleX;
						Bodypiece[i].rotationPointY = BodyRotationPointY;
						Bodypiece[i].rotationPointZ = BodyRotationPointZ;
					}


					// extended wings
					float lwrpy = 3.5F;
					float lwrpz = 6F;

					for (int i = 0; i < LeftWingExt.Length; i++)
					{
						LeftWingExt[i].rotateAngleX = (float)(BodyRotateAngleX + (float)Math.PI * 0.75);
						LeftWingExt[i].rotationPointY = BodyRotationPointY + lwrpy;
						LeftWingExt[i].rotationPointZ = BodyRotationPointZ + lwrpz;
						// point the wings up
						LeftWingExt[i].rotateAngleX = 2.5F;
						LeftWingExt[i].rotateAngleZ = -6F;
					}

					float rwrpy = 4.5F;
					float rwrpz = 6F;

					for (int i = 0; i < LeftWingExt.Length; i++)
					{
						RightWingExt[i].rotateAngleX = (float)(BodyRotateAngleX + (float)Math.PI * 0.75);
						RightWingExt[i].rotationPointY = BodyRotationPointY + rwrpy;
						RightWingExt[i].rotationPointZ = BodyRotationPointZ + rwrpz;
						// point the wings up
						RightWingExt[i].rotateAngleX = 2.5F;
						RightWingExt[i].rotateAngleZ = 6F;
					}

					// legs stuff
					RightLeg.rotateAngleX -= 0F;
					LeftLeg.rotateAngleX -= 0F;
					rightarm.rotateAngleX -= 0.4F;
					SteveArm.rotateAngleX += 0.4F;
					unicornarm.rotateAngleX += 0.4F;
					LeftArm.rotateAngleX -= 0.4F;
					RightLeg.rotationPointZ = 10F;
					LeftLeg.rotationPointZ = 10F;
					RightLeg.rotationPointY = 7F;
					LeftLeg.rotationPointY = 7F;

					// head stuff
					float headRotationPointY;
					float headRotationPointZ;
					float headRotationPointX;
					if (isSleeping)
					{
						headRotationPointY = 2F;
						headRotationPointZ = -1F;
						headRotationPointX = 1F;
					}
					else
					{
						headRotationPointY = 6F;
						headRotationPointZ = -2F;
						headRotationPointX = 0F;
					}
					head.rotationPointY = headRotationPointY;
					head.rotationPointZ = headRotationPointZ;
					head.rotationPointX = headRotationPointX;
					helmet.rotationPointY = headRotationPointY;
					helmet.rotationPointZ = headRotationPointZ;
					helmet.rotationPointX = headRotationPointX;
					headpiece[0].rotationPointY = headRotationPointY;
					headpiece[0].rotationPointZ = headRotationPointZ;
					headpiece[0].rotationPointX = headRotationPointX;
					headpiece[1].rotationPointY = headRotationPointY;
					headpiece[1].rotationPointZ = headRotationPointZ;
					headpiece[1].rotationPointX = headRotationPointX;
					headpiece[2].rotationPointY = headRotationPointY;
					headpiece[2].rotationPointZ = headRotationPointZ;
					headpiece[2].rotationPointX = headRotationPointX;

					// tail stuff
					float txf = 0F; // tail horizontal fudge
					float tyf = 8F; // tail vertical fudge
					float tzf = -14F; // tail depth fudge
					float TailRotationPointX = 0.0F - txf;
					float TailRotationPointY = 9F - tyf;
					float TailRotationPointZ = -4F - tzf;
					float TailRotateAngleX = 0.0F;
					for (int i = 0; i < Tail.Length; i++)
					{
						Tail[i].rotationPointX = TailRotationPointX;
						Tail[i].rotationPointY = TailRotationPointY;
						Tail[i].rotationPointZ = TailRotationPointZ;
						Tail[i].rotateAngleX = TailRotateAngleX;
					}

				}
				else
				{
					// body stuff
					float BodyRotateAngleX = 0F;
					float BodyRotationPointY = 0F;
					float BodyRotationPointZ = 0F;
					Body.rotateAngleX = BodyRotateAngleX;
					Body.rotationPointY = BodyRotationPointY;
					Body.rotationPointZ = BodyRotationPointZ;
					for (int i = 0; i < Bodypiece.Length; i++)
					{
						Bodypiece[i].rotateAngleX = BodyRotateAngleX;
						Bodypiece[i].rotationPointY = BodyRotationPointY;
						Bodypiece[i].rotationPointZ = BodyRotationPointZ;
					}

					if (isPegasus)
					{
						if (!isFlying)
						{
							// wings folded
							for (int i = 0; i < LeftWing.Length; i++)
							{
								LeftWing[i].rotateAngleX = (float)(BodyRotateAngleX + (float)Math.PI * 0.5);
								LeftWing[i].rotationPointY = BodyRotationPointY + 13F;
								LeftWing[i].rotationPointZ = BodyRotationPointZ - 3F;
							}
							for (int i = 0; i < RightWing.Length; i++)
							{
								RightWing[i].rotateAngleX = (float)(BodyRotateAngleX + (float)Math.PI * 0.5);
								RightWing[i].rotationPointY = BodyRotationPointY + 13F;
								RightWing[i].rotationPointZ = BodyRotationPointZ - 3F;
							}
						}
						else
						{
							// wings extended
							float lwrpy = 5.5F;
							float lwrpz = 3F;

							for (int i = 0; i < LeftWingExt.Length; i++)
							{
								LeftWingExt[i].rotateAngleX = (float)(BodyRotateAngleX + (float)Math.PI * 0.5);
								LeftWingExt[i].rotationPointY = BodyRotationPointY + lwrpy;
								LeftWingExt[i].rotationPointZ = BodyRotationPointZ + lwrpz;
							}

							float rwrpy = 6.5F;
							float rwrpz = 3F;

							for (int i = 0; i < RightWingExt.Length; i++)
							{
								RightWingExt[i].rotateAngleX = (float)(BodyRotateAngleX + (float)Math.PI * 0.5);
								RightWingExt[i].rotationPointY = BodyRotationPointY + rwrpy;
								RightWingExt[i].rotationPointZ = BodyRotationPointZ + rwrpz;
							}
						}
					}

					// legs stuff
					RightLeg.rotationPointZ = 10F;
					LeftLeg.rotationPointZ = 10F;
					RightLeg.rotationPointY = 8F;
					LeftLeg.rotationPointY = 8F;

					// swing the front arms a bit when still
					// scratch that, only swing steve arm and pony tail
					float ArmRotateAngleZ = (float)Math.Cos(Loop * 0.09F) * 0.05F + 0.05F;
					float ArmRotateAngleX = (float)Math.Sin(Loop * 0.067F) * 0.05F;
					SteveArm.rotateAngleZ += ArmRotateAngleZ;
					unicornarm.rotateAngleZ += ArmRotateAngleZ;
					SteveArm.rotateAngleX += ArmRotateAngleX;
					unicornarm.rotateAngleX += ArmRotateAngleX;

					if (isPegasus && isFlying)
					{
						// swing (flap) the large wings a bit
						WingRotateAngleY = (float)Math.Sin(Loop * 0.067F * 8) * 1F;
						WingRotateAngleZ = (float)Math.Sin(Loop * 0.067F * 8) * 1F;
						for (int i = 0; i < LeftWingExt.Length; i++)
						{
							LeftWingExt[i].rotateAngleX = 2.5F;
							LeftWingExt[i].rotateAngleZ = -WingRotateAngleZ - 4.712F - .4F;
						}
						for (int i = 0; i < RightWingExt.Length; i++)
						{
							RightWingExt[i].rotateAngleX = 2.5F;
							RightWingExt[i].rotateAngleZ = WingRotateAngleZ + 4.712F + .4F;
						}
					}

					// head stuff
					float headRotationPointY;
					float headRotationPointZ;
					float headRotationPointX;
					if (isSleeping)
					{
						headRotationPointY = 2F;
						headRotationPointZ = 1F;
						headRotationPointX = 1F;
					}
					else
					{
						headRotationPointY = 0F;
						headRotationPointZ = 0F;
						headRotationPointX = 0F;
					}
					head.rotationPointY = headRotationPointY;
					head.rotationPointZ = headRotationPointZ;
					head.rotationPointX = headRotationPointX;
					helmet.rotationPointY = headRotationPointY;
					helmet.rotationPointZ = headRotationPointZ;
					helmet.rotationPointX = headRotationPointX;
					headpiece[0].rotationPointY = headRotationPointY;
					headpiece[0].rotationPointZ = headRotationPointZ;
					headpiece[0].rotationPointX = headRotationPointX;
					headpiece[1].rotationPointY = headRotationPointY;
					headpiece[1].rotationPointZ = headRotationPointZ;
					headpiece[1].rotationPointX = headRotationPointX;
					headpiece[2].rotationPointY = headRotationPointY;
					headpiece[2].rotationPointZ = headRotationPointZ;
					headpiece[2].rotationPointX = headRotationPointX;

					float txf = 0F; // tail horizontal fudge
					float tyf = 8F; // tail vertical fudge
					float tzf = -14F; // tail depth fudge
					float TailRotationPointX = 0F - txf;
					float TailRotationPointY = 9F - tyf;
					float TailRotationPointZ = 0F - tzf;
					float TailRotateAngleX = 0.5F * Moveswing;
					for (int i = 0; i < Tail.Length; i++)
					{
						Tail[i].rotationPointX = TailRotationPointX;
						Tail[i].rotationPointY = TailRotationPointY;
						Tail[i].rotationPointZ = TailRotationPointZ;
						if (rainboom)
						{
							Tail[i].rotateAngleX = 1.571F + 0.1F * (float)Math.Sin(Move);
						}
						else
						{
							Tail[i].rotateAngleX = TailRotateAngleX;
						}
					}

					// swing the tail a bit
					for (int i = 0; i < Tail.Length; i++)
					{
						if (rainboom)
						{
							// does nothing
						}
						else
						{
							Tail[i].rotateAngleX += ArmRotateAngleX;
						}
					}
				}

				//// finally, whatever else we did, tilt the wing feathers all over the place
				//// shortest feather of large wing, to be rotated 60 degrees
				LeftWingExt[2].rotateAngleX = LeftWingExt[2].rotateAngleX - .85F;
				//// lower middle feather of large wing, also to be rotated 60 degrees
				LeftWingExt[3].rotateAngleX = LeftWingExt[3].rotateAngleX - .75F;
				//// upper middle feather of large wing, to be rotated 40 degrees
				LeftWingExt[4].rotateAngleX = LeftWingExt[4].rotateAngleX - 0.5F;
				//// other feathers of smaller wing, to be rotated 60 degrees
				LeftWingExt[6].rotateAngleX = LeftWingExt[6].rotateAngleX - .85F;

				//// shortest feather of large wing, to be rotated 60 degrees
				RightWingExt[2].rotateAngleX = RightWingExt[2].rotateAngleX - .85F;
				//// lower middle feather of large wing, also to be rotated 60 degrees
				RightWingExt[3].rotateAngleX = RightWingExt[3].rotateAngleX - .75F;
				//// upper middle feather of large wing, to be rotated 40 degrees
				RightWingExt[4].rotateAngleX = RightWingExt[4].rotateAngleX - 0.5F;
				//// other feathers of smaller wing, to be rotated 60 degrees
				RightWingExt[6].rotateAngleX = RightWingExt[6].rotateAngleX - .85F;


				// finally, whatever else we did, tilt the tail joining piece up a bit
				Bodypiece[9].rotateAngleX = Bodypiece[9].rotateAngleX + 0.5F;
				Bodypiece[10].rotateAngleX = Bodypiece[10].rotateAngleX + 0.5F;
				Bodypiece[11].rotateAngleX = Bodypiece[11].rotateAngleX + 0.5F;
				Bodypiece[12].rotateAngleX = Bodypiece[12].rotateAngleX + 0.5F;

				// move the tail into position if we are rainbooming
				if (rainboom)
				{
					for (int i = 0; i < Tail.Length; i++)
					{
						Tail[i].rotationPointY = Tail[i].rotationPointY + 6;
						Tail[i].rotationPointZ = Tail[i].rotationPointZ + 1;
					}
				}

				if (isRiding)
				{
					// We need to move every single piece up and forward (except Steve's arm)
					// so the pony stands in the boat.
					float ShiftY = -10F;
					float ShiftZ = -10F;
					//    rightarm.rotateAngleX += -0.6283185F;
					//      SteveArm.rotateAngleX += -0.6283185F;
					//    LeftArm.rotateAngleX += -0.6283185F;
					//    RightLeg.rotateAngleX = -1.256637F;
					//    LeftLeg.rotateAngleX = -1.256637F;
					//    RightLeg.rotateAngleY = (float)Math.PI * 0.1F;
					//    LeftLeg.rotateAngleY = -(float)Math.PI * 0.1F;
					head.rotationPointY = head.rotationPointY + ShiftY;
					head.rotationPointZ = head.rotationPointZ + ShiftZ;
					headpiece[0].rotationPointY = headpiece[0].rotationPointY + ShiftY;
					headpiece[0].rotationPointZ = headpiece[0].rotationPointZ + ShiftZ;
					headpiece[1].rotationPointY = headpiece[1].rotationPointY + ShiftY;
					headpiece[1].rotationPointZ = headpiece[1].rotationPointZ + ShiftZ;
					//if (isUnicorn) {
					headpiece[2].rotationPointY = headpiece[2].rotationPointY + ShiftY;
					headpiece[2].rotationPointZ = headpiece[2].rotationPointZ + ShiftZ;
					//}
					//for(int i = 0; i < headpiece.Length; i++)
					//{
					//      headpiece[i].rotationPointY = headpiece[i].rotationPointY + ShiftY;
					//      headpiece[i].rotationPointZ = headpiece[i].rotationPointZ + ShiftZ;
					//}
					helmet.rotationPointY = helmet.rotationPointY + ShiftY;
					helmet.rotationPointZ = helmet.rotationPointZ + ShiftZ;
					Body.rotationPointY = Body.rotationPointY + ShiftY;
					Body.rotationPointZ = Body.rotationPointZ + ShiftZ;
					for (int i = 0; i < Bodypiece.Length; i++)
					{
						Bodypiece[i].rotationPointY = Bodypiece[i].rotationPointY + ShiftY;
						Bodypiece[i].rotationPointZ = Bodypiece[i].rotationPointZ + ShiftZ;
					}
					LeftArm.rotationPointY = LeftArm.rotationPointY + ShiftY;
					LeftArm.rotationPointZ = LeftArm.rotationPointZ + ShiftZ;
					rightarm.rotationPointY = rightarm.rotationPointY + ShiftY;
					rightarm.rotationPointZ = rightarm.rotationPointZ + ShiftZ;
					LeftLeg.rotationPointY = LeftLeg.rotationPointY + ShiftY;
					LeftLeg.rotationPointZ = LeftLeg.rotationPointZ + ShiftZ;
					RightLeg.rotationPointY = RightLeg.rotationPointY + ShiftY;
					RightLeg.rotationPointZ = RightLeg.rotationPointZ + ShiftZ;
					for (int i = 0; i < Tail.Length; i++)
					{
						Tail[i].rotationPointY = Tail[i].rotationPointY + ShiftY;
						Tail[i].rotationPointZ = Tail[i].rotationPointZ + ShiftZ;
					}

					// wings folded
					for (int i = 0; i < LeftWing.Length; i++)
					{
						LeftWing[i].rotationPointY = LeftWing[i].rotationPointY + ShiftY;
						LeftWing[i].rotationPointZ = LeftWing[i].rotationPointZ + ShiftZ;
					}
					for (int i = 0; i < RightWing.Length; i++)
					{
						RightWing[i].rotationPointY = RightWing[i].rotationPointY + ShiftY;
						RightWing[i].rotationPointZ = RightWing[i].rotationPointZ + ShiftZ;
					}

					//wings extended
					for (int i = 0; i < LeftWingExt.Length; i++)
					{
						LeftWingExt[i].rotationPointY = LeftWingExt[i].rotationPointY + ShiftY;
						LeftWingExt[i].rotationPointZ = LeftWingExt[i].rotationPointZ + ShiftZ;
					}

					for (int i = 0; i < RightWingExt.Length; i++)
					{
						RightWingExt[i].rotationPointY = RightWingExt[i].rotationPointY + ShiftY;
						RightWingExt[i].rotationPointZ = RightWingExt[i].rotationPointZ + ShiftZ;
					}
				}

				if (isSleeping)
				{
					// move the head down into the body, and lock it rotated fully 90 degrees.
					// move the legs out to the sides of the body (a bit)
					// and rotate the front legs fully forward and the back legs fully back (a bit like a rainboom pose).
					rightarm.rotationPointZ = rightarm.rotationPointZ + 6F;
					LeftArm.rotationPointZ = LeftArm.rotationPointZ + 6F;
					RightLeg.rotationPointZ = RightLeg.rotationPointZ - 8F;
					LeftLeg.rotationPointZ = LeftLeg.rotationPointZ - 8F;
					rightarm.rotationPointY = rightarm.rotationPointY + 2F;
					LeftArm.rotationPointY = LeftArm.rotationPointY + 2F;
					RightLeg.rotationPointY = RightLeg.rotationPointY + 2F;
					LeftLeg.rotationPointY = LeftLeg.rotationPointY + 2F;
				}

				if (aimedBow)
				{
					if (isUnicorn)
					{
						float f7 = 0.0F;
						float f9 = 0.0F;
						unicornarm.rotateAngleZ = 0.0F;
						unicornarm.rotateAngleY = -(0.1F - f7 * 0.6F) + head.rotateAngleY;
						unicornarm.rotateAngleX = 4.712F + head.rotateAngleX;
						unicornarm.rotateAngleX -= f7 * 1.2F - f9 * 0.4F;
						float f2 = 0; // I am lazy
						unicornarm.rotateAngleZ += (float)Math.Cos(f2 * 0.09F) * 0.05F + 0.05F;
						unicornarm.rotateAngleX += (float)Math.Sin(f2 * 0.067F) * 0.05F;
					}
					else
					{
						float f7 = 0.0F;
						float f9 = 0.0F;
						rightarm.rotateAngleZ = 0.0F;
						rightarm.rotateAngleY = -(0.1F - f7 * 0.6F) + head.rotateAngleY;
						rightarm.rotateAngleX = 4.712F + head.rotateAngleX;
						rightarm.rotateAngleX -= f7 * 1.2F - f9 * 0.4F;
						float f2 = 0; // I am lazy
						rightarm.rotateAngleZ += (float)Math.Cos(f2 * 0.09F) * 0.05F + 0.05F;
						rightarm.rotateAngleX += (float)Math.Sin(f2 * 0.067F) * 0.05F;
						// move the arm back a bit so it looks less weird
						rightarm.rotationPointZ = rightarm.rotationPointZ + 1F;
					}
				}
			}
		}


		// Decompiled by Jad v1.5.8g. Copyright 2001 Pavel Kouznetsov.
		// Jad home page: http://www.kpdus.com/jad.html
		// Decompiler options: packimports(3) braces deadcode fieldsfirst

		// Referenced classes of package net.minecraft.src:
		//                  ModelPlayer, ModelRenderer, PlaneRenderer, AniParams,
		//                  MathHelper, EntityPlayer, InventoryPlayer, ItemStack,
		//                  RenderManager, RenderEngine, Tessellator

		public class pm_newPonyAdv : ModelBase
		{

			private bool rainboom;
			private const float Pi = 3.141593F;
			private float WingRotateAngleX;
			private float WingRotateAngleY;
			private float WingRotateAngleZ;
			private float TailRotateAngleY;
			private float NeckRotX = 0.166F;
			public int tailstop = 0;
			public ModelRenderer head;
			public ModelRenderer[] headpiece;
			public ModelRenderer helmet;
			public ModelRenderer Body;
			public PlaneRenderer[] Bodypiece;
			public PlaneRenderer[] BodypieceNeck;
			public PlaneRenderer[] MuzzleFemale;
			public PlaneRenderer[] MuzzleMale;
			public ModelRenderer rightarm;
			public ModelRenderer LeftArm;
			public ModelRenderer RightLeg;
			public ModelRenderer LeftLeg;
			public ModelRenderer SteveArm;
			public ModelRenderer unicornarm;
			public PlaneRenderer[] Tail;
			public ModelRenderer[] LeftWing;
			public ModelRenderer[] RightWing;
			public ModelRenderer[] LeftWingExt;
			public ModelRenderer[] RightWingExt;
			float strech;

			public pm_newPonyAdv()
			{
				//            super(s);
			}

			public void init()
			{
				init(0.0F);
			}

			public void init(float f)
			{
				init(f, 0.0F);
			}

			public pm_newPonyAdv init(float f, float f1)
			{
				strech = f1;
				float f2 = 0.0F;
				float f3 = 0.0F;
				float f4 = 0.0F;
				head = new ModelRenderer(this, 0, 0, VisiblePartFlags.HeadFlag, false, false);
				head.addBox("Head", -4F, -5F, -6F, 8, 8, 8, strech);
				head.setRotationPoint(f2, f3 + f, f4);
				headpiece = new ModelRenderer[3];
				headpiece[0] = new ModelRenderer(this, 12, 16, VisiblePartFlags.HeadFlag, false, false);
				headpiece[0].addBox("Ear", -4F, -7F, -1F, 2, 2, 2, strech);
				headpiece[0].setRotationPoint(f2, f3 + f, f4);
				headpiece[1] = new ModelRenderer(this, 12, 16, VisiblePartFlags.HeadFlag, false, false);
				headpiece[1].mirror = true;
				headpiece[1].addBox("Ear", 2.0F, -7F, -1F, 2, 2, 2, strech);
				headpiece[1].setRotationPoint(f2, f3 + f, f4);
				headpiece[2] = new ModelRenderer(this, 0, 3, VisiblePartFlags.HeadFlag, true, false);
				headpiece[2].addBox("Horn", -0.5F, -11F, -3.5F, 1, 4, 1, strech);
				headpiece[2].setRotationPoint(f2, f3 + f, f4);
				MuzzleFemale = new PlaneRenderer[10];
				MuzzleFemale[0] = new PlaneRenderer(this, "Female Muzzle|Female Muzzle", 10, 14, VisiblePartFlags.HeadFlag, false, false);
				MuzzleFemale[0].addBackPlane(-2F, 1.0F, -7F, 4, 2, 0, strech);
				MuzzleFemale[0].setRotationPoint(f2, f3 + f, f4);
				MuzzleFemale[1] = new PlaneRenderer(this, "Female Muzzle|Female Muzzle", 11, 13, VisiblePartFlags.HeadFlag, false, false);
				MuzzleFemale[1].addBackPlane(-1F, 0F, -7F, 2, 1, 0, strech);
				MuzzleFemale[1].setRotationPoint(f2, f3 + f, f4);
				MuzzleFemale[2] = new PlaneRenderer(this, "Female Muzzle|Female Muzzle", 9, 14, VisiblePartFlags.HeadFlag, false, false);
				MuzzleFemale[2].addTopPlane(-2F, 1.0F, -7F, 1, 0, 1, strech);
				MuzzleFemale[2].setRotationPoint(f2, f3 + f, f4);
				MuzzleFemale[3] = new PlaneRenderer(this, "Female Muzzle|Female Muzzle", 14, 14, VisiblePartFlags.HeadFlag, false, false);
				MuzzleFemale[3].addTopPlane(1.0F, 1.0F, -7F, 1, 0, 1, strech);
				MuzzleFemale[3].setRotationPoint(f2, f3 + f, f4);
				MuzzleFemale[4] = new PlaneRenderer(this, "Female Muzzle|Female Muzzle", 11, 12, VisiblePartFlags.HeadFlag, false, false);
				MuzzleFemale[4].addTopPlane(-1F, 0F, -7F, 2, 0, 1, strech);
				MuzzleFemale[4].setRotationPoint(f2, f3 + f, f4);
				MuzzleFemale[5] = new PlaneRenderer(this, "Female Muzzle|Female Muzzle", 18, 7, VisiblePartFlags.HeadFlag, false, false);
				MuzzleFemale[5].addTopPlane(-2F, 3F, -7F, 4, 0, 1, strech);
				MuzzleFemale[5].setRotationPoint(f2, f3 + f, f4);
				MuzzleFemale[6] = new PlaneRenderer(this, "Female Muzzle|Female Muzzle", 9, 14, VisiblePartFlags.HeadFlag, false, false);
				MuzzleFemale[6].addSidePlane(-2F, 1.0F, -7F, 0, 2, 1, strech);
				MuzzleFemale[6].setRotationPoint(f2, f3 + f, f4);
				MuzzleFemale[7] = new PlaneRenderer(this, "Female Muzzle|Female Muzzle", 14, 14, VisiblePartFlags.HeadFlag, false, false);
				MuzzleFemale[7].addSidePlane(2.0F, 1.0F, -7F, 0, 2, 1, strech);
				MuzzleFemale[7].setRotationPoint(f2, f3 + f, f4);
				MuzzleFemale[8] = new PlaneRenderer(this, "Female Muzzle|Female Muzzle", 11, 12, VisiblePartFlags.HeadFlag, false, false);
				MuzzleFemale[8].addSidePlane(-1F, 0F, -7F, 0, 1, 1, strech);
				MuzzleFemale[8].setRotationPoint(f2, f3 + f, f4);
				MuzzleFemale[9] = new PlaneRenderer(this, "Female Muzzle|Female Muzzle", 12, 12, VisiblePartFlags.HeadFlag, false, false);
				MuzzleFemale[9].addSidePlane(1.0F, 0F, -7F, 0, 1, 1, strech);
				MuzzleFemale[9].setRotationPoint(f2, f3 + f, f4);
				MuzzleMale = new PlaneRenderer[5];
				MuzzleMale[0] = new PlaneRenderer(this, "Male Muzzle|Male Muzzle", 10, 13, VisiblePartFlags.HeadFlag, false, false);
				MuzzleMale[0].addBackPlane(-2.0F, 0F, -7.0F, 4, 3, 0, strech);
				MuzzleMale[0].setRotationPoint(f2, f3 + f, f4);
				MuzzleMale[1] = new PlaneRenderer(this, "Male Muzzle|Male Muzzle", 10, 13, VisiblePartFlags.HeadFlag, false, false);
				MuzzleMale[1].addTopPlane(-2.0F, 0F, -7.0F, 4, 0, 1, strech);
				MuzzleMale[1].setRotationPoint(f2, f3 + f, f4);
				MuzzleMale[2] = new PlaneRenderer(this, "Male Muzzle|Male Muzzle", 18, 7, VisiblePartFlags.HeadFlag, false, false);
				MuzzleMale[2].addTopPlane(-2.0F, 3.0F, -7.0F, 4, 0, 1, strech);
				MuzzleMale[2].setRotationPoint(f2, f3 + f, f4);
				MuzzleMale[3] = new PlaneRenderer(this, "Male Muzzle|Male Muzzle", 10, 13, VisiblePartFlags.HeadFlag, false, false);
				MuzzleMale[3].addSidePlane(-2.0F, 0F, -7.0F, 0, 3, 1, strech);
				MuzzleMale[3].setRotationPoint(f2, f3 + f, f4);
				MuzzleMale[4] = new PlaneRenderer(this, "Male Muzzle|Male Muzzle", 13, 13, VisiblePartFlags.HeadFlag, false, false);
				MuzzleMale[4].addSidePlane(2.0F, 0F, -7.0F, 0, 3, 1, strech);
				MuzzleMale[4].setRotationPoint(f2, f3 + f, f4);
				helmet = new ModelRenderer(this, 32, 0, VisiblePartFlags.HeadFlag, true, false);
				helmet.addBox("Hair", -4F, -5F, -6F, 8, 8, 8, strech + 0.5F);
				helmet.setRotationPoint(f2, f3 + f, f4);
				float f5 = 0.0F;
				float f6 = 0.0F;
				float f7 = 0.0F;
				Body = new ModelRenderer(this, 16, 16, VisiblePartFlags.HeadFlag, false, false);
				Body.addBox("Body|Body", -4F, 4F, -2F, 8, 8, 4, strech);
				Body.setRotationPoint(f5, f6 + f, f7);
				Bodypiece = new PlaneRenderer[14];
				Bodypiece[0] = new PlaneRenderer(this, "Body|Body", 24, 0, VisiblePartFlags.HeadFlag, false, false);
				Bodypiece[0].addSidePlane(-4F, 4F, 2.0F, 0, 8, 8, strech);
				Bodypiece[0].setRotationPoint(f5, f6 + f, f7);
				Bodypiece[1] = new PlaneRenderer(this, "Body|Body", 24, 0, VisiblePartFlags.HeadFlag, false, false);
				Bodypiece[1].addSidePlane(4F, 4F, 2.0F, 0, 8, 8, strech);
				Bodypiece[1].setRotationPoint(f5, f6 + f, f7);
				Bodypiece[2] = new PlaneRenderer(this, "Body|Body", 32, 20, VisiblePartFlags.HeadFlag, false, false);
				Bodypiece[2].addTopPlane(-4F, 4F, 2.0F, 8, 0, 12, strech);
				Bodypiece[2].setRotationPoint(f2, f3 + f, f4);
				Bodypiece[3] = new PlaneRenderer(this, "Body|Body", 56, 0, VisiblePartFlags.HeadFlag, false, false);
				Bodypiece[3].addTopPlane(-4F, 12F, 2.0F, 8, 0, 8, strech);
				Bodypiece[3].setRotationPoint(f2, f3 + f, f4);
				Bodypiece[4] = new PlaneRenderer(this, "Cutiemark|Body", 4, 0, VisiblePartFlags.HeadFlag, false, false);
				Bodypiece[4].addSidePlane(-4F, 4F, 10F, 0, 8, 4, strech);
				Bodypiece[4].setRotationPoint(f5, f6 + f, f7);
				Bodypiece[5] = new PlaneRenderer(this, "Body|Body", 4, 0, VisiblePartFlags.HeadFlag, false, false);
				Bodypiece[5].addSidePlane(4F, 4F, 10F, 0, 8, 4, strech);
				Bodypiece[5].setRotationPoint(f5, f6 + f, f7);
				Bodypiece[6] = new PlaneRenderer(this, "Body|Body", 36, 16, VisiblePartFlags.HeadFlag, false, false);
				Bodypiece[6].addBackPlane(-4F, 4F, 14F, 8, 4, 0, strech);
				Bodypiece[6].setRotationPoint(f2, f3 + f, f4);
				Bodypiece[7] = new PlaneRenderer(this, "Body|Body", 36, 16, VisiblePartFlags.HeadFlag, false, false);
				Bodypiece[7].addTopPlane(-4F, 12F, 10F, 8, 0, 4, strech);
				Bodypiece[7].setRotationPoint(f2, f3 + f, f4);
				Bodypiece[8] = new PlaneRenderer(this, "Body|Body", 36, 16, VisiblePartFlags.HeadFlag, false, false);
				Bodypiece[8].addBackPlane(-4F, 8F, 14F, 8, 4, 0, strech);
				Bodypiece[8].setRotationPoint(f2, f3 + f, f4);
				Bodypiece[9] = new PlaneRenderer(this, "Body|Body", 32, 0, VisiblePartFlags.HeadFlag, false, false);
				Bodypiece[9].addTopPlane(-1F, 10F, 8F, 2, 0, 6, strech);
				Bodypiece[9].setRotationPoint(f2, f3 + f, f4);
				Bodypiece[10] = new PlaneRenderer(this, "Body|Body", 32, 0, VisiblePartFlags.HeadFlag, false, false);
				Bodypiece[10].addTopPlane(-1F, 12F, 8F, 2, 0, 6, strech);
				Bodypiece[10].setRotationPoint(f2, f3 + f, f4);
				Bodypiece[11] = new PlaneRenderer(this, "Body|Body", 32, 0, VisiblePartFlags.HeadFlag, false, false);
				Bodypiece[11].mirror = true;
				Bodypiece[11].addSidePlane(-1F, 10F, 8F, 0, 2, 6, strech);
				Bodypiece[11].setRotationPoint(f2, f3 + f, f4);
				Bodypiece[12] = new PlaneRenderer(this, "Body|Body", 32, 0, VisiblePartFlags.HeadFlag, false, false);
				Bodypiece[12].addSidePlane(1.0F, 10F, 8F, 0, 2, 6, strech);
				Bodypiece[12].setRotationPoint(f2, f3 + f, f4);
				Bodypiece[13] = new PlaneRenderer(this, "Body|Body", 32, 0, VisiblePartFlags.HeadFlag, false, false);
				Bodypiece[13].addBackPlane(-1F, 10F, 14F, 2, 2, 0, strech);
				Bodypiece[13].setRotationPoint(f2, f3 + f, f4);

				BodypieceNeck = new PlaneRenderer[4];
				BodypieceNeck[0] = new PlaneRenderer(this, "Neck|Neck", 0, 16, VisiblePartFlags.HeadFlag, false, false);
				BodypieceNeck[0].addBackPlane(-2F, 1.2F, -2.8F, 4, 4, 0, strech);
				BodypieceNeck[0].setRotationPoint(f2, f3 + f, f4);
				BodypieceNeck[1] = new PlaneRenderer(this, "Neck|Neck", 0, 16, VisiblePartFlags.HeadFlag, false, false);
				BodypieceNeck[1].addBackPlane(-2F, 1.2F, 1.2F, 4, 4, 0, strech);
				BodypieceNeck[1].setRotationPoint(f2, f3 + f, f4);
				BodypieceNeck[2] = new PlaneRenderer(this, "Neck|Neck", 0, 16, VisiblePartFlags.HeadFlag, false, false);
				BodypieceNeck[2].addSidePlane(-2F, 1.2F, -2.8F, 0, 4, 4, strech);
				BodypieceNeck[2].setRotationPoint(f2, f3 + f, f4);
				BodypieceNeck[3] = new PlaneRenderer(this, "Neck|Neck", 0, 16, VisiblePartFlags.HeadFlag, false, false);
				BodypieceNeck[3].addSidePlane(2F, 1.2F, -2.8F, 0, 4, 4, strech);
				BodypieceNeck[3].setRotationPoint(f2, f3 + f, f4);

				BodypieceNeck[0].rotateAngleX = NeckRotX;
				BodypieceNeck[1].rotateAngleX = NeckRotX;
				BodypieceNeck[2].rotateAngleX = NeckRotX;
				BodypieceNeck[3].rotateAngleX = NeckRotX;

				rightarm = new ModelRenderer(this, 40, 16, VisiblePartFlags.HeadFlag, false, false);
				rightarm.addBox("Leg", -2F, 4F, -2F, 4, 12, 4, strech);
				rightarm.setRotationPoint(-3F, 8F + f, 0.0F);
				LeftArm = new ModelRenderer(this, 40, 16, VisiblePartFlags.HeadFlag, false, false);
				LeftArm.mirror = true;
				LeftArm.addBox("Leg", -2F, 4F, -2F, 4, 12, 4, strech);
				LeftArm.setRotationPoint(3F, 8F + f, 0.0F);
				RightLeg = new ModelRenderer(this, 0, 16, VisiblePartFlags.HeadFlag, false, false);
				RightLeg.addBox("Leg", -2F, 4F, -2F, 4, 12, 4, strech);
				RightLeg.setRotationPoint(-3F, 0.0F + f, 0.0F);
				LeftLeg = new ModelRenderer(this, 0, 16, VisiblePartFlags.HeadFlag, false, false);
				LeftLeg.mirror = true;
				LeftLeg.addBox("Leg", -2F, 4F, -2F, 4, 12, 4, strech);
				LeftLeg.setRotationPoint(3F, 0.0F + f, 0.0F);
				SteveArm = new ModelRenderer(this, 40, 16, VisiblePartFlags.HeadFlag, false, false);
				SteveArm.addBox("", -3F, -2F, -2F, 4, 12, 4, strech);
				SteveArm.setRotationPoint(-5F, 2.0F + f, 0.0F);
				this.boxList.Remove(SteveArm);
				unicornarm = new ModelRenderer(this, 40, 16, VisiblePartFlags.HeadFlag, false, false);
				unicornarm.addBox("", -3F, -2F, -2F, 4, 12, 4, strech);
				unicornarm.setRotationPoint(-5F, 2.0F + f, 0.0F);
				this.boxList.Remove(unicornarm);
				float f8 = 0.0F;
				float f9 = 8F;
				float f10 = -14F;
				float f11 = 0.0F - f8;
				float f12 = 8.8F - f9;
				float f13 = 0.0F;

				Tail = new PlaneRenderer[21];
				Tail[0] = new PlaneRenderer(this, "Tail|Tail", 32, 0, VisiblePartFlags.HeadFlag, false, false);
				Tail[0].addTopPlane(-2F + f8, -7F + f9, 16F + f10, 4, 0, 4, strech);
				Tail[0].setRotationPoint(f11, f12 + f, f13);
				Tail[1] = new PlaneRenderer(this, "Tail|Tail", 36, 0, VisiblePartFlags.HeadFlag, false, false);
				Tail[1].addSidePlane(-2F + f8, -7F + f9, 16F + f10, 0, 4, 4, strech);
				Tail[1].setRotationPoint(f11, f12 + f, f13);
				Tail[2] = new PlaneRenderer(this, "Tail|Tail", 32, 0, VisiblePartFlags.HeadFlag, false, false);
				Tail[2].addBackPlane(-2F + f8, -7F + f9, 16F + f10, 4, 4, 0, strech);
				Tail[2].setRotationPoint(f11, f12 + f, f13);
				Tail[3] = new PlaneRenderer(this, "Tail|Tail", 36, 0, VisiblePartFlags.HeadFlag, false, false);
				Tail[3].addSidePlane(2F + f8, -7F + f9, 16F + f10, 0, 4, 4, strech);
				Tail[3].setRotationPoint(f11, f12 + f, f13);
				Tail[4] = new PlaneRenderer(this, "Tail|Tail", 32, 0, VisiblePartFlags.HeadFlag, false, false);
				Tail[4].addBackPlane(-2F + f8, -7F + f9, 20F + f10, 4, 4, 0, strech);
				Tail[4].setRotationPoint(f11, f12 + f, f13);
				Tail[5] = new PlaneRenderer(this, "Tail|Tail", 32, 0, VisiblePartFlags.HeadFlag, false, false);
				Tail[5].addTopPlane(-2F + f8, -3F + f9, 16F + f10, 4, 0, 4, strech);
				Tail[5].setRotationPoint(f11, f12 + f, f13);
				Tail[6] = new PlaneRenderer(this, "Tail|Tail", 36, 4, VisiblePartFlags.HeadFlag, false, false);
				Tail[6].addSidePlane(-2F + f8, -3F + f9, 16F + f10, 0, 4, 4, strech);
				Tail[6].setRotationPoint(f11, f12 + f, f13);
				Tail[7] = new PlaneRenderer(this, "Tail|Tail", 32, 4, VisiblePartFlags.HeadFlag, false, false);
				Tail[7].addBackPlane(-2F + f8, -3F + f9, 16F + f10, 4, 4, 0, strech);
				Tail[7].setRotationPoint(f11, f12 + f, f13);
				Tail[8] = new PlaneRenderer(this, "Tail|Tail", 36, 4, VisiblePartFlags.HeadFlag, false, false);
				Tail[8].addSidePlane(2F + f8, -3F + f9, 16F + f10, 0, 4, 4, strech);
				Tail[8].setRotationPoint(f11, f12 + f, f13);
				Tail[9] = new PlaneRenderer(this, "Tail|Tail", 32, 4, VisiblePartFlags.HeadFlag, false, false);
				Tail[9].addBackPlane(-2F + f8, -3F + f9, 20F + f10, 4, 4, 0, strech);
				Tail[9].setRotationPoint(f11, f12 + f, f13);
				Tail[10] = new PlaneRenderer(this, "Tail|Tail", 32, 0, VisiblePartFlags.HeadFlag, false, false);
				Tail[10].addTopPlane(-2F + f8, 1F + f9, 16F + f10, 4, 0, 4, strech);
				Tail[10].setRotationPoint(f11, f12 + f, f13);
				Tail[11] = new PlaneRenderer(this, "Tail|Tail", 36, 0, VisiblePartFlags.HeadFlag, false, false);
				Tail[11].addSidePlane(-2F + f8, 1F + f9, 16F + f10, 0, 4, 4, strech);
				Tail[11].setRotationPoint(f11, f12 + f, f13);
				Tail[12] = new PlaneRenderer(this, "Tail|Tail", 32, 0, VisiblePartFlags.HeadFlag, false, false);
				Tail[12].addBackPlane(-2F + f8, 1F + f9, 16F + f10, 4, 4, 0, strech);
				Tail[12].setRotationPoint(f11, f12 + f, f13);
				Tail[13] = new PlaneRenderer(this, "Tail|Tail", 36, 0, VisiblePartFlags.HeadFlag, false, false);
				Tail[13].addSidePlane(2F + f8, 1F + f9, 16F + f10, 0, 4, 4, strech);
				Tail[13].setRotationPoint(f11, f12 + f, f13);
				Tail[14] = new PlaneRenderer(this, "Tail|Tail", 32, 0, VisiblePartFlags.HeadFlag, false, false);
				Tail[14].addBackPlane(-2F + f8, 1F + f9, 20F + f10, 4, 4, 0, strech);
				Tail[14].setRotationPoint(f11, f12 + f, f13);
				Tail[15] = new PlaneRenderer(this, "Tail|Tail", 32, 0, VisiblePartFlags.HeadFlag, false, false);
				Tail[15].addTopPlane(-2F + f8, 5F + f9, 16F + f10, 4, 0, 4, strech);
				Tail[15].setRotationPoint(f11, f12 + f, f13);
				Tail[16] = new PlaneRenderer(this, "Tail|Tail", 36, 4, VisiblePartFlags.HeadFlag, false, false);
				Tail[16].addSidePlane(-2F + f8, 5F + f9, 16F + f10, 0, 4, 4, strech);
				Tail[16].setRotationPoint(f11, f12 + f, f13);
				Tail[17] = new PlaneRenderer(this, "Tail|Tail", 32, 4, VisiblePartFlags.HeadFlag, false, false);
				Tail[17].addBackPlane(-2F + f8, 5F + f9, 16F + f10, 4, 4, 0, strech);
				Tail[17].setRotationPoint(f11, f12 + f, f13);
				Tail[18] = new PlaneRenderer(this, "Tail|Tail", 36, 4, VisiblePartFlags.HeadFlag, false, false);
				Tail[18].addSidePlane(2F + f8, 5F + f9, 16F + f10, 0, 4, 4, strech);
				Tail[18].setRotationPoint(f11, f12 + f, f13);
				Tail[19] = new PlaneRenderer(this, "Tail|Tail", 32, 4, VisiblePartFlags.HeadFlag, false, false);
				Tail[19].addBackPlane(-2F + f8, 5F + f9, 20F + f10, 4, 4, 0, strech);
				Tail[19].setRotationPoint(f11, f12 + f, f13);
				Tail[20] = new PlaneRenderer(this, "Tail|Tail", 32, 0, VisiblePartFlags.HeadFlag, false, false);
				Tail[20].addTopPlane(-2F + f8, 9F + f9, 16F + f10, 4, 0, 4, strech);
				Tail[20].setRotationPoint(f11, f12 + f, f13);

				TailRotateAngleY = Tail[0].rotateAngleY;

				float f14 = 0.0F;
				float f15 = 0.0F;
				float f16 = 0.0F;
				LeftWing = new ModelRenderer[3];
				LeftWing[0] = new ModelRenderer(this, 56, 16, VisiblePartFlags.HeadFlag, true, false);
				LeftWing[0].mirror = true;
				LeftWing[0].addBox("Wing|Left Wing", 4F, 5F, 2.0F, 2, 6, 2, strech);
				LeftWing[0].setRotationPoint(f14, f15 + f, f16);
				LeftWing[1] = new ModelRenderer(this, 56, 16, VisiblePartFlags.HeadFlag, true, false);
				LeftWing[1].mirror = true;
				LeftWing[1].addBox("Wing|Left Wing", 4F, 5F, 4F, 2, 8, 2, strech);
				LeftWing[1].setRotationPoint(f14, f15 + f, f16);
				LeftWing[2] = new ModelRenderer(this, 56, 16, VisiblePartFlags.HeadFlag, true, false);
				LeftWing[2].mirror = true;
				LeftWing[2].addBox("Wing|Left Wing", 4F, 5F, 6F, 2, 6, 2, strech);
				LeftWing[2].setRotationPoint(f14, f15 + f, f16);
				RightWing = new ModelRenderer[3];
				RightWing[0] = new ModelRenderer(this, 56, 16, VisiblePartFlags.HeadFlag, true, false);
				RightWing[0].addBox("Wing|Right Wing", -6F, 5F, 2.0F, 2, 6, 2, strech);
				RightWing[0].setRotationPoint(f14, f15 + f, f16);
				RightWing[1] = new ModelRenderer(this, 56, 16, VisiblePartFlags.HeadFlag, true, false);
				RightWing[1].addBox("Wing|Right Wing", -6F, 5F, 4F, 2, 8, 2, strech);
				RightWing[1].setRotationPoint(f14, f15 + f, f16);
				RightWing[2] = new ModelRenderer(this, 56, 16, VisiblePartFlags.HeadFlag, true, false);
				RightWing[2].addBox("Wing|Right Wing", -6F, 5F, 6F, 2, 6, 2, strech);
				RightWing[2].setRotationPoint(f14, f15 + f, f16);
				float f17 = f2 + 4.5F;
				float f18 = f3 + 5F;
				float f19 = f4 + 6F;
				LeftWingExt = new ModelRenderer[7];
				LeftWingExt[0] = new ModelRenderer(this, 56, 19, VisiblePartFlags.HeadFlag, true, false);
				LeftWingExt[0].mirror = true;
				LeftWingExt[0].addBox("Wing|Left Wing Extended", 0.0F, 0.0F, 0.0F, 1, 8, 2, strech + 0.1F);
				LeftWingExt[0].setRotationPoint(f17, f18 + f, f19);
				LeftWingExt[1] = new ModelRenderer(this, 56, 19, VisiblePartFlags.HeadFlag, true, false);
				LeftWingExt[1].mirror = true;
				LeftWingExt[1].addBox("Wing|Left Wing Extended", 0.0F, 8F, 0.0F, 1, 6, 2, strech + 0.1F);
				LeftWingExt[1].setRotationPoint(f17, f18 + f, f19);
				LeftWingExt[2] = new ModelRenderer(this, 56, 19, VisiblePartFlags.HeadFlag, true, false);
				LeftWingExt[2].mirror = true;
				LeftWingExt[2].addBox("Wing|Left Wing Extended", 0.0F, -1.2F, -0.2F, 1, 8, 2, strech - 0.2F);
				LeftWingExt[2].setRotationPoint(f17, f18 + f, f19);
				LeftWingExt[3] = new ModelRenderer(this, 56, 19, VisiblePartFlags.HeadFlag, true, false);
				LeftWingExt[3].mirror = true;
				LeftWingExt[3].addBox("Wing|Left Wing Extended", 0.0F, 1.8F, 1.3F, 1, 8, 2, strech - 0.1F);
				LeftWingExt[3].setRotationPoint(f17, f18 + f, f19);
				LeftWingExt[4] = new ModelRenderer(this, 56, 19, VisiblePartFlags.HeadFlag, true, false);
				LeftWingExt[4].mirror = true;
				LeftWingExt[4].addBox("Wing|Left Wing Extended", 0.0F, 5F, 2.0F, 1, 8, 2, strech);
				LeftWingExt[4].setRotationPoint(f17, f18 + f, f19);
				LeftWingExt[5] = new ModelRenderer(this, 56, 19, VisiblePartFlags.HeadFlag, true, false);
				LeftWingExt[5].mirror = true;
				LeftWingExt[5].addBox("Wing|Left Wing Extended", 0.0F, 0.0F, -0.2F, 1, 6, 2, strech + 0.3F);
				LeftWingExt[5].setRotationPoint(f17, f18 + f, f19);
				LeftWingExt[6] = new ModelRenderer(this, 56, 19, VisiblePartFlags.HeadFlag, true, false);
				LeftWingExt[6].mirror = true;
				LeftWingExt[6].addBox("Wing|Left Wing Extended", 0.0F, 0.0F, 0.2F, 1, 3, 2, strech + 0.2F);
				LeftWingExt[6].setRotationPoint(f17, f18 + f, f19);
				float f20 = f2 - 4.5F;
				float f21 = f3 + 5F;
				float f22 = f4 + 6F;
				RightWingExt = new ModelRenderer[7];
				RightWingExt[0] = new ModelRenderer(this, 56, 19, VisiblePartFlags.HeadFlag, true, false);
				RightWingExt[0].mirror = true;
				RightWingExt[0].addBox("Wing|Right Wing Extended", 0.0F, 0.0F, 0.0F, 1, 8, 2, strech + 0.1F);
				RightWingExt[0].setRotationPoint(f20, f21 + f, f22);
				RightWingExt[1] = new ModelRenderer(this, 56, 19, VisiblePartFlags.HeadFlag, true, false);
				RightWingExt[1].mirror = true;
				RightWingExt[1].addBox("Wing|Right Wing Extended", 0.0F, 8F, 0.0F, 1, 6, 2, strech + 0.1F);
				RightWingExt[1].setRotationPoint(f20, f21 + f, f22);
				RightWingExt[2] = new ModelRenderer(this, 56, 19, VisiblePartFlags.HeadFlag, true, false);
				RightWingExt[2].mirror = true;
				RightWingExt[2].addBox("Wing|Right Wing Extended", 0.0F, -1.2F, -0.2F, 1, 8, 2, strech - 0.2F);
				RightWingExt[2].setRotationPoint(f20, f21 + f, f22);
				RightWingExt[3] = new ModelRenderer(this, 56, 19, VisiblePartFlags.HeadFlag, true, false);
				RightWingExt[3].mirror = true;
				RightWingExt[3].addBox("Wing|Right Wing Extended", 0.0F, 1.8F, 1.3F, 1, 8, 2, strech - 0.1F);
				RightWingExt[3].setRotationPoint(f20, f21 + f, f22);
				RightWingExt[4] = new ModelRenderer(this, 56, 19, VisiblePartFlags.HeadFlag, true, false);
				RightWingExt[4].mirror = true;
				RightWingExt[4].addBox("Wing|Right Wing Extended", 0.0F, 5F, 2.0F, 1, 8, 2, strech);
				RightWingExt[4].setRotationPoint(f20, f21 + f, f22);
				RightWingExt[5] = new ModelRenderer(this, 56, 19, VisiblePartFlags.HeadFlag, true, false);
				RightWingExt[5].mirror = true;
				RightWingExt[5].addBox("Wing|Right Wing Extended", 0.0F, 0.0F, -0.2F, 1, 6, 2, strech + 0.3F);
				RightWingExt[5].setRotationPoint(f20, f21 + f, f22);
				RightWingExt[6] = new ModelRenderer(this, 56, 19, VisiblePartFlags.HeadFlag, true, false);
				RightWingExt[6].mirror = true;
				RightWingExt[6].addBox("Wing|Right Wing Extended", 0.0F, 0.0F, 0.2F, 1, 3, 2, strech + 0.2F);
				RightWingExt[6].setRotationPoint(f20, f21 + f, f22);
				WingRotateAngleX = LeftWingExt[0].rotateAngleX;
				WingRotateAngleY = LeftWingExt[0].rotateAngleY;
				WingRotateAngleZ = LeftWingExt[0].rotateAngleZ;

				animate();

				return this;
			}

			public void animate()
			{
				bool isSleeping = false;
				bool isMale = true;
				bool isFlying = false;
				bool isPegasus = true;
				bool isUnicorn = true;
				bool issneak = false;
				int wantTail = 0;
				int heldItemRight = 0;
				bool aimedBow = false;

				float f = 0;
				float f1 = 0;
				float f2 = 0;
				float f3 = 0;
				float f4 = 0;
				float f5 = onGround;
				rainboom = false;
				float f6;
				float f7;
				if (isSleeping)
				{
					f6 = 1.4F;
					f7 = 0.1F;
				}
				else
				{
					f6 = f3 / 57.29578F;
					f7 = f4 / 57.29578F;
				}

				if (f7 > .5F)
				{
					f7 = .5F;
				}

				if (f7 < -.5F)
				{
					f7 = -.5F;
				}

				head.rotateAngleY = f6;
				head.rotateAngleX = f7;

				if (isMale)
				{
					for (int i = 0; i < MuzzleMale.Length; i++)
					{
						MuzzleMale[i].rotateAngleY = f6;
						MuzzleMale[i].rotateAngleX = f7;
					}
				}
				else
				{
					for (int i = 0; i < MuzzleFemale.Length; i++)
					{
						MuzzleFemale[i].rotateAngleY = f6;
						MuzzleFemale[i].rotateAngleX = f7;
					}
				}

				headpiece[0].rotateAngleY = f6;
				headpiece[0].rotateAngleX = f7;
				headpiece[1].rotateAngleY = f6;
				headpiece[1].rotateAngleX = f7;
				headpiece[2].rotateAngleY = f6;
				headpiece[2].rotateAngleX = f7;
				helmet.rotateAngleY = f6;
				helmet.rotateAngleX = f7;
				headpiece[2].rotateAngleX = f7 + 0.5F;
				float f8;
				float f9;
				float f10;
				float f11;
				if (!isFlying || !isPegasus)
				{
					f8 = (float)Math.Cos(f * 0.6662F + 3.141593F) * 0.6F * f1;
					f9 = (float)Math.Cos(f * 0.6662F) * 0.6F * f1;
					f10 = (float)Math.Cos(f * 0.6662F) * 0.3F * f1;
					f11 = (float)Math.Cos(f * 0.6662F + 3.141593F) * 0.3F * f1;
					rightarm.rotateAngleY = 0.0F;
					SteveArm.rotateAngleY = 0.0F;
					unicornarm.rotateAngleY = 0.0F;
					LeftArm.rotateAngleY = 0.0F;
					RightLeg.rotateAngleY = 0.0F;
					LeftLeg.rotateAngleY = 0.0F;
				}
				else
				{
					if (f1 < 0.9999F)
					{
						rainboom = false;
						f8 = (float)Math.Sin(0.0F - f1 * 0.5F);
						f9 = (float)Math.Sin(0.0F - f1 * 0.5F);
						f10 = (float)Math.Sin(f1 * 0.5F);
						f11 = (float)Math.Sin(f1 * 0.5F);
					}
					else
					{
						rainboom = true;
						f8 = 4.712F;
						f9 = 4.712F;
						f10 = 1.571F;
						f11 = 1.571F;
					}
					rightarm.rotateAngleY = 0.2F;
					SteveArm.rotateAngleY = 0.2F;
					LeftArm.rotateAngleY = -0.2F;
					RightLeg.rotateAngleY = -0.2F;
					LeftLeg.rotateAngleY = 0.2F;
				}
				if (isSleeping)
				{
					f8 = 4.712F;
					f9 = 4.712F;
					f10 = 1.571F;
					f11 = 1.571F;
				}
				rightarm.rotateAngleX = f8;
				SteveArm.rotateAngleX = f8;
				unicornarm.rotateAngleX = 0.0F;
				LeftArm.rotateAngleX = f9;
				RightLeg.rotateAngleX = f10;
				LeftLeg.rotateAngleX = f11;
				rightarm.rotateAngleZ = 0.0F;
				SteveArm.rotateAngleZ = 0.0F;
				unicornarm.rotateAngleZ = 0.0F;
				LeftArm.rotateAngleZ = 0.0F;

				tailstop = 0;
				tailstop = Tail.Length - (wantTail * 5);
				if (tailstop <= 1) { tailstop = 0; }
				for (int j = 0; j < tailstop; j++)
				{
					if (rainboom)
					{
						Tail[j].rotateAngleZ = 0.0F;
					}
					else
					{
						Tail[j].rotateAngleZ = (float)Math.Cos(f * 0.8F) * 0.2F * f1;
					}
				}

				if (heldItemRight != 0 && !rainboom && !isUnicorn)
				{
					rightarm.rotateAngleX = rightarm.rotateAngleX * 0.5F - 0.3141593F;
					SteveArm.rotateAngleX = SteveArm.rotateAngleX * 0.5F - 0.3141593F;
				}
				float f12 = 0.0F;
				if (f5 > -9990F && !isUnicorn)
				{
					f12 = (float)Math.Sin(Math.Sqrt(f5) * 3.141593F * 2.0F) * 0.2F;
				}
				Body.rotateAngleY = (float)((double)f12 * 0.20000000000000001D);
				for (int k = 0; k < Bodypiece.Length; k++)
				{
					Bodypiece[k].rotateAngleY = (float)((double)f12 * 0.20000000000000001D);
				}
				for (int kk = 0; kk < BodypieceNeck.Length; kk++)
				{
					BodypieceNeck[kk].rotateAngleY = (float)((double)f12 * 0.20000000000000001D);
				}

				for (int l = 0; l < LeftWing.Length; l++)
				{
					LeftWing[l].rotateAngleY = (float)((double)f12 * 0.20000000000000001D);
				}

				for (int i1 = 0; i1 < RightWing.Length; i1++)
				{
					RightWing[i1].rotateAngleY = (float)((double)f12 * 0.20000000000000001D);
				}

				tailstop = 0;
				tailstop = Tail.Length - (wantTail * 5);
				if (tailstop <= 1) { tailstop = 0; }
				for (int j1 = 0; j1 < tailstop; j1++)
				{
					Tail[j1].rotateAngleY = f12;
				}

				float f13 = (float)Math.Sin(Body.rotateAngleY) * 5F;
				float f14 = (float)Math.Cos(Body.rotateAngleY) * 5F;
				float f15 = 4F;
				if (issneak && !isFlying)
				{
					f15 = 0.0F;
				}
				if (isSleeping)
				{
					f15 = 2.6F;
				}
				if (rainboom)
				{
					rightarm.rotationPointZ = f13 + 2.0F;
					SteveArm.rotationPointZ = f13 + 2.0F;
					LeftArm.rotationPointZ = (0.0F - f13) + 2.0F;
				}
				else
				{
					rightarm.rotationPointZ = f13 + 1.0F;
					SteveArm.rotationPointZ = f13 + 1.0F;
					LeftArm.rotationPointZ = (0.0F - f13) + 1.0F;
				}
				rightarm.rotationPointX = (0.0F - f14 - 1.0F) + f15;
				SteveArm.rotationPointX = 0.0F - f14;
				LeftArm.rotationPointX = (f14 + 1.0F) - f15;
				RightLeg.rotationPointX = (0.0F - f14 - 1.0F) + f15;
				LeftLeg.rotationPointX = (f14 + 1.0F) - f15;
				rightarm.rotateAngleY += Body.rotateAngleY;
				LeftArm.rotateAngleY += Body.rotateAngleY;
				LeftArm.rotateAngleX += Body.rotateAngleY;
				rightarm.rotationPointY = 8F;
				LeftArm.rotationPointY = 8F;
				RightLeg.rotationPointY = 4F;
				LeftLeg.rotationPointY = 4F;
				if (f5 > -9990F)
				{
					float f16 = f5;
					f16 = 1.0F - f5;
					f16 *= f16 * f16;
					f16 = 1.0F - f16;
					float f22 = (float)Math.Sin(f16 * 3.141593F);
					float f28 = (float)Math.Sin(f5 * 3.141593F);
					float f33 = f28 * -(head.rotateAngleX - 0.7F) * 0.75F;
					if (isUnicorn)
					{
						unicornarm.rotateAngleX -= (float)((double)f22 * 1.2D + (double)f33);
						unicornarm.rotateAngleY += Body.rotateAngleY * 2.0F;
						unicornarm.rotateAngleZ = f28 * -0.4F;
					}
					else
					{
						rightarm.rotateAngleX -= (float)((double)f22 * 1.2D + (double)f33);
						rightarm.rotateAngleY += Body.rotateAngleY * 2.0F;
						rightarm.rotateAngleZ = f28 * -0.4F;
						SteveArm.rotateAngleX -= (float)((double)f22 * 1.2D + (double)f33);
						SteveArm.rotateAngleY += Body.rotateAngleY * 2.0F;
						SteveArm.rotateAngleZ = f28 * -0.4F;
					}
				}
				if (issneak && !isFlying)
				{
					float f17 = 0.4F;
					float f23 = 7F;
					float f29 = -4F;
					Body.rotateAngleX = f17;
					Body.rotationPointY = f23;
					Body.rotationPointZ = f29;
					for (int k3 = 0; k3 < Bodypiece.Length; k3++)
					{
						Bodypiece[k3].rotateAngleX = f17;
						Bodypiece[k3].rotationPointY = f23;
						Bodypiece[k3].rotationPointZ = f29;
					}

					for (int k3 = 0; k3 < BodypieceNeck.Length; k3++)
					{
						BodypieceNeck[k3].rotateAngleX = NeckRotX + f17;
						BodypieceNeck[k3].rotationPointY = f23;
						BodypieceNeck[k3].rotationPointZ = f29;
					}

					float f34 = 3.5F;
					float f37 = 6F;
					for (int k4 = 0; k4 < LeftWingExt.Length; k4++)
					{
						LeftWingExt[k4].rotateAngleX = (float)((double)f17 + 2.3561947345733643D);
						LeftWingExt[k4].rotationPointY = f23 + f34;
						LeftWingExt[k4].rotationPointZ = f29 + f37;
						LeftWingExt[k4].rotateAngleX = 2.5F;
						LeftWingExt[k4].rotateAngleZ = -6F;
					}

					float f40 = 4.5F;
					float f43 = 6F;
					for (int k5 = 0; k5 < LeftWingExt.Length; k5++)
					{
						RightWingExt[k5].rotateAngleX = (float)((double)f17 + 2.3561947345733643D);
						RightWingExt[k5].rotationPointY = f23 + f40;
						RightWingExt[k5].rotationPointZ = f29 + f43;
						RightWingExt[k5].rotateAngleX = 2.5F;
						RightWingExt[k5].rotateAngleZ = 6F;
					}

					RightLeg.rotateAngleX -= 0.0F;
					LeftLeg.rotateAngleX -= 0.0F;
					rightarm.rotateAngleX -= 0.4F;
					SteveArm.rotateAngleX += 0.4F;
					unicornarm.rotateAngleX += 0.4F;
					LeftArm.rotateAngleX -= 0.4F;
					RightLeg.rotationPointZ = 10F;
					LeftLeg.rotationPointZ = 10F;
					RightLeg.rotationPointY = 7F;
					LeftLeg.rotationPointY = 7F;
					float f46;
					float f48;
					float f50;
					if (isSleeping)
					{
						f46 = 2.0F;
						f48 = -1F;
						f50 = 1.0F;
					}
					else
					{
						f46 = 6F;
						f48 = -2F;
						f50 = 0.0F;
					}
					head.rotationPointY = f46;
					head.rotationPointZ = f48;
					head.rotationPointX = f50;
					helmet.rotationPointY = f46;
					helmet.rotationPointZ = f48;
					helmet.rotationPointX = f50;
					headpiece[0].rotationPointY = f46;
					headpiece[0].rotationPointZ = f48;
					headpiece[0].rotationPointX = f50;
					headpiece[1].rotationPointY = f46;
					headpiece[1].rotationPointZ = f48;
					headpiece[1].rotationPointX = f50;
					headpiece[2].rotationPointY = f46;
					headpiece[2].rotationPointZ = f48;
					headpiece[2].rotationPointX = f50;

					if (isMale)
					{
						for (int j6 = 0; j6 < MuzzleMale.Length; j6++)
						{
							MuzzleMale[j6].rotationPointY = f46;
							MuzzleMale[j6].rotationPointX = f50;
							MuzzleMale[j6].rotationPointZ = f48;
						}
					}
					else
					{
						for (int j6 = 0; j6 < MuzzleFemale.Length; j6++)
						{
							MuzzleFemale[j6].rotationPointY = f46;
							MuzzleFemale[j6].rotationPointX = f50;
							MuzzleFemale[j6].rotationPointZ = f48;
						}
					}

					float f52 = 0.0F;
					float f54 = 8F;
					float f56 = -14F;
					float f58 = 0.0F - f52;
					float f60 = 8.8F - f54;
					float f62 = -4F - f56;
					float f63 = 0.0F;
					tailstop = 0;
					tailstop = Tail.Length - (wantTail * 5);
					if (tailstop <= 1) { tailstop = 0; }
					for (int i7 = 0; i7 < tailstop; i7++)
					{
						Tail[i7].rotationPointX = f58;
						Tail[i7].rotationPointY = f60;
						Tail[i7].rotationPointZ = f62;
						Tail[i7].rotateAngleX = f63;
					}

				}
				else
				{
					float f18 = 0.0F;
					float f24 = 0.0F;
					float f30 = 0.0F;
					Body.rotateAngleX = f18;
					Body.rotationPointY = f24;
					Body.rotationPointZ = f30;
					for (int l3 = 0; l3 < Bodypiece.Length; l3++)
					{
						Bodypiece[l3].rotateAngleX = f18;
						Bodypiece[l3].rotationPointY = f24;
						Bodypiece[l3].rotationPointZ = f30;
					}
					for (int l3 = 0; l3 < BodypieceNeck.Length; l3++)
					{
						BodypieceNeck[l3].rotateAngleX = NeckRotX + f18;
						BodypieceNeck[l3].rotationPointY = f24;
						BodypieceNeck[l3].rotationPointZ = f30;
					}


					if (isPegasus)
					{
						if (!isFlying)
						{
							for (int i4 = 0; i4 < LeftWing.Length; i4++)
							{
								LeftWing[i4].rotateAngleX = (float)((double)f18 + 1.5707964897155762D);
								LeftWing[i4].rotationPointY = f24 + 13F;
								LeftWing[i4].rotationPointZ = f30 - 3F;
							}

							for (int j4 = 0; j4 < RightWing.Length; j4++)
							{
								RightWing[j4].rotateAngleX = (float)((double)f18 + 1.5707964897155762D);
								RightWing[j4].rotationPointY = f24 + 13F;
								RightWing[j4].rotationPointZ = f30 - 3F;
							}

						}
						else
						{
							float f35 = 5.5F;
							float f38 = 3F;
							for (int l4 = 0; l4 < LeftWingExt.Length; l4++)
							{
								LeftWingExt[l4].rotateAngleX = (float)((double)f18 + 1.5707964897155762D);
								LeftWingExt[l4].rotationPointY = f24 + f35;
								LeftWingExt[l4].rotationPointZ = f30 + f38;
							}

							float f41 = 6.5F;
							float f44 = 3F;
							for (int l5 = 0; l5 < RightWingExt.Length; l5++)
							{
								RightWingExt[l5].rotateAngleX = (float)((double)f18 + 1.5707964897155762D);
								RightWingExt[l5].rotationPointY = f24 + f41;
								RightWingExt[l5].rotationPointZ = f30 + f44;
							}

						}
					}
					RightLeg.rotationPointZ = 10F;
					LeftLeg.rotationPointZ = 10F;
					RightLeg.rotationPointY = 8F;
					LeftLeg.rotationPointY = 8F;
					float f36 = (float)Math.Cos(f2 * 0.09F) * 0.05F + 0.05F;
					float f39 = (float)Math.Sin(f2 * 0.067F) * 0.05F;
					SteveArm.rotateAngleZ += f36;
					unicornarm.rotateAngleZ += f36;
					SteveArm.rotateAngleX += f39;
					unicornarm.rotateAngleX += f39;
					if (isPegasus && isFlying)
					{
						WingRotateAngleY = (float)Math.Sin(f2 * 0.067F * 8F) * 1.0F;
						WingRotateAngleZ = (float)Math.Sin(f2 * 0.067F * 8F) * 1.0F;
						for (int i5 = 0; i5 < LeftWingExt.Length; i5++)
						{
							LeftWingExt[i5].rotateAngleX = 2.5F;
							LeftWingExt[i5].rotateAngleZ = -WingRotateAngleZ - 4.712F - 0.4F;
						}

						for (int j5 = 0; j5 < RightWingExt.Length; j5++)
						{
							RightWingExt[j5].rotateAngleX = 2.5F;
							RightWingExt[j5].rotateAngleZ = WingRotateAngleZ + 4.712F + 0.4F;
						}

					}
					float f42;
					float f45;
					float f47;
					if (isSleeping)
					{
						f42 = 2.0F;
						f45 = 1.0F;
						f47 = 1.0F;
					}
					else
					{
						f42 = 0.0F;
						f45 = 0.0F;
						f47 = 0.0F;
					}
					head.rotationPointY = f42;
					head.rotationPointZ = f45;
					head.rotationPointX = f47;
					helmet.rotationPointY = f42;
					helmet.rotationPointZ = f45;
					helmet.rotationPointX = f47;
					headpiece[0].rotationPointY = f42;
					headpiece[0].rotationPointZ = f45;
					headpiece[0].rotationPointX = f47;
					headpiece[1].rotationPointY = f42;
					headpiece[1].rotationPointZ = f45;
					headpiece[1].rotationPointX = f47;
					headpiece[2].rotationPointY = f42;
					headpiece[2].rotationPointZ = f45;
					headpiece[2].rotationPointX = f47;

					if (isMale)
					{
						for (int i6 = 0; i6 < MuzzleMale.Length; i6++)
						{
							MuzzleMale[i6].rotationPointY = f42;
							MuzzleMale[i6].rotationPointX = f45;
							MuzzleMale[i6].rotationPointZ = f47;
						}
					}
					else
					{
						for (int i6 = 0; i6 < MuzzleFemale.Length; i6++)
						{
							MuzzleFemale[i6].rotationPointY = f42;
							MuzzleFemale[i6].rotationPointX = f45;
							MuzzleFemale[i6].rotationPointZ = f47;
						}
					}

					float f49 = 0.0F;
					float f51 = 8F;
					float f53 = -14F;
					float f55 = 0.0F - f49;
					float f57 = 8.8F - f51;
					float f59 = 0.0F - f53;
					float f61 = 0.5F * f1;
					tailstop = 0;
					tailstop = Tail.Length - (wantTail * 5);
					if (tailstop <= 1) { tailstop = 0; }
					for (int k6 = 0; k6 < tailstop; k6++)
					{
						Tail[k6].rotationPointX = f55;
						Tail[k6].rotationPointY = f57;
						Tail[k6].rotationPointZ = f59;
						if (rainboom)
						{
							Tail[k6].rotateAngleX = 1.571F + 0.1F * (float)Math.Sin(f);
						}
						else
						{
							Tail[k6].rotateAngleX = f61;
						}
					}

					tailstop = 0;
					tailstop = Tail.Length - (wantTail * 5);
					if (tailstop <= 1) { tailstop = 0; }
					for (int l6 = 0; l6 < tailstop; l6++)
					{
						if (!rainboom)
						{
							Tail[l6].rotateAngleX += f39;
						}
					}

				}
				LeftWingExt[2].rotateAngleX = LeftWingExt[2].rotateAngleX - 0.85F;
				LeftWingExt[3].rotateAngleX = LeftWingExt[3].rotateAngleX - 0.75F;
				LeftWingExt[4].rotateAngleX = LeftWingExt[4].rotateAngleX - 0.5F;
				LeftWingExt[6].rotateAngleX = LeftWingExt[6].rotateAngleX - 0.85F;
				RightWingExt[2].rotateAngleX = RightWingExt[2].rotateAngleX - 0.85F;
				RightWingExt[3].rotateAngleX = RightWingExt[3].rotateAngleX - 0.75F;
				RightWingExt[4].rotateAngleX = RightWingExt[4].rotateAngleX - 0.5F;
				RightWingExt[6].rotateAngleX = RightWingExt[6].rotateAngleX - 0.85F;
				Bodypiece[9].rotateAngleX = Bodypiece[9].rotateAngleX + 0.5F;
				Bodypiece[10].rotateAngleX = Bodypiece[10].rotateAngleX + 0.5F;
				Bodypiece[11].rotateAngleX = Bodypiece[11].rotateAngleX + 0.5F;
				Bodypiece[12].rotateAngleX = Bodypiece[12].rotateAngleX + 0.5F;
				Bodypiece[13].rotateAngleX = Bodypiece[13].rotateAngleX + 0.5F;

				if (rainboom)
				{
					tailstop = 0;
					tailstop = Tail.Length - (wantTail * 5);
					if (tailstop <= 1) { tailstop = 0; }
					for (int k1 = 0; k1 < tailstop; k1++)
					{
						Tail[k1].rotationPointY = Tail[k1].rotationPointY + 6F;
						Tail[k1].rotationPointZ = Tail[k1].rotationPointZ + 1.0F;
					}

				}
				if (isRiding)
				{
					float f19 = -10F;
					float f25 = -10F;
					head.rotationPointY = head.rotationPointY + f19;
					head.rotationPointZ = head.rotationPointZ + f25;
					headpiece[0].rotationPointY = headpiece[0].rotationPointY + f19;
					headpiece[0].rotationPointZ = headpiece[0].rotationPointZ + f25;
					headpiece[1].rotationPointY = headpiece[1].rotationPointY + f19;
					headpiece[1].rotationPointZ = headpiece[1].rotationPointZ + f25;
					headpiece[2].rotationPointY = headpiece[2].rotationPointY + f19;
					headpiece[2].rotationPointZ = headpiece[2].rotationPointZ + f25;

					if (isMale)
					{
						for (int l1 = 0; l1 < MuzzleMale.Length; l1++)
						{
							MuzzleMale[l1].rotationPointY = MuzzleMale[l1].rotationPointY + f19;
							MuzzleMale[l1].rotationPointZ = MuzzleMale[l1].rotationPointZ + f25;
						}
					}
					else
					{
						for (int l1 = 0; l1 < MuzzleFemale.Length; l1++)
						{
							MuzzleFemale[l1].rotationPointY = MuzzleFemale[l1].rotationPointY + f19;
							MuzzleFemale[l1].rotationPointZ = MuzzleFemale[l1].rotationPointZ + f25;
						}
					}

					helmet.rotationPointY = helmet.rotationPointY + f19;
					helmet.rotationPointZ = helmet.rotationPointZ + f25;
					Body.rotationPointY = Body.rotationPointY + f19;
					Body.rotationPointZ = Body.rotationPointZ + f25;
					for (int i2 = 0; i2 < Bodypiece.Length; i2++)
					{
						Bodypiece[i2].rotationPointY = Bodypiece[i2].rotationPointY + f19;
						Bodypiece[i2].rotationPointZ = Bodypiece[i2].rotationPointZ + f25;
					}

					LeftArm.rotationPointY = LeftArm.rotationPointY + f19;
					LeftArm.rotationPointZ = LeftArm.rotationPointZ + f25;
					rightarm.rotationPointY = rightarm.rotationPointY + f19;
					rightarm.rotationPointZ = rightarm.rotationPointZ + f25;
					LeftLeg.rotationPointY = LeftLeg.rotationPointY + f19;
					LeftLeg.rotationPointZ = LeftLeg.rotationPointZ + f25;
					RightLeg.rotationPointY = RightLeg.rotationPointY + f19;
					RightLeg.rotationPointZ = RightLeg.rotationPointZ + f25;
					tailstop = 0;
					tailstop = Tail.Length - (wantTail * 5);
					if (tailstop <= 1) { tailstop = 0; }
					for (int j2 = 0; j2 < tailstop; j2++)
					{
						Tail[j2].rotationPointY = Tail[j2].rotationPointY + f19;
						Tail[j2].rotationPointZ = Tail[j2].rotationPointZ + f25;
					}

					for (int k2 = 0; k2 < LeftWing.Length; k2++)
					{
						LeftWing[k2].rotationPointY = LeftWing[k2].rotationPointY + f19;
						LeftWing[k2].rotationPointZ = LeftWing[k2].rotationPointZ + f25;
					}

					for (int l2 = 0; l2 < RightWing.Length; l2++)
					{
						RightWing[l2].rotationPointY = RightWing[l2].rotationPointY + f19;
						RightWing[l2].rotationPointZ = RightWing[l2].rotationPointZ + f25;
					}

					for (int i3 = 0; i3 < LeftWingExt.Length; i3++)
					{
						LeftWingExt[i3].rotationPointY = LeftWingExt[i3].rotationPointY + f19;
						LeftWingExt[i3].rotationPointZ = LeftWingExt[i3].rotationPointZ + f25;
					}

					for (int j3 = 0; j3 < RightWingExt.Length; j3++)
					{
						RightWingExt[j3].rotationPointY = RightWingExt[j3].rotationPointY + f19;
						RightWingExt[j3].rotationPointZ = RightWingExt[j3].rotationPointZ + f25;
					}

				}
				if (isSleeping)
				{
					rightarm.rotationPointZ = rightarm.rotationPointZ + 6F;
					LeftArm.rotationPointZ = LeftArm.rotationPointZ + 6F;
					RightLeg.rotationPointZ = RightLeg.rotationPointZ - 8F;
					LeftLeg.rotationPointZ = LeftLeg.rotationPointZ - 8F;
					rightarm.rotationPointY = rightarm.rotationPointY + 2.0F;
					LeftArm.rotationPointY = LeftArm.rotationPointY + 2.0F;
					RightLeg.rotationPointY = RightLeg.rotationPointY + 2.0F;
					LeftLeg.rotationPointY = LeftLeg.rotationPointY + 2.0F;
				}
				if (aimedBow)
				{
					if (isUnicorn)
					{
						float f20 = 0.0F;
						float f26 = 0.0F;
						unicornarm.rotateAngleZ = 0.0F;
						unicornarm.rotateAngleY = -(0.1F - f20 * 0.6F) + head.rotateAngleY;
						unicornarm.rotateAngleX = 4.712F + head.rotateAngleX;
						unicornarm.rotateAngleX -= f20 * 1.2F - f26 * 0.4F;
						float f31 = 0;
						unicornarm.rotateAngleZ += (float)Math.Cos(f31 * 0.09F) * 0.05F + 0.05F;
						unicornarm.rotateAngleX += (float)Math.Sin(f31 * 0.067F) * 0.05F;
					}
					else
					{
						float f21 = 0.0F;
						float f27 = 0.0F;
						rightarm.rotateAngleZ = 0.0F;
						rightarm.rotateAngleY = -(0.1F - f21 * 0.6F) + head.rotateAngleY;
						rightarm.rotateAngleX = 4.712F + head.rotateAngleX;
						rightarm.rotateAngleX -= f21 * 1.2F - f27 * 0.4F;
						float f32 = 0;
						rightarm.rotateAngleZ += (float)Math.Cos(f32 * 0.09F) * 0.05F + 0.05F;
						rightarm.rotateAngleX += (float)Math.Sin(f32 * 0.067F) * 0.05F;
						rightarm.rotationPointZ = rightarm.rotationPointZ + 1.0F;
					}
				}
			}

			/*
			public void render(AniParams aniparams, boolean flag)
			{
					if(flag)
					{
							head.render(scale);
							headpiece[0].render(scale);
							headpiece[1].render(scale);
							if(isUnicorn)
							{
									headpiece[2].render(scale);
							}

					if(isMale)
					{
						for(int i = 0; i < MuzzleMale.length; i++)
						{
							MuzzleMale[i].render(scale);
						}
					} else {
						for(int i = 0; i < MuzzleFemale.length; i++)
						{
							MuzzleFemale[i].render(scale);
						}
					}

							helmet.render(scale);
							Body.render(scale);
							for(int j = 0; j < Bodypiece.length; j++)
							{
									Bodypiece[j].render(scale);
							}

							for(int j = 0; j < BodypieceNeck.length; j++)
							{
									BodypieceNeck[j].render(scale);
							}            
            
							LeftArm.render(scale);
							rightarm.render(scale);
							LeftLeg.render(scale);
							RightLeg.render(scale);
					int tailstop = 0;
					tailstop = Tail.length - (wantTail * 5);
					if(tailstop <= 1) {tailstop = 0;}
							for(int k = 0; k < tailstop; k++)
							{
									Tail[k].render(scale);
							}

							if(isPegasus)
							{
									if(isFlying || issneak)
									{
											for(int l = 0; l < LeftWingExt.length; l++)
											{
													LeftWingExt[l].render(scale);
											}

											for(int i1 = 0; i1 < RightWingExt.length; i1++)
											{
													RightWingExt[i1].render(scale);
											}

									} else
									{
											for(int j1 = 0; j1 < LeftWing.length; j1++)
											{
													LeftWing[j1].render(scale);
											}

											for(int k1 = 0; k1 < RightWing.length; k1++)
											{
													RightWing[k1].render(scale);
											}

									}
							}
					} else
					{
							SteveArm.render(scale);
					}
			}

			public void specials(RenderManager rendermanager, EntityPlayer entityplayer)
			{
					if(!isSleeping)
					{
							if(isUnicorn)
							{
									if(aimedBow)
									{
											renderDrop(rendermanager, entityplayer, unicornarm, 1.0F, 0.15F, 0.9375F, 0.0625F);
									} else
									{
											renderDrop(rendermanager, entityplayer, unicornarm, 1.0F, 0.35F, 0.5375F, -0.45F);
									}
							} else
							{
									renderDrop(rendermanager, entityplayer, rightarm, 1.0F, -0.0625F, 0.8375F, 0.0625F);
							}
					}
					renderPumpkin(rendermanager, entityplayer, head, 0.625F, 0.0F, -0.08F, -0.15F);
			}*/
		}

		public class ModelOzelot : ModelBase
		{
			ModelRenderer a;
			ModelRenderer b;
			ModelRenderer c;
			ModelRenderer d;
			ModelRenderer e;
			ModelRenderer f;
			ModelRenderer g;
			ModelRenderer n;
			int o;

			public ModelOzelot()
			{
				o = 0;
				g = new ModelRenderer(this, "head", VisiblePartFlags.HeadFlag, false, false);
				g.setTextureOffset(0, 0).addBox("main", -2.5F, -2F, -3F, 5, 4, 5);
				g.setTextureOffset(0, 24).addBox("nose", -1.5F, 0.0F, -4F, 3, 2, 2);
				g.setTextureOffset(0, 10).addBox("ear1", -2F, -3F, 0.0F, 1, 1, 2);
				g.setTextureOffset(6, 10).addBox("ear2", 1.0F, -3F, 0.0F, 1, 1, 2);
				g.setRotationPoint(0.0F, 15F, -9F);
				n = new ModelRenderer(this, 20, 0, VisiblePartFlags.HeadFlag, false, false);
				n.addBox("Body", -2F, 3F, -8F, 4, 16, 6, 0.0F);
				n.setRotationPoint(0.0F, 12F, -10F);
				e = new ModelRenderer(this, 0, 15, VisiblePartFlags.HeadFlag, false, false);
				e.addBox("Tail", -0.5F, 0.0F, 0.0F, 1, 8, 1);
				e.rotateAngleX = 0.9F;
				e.setRotationPoint(0.0F, 15F, 8F);
				f = new ModelRenderer(this, 4, 15, VisiblePartFlags.HeadFlag, false, false);
				f.addBox("Tail", -0.5F, 0.0F, 0.0F, 1, 8, 1);
				f.setRotationPoint(0.0F, 20F, 14F);
				a = new ModelRenderer(this, 8, 13, VisiblePartFlags.HeadFlag, false, false);
				a.addBox("Leg", -1F, 0.0F, 1.0F, 2, 6, 2);
				a.setRotationPoint(1.1F, 18F, 5F);
				b = new ModelRenderer(this, 8, 13, VisiblePartFlags.HeadFlag, false, false);
				b.addBox("Leg", -1F, 0.0F, 1.0F, 2, 6, 2);
				b.setRotationPoint(-1.1F, 18F, 5F);
				c = new ModelRenderer(this, 40, 0, VisiblePartFlags.HeadFlag, false, false);
				c.addBox("Leg", -1F, 0.0F, 0.0F, 2, 10, 2);
				c.setRotationPoint(1.2F, 13.8F, -5F);
				d = new ModelRenderer(this, 40, 0, VisiblePartFlags.HeadFlag, false, false);
				d.addBox("Leg", -1F, 0.0F, 0.0F, 2, 10, 2);
				d.setRotationPoint(-1.2F, 13.8F, -5F);

				setup2(0, 0, 0, 0, 0, 0);
			}

			public void setup2(float f1, float f2, float f3, float f4, float f5, float f6)
			{
				setup(f1, f2, f3, f4, f5, f6);
				setup3(f1, f2, f3);
			}

			public void setup(float f1, float f2, float f3, float f4, float f5, float f6)
			{
				g.rotateAngleX = f5 / 57.29578F;
				g.rotateAngleY = f4 / 57.29578F;
				if (o != 3)
				{
					n.rotateAngleX = 1.570796F;
					if (o == 2)
					{
						a.rotateAngleX = (f1 * 0.6662F) * 1.0F * f2;
						b.rotateAngleX = (f1 * 0.6662F + 0.3F) * 1.0F * f2;
						c.rotateAngleX = (f1 * 0.6662F + 3.141593F + 0.3F) * 1.0F * f2;
						d.rotateAngleX = (f1 * 0.6662F + 3.141593F) * 1.0F * f2;
						f.rotateAngleX = 1.727876F + 0.3141593F * (f1) * f2;
					}
					else
					{
						a.rotateAngleX = (f1 * 0.6662F) * 1.0F * f2;
						b.rotateAngleX = (f1 * 0.6662F + 3.141593F) * 1.0F * f2;
						c.rotateAngleX = (f1 * 0.6662F + 3.141593F) * 1.0F * f2;
						d.rotateAngleX = (f1 * 0.6662F) * 1.0F * f2;
						if (o == 1)
						{
							f.rotateAngleX = 1.727876F + 0.7853982F * (f1) * f2;
						}
						else
						{
							f.rotateAngleX = 1.727876F + 0.4712389F * (f1) * f2;
						}
					}
				}
			}

			public void setup3(float f1, float f2, float f3)
			{
				if (/*itemfishingrod.Z()*/true)
				{
					n.rotationPointY = 13F;
					g.rotationPointY = 17F;
					e.rotationPointY = 16F;
					e.rotationPointZ = 8F;
					f.rotationPointY = 16F;
					f.rotationPointZ = 16F;
					e.rotateAngleX = 1.570796F;
					f.rotateAngleX = 1.570796F;
					c.rotationPointY = d.rotationPointY = 13.8F;
					a.rotationPointY = b.rotationPointY = 18F;
					a.rotationPointZ = b.rotationPointZ = 5F;
					o = 0;
				}
				else if (/*itemfishingrod.aa()*/false)
				{
					n.rotationPointY = 12F;
					g.rotationPointY = 15F;
					e.rotationPointY = 15F;
					f.rotationPointY = 15F;
					f.rotationPointZ = 16F;
					e.rotateAngleX = 1.570796F;
					f.rotateAngleX = 1.570796F;
					c.rotationPointY = d.rotationPointY = 13.8F;
					a.rotationPointY = b.rotationPointY = 18F;
					a.rotationPointZ = b.rotationPointZ = 5F;
					o = 2;
				}
				else if (/*itemfishingrod.ah()*/false)
				{
					n.rotateAngleX = 0.7853982F;
					n.rotationPointY = 8F;
					n.rotationPointZ = -5F;
					g.rotationPointY = 11.7F;
					g.rotationPointZ = -8F;
					e.rotationPointY = 23F;
					e.rotationPointZ = 6F;
					f.rotationPointY = 22F;
					f.rotationPointZ = 13.2F;
					e.rotateAngleX = 1.727876F;
					f.rotateAngleX = 2.670354F;
					a.rotateAngleX = b.rotateAngleX = -1.570796F;
					a.rotationPointZ = b.rotationPointZ = 4F;
					c.rotateAngleX = d.rotateAngleX = -0.1570796F;
					c.rotationPointY = d.rotationPointY = 15.8F;
					c.rotationPointZ = d.rotationPointZ = -7F;
					a.rotationPointY = b.rotationPointY = 21F;
					a.rotationPointZ = b.rotationPointZ = 1.0F;
					o = 3;
				}
				else
				{
					n.rotationPointY = 12F;
					n.rotationPointZ = -10F;
					g.rotationPointY = 15F;
					g.rotationPointZ = -9F;
					e.rotationPointY = 15F;
					e.rotationPointZ = 8F;
					f.rotationPointY = 20F;
					f.rotationPointZ = 14F;
					c.rotationPointY = d.rotationPointY = 13.8F;
					a.rotationPointY = b.rotationPointY = 18F;
					a.rotationPointZ = b.rotationPointZ = 5F;
					e.rotateAngleX = 0.9F;
					o = 1;
				}
			}
		}

		public class ModelGolem : ModelBase
		{
			public ModelRenderer a;
			public ModelRenderer b;
			public ModelRenderer c;
			public ModelRenderer d;
			public ModelRenderer e;
			public ModelRenderer f;

			public ModelGolem() :
				this (0.0f)
			{
			}

			public ModelGolem(float f1) :
				this(f1, -7F)
			{
			}

			public ModelGolem(float f1, float f2)
			{
				byte byte0 = 96;
				byte byte1 = 96;
				a = (new ModelRenderer(this, VisiblePartFlags.ChestFlag, false, false)).setTextureSize(byte0, byte1);
				a.setRotationPoint(0.0F, 0.0F + f2, 0.0F);
				a.setTextureOffset(0, 0).addBox("Head", -4F, -12F, -7.5F, 8, 10, 8, f1);
				a.setTextureOffset(24, 0).addBox("Nose", -1F, -5F, -9.5F, 2, 4, 2, f1);
				b = (new ModelRenderer(this, VisiblePartFlags.ChestFlag, false, false)).setTextureSize(byte0, byte1);
				b.setRotationPoint(0.0F, 0.0F + f2, 0.0F);
				b.setTextureOffset(0, 40).addBox("Chest", -9F, -2F, -6F, 18, 12, 11, f1);
				b.setTextureOffset(0, 70).addBox("Stomach", -4.5F, 10F, -3F, 9, 5, 6, f1 + 0.5F);
				c = (new ModelRenderer(this, VisiblePartFlags.ChestFlag, false, false)).setTextureSize(byte0, byte1);
				c.setRotationPoint(0.0F, -7F, 0.0F);
				c.setTextureOffset(60, 21).addBox("Right Arm", -13F, -2.5F, -3F, 4, 30, 6, f1);
				d = (new ModelRenderer(this, VisiblePartFlags.ChestFlag, false, false)).setTextureSize(byte0, byte1);
				d.setRotationPoint(0.0F, -7F, 0.0F);
				d.setTextureOffset(60, 58).addBox("Left Arm", 9F, -2.5F, -3F, 4, 30, 6, f1);
				e = (new ModelRenderer(this, 0, 22, VisiblePartFlags.ChestFlag, false, false)).setTextureSize(byte0, byte1);
				e.setRotationPoint(-4F, 18F + f2, 0.0F);
				e.setTextureOffset(37, 0).addBox("Right Leg", -3.5F, -3F, -3F, 6, 16, 5, f1);
				f = (new ModelRenderer(this, 0, 22, VisiblePartFlags.ChestFlag, false, false)).setTextureSize(byte0, byte1);
				f.mirror = true;
				f.setTextureOffset(60, 0).setRotationPoint(4F, 18F + f2, 0.0F);
				f.addBox("Left Leg", -3.5F, -3F, -3F, 6, 16, 5, f1);

				setup1(0, 0, 0, 0, 0, 0);
				setup2(0, 0, 0);
			}

			public void setup1(float f1, float f2, float f3, float f4, float f5, float f6)
			{
				a.rotateAngleY = f4 / 57.29578F;
				a.rotateAngleX = f5 / 57.29578F;
				e.rotateAngleX = -1.5F * getValue(f1, 13F) * f2;
				f.rotateAngleX = 1.5F * getValue(f1, 13F) * f2;
				e.rotateAngleY = 0.0F;
				f.rotateAngleY = 0.0F;
			}

			public void setup2(float f1, float f2, float f3)
			{
				int i = 0;
				if (i > 0)
				{
					c.rotateAngleX = -2F + 1.5F * getValue((float)i - f3, 10F);
					d.rotateAngleX = -2F + 1.5F * getValue((float)i - f3, 10F);
				}
				else
				{
					int j = 0;//tg1.D_();
					if (j > 0)
					{
						c.rotateAngleX = -0.8F + 0.025F * getValue(j, 70F);
						d.rotateAngleX = 0.0F;
					}
					else
					{
						c.rotateAngleX = (-0.2F + 1.5F * getValue(f1, 13F)) * f2;
						d.rotateAngleX = (-0.2F - 1.5F * getValue(f1, 13F)) * f2;
					}
				}
			}

			private float getValue(float f1, float f2)
			{
				return (Math.Abs(f1 % f2 - f2 * 0.5F) - f2 * 0.25F) / (f2 * 0.25F);
			}
		}


		public static void LoadModels()
		{
			new ModelPig().Save("Pig", 1, 64, 32, "Models\\Mobs\\Passive\\Pig.xml");
			new ModelBiped().Save("Human", 1, 64, 32, "Models\\Mobs\\Passive\\Human.xml");
			new ModelVillager().Save("Villager", 1, 64, 64, "Models\\Mobs\\Passive\\Villager.xml");
			new ModelCow().Save("Cow", 1, 64, 32, "Models\\Mobs\\Passive\\Cow.xml");
			new ModelChicken().Save("Chicken", 1, 64, 32, "Models\\Mobs\\Passive\\Chicken.xml");
			new ModelSquid().Save("Squid", 1, 64, 32, "Models\\Mobs\\Passive\\Squid.xml");
			new ModelWolf().Save("Wolf", 1, 64, 32, "Models\\Mobs\\Passive\\Wolf.xml");
			new ModelSheep1().Save("Sheep Fur", 1, 64, 32, "Models\\Mobs\\Passive\\Sheep Fur.xml");
			new ModelSheep2().Save("Sheep", 1, 64, 32, "Models\\Mobs\\Passive\\Sheep.xml");
			new ModelSnowMan().Save("SnowMan", 1, 64, 64, "Models\\Mobs\\Passive\\SnowMan.xml");

			new ModelChest().Save("Chest", 1, 64, 64, "Models\\Other\\Chest.xml");
			new ModelLargeChest().Save("Large Chest", 1, 128, 64, "Models\\Other\\LargeChest.xml");
			new ModelBoat().Save("Boat", 1, 64, 32, "Models\\Other\\Boat.xml");
			new SignModel().Save("Sign", 1, 64, 32, "Models\\Other\\Sign.xml");
			new ModelBook().Save("Book", 1, 64, 32, "Models\\Other\\Book.xml");
			new ModelMinecart().Save("Minecart", 1, 64, 32, "Models\\Other\\Minecart.xml");
			new ModelEnderCrystal().Save("Ender Crystal", 1, 128, 64, "Models\\Other\\EnderCrystal.xml");

			new ModelCreeper().Save("Creeper", 1, 64, 32, "Models\\Mobs\\Hostile\\Creeper.xml");
			new ModelSlime(0).Save("Tiny Slime", 1, 64, 32, "Models\\Mobs\\Hostile\\TinySlime.xml");
			new ModelSlime(16).Save("Small Slime", 2, 64, 32, "Models\\Mobs\\Hostile\\SmallSlime.xml");
			new ModelSlime(16).Save("Medium Slime", 3, 64, 32, "Models\\Mobs\\Hostile\\MediumSlime.xml");
			new ModelSlime(16).Save("Huge Slime", 4, 64, 32, "Models\\Mobs\\Hostile\\HugeSlime.xml");
			new ModelMagmaCube().Save("Tiny Magma Cube", 1, 64, 32, "Models\\Mobs\\Hostile\\TinyMagmaCube.xml");
			new ModelMagmaCube().Save("Small Magma Cube", 2, 64, 32, "Models\\Mobs\\Hostile\\SmallMagmaCube.xml");
			new ModelMagmaCube().Save("Medium Magma Cube", 3, 64, 32, "Models\\Mobs\\Hostile\\MediumMagmaCube.xml");
			new ModelMagmaCube().Save("Huge Magma Cube", 4, 64, 32, "Models\\Mobs\\Hostile\\HugeMagmaCube.xml");
			new ModelBlaze().Save("Blaze", 1, 64, 32, "Models\\Mobs\\Hostile\\Blaze.xml");
			new ModelSilverfish().Save("Silverfish", 1, 64, 32, "Models\\Mobs\\Hostile\\Silverfish.xml");
			new ModelEnderman().Save("Enderman", 1, 64, 32, "Models\\Mobs\\Hostile\\Enderman.xml");
			new ModelGhast().Save("Ghast", 1, 64, 32, "Models\\Mobs\\Hostile\\Ghast.xml");
			new ModelSpider().Save("Spider", 1, 64, 32, "Models\\Mobs\\Hostile\\Spider.xml");
			new ModelZombie().Save("Zombie", 1, 64, 32, "Models\\Mobs\\Hostile\\Zombie.xml");
			new ModelSkeleton().Save("Skeleton", 1, 64, 32, "Models\\Mobs\\Hostile\\Skeleton.xml");
			new ModelCloak().Save("Cloak", 1, 64, 32, "Models\\Other\\Cloak.xml");
			new ModelArmor().Save("Armor", 1, 64, 32, "Models\\Other\\Armor.xml");

			new ModelOzelot().Save("Ozelot", 1, 64, 32, "Models\\Mobs\\Passive\\Ozelot.xml");
			new ModelGolem().Save("Golem", 1, 128, 128, "Models\\Mobs\\Passive\\Golem.xml");

			new pm_Pony().init(true, true).Save("Pony", 1, 64, 32, "Models\\Mine Little Pony\\Pony.xml");
			new pm_newPonyAdv().init(0, 0).Save("New Pony", 1, 64, 32, "Models\\Mine Little Pony\\New Pony.xml");

			Directory.CreateDirectory("Models");

			ModelFormatTCN tcnParser = new ModelFormatTCN();

			foreach (var m in Directory.GetFiles("Models", "*.*", SearchOption.AllDirectories))
			{	
				try
				{
					Model model = tcnParser.Load(m);

					if (model == null)
						continue;

					model.File = new FileInfo(m);
					Models.Add(model.Path, model);
				}
				catch
				{
				}
			}
		}

		public static Model GetModelForPath(string p)
		{
			if (!Models.ContainsKey(p))
				return null;

			return Models[p];
		}
	}
}
