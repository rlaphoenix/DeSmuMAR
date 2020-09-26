using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;

namespace DeSmuMAR {
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
		static string SETTINGS_FILE = Path.Combine(LOCATEME, "DeSmuMAR.ini");
		static string DESMUME_LOCATION = Path.Combine(LOCATEME, "DeSmuME.exe");
		static string DESMUME_SETTINGS_FILE = Path.Combine(LOCATEME, "desmume.ini");
		static bool SETTINGS_FORCE_OVERWRITE = false;
		static string[] SETTINGS = File.Exists(SETTINGS_FILE) ? File.ReadAllLines(SETTINGS_FILE, Encoding.UTF8) : null;
		static string ARGUMENT = null;

		static void Main(string[] args) {

			ARGUMENT = args.Length != 0 ? "\"" + args[0] + "\"" : string.Empty;

			#region Check for DeSmuME
			GetDesmume();
			#endregion
			#region Get Calculations
			bool WidthSafe = false;
			bool HeightSafe = false;
			int LCDLayout = 1;
			int Width = -1;
			int Height = -1;
			while (!WidthSafe && !HeightSafe) {
				string _ar = string.Empty;
				bool _ar_baddata = false;
				while (!(_ar = GetSetting<string>("AspectRatio")).Contains(":") || !_ar.Replace(":", string.Empty).All(char.IsDigit)) {
					SETTINGS_FORCE_OVERWRITE = true;
					_ar_baddata = true;
					Log("Incorrect Ratio Value, are you missing digits or ':'?", LogTypes.Warning);
				}
				if(_ar_baddata) {
					SETTINGS_FORCE_OVERWRITE = false;
				}
				int[] AspectRatio = _ar.Split(':').Select(int.Parse).ToArray();
				bool BothScreens = GetSetting<bool>("Screens", SettingsFlags.BoolOnly);
				LCDLayout = BothScreens ? 1 : 2;
				Height = GetSetting<int>("Resolution", SettingsFlags.NumericOnly);
				Width = Height / AspectRatio[1] * AspectRatio[0] * (LCDLayout == 1 ? 2 : 1);
				WidthSafe = Width < SystemParameters.VirtualScreenWidth;
				HeightSafe = Height < SystemParameters.VirtualScreenHeight;
				if (!WidthSafe || !HeightSafe) {
					Log(
						"Oh no! That combination you chose would result in a DeSmuME window that is bigger than your display.\n" +
						"To reduce the size, you can either reduce the resolution or use a smaller aspect ratio (or both :D)\n" +
						((SystemParameters.VirtualScreenWidth / SystemParameters.VirtualScreenHeight) == (Width / Height) ? "Since you are trying to use the same aspect ratio for DeSmuME as your Screen, i'm assuming you want to go fullscreen, if this is the case, you should lower the resolution or set the screens amount to \"One LCD\" in DeSmuME as that will help lower the window size.\n" : string.Empty) +
						"If your curious, you tried to resize DeSmuME to " + Width + "x" + Height + "\n" +
						"That size is: " +
						(!WidthSafe ? (Width - SystemParameters.VirtualScreenWidth).ToString() + " pixels wider" : string.Empty) +
						(!WidthSafe && !HeightSafe ? " & " : string.Empty) +
						(!HeightSafe ? (Height - SystemParameters.VirtualScreenHeight).ToString() + " pixels taller" : string.Empty) +
						" than your display :O"
					, LogTypes.Warning);
					SETTINGS_FORCE_OVERWRITE = true;
				}
			}
			SETTINGS_FORCE_OVERWRITE = false;
			Log("Chunking some numbers...");
			#endregion
			#region Force some DeSmuME Settings
			if (!File.Exists(DESMUME_SETTINGS_FILE)) {
				// This is simply a default INI Created by DeSmuME thats necessary.
				File.WriteAllText(DESMUME_SETTINGS_FILE, Encoding.UTF8.GetString(Convert.FromBase64String("WzNEXQpSZW5kZXJlcj0yCltWaWRlb10KV2luZG93IFJvdGF0ZT0wCkZpbHRlcj0wCldpZHRoPTI1NgpIZWlnaHQ9Mzg0CldpbmRvdyBTaXplPTAKV2luZG93IHdpZHRoPTI1NgpXaW5kb3cgaGVpZ2h0PTM4NApXaW5kb3dQb3NYPTc4CldpbmRvd1Bvc1k9NzgKW0Rpc3BsYXldCkZyYW1lQ291bnRlcj0wClNjcmVlbkdhcD0wCltSYW1XYXRjaF0KU2F2ZVdpbmRvd1Bvcz0wClJXV2luZG93UG9zWD0wClJXV2luZG93UG9zWT0wCkF1dG8tbG9hZD0wCltXYXRjaGVzXQpSZWNlbnQgV2F0Y2ggMT0KUmVjZW50IFdhdGNoIDI9ClJlY2VudCBXYXRjaCAzPQpSZWNlbnQgV2F0Y2ggND0KUmVjZW50IFdhdGNoIDU9CltTY3JpcHRpbmddClJlY2VudCBMdWEgU2NyaXB0IDE9ClJlY2VudCBMdWEgU2NyaXB0IDI9ClJlY2VudCBMdWEgU2NyaXB0IDM9ClJlY2VudCBMdWEgU2NyaXB0IDQ9ClJlY2VudCBMdWEgU2NyaXB0IDU9ClJlY2VudCBMdWEgU2NyaXB0IDY9ClJlY2VudCBMdWEgU2NyaXB0IDc9ClJlY2VudCBMdWEgU2NyaXB0IDg9ClJlY2VudCBMdWEgU2NyaXB0IDk9ClJlY2VudCBMdWEgU2NyaXB0IDEwPQpSZWNlbnQgTHVhIFNjcmlwdCAxMT0KUmVjZW50IEx1YSBTY3JpcHQgMTI9ClJlY2VudCBMdWEgU2NyaXB0IDEzPQpSZWNlbnQgTHVhIFNjcmlwdCAxND0KUmVjZW50IEx1YSBTY3JpcHQgMTU9CltTb3VuZF0KVm9sdW1lPTEwMApbQ29uc29sZV0KUG9zWD01MgpQb3NZPTUyCldpZHRoPTEyMzMKSGVpZ2h0PTUxOQo=")));
			}
			UpdateINI(DESMUME_SETTINGS_FILE, new string[][] {
				new string[] {"3D", "Renderer", "2"},
				new string[] {"Console", "Show", "0"},
				new string[] {"Display", "Show Toolbar", "0"},
				new string[] {"Video", "Window Force Ratio", "0"},
				new string[] {"Video", "LCDsLayout", LCDLayout.ToString() },
				new string[] {"Video", "Window Lockdown", "0"},
				new string[] {"Video", "Display Method", "1"},
				new string[] {"Video", "Window width", "1"},
				new string[] {"Video", "Window width", Width.ToString()},
				new string[] {"Video", "Window height", Height.ToString()}
			});
			#endregion
			#region Run DeSmuME.exe
			Process p = new Process() {
				StartInfo = new ProcessStartInfo() {
					FileName = DESMUME_LOCATION,
					Arguments = ARGUMENT
				}
			};
			p.Start();
			p.WaitForInputIdle();
			#endregion

		}
		
