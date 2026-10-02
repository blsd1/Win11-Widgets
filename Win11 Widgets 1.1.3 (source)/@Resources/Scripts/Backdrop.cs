// Win11 Widgets for Rainmeter - Backdrop helper
//	Author - ketamine
//
//	Gives skin windows a blurred or acrylic backdrop (the same window composition effect TranslucentTB
//	uses for the taskbar), clipped to the skin's rounded card, or removes it again.
//
//	Usage:
//		Backdrop.exe <none|blur|acrylic> <AARRGGBB tint> <card width> <card height> <corner radius> <skin .ini path> [...]
//	Rainmeter titles each skin window with its full .ini path; matching windows are changed.
//	A card width/height of 0 leaves the backdrop on the whole window.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

static class Backdrop
{
	[StructLayout(LayoutKind.Sequential)]
	struct AccentPolicy { public int AccentState, AccentFlags, GradientColor, AnimationId; }

	[StructLayout(LayoutKind.Sequential)]
	struct WindowCompositionAttributeData { public int Attribute; public IntPtr Data; public int SizeOfData; }

	delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

	[DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc proc, IntPtr lParam);
	[DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr hwnd, StringBuilder name, int max);
	[DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int max);
	[DllImport("user32.dll")] static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
	[DllImport("user32.dll")] static extern int SetWindowRgn(IntPtr hwnd, IntPtr region, bool redraw);
	[DllImport("gdi32.dll")] static extern IntPtr CreateRoundRectRgn(int left, int top, int right, int bottom, int width, int height);

	const int WCA_ACCENT_POLICY = 19;
	const int ACCENT_DISABLED = 0, ACCENT_ENABLE_BLURBEHIND = 3, ACCENT_ENABLE_ACRYLICBLURBEHIND = 4;

	static int Main(string[] args)
	{
		if (args.Length < 6) { Console.Error.WriteLine("Usage: Backdrop.exe <none|blur|acrylic> <AARRGGBB> <width> <height> <radius> <skin path>..."); return 1; }

		string style = args[0].ToLowerInvariant();
		int state = style == "blur" ? ACCENT_ENABLE_BLURBEHIND : style == "acrylic" ? ACCENT_ENABLE_ACRYLICBLURBEHIND : ACCENT_DISABLED;
		// The accent policy wants the tint as AABBGGRR.
		uint argb = uint.Parse(args[1], NumberStyles.HexNumber);
		int abgr = (int)((argb & 0xFF00FF00) | ((argb & 0xFF) << 16) | ((argb >> 16) & 0xFF));
		int width = ParseSize(args[2]), height = ParseSize(args[3]), radius = ParseSize(args[4]);

		var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		for (int i = 5; i < args.Length; i++) paths.Add(args[i]);

		EnumWindows((hwnd, _) =>
		{
			var name = new StringBuilder(64);
			GetClassName(hwnd, name, name.Capacity);
			if (name.ToString() != "RainmeterMeterWindow") return true;
			var title = new StringBuilder(1024);
			GetWindowText(hwnd, title, title.Capacity);
			if (paths.Contains(title.ToString())) Apply(hwnd, state, abgr, width, height, radius);
			return true;
		}, IntPtr.Zero);
		return 0;
	}

	// Sizes may come in as Rainmeter formulas that were already resolved, e.g. "361" or "361.000000".
	static int ParseSize(string text)
	{
		double value;
		return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? (int)Math.Round(value) : 0;
	}

	static void Apply(IntPtr hwnd, int state, int tint, int width, int height, int radius)
	{
		// Flag 2 tints the blur with GradientColor; only set it for a visible tint, as a fully
		// transparent tint turns the blur black.
		bool tinted = state != ACCENT_DISABLED && ((uint)tint >> 24) != 0;
		var policy = new AccentPolicy { AccentState = state, AccentFlags = tinted ? 2 : 0, GradientColor = tint };
		int size = Marshal.SizeOf(policy);
		IntPtr ptr = Marshal.AllocHGlobal(size);
		try
		{
			Marshal.StructureToPtr(policy, ptr, false);
			var data = new WindowCompositionAttributeData { Attribute = WCA_ACCENT_POLICY, Data = ptr, SizeOfData = size };
			SetWindowCompositionAttribute(hwnd, ref data);
		}
		finally { Marshal.FreeHGlobal(ptr); }

		// The backdrop fills the whole window, which can be larger than the card, so clip the window
		// to the rounded card (the system owns the region afterwards). No backdrop: remove the clip.
		IntPtr region = state != ACCENT_DISABLED && width > 0 && height > 0
			? CreateRoundRectRgn(0, 0, width + 1, height + 1, radius * 2, radius * 2)
			: IntPtr.Zero;
		SetWindowRgn(hwnd, region, true);
	}
}
