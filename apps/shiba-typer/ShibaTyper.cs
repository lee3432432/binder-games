// 시바 타자왕 (Shiba Typer) — 바탕화면 위에서 같이 타자 치는 시바견
// .NET Framework 4.x (Windows 10/11 기본 탑재) 단일 실행 파일
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("시바 타자왕")]
[assembly: AssemblyProduct("Shiba Typer")]
[assembly: AssemblyCompany("Command Play")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace ShibaTyper
{
    static class Native
    {
        public const int WH_KEYBOARD_LL = 13, WH_MOUSE_LL = 14;
        public const int WM_KEYDOWN = 0x100, WM_KEYUP = 0x101, WM_SYSKEYDOWN = 0x104, WM_SYSKEYUP = 0x105;
        public const int WM_MOUSEMOVE = 0x200, WM_LBUTTONDOWN = 0x201, WM_RBUTTONDOWN = 0x204, WM_MBUTTONDOWN = 0x207, WM_MOUSEWHEEL = 0x20A;
        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
        [StructLayout(LayoutKind.Sequential)] public struct KBDLLHOOKSTRUCT { public uint vkCode, scanCode, flags, time; public IntPtr extra; }
        [DllImport("user32.dll", SetLastError = true)] public static extern IntPtr SetWindowsHookEx(int idHook, HookProc fn, IntPtr hMod, uint threadId);
        [DllImport("user32.dll")] public static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr GetModuleHandle(string name);
        [DllImport("user32.dll")] public static extern bool ReleaseCapture();
        [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);
        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();

        [StructLayout(LayoutKind.Sequential)] public struct POINT { public int x, y; public POINT(int x, int y) { this.x = x; this.y = y; } }
        [StructLayout(LayoutKind.Sequential)] public struct SIZE { public int cx, cy; public SIZE(int x, int y) { cx = x; cy = y; } }
        [StructLayout(LayoutKind.Sequential, Pack = 1)] public struct BLENDFUNCTION { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }
        [DllImport("user32.dll", SetLastError = true)] public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pprSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);
        [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll")] public static extern IntPtr CreateCompatibleDC(IntPtr hDC);
        [DllImport("gdi32.dll")] public static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll")] public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
        [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr hObject);
    }

    // 애니메이션·기록 (웹 버전 core.js 와 같은 규칙)
    class Shiba
    {
        public readonly Image[] Frames = new Image[6];
        long lU, rU, mU, bounce; int alt;
        readonly Queue<long> stamps = new Queue<long>();
        public string Day; public int Count, Best; public bool ShowStats = true;
        readonly Stopwatch sw = Stopwatch.StartNew();
        public long Now { get { return sw.ElapsedMilliseconds; } }
        readonly string statsPath;

        public Shiba()
        {
            var asm = Assembly.GetExecutingAssembly();
            for (int i = 0; i < 6; i++) using (var s = asm.GetManifestResourceStream("f" + i + ".png")) Frames[i] = Image.FromStream(s);
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ShibaTyper");
            try { Directory.CreateDirectory(dir); } catch { }
            statsPath = Path.Combine(dir, "stats.txt");
            Load();
        }
        static string Today() { return DateTime.Now.ToString("yyyy-MM-dd"); }
        void Load()
        {
            Day = Today(); Count = 0; Best = 0;
            try { var p = File.ReadAllText(statsPath).Split(','); if (p.Length >= 3 && p[0] == Day) { Count = int.Parse(p[1]); Best = int.Parse(p[2]); } } catch { }
        }
        public void Save() { try { File.WriteAllText(statsPath, Day + "," + Count + "," + Best); } catch { } }
        public void Reset() { Day = Today(); Count = 0; Best = 0; stamps.Clear(); Save(); }

        public void Key(char side)
        {
            long t = Now; if (Day != Today()) Reset();
            if (side == 'B') { lU = rU = t + 110; }
            else if (side == 'L') lU = t + 100;
            else if (side == 'R') rU = t + 100;
            else { if ((alt ^= 1) == 1) lU = t + 100; else rU = t + 100; }
            bounce = t; Count++; stamps.Enqueue(t); Cpm(); if (Count % 20 == 0) Save();
        }
        public void Mouse(bool click) { long t = Now; mU = t + (click ? 900 : 650); if (click) bounce = t; }
        public int Cpm()
        {
            long t = Now; while (stamps.Count > 0 && stamps.Peek() < t - 60000) stamps.Dequeue();
            if (stamps.Count < 3) return 0;
            double span = Math.Max(10, Math.Min(60, (t - stamps.Peek()) / 1000.0));
            int v = (int)Math.Round(stamps.Count * 60 / span); if (stamps.Count >= 8 && v > Best) Best = v; return v;
        }
        public int Frame()
        {
            long t = Now; bool L = t < lU, R = t < rU, M = t < mU;
            if (R) return L ? 3 : 2; if (M) return L ? 5 : 4; return L ? 1 : 0;
        }
        // 현재 장면을 W×H 투명 비트맵에 그림
        public void Draw(Graphics g, int W, int H)
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = SmoothingMode.AntiAlias; g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality; g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            var f = Frames[Frame()];
            double e = (Now - bounce) / 1000.0; double b = e < .12 ? Math.Sin(e / .12 * Math.PI) * .018 : 0;
            float top = ShowStats ? H * .13f : 0;
            float s = Math.Min((float)W / f.Width, (H - top) / f.Height); float w = f.Width * s, h = f.Height * s;
            var st = g.Save();
            g.TranslateTransform(W / 2f, H); g.ScaleTransform((float)(1 + b * .4), (float)(1 - b));
            g.DrawImage(f, -w / 2, -h, w, h);
            g.Restore(st);
            if (ShowStats)
            {
                int c = Cpm(); string txt = "오늘 " + Count.ToString("N0") + "타 · " + c + "타/분";
                using (var font = new Font("Malgun Gothic", H * .040f, FontStyle.Bold, GraphicsUnit.Pixel))
                {
                    var sz = g.MeasureString(txt, font);
                    float bw = sz.Width + H * .06f, bh = H * .085f, px = W / 2f, py = H * .065f;
                    using (var path = Pill(px - bw / 2, py - bh / 2, bw, bh))
                    using (var fill = new SolidBrush(Color.FromArgb(238, 255, 255, 255)))
                    using (var pen = new Pen(Color.FromArgb(34, 34, 34), Math.Max(1.5f, H * .008f)))
                    { g.FillPath(fill, path); g.DrawPath(pen, path); }
                    using (var br = new SolidBrush(c >= 300 ? Color.FromArgb(232, 89, 12) : Color.FromArgb(34, 34, 34)))
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        g.DrawString(txt, font, br, new RectangleF(px - bw / 2, py - bh / 2 + 1, bw, bh), sf);
                }
            }
        }
        static GraphicsPath Pill(float x, float y, float w, float h)
        {
            var p = new GraphicsPath(); float d = h;
            p.AddArc(x, y, d, d, 90, 180); p.AddArc(x + w - d, y, d, d, 270, 180); p.CloseFigure(); return p;
        }
    }

    class ShibaForm : Form
    {
        readonly Shiba shiba = new Shiba();
        readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        readonly NotifyIcon tray = new NotifyIcon();
        readonly ContextMenuStrip menu = new ContextMenuStrip();
        Native.HookProc kbProc, msProc; IntPtr kbHook = IntPtr.Zero, msHook = IntPtr.Zero;
        readonly HashSet<uint> down = new HashSet<uint>();
        float scale = 1f, dpi = 1f; long lastMove;
        const int BASE = 300;
        static readonly HashSet<uint> LEFT = new HashSet<uint> {
            0x1B, 0xC0, 0x31, 0x32, 0x33, 0x34, 0x35, 0x09, 0x51, 0x57, 0x45, 0x52, 0x54, 0x14,
            0x41, 0x53, 0x44, 0x46, 0x47, 0xA0, 0x10, 0x5A, 0x58, 0x43, 0x56, 0x42, 0xA2, 0x11, 0x5B, 0xA4, 0x12,
            0x70, 0x71, 0x72, 0x73, 0x74 };

        public ShibaForm()
        {
            FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; TopMost = true;
            StartPosition = FormStartPosition.Manual; Text = "시바 타자왕";
            using (var g = CreateGraphics()) dpi = g.DpiX / 96f;
            var wa = Screen.PrimaryScreen.WorkingArea; int sz = Px();
            Bounds = new Rectangle(wa.Right - sz - (int)(40 * dpi), wa.Bottom - sz, sz, sz);

            var asm = Assembly.GetExecutingAssembly();
            using (var s = asm.GetManifestResourceStream("icon.ico")) tray.Icon = new Icon(s);
            tray.Text = "시바 타자왕"; tray.Visible = true; tray.ContextMenuStrip = menu;
            tray.MouseClick += (o, e) => { if (e.Button == MouseButtons.Left) { Show(); Render(); } };
            BuildMenu();

            timer.Interval = 16; timer.Tick += (o, e) => Render(); timer.Start();
            Load += (o, e) => { Render(); InstallHooks(); };
            MouseDown += OnDown;
        }
        int Px() { return (int)Math.Round(BASE * scale * dpi); }

        protected override CreateParams CreateParams
        {
            get { var cp = base.CreateParams; cp.ExStyle |= 0x80000 /*LAYERED*/ | 0x80 /*TOOLWINDOW*/ | 0x08 /*TOPMOST*/ | 0x08000000 /*NOACTIVATE*/; return cp; }
        }
        protected override bool ShowWithoutActivation { get { return true; } }

        void BuildMenu()
        {
            menu.Items.Add(new ToolStripMenuItem("시바 타자왕") { Enabled = false });
            menu.Items.Add(new ToolStripSeparator());
            var size = new ToolStripMenuItem("크기");
            foreach (var p in new[] { Tuple.Create("작게", .7f), Tuple.Create("보통", 1f), Tuple.Create("크게", 1.4f) })
            {
                var it = new ToolStripMenuItem(p.Item1) { Checked = p.Item2 == 1f }; float v = p.Item2;
                it.Click += (o, e) => { foreach (ToolStripMenuItem x in size.DropDownItems) x.Checked = false; it.Checked = true; SetScale(v); };
                size.DropDownItems.Add(it);
            }
            menu.Items.Add(size);
            var stats = new ToolStripMenuItem("기록 말풍선 보이기") { Checked = true, CheckOnClick = true };
            stats.Click += (o, e) => shiba.ShowStats = stats.Checked; menu.Items.Add(stats);
            var top = new ToolStripMenuItem("항상 위에") { Checked = true, CheckOnClick = true };
            top.Click += (o, e) => TopMost = top.Checked; menu.Items.Add(top);
            var home = new ToolStripMenuItem("오른쪽 아래로 이동");
            home.Click += (o, e) => { var wa = Screen.PrimaryScreen.WorkingArea; Location = new Point(wa.Right - Width - (int)(40 * dpi), wa.Bottom - Height); }; menu.Items.Add(home);
            var reset = new ToolStripMenuItem("오늘 기록 초기화"); reset.Click += (o, e) => shiba.Reset(); menu.Items.Add(reset);
            menu.Items.Add(new ToolStripSeparator());
            var quit = new ToolStripMenuItem("종료"); quit.Click += (o, e) => Close(); menu.Items.Add(quit);
        }
        void SetScale(float s)
        {
            scale = s; int sz = Px(); var r = Bounds;
            Bounds = new Rectangle(r.Right - sz, r.Bottom - sz, sz, sz); Render();
        }
        void OnDown(object o, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { Native.ReleaseCapture(); Native.SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero); }
            else if (e.Button == MouseButtons.Right) menu.Show(Cursor.Position);
        }

        // ── 전역 키보드·마우스 훅 ──
        void InstallHooks()
        {
            try
            {
                kbProc = KbHook; msProc = MsHook;
                IntPtr mod = Native.GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName);
                kbHook = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, kbProc, mod, 0);
                msHook = Native.SetWindowsHookEx(Native.WH_MOUSE_LL, msProc, mod, 0);
            }
            catch { }
        }
        IntPtr KbHook(int nCode, IntPtr w, IntPtr l)
        {
            if (nCode >= 0)
            {
                var k = (Native.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(l, typeof(Native.KBDLLHOOKSTRUCT));
                int m = w.ToInt32();
                if (m == Native.WM_KEYDOWN || m == Native.WM_SYSKEYDOWN)
                {
                    if (down.Add(k.vkCode)) shiba.Key(k.vkCode == 0x20 ? 'B' : LEFT.Contains(k.vkCode) ? 'L' : 'R');
                }
                else if (m == Native.WM_KEYUP || m == Native.WM_SYSKEYUP) down.Remove(k.vkCode);
            }
            return Native.CallNextHookEx(kbHook, nCode, w, l);
        }
        IntPtr MsHook(int nCode, IntPtr w, IntPtr l)
        {
            if (nCode >= 0)
            {
                int m = w.ToInt32();
                if (m == Native.WM_MOUSEMOVE || m == Native.WM_MOUSEWHEEL) { long t = shiba.Now; if (t - lastMove > 40) { lastMove = t; shiba.Mouse(false); } }
                else if (m == Native.WM_LBUTTONDOWN || m == Native.WM_RBUTTONDOWN || m == Native.WM_MBUTTONDOWN) shiba.Mouse(true);
            }
            return Native.CallNextHookEx(msHook, nCode, w, l);
        }

        // ── 픽셀 단위 투명 창 그리기 ──
        int lastFrame = -1; long lastDraw;
        void Render()
        {
            if (!IsHandleCreated || !Visible) return;
            long t = shiba.Now; int fr = shiba.Frame();
            if (fr == lastFrame && t - lastDraw < 250) return; // 변화 없으면 가끔만 다시 그림
            lastFrame = fr; lastDraw = t;
            int W = Width, H = Height;
            using (var bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bmp)) shiba.Draw(g, W, H);
                SetBitmap(bmp);
            }
        }
        void SetBitmap(Bitmap bmp)
        {
            IntPtr screenDc = Native.GetDC(IntPtr.Zero), memDc = Native.CreateCompatibleDC(screenDc), hBmp = IntPtr.Zero, old = IntPtr.Zero;
            try
            {
                hBmp = bmp.GetHbitmap(Color.FromArgb(0)); old = Native.SelectObject(memDc, hBmp);
                var size = new Native.SIZE(bmp.Width, bmp.Height); var src = new Native.POINT(0, 0); var dst = new Native.POINT(Left, Top);
                var blend = new Native.BLENDFUNCTION { BlendOp = 0, BlendFlags = 0, SourceConstantAlpha = 255, AlphaFormat = 1 };
                Native.UpdateLayeredWindow(Handle, screenDc, ref dst, ref size, memDc, ref src, 0, ref blend, 2);
            }
            finally
            {
                Native.ReleaseDC(IntPtr.Zero, screenDc);
                if (hBmp != IntPtr.Zero) { Native.SelectObject(memDc, old); Native.DeleteObject(hBmp); }
                Native.DeleteDC(memDc);
            }
        }
        protected override void OnMove(EventArgs e) { base.OnMove(e); lastFrame = -1; Render(); }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            shiba.Save(); tray.Visible = false;
            if (kbHook != IntPtr.Zero) Native.UnhookWindowsHookEx(kbHook);
            if (msHook != IntPtr.Zero) Native.UnhookWindowsHookEx(msHook);
            base.OnFormClosing(e);
        }

        // 리눅스에서 그림만 검사하는 테스트 모드
        public static void TestRender(string outDir)
        {
            var s = new Shiba(); int n = 0;
            Action<string> snap = name => { using (var b = new Bitmap(300, 300, PixelFormat.Format32bppArgb)) { using (var g = Graphics.FromImage(b)) s.Draw(g, 300, 300); b.Save(Path.Combine(outDir, (n++) + "_" + name + ".png")); } };
            snap("idle"); for (int i = 0; i < 9; i++) s.Key('A'); s.Key('L'); snap("left"); s.Key('R'); snap("right"); s.Key('B'); snap("both");
            Thread.Sleep(150); s.Mouse(false); snap("mouse"); s.Key('L'); snap("mouse_left");
        }

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 2 && args[0] == "--test-render") { TestRender(args[1]); return; }
            bool created; using (var mx = new Mutex(true, "ShibaTyper_Single", out created))
            {
                if (!created) return;
                try { Native.SetProcessDPIAware(); } catch { }
                Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new ShibaForm());
            }
        }
    }
}
