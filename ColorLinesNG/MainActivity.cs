using System;
using System.Drawing;

using Android.Graphics;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Content.PM;

using CLRenderer;

namespace ColorLinesNG {
	[Activity(Label = "ColorLinesNG",
		MainLauncher = true,
		Icon = "@drawable/icon",
		ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden,
		Theme="@android:style/Theme.NoTitleBar"
#if __ANDROID_11__
		,HardwareAccelerated=false
#endif
	)]
	public class MainActivity : Activity {
		GLView view;

		protected override void OnCreate(Bundle bundle) {
			base.OnCreate(bundle);

			// Create our OpenGL view, and display it
			view = new GLView(this);
			
			GLView.SpecialRefresh = SpecialRefresh();

			view.Textures = new JavaList<Bitmap>();
			foreach (string cellColour in cellColours) {
				using (System.IO.StreamReader sr = new System.IO.StreamReader(Assets.Open(String.Format("textures/CLNG_{0}.png", cellColour)))) {
					view.Textures.Add(BitmapFactory.DecodeStream(sr.BaseStream));
				}
			}

			SetContentView(view);
		}
		private bool SpecialRefresh() {
			string manufacturer = Build.Manufacturer.ToLower();
			string product = Build.Product.ToLower();
			return manufacturer == "asus" && (
				product.Contains("zenfone")
				|| product.Contains("padfone")
				);
		}

		protected override void OnPause() {
			base.OnPause();
			view.Pause();
		}

		protected override void OnResume() {
			base.OnResume();
			view.Resume();
		}

		private string []cellColours = {
			"Cell",
			"Red",
			"Yellow",
			"Green",
			"Cyan",
			"Blue",
			"Pink",
			"Brown",
//			"Label",
			"Label10",
		};
	}
}

