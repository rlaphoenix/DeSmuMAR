using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

		private static readonly string Home = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
		private static readonly string DemumarSettingsFile = Path.Combine(Home, "DeSmuMAR.ini");
		private static readonly string DesmumeLocation = Path.Combine(Home, "DeSmuME.exe");
		private static readonly string DesmumeSettingsFile = Path.Combine(Home, "desmume.ini");

		private static string[] Settings => File.Exists(DemumarSettingsFile) ? File.ReadAllLines(DemumarSettingsFile, Encoding.UTF8) : null;

		static void Main(string[] args) {

			// Ensure DeSmuME is available
			if (!File.Exists(DesmumeLocation)) {
				Log("There's no \"DeSmuME.exe\" next to DeSmuMAR, want to automatically download the latest Dev build? (y/n)");
                if (Console.ReadKey().Key != ConsoleKey.Y) {
                    Log(
                        "Alright, feel free to download it yourself. Make sure it's a recent Dev build (not stable!) " +
                        "due to some features and abilities being missing in the stable builds. If you want a really " +
                        "nice experience with different aspect ratio's, then take my warning!"
                    );
                    return;
                } else {
                    if (!DownloadDesmumeDev(DesmumeLocation)) {
						Log("Failed to download DeSmuME, closing DeSmuMAR...", LogTypes.Error);
						return;
                    } else {
						Log("Downloaded DeSmuME and placed it to \"" + DesmumeLocation + "\".");
					}
                }
            }

			// Ensure the aspect ratio set won't escape the size constraints of the display
			(int Width, int Height, int LCDLayout) = EnsureSizeConstraint(
				(int)SystemParameters.VirtualScreenWidth, (int)SystemParameters.VirtualScreenHeight
			);

			// Force some DeSmuME settings that are necessary for custom aspect ratio use
			if (!File.Exists(DesmumeSettingsFile)) {
				Log("Opening DeSmuME just for a second for it to create a default configuration file...");
				RunDesmume().Kill();
			}
			UpdateINI(DesmumeSettingsFile, new string[][] {
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

			// Run DeSmuME with the first argument provided to DeSmuMAR (if available, this should be the ROM path)
			RunDesmume(args.Length > 0 ? "\"" + args[0] + "\"" : null);

		}

		private static bool DownloadDesmumeDev(string savePath) {

			Log("Fetching Status of AppVeyor zeromus/desmume...");
            string appveyorRes;
            try {
				appveyorRes = new WebClient { Headers = { { "Accept", "application/json" } } }
					.DownloadString("https://ci.appveyor.com/api/projects/zeromus/desmume");
				if (string.IsNullOrEmpty(appveyorRes)) {
					Log("The response from AppVeyor was empty or failed to be read.", LogTypes.Error);
					return false;
				}
			} catch (WebException e) {
				Log("A download error occured: " + e.Message, LogTypes.Error);
				return false;
			}

			Log("Reading response as JSON...");
			JObject appveyor;
			try {
				appveyor = JObject.Parse(appveyorRes);
			} catch (JsonReaderException) {
				Log("The response from AppVeyor was empty or corrupted.", LogTypes.Error);
				return false;
			}

			Log("Fetching Latest Job ID...");
			string latestJobId = (string)appveyor.SelectToken("build.jobs[0].jobId");
			if (string.IsNullOrEmpty(latestJobId)) {
				Log("Job ID was not found in the AppVeyor response.", LogTypes.Error);
				return false;
            }

			Log("Downloading DeSmuME-VS2019-x64-Release.exe Job Artifact...");
			try {
				new WebClient().DownloadFile(
					"https://ci.appveyor.com/api/buildjobs/" + latestJobId + "/artifacts/desmume/src/frontend/windows/__bins/DeSmuME-VS2019-x64-Release.exe",
					savePath
				);
			} catch (WebException e) {
				Log("A download error occured: " + e.Message, LogTypes.Error);
				return false;
            }

			return true;
		}

		private static Process RunDesmume(string Argument=null) {
			Process p = new Process() {
				StartInfo = new ProcessStartInfo() {
					FileName = DesmumeLocation,
					Arguments = Argument
				}
			};
			p.Start();
			p.WaitForInputIdle();
			return p;
		}

		private static (int, int, int) EnsureSizeConstraint(int maxWidth, int maxHeight) {
			SettingsFlags flags = SettingsFlags.None;
			bool WidthSafe = false;
			bool HeightSafe = false;
			int Width = -1;
			int Height = -1;
			int LCDLayout = -1;
			while (!WidthSafe || !HeightSafe) {
				int[] AspectRatio = GetSetting<string>("AspectRatio", flags).Split(':').Select(int.Parse).ToArray();
				LCDLayout = GetSetting<bool>("Screens", flags | SettingsFlags.BoolOnly) ? 1 : 2;
				Height = GetSetting<int>("Resolution", flags | SettingsFlags.NumericOnly);
				Width = Height / AspectRatio[1] * AspectRatio[0] * (LCDLayout == 1 ? 2 : 1);
				WidthSafe = Width < maxWidth;
				HeightSafe = Height < maxHeight;
				if (!WidthSafe || !HeightSafe) {
					Log(
						"Oh no! The Aspect Ratio and Resolution you chose would result in a DeSmuME window that is bigger than your display.\n" +
						"To reduce the size, try reduce the resolution or use a smaller aspect ratio\n" +
						((maxWidth / maxHeight) == (Width / Height) ? "Since you are trying to use the same aspect ratio for DeSmuME as your Screen, i'm assuming you want to go fullscreen, if this is the case, you should lower the resolution or set the screens amount to \"One LCD\" in DeSmuME as that will help lower the window size.\n" : string.Empty) +
						"If your curious, you tried to resize DeSmuME to " + Width + "x" + Height + "\n" +
						"That size is: " +
						(!WidthSafe ? (Width - maxWidth).ToString() + " pixels wider" : string.Empty) +
						(!WidthSafe && !HeightSafe ? " & " : string.Empty) +
						(!HeightSafe ? (Height - maxHeight).ToString() + " pixels taller" : string.Empty) +
						" than your display :O"
					, LogTypes.Warning);
					Log("Let's go ahead and reset the AspectRatio, Screens, and Resolution value's so you can enter new values.");
					flags = SettingsFlags.ForceReset;
				}
			}
			return (Width, Height, LCDLayout);
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
			UniqueOnly=32,
			ForceReset=64
		}
		static T GetSetting<T>(string Setting, SettingsFlags Flags = SettingsFlags.None) {
			while(true) {
				if (!Flags.HasFlag(SettingsFlags.ForceReset) && Settings != null) {
					IEnumerable<string> KeysMatched = Settings.Where(x => x.StartsWith(Setting + "="));
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
				// Apply changes to the input value based on the flag
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
				// Run sanitization checks on the Answer based on Setting key
				bool invalid = false;
				switch (Setting) {
					case "AspectRatio":
						if (!Answer.Contains(":") || !Answer.Replace(":", string.Empty).All(char.IsDigit)) {
							Log("Invalid aspect ratio format.");
							invalid = true;
						}
						break;
				}
				// If sanitization check passed, return the new answer, otherwise loop
				if (!invalid) {
					UpdateINI(DemumarSettingsFile, new string[][] {
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
}
