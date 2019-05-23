using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int uFlags);
		private const int HWND_TOPMOST = -1;
		private const int SWP_NOMOVE = 0x0002;
		private const int SWP_NOSIZE = 0x0001;
		#endregion
		static string LOCATEME = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
		static string SETTINGS_FILE = Path.Combine(LOCATEME, "DeSmuME Resizer.ini");
		static string[] SETTINGS = File.Exists(SETTINGS_FILE) ? File.ReadAllLines(SETTINGS_FILE, Encoding.UTF8) : null;

		static void Main(string[] args) {

			#region Get Calculations
			int[] AspectRatio = GetSetting<string>("AspectRatio").Split(':').Select(int.Parse).ToArray();
			int Screens = GetSetting<int>("Screens", SettingsFlags.NumericOnly);
			int Height = GetSetting<int>("Resolution", SettingsFlags.NumericOnly);
			int Width = Height / AspectRatio[1] * AspectRatio[0] * Screens;
			#endregion
			#region Run DeSmuME.exe
			Process p = new Process() {
				StartInfo = new ProcessStartInfo() {
					FileName = Path.Combine(LOCATEME, "DeSmuME.exe"),
					Arguments = args.Length != 0 ? "\"" + args[0] + "\"" : string.Empty
				}
			};
			p.Start();
			p.WaitForInputIdle();
			#endregion
			#region Do Resize
			while (true) {
				Console.WriteLine("Starting Resize");
				IntPtr handle = p.MainWindowHandle;
				Console.WriteLine("Got Window Handle");
				RECT Rect = new RECT();
				while (!GetWindowRect(handle, ref Rect)) {
					Thread.Sleep(10);
					Console.WriteLine("Failed to get Handle's Bounds, retrying...");
				}
				Console.WriteLine("Got Window Bounds");
				while (!MoveWindow(handle, Rect.left, Rect.top, Width + 16, Height + 59, true)) {
					Console.WriteLine("Failed to resize Handle for some reason, retrying...");
				}
				Console.WriteLine("Resized Window");
				while (!GetWindowRect(handle, ref Rect)) {
					Thread.Sleep(10);
					Console.WriteLine("Failed to update Handle's Bounds, retrying...");
				}
				Console.WriteLine("Updated Window Bounds");
				int ResultingHeight = Rect.bottom - Rect.top;
				int ResultingWidth = Rect.right - Rect.left;
				if (ResultingWidth == Width + 16 && ResultingHeight == Height + 59) {
					Console.WriteLine("Resize verified to be correct, nice!");
					break;
				}
				if(ResultingHeight == 1019 && ResultingWidth == 2576) {
					Console.ForegroundColor = ConsoleColor.DarkRed;
					Console.BackgroundColor = ConsoleColor.White;
					Console.WriteLine("DeSmuME has View -> \"Maintain Aspect Ratio\" enabled, this needs to be disabled.");
					SetWindowPos(Process.GetCurrentProcess().MainWindowHandle, new IntPtr(HWND_TOPMOST), 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
					Console.ReadLine();
					break;
				}
				Console.WriteLine("Something isn't quite right, everything went succesful except for the verification of the resulting window bounds, retrying in 2 seconds...");
				Thread.Sleep(2000);
			}
			#endregion

		}

		[Flags]
		enum SettingsFlags {
			None,
			NumericOnly,
			AlphaOnly,
			AlphaNumericOnly,
			Lowercase
		}
		static T GetSetting<T>(string Setting, SettingsFlags Flags = SettingsFlags.None) {
			if (SETTINGS != null) {
				IEnumerable<string> KeysMatched = SETTINGS.Where(x => x.StartsWith(Setting + "="));
				if (KeysMatched != null && KeysMatched.Any()) {
					return (T)Convert.ChangeType(KeysMatched.First().Split('=')[1], typeof(T));
				}
			}
			string Message = string.Empty;
			switch (Setting) {
				case "AspectRatio":
				Message = "Which aspect ratio do you want (e.g. 4:3, 16:9, 21:9, e.t.c)";
				break;
				case "Screens":
				Message = "How many Screens is DeSmuME set up to show? (1/2)";
				break;
				case "Resolution":
				Message = "Which resolution do you want (e.g. 720, 1080, 1440, e.t.c)";
				break;
				default:
				Message = "ERROR OCCURED!";
				break;
			}
			Console.WriteLine(Message + ":");
			string Answer = Console.ReadLine();
			if (Flags.HasFlag(SettingsFlags.NumericOnly)) {
				Regex.Replace(Answer, "[^0-9]", string.Empty);
			}
			if (Flags.HasFlag(SettingsFlags.AlphaOnly)) {
				Regex.Replace(Answer, "[^a-zA-Z]", string.Empty);
			}
			if (Flags.HasFlag(SettingsFlags.AlphaNumericOnly)) {
				Regex.Replace(Answer, "[^a-zA-Z0-9]", string.Empty);
			}
			if (Flags.HasFlag(SettingsFlags.Lowercase)) {
				Answer = Answer.ToLowerInvariant();
			}
			File.AppendAllText(SETTINGS_FILE, Setting + "=" + Answer + "\n");
			return (T)Convert.ChangeType(Answer, typeof(T));
		}

	}
}