		private static void GetDesmume() {
			if (File.Exists(DesmumeLocation)) return;
			
			Log(
				"DeSmuME isn't next to DeSmuMAR, do you want DeSmuMAR to automatically download the " +
				"latest DeSmuME DEV Build from the AppVeyor CI? (y/n)"
			);
			if (Console.ReadKey().Key != ConsoleKey.Y) return;
			
			Console.Write("\nFetching DeSmuME AppVeyor CI Status...");
			var appveyorApi = new WebClient {Headers = {{"Accept", "application/json"}}}
				.DownloadString("https://ci.appveyor.com/api/projects/zeromus/desmume");

			Console.Write("\nFetching Latest Job ID...");
			var latestJobId = Regex.Match(appveyorApi, "\"jobId\":\"([^\"]*)");
			if (!latestJobId.Success) {
				Log("FAILED! The response from AppVeyor was unexpected. Closing DeSmuMAR in 5 seconds...", LogTypes.Error);
				Thread.Sleep(5000);
			} else {
				Console.Write(" DONE! " + latestJobId.Groups[1].Value + "\nDownloading \"DeSmuME-VS2019-x64-Release.exe\"......");
				new WebClient().DownloadFile("https://ci.appveyor.com/api/buildjobs/" + latestJobId.Groups[1].Value + "/artifacts/desmume/src/frontend/windows/__bins/DeSmuME-VS2019-x64-Release.exe", DesmumeLocation);
				Log("DONE! Downloaded DeSmuME! Restarting DeSmuMAR in 5 seconds...");
				Thread.Sleep(5000);
				Process.Start(LocateMe, _argument);
			}
			Environment.Exit(0);
		}
		
