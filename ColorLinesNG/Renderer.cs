using System;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES11;
using OpenTK.Platform;
using OpenTK.Platform.Android;

using Android.Graphics;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Android.Content;
using Android.Util;

using ColorLinesNG;

namespace CLRenderer {
	public class CLReLabelEntity {
		public CLReLabelEntity next;
		private string text;
		private float x, y;
		private float textInterval;
		private Paint paint;
		public CLReLabelEntity(string text, float x, float y, float size, Color colour, Paint.Align align, float textInterval) {
			this.text = text;
			this.x = x;
			this.y = y;
			this.textInterval = textInterval;
			this.paint = new Paint(PaintFlags.AntiAlias);
			this.paint.TextSize = size*50;
			this.paint.Color = colour;
			this.paint.TextAlign = align;
		}
		public void Render(Canvas canvas, Typeface font, float left, float right, float bottom, float top) {
			float appOpenGLWidth = GLView.right - GLView.left,
				appOpenGLHeight = GLView.top - GLView.bottom;
			int appNativeWidth = GLView.width,
				appNativeHeight = GLView.height;
			float xAbsOpenGL = (this.x - GLView.left) / appOpenGLWidth,
				yAbsOpenGL = (GLView.top - this.y) / appOpenGLHeight;
			float xAbsNative = xAbsOpenGL * appNativeWidth,
				yAbsNative = yAbsOpenGL * appNativeHeight;
			
			string []textLines = text.Split('\n');
			
			this.paint.TextSize *= (float)canvas.Width / GLView.width * (float)canvas.Height / GLView.height;
			paint.SetTypeface(font);
			for (int i = 0; i < textLines.Length; i++)
				canvas.DrawText(textLines[i], xAbsNative, yAbsNative+i*this.paint.TextSize*this.textInterval, paint);
		}
	}
	public class CLReEntity {
		public CLReEntity next;
		public float []Verticies;
		public Color Fill, Border;
		private float []textureCoords;
		private bool hasTexture;
		public int textureId;
		public All Type {
			get {
				return this.type;
			}
			set {
				if (value != All.TriangleStrip || value != All.TriangleFan)
					value = All.TriangleStrip;
			}
		}
		private All type;
		public CLReEntity(float []verticies, Color fill, Color border, float []textureCoords, All type = All.TriangleStrip, int textureId = -1, bool hasTexture = false) {
			this.Verticies = verticies;
			this.Fill = fill;
			this.Border = border;
			this.type = type;
			this.textureId = textureId;
			this.hasTexture = hasTexture;
			this.next = null;
			if (textureCoords == null)
				this.textureCoords = defaultTextureCoords;
			else
				this.textureCoords = textureCoords;
		}
		private byte []ColorToBytes(Color c, uint size) {
			byte []b = new byte[size*4];
			for (uint i = 0; i < size*4; i++) {
				if (i%4 == 0) b[i] = c.R;
				else if (i%4 == 1) b[i] = c.G;
				else if (i%4 == 2) b[i] = c.B;
				else if (i%4 == 3) b[i] = c.A;
			}
			return b;
		}
		public void Render() {
			if (this.hasTexture) {
				GL.Enable(All.Texture2D);
				GL.BindTexture(All.Texture2D, this.textureId);
				GL.EnableClientState(All.VertexArray);
				GL.EnableClientState(All.TextureCoordArray);

				GL.VertexPointer(2, All.Float, 0, this.Verticies);
				GL.TexCoordPointer(2, All.Float, 0, this.textureCoords);
				GL.DrawArrays(All.TriangleStrip, 0, 4);
				
				GL.DisableClientState(All.VertexArray);
				GL.DisableClientState(All.TextureCoordArray);
			} else {
				GL.Disable(All.Texture2D);
				GL.VertexPointer(2, All.Float, 0, this.Verticies);
				GL.EnableClientState(All.VertexArray);
				GL.ColorPointer(4, All.UnsignedByte, 0, ColorToBytes(this.Fill, ((uint)(this.Verticies.Length))/2));
				GL.EnableClientState(All.ColorArray);

				if (this.Type == All.TriangleStrip)
					GL.DrawArrays(All.TriangleStrip, 0, 4);
				else
					GL.DrawArrays(All.TriangleFan, 0, (this.Verticies.Length)/2);

				GL.DisableClientState(All.VertexArray);
				GL.DisableClientState(All.ColorArray);
			}
		}
		float[] square_vertices5 = {
			-1.0f, -1.0f,
			1.0f, -1.0f, 
			1.0f, 1.0f,
			-1.0f, 1.0f,
		};
		float[] defaultTextureCoords = {
			0, 1,
			1, 1,
			0, 0,
			1, 0,
		};
		byte[] square_colors = {
			255, 255,   0, 255,
			0,   255, 255, 255,
			0,     0,    0,  0,
			255,   0,  255, 255,
		};
		byte []triangleFan_colours = {
			255, 255,   0, 255,
			0,   255, 255, 255,
			0,     0,    0,  0,
			255,   0,  255, 255,
		};
		float []triangleFan_verticies = {
			-1.0f, -1.0f,
			-1.0f, 1.0f,
			1.0f, 0.0f,
			0.7f, 0.7f,
		};
	}
	public class CLReDraw {
		//TODO: change amount of circle verticies depending on screen size/pixel density
		private const uint circleVerticies = 32;
		public static void Circle(float xCentre, float yCentre, float radius, Color colour) {
			float []verticies = new float[(circleVerticies+2)*2]; //+2 verticies for centre and the same end point as the start one
			verticies[0] = xCentre;
			verticies[1] = yCentre;
			for(int i = 0; i <= circleVerticies; i++){
				double angle = 2 * Math.PI * (double)i / circleVerticies;
				verticies[i*2+2] = xCentre + (float)Math.Cos(angle) * radius;
				verticies[i*2+3] = yCentre + (float)Math.Sin(angle) * radius;
			}
			CLReQueue.AddToQueue(new CLReEntity(verticies, colour, Color.Cyan, null, All.TriangleFan));
		}
		
