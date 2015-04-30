using System;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES11;
using OpenTK.Platform;
using OpenTK.Platform.Android;

using Android.Graphics;
using Android.Views;
using Android.Widget;
using Android.Content;
using Android.Runtime;
using Android.Util;

using CLRenderer;

namespace ColorLinesNG {
	class GLView : AndroidGameView {
		private CLReQueue reQueue;
		private CLField field;
		
		public static bool SpecialRefresh;

		public static Context context;

		public int []textureIds;
		public JavaList<Bitmap> Textures;

		public static Typeface font;
		public static EditText et;
		
		public GLView(Context context)
			: base(context) {
			reQueue = new CLReQueue();
			GLView.context = context;
			
			this.SetWillNotDraw(false);
			font = Typeface.CreateFromAsset(GLView.context.Assets, "fonts/PressStart2P.ttf");
		}

		private void Init() {
			if (this.field == null) {
				textureIds = new int[Textures.Count];
			
				GL.Enable(All.CullFace);
				GL.CullFace(All.Back);

				GL.Enable(All.Blend);
				GL.BlendFunc(All.SrcAlpha, All.OneMinusSrcAlpha);

				// create texture ids
				GL.Enable(All.Texture2D);
				GL.GenTextures(Textures.Count, textureIds);

				for (int i = 0; i < Textures.Count; i++) {
					LoadTexture((Bitmap)Textures.Get(i), textureIds[i]);
				}

				this.field = new CLField(textureIds, GLView.context);
				this.field.MakeLabels(GLView.context.Resources.Configuration.Orientation == Android.Content.Res.Orientation.Landscape, left, right, bottom, top, scale);
			}
		}

		public static float bottom = 1.0f, top = 1.0f, left = 1.0f, right = 1.0f, scale = 1.0f;
		public static int width = 480, height = 762;
		private bool firstCall = true;
		private void SetScreenRatio(int w, int h) {
			if (w > h) {
				top = 1.0f;
				bottom = -top;
				right = (float)w / h;
				left = -right;
				scale = 1.5875f / right;
			} else {
				top = (float)h / w;
				bottom = -top;
				right = 1.0f;
				left = -right;
				scale = 1.5875f / top;
			}
			width = w;
			height = h;
			if (firstCall) {
				firstCall = false;
				return;
			}
			if (SpecialRefresh) {
				base.DestroyFrameBuffer();
				base.CreateFrameBuffer();
			}

			this.field.MakeLabels(GLView.context.Resources.Configuration.Orientation == Android.Content.Res.Orientation.Landscape, left, right, bottom, top, scale);
		}

		// This gets called when the drawing surface is ready
		protected override void OnLoad(EventArgs e) {
			base.OnLoad(e);

			Init();

			// Run the render loop
			Run();
		}

		void LoadTexture(Bitmap texture, int tex_id) {
			GL.BindTexture(All.Texture2D, tex_id);

			// setup texture parameters
			GL.TexParameterx(All.Texture2D, All.TextureMagFilter, (int)All.Nearest);
			GL.TexParameterx(All.Texture2D, All.TextureMinFilter, (int)All.Nearest);
			GL.TexParameterx(All.Texture2D, All.TextureWrapS, (int)All.ClampToEdge);
			GL.TexParameterx(All.Texture2D, All.TextureWrapT, (int)All.ClampToEdge);

			Android.Opengl.GLUtils.TexImage2D((int)All.Texture2D, 0, texture, 0); 
		}


		// This method is called everytime the context needs
		// to be recreated. Use it to set any egl-specific settings
		// prior to context creation
		//
		// In this particular case, we demonstrate how to set
		// the graphics mode and fallback in case the device doesn't
		// support the defaults
		protected override void CreateFrameBuffer() {
			// the default GraphicsMode that is set consists of (16, 16, 0, 0, 2, false)
			try {
				Log.Verbose("GLCube", "Loading with default settings");
				GraphicsMode = new AndroidGraphicsMode(new ColorFormat(32), 24, 8, 4, 2, false);

				// if you don't call this, the context won't be created
				base.CreateFrameBuffer();
				return;
			} catch (Exception ex) {
				Log.Verbose("GLCube", "{0}", ex);
			}

			// this is a graphics setting that sets everything to the lowest mode possible so
			// the device returns a reliable graphics setting.
			try {
				Log.Verbose("GLCube", "Loading with custom Android settings (low mode)");
				GraphicsMode = new AndroidGraphicsMode(0, 0, 0, 0, 0, false);

				// if you don't call this, the context won't be created
				base.CreateFrameBuffer();
				return;
			} catch (Exception ex) {
				Log.Verbose("GLCube", "{0}", ex);
			}
			throw new Exception("Can't load egl, aborting");
		}

		protected override void OnSizeChanged(int w, int h, int oldw, int oldh) {
			base.OnSizeChanged(w, h, oldw, oldh);
			SetScreenRatio(w, h);
		}

		private void DealWithTouch(float x, float y) {
			if (this.field.CheckLabelsActions(x, y))
				if (this.field.quit)
					System.Environment.Exit(0);
				else
					return;
			if (!this.field.SelectBall(x, y))
				this.field.MoveBall(x, y);
		}
		private void ScreenToOpenGLCoordinates(float xIn, float yIn, out float xOut, out float yOut) {
			int appNativeWidth = Resources.DisplayMetrics.WidthPixels,
				appNativeHeight = Resources.DisplayMetrics.HeightPixels - GetStatusBarHeight();
			float appOpenGLWidth = right - left,
				appOpenGLHeight = top - bottom;
			float xAbsNative = xIn / appNativeWidth,
				yAbsNative = yIn / appNativeHeight;
			float xAbsOpenGL = xAbsNative * appOpenGLWidth,
				yAbsOpenGL = yAbsNative * appOpenGLHeight;
			xOut = left + xAbsOpenGL;
			yOut = top - yAbsOpenGL;
		}
		private int GetStatusBarHeight() {
			int result = 0;
			int resourceId = Resources.GetIdentifier("status_bar_height", "dimen", "android");
			if (resourceId > 0) {
				result = Resources.GetDimensionPixelSize(resourceId);
			}
			return result;
		}
		public override bool OnTouchEvent(MotionEvent e) {
			float touchX, touchY;
			if (e.Action != MotionEventActions.Down)
				return true;
			ScreenToOpenGLCoordinates(e.GetX(), e.GetY(), out touchX, out touchY);
			DealWithTouch(touchX, touchY);
			return base.OnTouchEvent(e);
		}

		// This gets called on each frame render
		protected override void OnRenderFrame(FrameEventArgs e) {
			base.OnRenderFrame(e);

			this.field.Draw(context.Resources.Configuration.Orientation == Android.Content.Res.Orientation.Landscape, left, right, bottom, top);

			GL.MatrixMode(All.Projection);
			GL.LoadIdentity();
			GL.Ortho(left, right, bottom, top, -1.0f, 1.0f);
			GL.MatrixMode(All.Modelview);

			GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
			GL.Clear((uint)All.ColorBufferBit);

			this.reQueue.Render();

			SwapBuffers();
			Invalidate();
		}

		protected override void OnDraw(Canvas canvas) {
			this.reQueue.Render(canvas, font, left, right, bottom, top);
//			et.RequestFocus();
/*			et.SetX(0.0f);
			et.SetY(0.0f);
			et.SetWidth(64);
			et.SetHeight(32);*/
		}
	}
}
