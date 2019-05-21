using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace DeSmuME_Resizer {
	class Program {

		#region API's and Structs
		[StructLayout(LayoutKind.Sequential)]
		public struct RECT {
			public int left;
			public int top;
			public int right;
			public int bottom;
		}
		[DllImport("user32.dll", SetLastError = true)]
		static extern bool GetWindowRect(IntPtr hWnd, ref RECT Rect);

		[DllImport("user32.dll", SetLastError = true)]
		static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int Width, int Height, bool Repaint);
		#endregion
		#region Constants
		const string SETTINGS_FILE = "DeSmuME Resizer.ini";
		#endregion

		static void Main(string[] args) {

			int Height = 0;
			if (File.Exists(SETTINGS_FILE)) {
				Console.WriteLine("Using previous resolution, to use a new one delete DeSmuME Resizer.ini or change it in that file.");
				Height = int.Parse(File.ReadAllText(SETTINGS_FILE, Encoding.UTF8).Split('=')[1]);
			} else {
				Console.WriteLine("Which resolution do you want (e.g. 720, 1080, 1440, e.t.c):");
				Height = int.Parse(Console.ReadLine().ToLowerInvariant().Replace("p", string.Empty));
				File.WriteAllText(SETTINGS_FILE, "SavedResolution=" + Height);
			}
			int Width = Height / 9 * 16 * 2;
			#region Run DeSmuME.exe
			Process p = new Process() {
				StartInfo = new ProcessStartInfo() {
					FileName = "DeSmuME.exe"
				}
			};
			p.Start();
			p.WaitForInputIdle();
			#endregion
			while (true) {
				Console.WriteLine("Starting Resize");
				IntPtr handle = p.MainWindowHandle;
				Console.WriteLine("Got Window Handle");
				RECT Rect = new RECT();
				while (!GetWindowRect(handle = p.MainWindowHandle, ref Rect)) {
					Thread.Sleep(10);
					Console.WriteLine("Failed to get Handle's Bounds, retrying...");
				}
				Console.WriteLine("Got Window Bounds");
				while (!MoveWindow(handle = p.MainWindowHandle, Rect.left, Rect.top, Width + 16, Height + 59, true)) {
					Console.WriteLine("Failed to resize Handle for some reason, retrying...");
				}
				Console.WriteLine("Resized Window");
				if (Rect.right - Rect.left == Width + 16 && Rect.bottom - Rect.top == Height + 59) {
					Console.WriteLine("Resize verified to be correct, nice!");
					break;
				}
				Console.WriteLine("Something isn't quite right, everything went succesful except for the verification of the resulting window bounds, retrying in 2 seconds...");
				Thread.Sleep(2000);
			}

		}
	}
}