		enum LogTypes {
			Info,
			Warning,
			Error
		}
		static void Log(string Message, LogTypes Type = LogTypes.Info) {
			Console.ForegroundColor = Type == LogTypes.Warning ? ConsoleColor.DarkMagenta : Type == LogTypes.Error ? ConsoleColor.DarkRed : ConsoleColor.Gray;
			if(Type != LogTypes.Info) {
				Console.BackgroundColor = ConsoleColor.White;
				SetWindowPos(Process.GetCurrentProcess().MainWindowHandle, new IntPtr(HWND_TOPMOST), 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
			}
			Console.WriteLine(Message);
			Console.ResetColor();
		}

		static int GCD(int a, int b) {
			return b == 0 ? Math.Abs(a) : GCD(b, a % b);
		}
		
		static void UpdateINI(string file, string[][] Settings) {
			List<string> CurrentSettings = File.Exists(file) ? File.ReadAllLines(file, Encoding.UTF8).ToList() : new List<string>();
			bool EmptyFile = CurrentSettings.Count == 0;
			foreach (string[] Setting in Settings.OrderBy(x => x[0]).Select(x => new string[] { "[" + x[0] + "]", x[1] + "=", x[2] })) {
				if(EmptyFile) {
					if(!CurrentSettings.Contains(Setting[0])) {
						CurrentSettings.Add(Setting[0]);
					}
					CurrentSettings.Add(Setting[1] + Setting[2]);
				} else {
					bool InSectionNow = false;
					for (int i = 0; i < CurrentSettings.Count; i++) {
						string CurrentLine = CurrentSettings[i];
						// We are now on a line of a starting section (e.g. [3D]) or at the end of the file
						if (CurrentLine.StartsWith("[") && CurrentLine.EndsWith("]") || i == CurrentSettings.Count - 1) {
							// If the section is the one were trying to apply a setting in, mark it so our later code does checks
							// If its not the one were trying to set to, but we were in it before, this must be the end of that section, meaning this wasnt in the INI yet, lets add it.
							if (CurrentLine == Setting[0]) {
								InSectionNow = true;
							} else if (InSectionNow) {
								CurrentSettings.Insert(i, Setting[1] + Setting[2]);
								break;
							}
						}
						if (InSectionNow && CurrentLine.StartsWith(Setting[1])) {
							CurrentSettings[i] = Setting[1] + Setting[2];
							break;
						}
					}
				}
			}
			File.WriteAllLines(file, CurrentSettings, Encoding.UTF8);
		}

		[Flags]
		enum SettingsFlags {
			None=0,
			NumericOnly=1,
			AlphaOnly=2,
			AlphaNumericOnly=4,
			BoolOnly=8,
			Lowercase=16,
			UniqueOnly=32
		}
		static T GetSetting<T>(string Setting, SettingsFlags Flags = SettingsFlags.None) {
			while(true) {
				if (!SETTINGS_FORCE_OVERWRITE && SETTINGS != null) {
					IEnumerable<string> KeysMatched = SETTINGS.Where(x => x.StartsWith(Setting + "="));
					if (KeysMatched != null && KeysMatched.Any()) {
						try {
							return (T)Convert.ChangeType(KeysMatched.First().Split('=')[1], typeof(T));
						} catch {
						}
					}
				}
				string Message = string.Empty;
				switch (Setting) {
					case "AspectRatio":
					Message = "Which aspect ratio do you want (e.g. 4:3, 16:9, 21:9, e.t.c)";
					break;
					case "Screens":
					Message = "Do you want both screens to be shown with DeSmuME? (y/n)";
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
				if (Flags.HasFlag(SettingsFlags.BoolOnly)) {
					Flags |= SettingsFlags.UniqueOnly | SettingsFlags.Lowercase;
				}
				if (Flags.HasFlag(SettingsFlags.NumericOnly)) {
					Answer = Regex.Replace(Answer, "[^0-9]", string.Empty);
				}
				if (Flags.HasFlag(SettingsFlags.AlphaOnly)) {
					Answer = Regex.Replace(Answer, "[^a-zA-Z]", string.Empty);
				}
				if (Flags.HasFlag(SettingsFlags.AlphaNumericOnly)) {
					Answer = Regex.Replace(Answer, "[^a-zA-Z0-9]", string.Empty);
				}
				if (Flags.HasFlag(SettingsFlags.UniqueOnly)) {
					Answer = new string(Answer.Distinct().ToArray());
				}
				if (Flags.HasFlag(SettingsFlags.Lowercase)) {
					Answer = Answer.ToLowerInvariant();
				}
				if (Flags.HasFlag(SettingsFlags.BoolOnly)) {
					Answer = Regex.Replace(Answer, "[^yn]", string.Empty).Replace("y", "true").Replace("n", "false");
				}
				UpdateINI(SETTINGS_FILE, new string[][] {
					new string[] {"General", Setting, Answer}
				});
				try {
					return (T)Convert.ChangeType(Answer, typeof(T));
				} catch (Exception ex) {
					Log(ex.Message.Replace("String was not recognized as a valid", "Invalid") + " Please type the expected response.", LogTypes.Warning);
				}
			}
		}

	}
}