		private static float []PosResToVerticies(float x, float y, float width, float height) {
			return new float[8] { x, y-height, x+width, y-height, x, y, x+width, y };
		}
		public static void Rect(float x, float y, float width, float height, Color colour) {
			CLReQueue.AddToQueue(new CLReEntity(PosResToVerticies(x, y, width, height), colour, Color.Cyan, textureCoords: null));
		}
		public static void Rect(float x, float y, float width, float height, int textureId, float []textureCoords) {
			CLReQueue.AddToQueue(new CLReEntity(PosResToVerticies(x, y, width, height), Color.Cyan, Color.Cyan, textureId: textureId, hasTexture: true, textureCoords: textureCoords));
		}
		public static void Rect(float x, float y, float width, float height, int textureId) {
			CLReQueue.AddToQueue(new CLReEntity(PosResToVerticies(x, y, width, height), Color.Cyan, Color.Cyan, textureId: textureId, hasTexture: true, textureCoords: null));
		}
		public static void Text(string text, float x, float y, float size, Color colour, Paint.Align align, float textInterval = 1.337f) {
			CLReQueue.AddToQueue(new CLReLabelEntity(text, x, y, size, colour, align, textInterval));
		}
		public static void EditText(float x, float y, float size, Color colour, Action a) {
			GLView.et = new EditText(GLView.context);

			float appOpenGLWidth = GLView.right - GLView.left,
				appOpenGLHeight = GLView.top - GLView.bottom;
			int appNativeWidth = GLView.width,
				appNativeHeight = GLView.height;
			float xAbsOpenGL = (x - GLView.left) / appOpenGLWidth,
				yAbsOpenGL = (GLView.top - y) / appOpenGLHeight;
			float xAbsNative = xAbsOpenGL * appNativeWidth,
				yAbsNative = yAbsOpenGL * appNativeHeight;

			GLView.et.SetX(xAbsNative);
			GLView.et.SetY(yAbsNative);
			GLView.et.SetBackgroundColor(Color.Transparent);
			GLView.et.TextSize = size*50;
			GLView.et.SetFilters(new Android.Text.IInputFilter[] { new Android.Text.InputFilterLengthFilter(11) });
			GLView.et.SetTextColor(colour);
//			GLView.et.SetTextIsSelectable(false);
			GLView.et.SetTypeface(GLView.font, TypefaceStyle.Normal);
			GLView.et.SetSingleLine(true);
			GLView.et.KeyPress += (object sender, View.KeyEventArgs e) => {
				if (e.Event.Action == KeyEventActions.Down && e.KeyCode == Keycode.Enter)
					a();
				GLView.et.ClearFocus();
				HideShowKeyboard((MainActivity)(GLView.context), true);
			};
			((MainActivity)(GLView.context)).AddContentView(GLView.et, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
			GLView.et.RequestFocus();
			GLView.et.RequestFocusFromTouch();
			HideShowKeyboard((MainActivity)(GLView.context), false);
		}
		public static void HideShowKeyboard(MainActivity activity, bool hide) {
			View view = activity.CurrentFocus;
			if(view == null) {
				view = new View(activity);
			}
			InputMethodManager inputMethodManager = (InputMethodManager)activity.GetSystemService(MainActivity.InputMethodService);
			if (hide)
				inputMethodManager.HideSoftInputFromWindow(view.WindowToken, 0);
			else
				inputMethodManager.ShowSoftInput(view, 0);
		}
	}
	public class CLReQueue {
		private static CLReEntity rentities;
		private static CLReLabelEntity lentities;
		public void Render() {
			CLReEntity r = rentities;
			for (;r != null; r = r.next) r.Render();
			this.Clear();
		}
		public void Render(Canvas canvas, Typeface font, float left, float right, float bottom, float top) {
			CLReLabelEntity l = lentities;
			for (;l != null; l = l.next) l.Render(canvas, font, left, right, bottom, top);
			this.ClearLabelEntities();
		}
		public static void AddToQueue(CLReEntity rentity) {
			if (rentities == null) {
				rentities = rentity;
				return;
			}
			CLReEntity r = rentities;
			while (r.next != null) r = r.next;
			r.next = rentity;
		}
		public static void AddToQueue(CLReLabelEntity lentity) {
			if (lentities == null) {
				lentities = lentity;
				return;
			}
			CLReLabelEntity l = lentities;
			while (l.next != null) l = l.next;
			l.next = lentity;
		}
		private void Clear() {
			CLReEntity r = rentities;
			while (r != null) {
				r = r.next;
				rentities = null;
				rentities = r;
			}
		}
		private void ClearLabelEntities() {
			CLReLabelEntity l = lentities;
			while (l != null) {
				l = l.next;
				lentities = null;
				lentities = l;
			}
		}

		static float[] square_vertices0 = {
			-1.0f, 0.0f,
			0.0f, 0.0f,
			-1.0f, 1.0f, 
			0.0f, 1.0f,
		};
		
		float[] square_vertices1 = {
			-0.5f, -0.5f,
			0.5f, -0.5f,
			-0.5f, 0.5f, 
			0.5f, 0.5f,
		};

		float[] square_vertices2 = {
			-0.7f, -0.7f,
			0.7f, -0.7f,
			-0.7f, 0.7f, 
			0.7f, 0.7f,
		};
		
		float[] square_vertices3 = {
			-0.7f, -0.7f,
			0.7f, -0.7f,
			-0.7f, 0.7f, 
			0.7f, 0.7f,
		};
		
		static float[] square_vertices5 = {
			-1.0f, -1.0f,
			1.0f, -1.0f,
			-1.0f, 1.0f, 
			1.0f, 1.0f,
		};
		
		byte[] square_colors = {
			255, 255,   0, 255,
			0,   255, 255, 255,
			0,     0,    0,  0,
			255,   0,  255, 255,
		};
	}
}