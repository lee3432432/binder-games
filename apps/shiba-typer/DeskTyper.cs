// 타자치는 토끼 (Desk Typer) — 바탕화면 위 책상 뒤에서 내 키보드·마우스를 따라 하는 캐릭터
// .NET Framework 4.x (Windows 10/11 기본 탑재) 단일 실행 파일
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("타자치는 토끼")]
[assembly: AssemblyProduct("Desk Typer")]
[assembly: AssemblyCompany("Command Play")]
[assembly: AssemblyVersion("2.0.0.0")]
[assembly: AssemblyFileVersion("2.0.0.0")]

namespace ShibaTyper
{
    static class Native
    {
        public const int WH_KEYBOARD_LL = 13, WM_KEYDOWN = 0x100, WM_KEYUP = 0x101, WM_SYSKEYDOWN = 0x104, WM_SYSKEYUP = 0x105;
        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
        [StructLayout(LayoutKind.Sequential)] public struct KBDLLHOOKSTRUCT { public uint vkCode, scanCode, flags, time; public IntPtr extra; }
        [DllImport("user32.dll", SetLastError = true)] public static extern IntPtr SetWindowsHookEx(int idHook, HookProc fn, IntPtr hMod, uint threadId);
        [DllImport("user32.dll")] public static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr GetModuleHandle(string name);
        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int vk);
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

    struct KeyPos { public float u, v, w; public KeyPos(float u, float v, float w) { this.u = u; this.v = v; this.w = w; } }

    // 웹 버전 desk.js 와 같은 좌표·규칙
    class Desk
    {
        public const float SW = 1536, SH = 1024;
        static readonly PointF[] PAD = { new PointF(254, 641), new PointF(665, 542), new PointF(801, 713), new PointF(370, 822) };
        static readonly PointF[] KB = { new PointF(772, 566), new PointF(1355, 417), new PointF(1496, 550), new PointF(892, 709) };
        static readonly PointF SH_MOUSE = new PointF(575, 525), SH_KEY = new PointF(905, 488), UP_SLEEVE = new PointF(1015, 430);
        const float MOUSE_S = .36f, GRIP_S = .40f, POINT_S = .36f, UP_S = .34f;
        static readonly PointF GRIP_PALM = new PointF(.36f, .62f), GRIP_SLEEVE = new PointF(.80f, .30f), POINT_TIP = new PointF(.14f, .94f), POINT_SLEEVE = new PointF(.74f, .17f), UP_SLEEVE_A = new PointF(.42f, .95f);
        const float KW = (.715f - .035f) / 15f;
        static readonly Dictionary<uint, KeyPos> KEYS = new Dictionary<uint, KeyPos> {
            {0x1b,new KeyPos(0.0577f,0.1300f,1f)},
            {0x70,new KeyPos(0.1551f,0.1300f,1f)},
            {0x71,new KeyPos(0.2027f,0.1300f,1f)},
            {0x72,new KeyPos(0.2503f,0.1300f,1f)},
            {0x73,new KeyPos(0.2979f,0.1300f,1f)},
            {0x74,new KeyPos(0.3455f,0.1300f,1f)},
            {0x75,new KeyPos(0.3931f,0.1300f,1f)},
            {0x76,new KeyPos(0.4407f,0.1300f,1f)},
            {0x77,new KeyPos(0.4883f,0.1300f,1f)},
            {0x78,new KeyPos(0.5359f,0.1300f,1f)},
            {0x79,new KeyPos(0.5835f,0.1300f,1f)},
            {0x7a,new KeyPos(0.6311f,0.1300f,1f)},
            {0x7b,new KeyPos(0.6787f,0.1300f,1f)},
            {0xc0,new KeyPos(0.0577f,0.2900f,1f)},
            {0x31,new KeyPos(0.1030f,0.2900f,1f)},
            {0x32,new KeyPos(0.1483f,0.2900f,1f)},
            {0x33,new KeyPos(0.1937f,0.2900f,1f)},
            {0x34,new KeyPos(0.2390f,0.2900f,1f)},
            {0x35,new KeyPos(0.2843f,0.2900f,1f)},
            {0x36,new KeyPos(0.3297f,0.2900f,1f)},
            {0x37,new KeyPos(0.3750f,0.2900f,1f)},
            {0x38,new KeyPos(0.4203f,0.2900f,1f)},
            {0x39,new KeyPos(0.4657f,0.2900f,1f)},
            {0x30,new KeyPos(0.5110f,0.2900f,1f)},
            {0xbd,new KeyPos(0.5563f,0.2900f,1f)},
            {0xbb,new KeyPos(0.6017f,0.2900f,1f)},
            {0x8,new KeyPos(0.6697f,0.2900f,2f)},
            {0x9,new KeyPos(0.0690f,0.4400f,1.5f)},
            {0x51,new KeyPos(0.1257f,0.4400f,1f)},
            {0x57,new KeyPos(0.1710f,0.4400f,1f)},
            {0x45,new KeyPos(0.2163f,0.4400f,1f)},
            {0x52,new KeyPos(0.2617f,0.4400f,1f)},
            {0x54,new KeyPos(0.3070f,0.4400f,1f)},
            {0x59,new KeyPos(0.3523f,0.4400f,1f)},
            {0x55,new KeyPos(0.3977f,0.4400f,1f)},
            {0x49,new KeyPos(0.4430f,0.4400f,1f)},
            {0x4f,new KeyPos(0.4883f,0.4400f,1f)},
            {0x50,new KeyPos(0.5337f,0.4400f,1f)},
            {0xdb,new KeyPos(0.5790f,0.4400f,1f)},
            {0xdd,new KeyPos(0.6243f,0.4400f,1f)},
            {0xdc,new KeyPos(0.6810f,0.4400f,1.5f)},
            {0x14,new KeyPos(0.0758f,0.5800f,1.8f)},
            {0x41,new KeyPos(0.1370f,0.5800f,1f)},
            {0x53,new KeyPos(0.1823f,0.5800f,1f)},
            {0x44,new KeyPos(0.2277f,0.5800f,1f)},
            {0x46,new KeyPos(0.2730f,0.5800f,1f)},
            {0x47,new KeyPos(0.3183f,0.5800f,1f)},
            {0x48,new KeyPos(0.3637f,0.5800f,1f)},
            {0x4a,new KeyPos(0.4090f,0.5800f,1f)},
            {0x4b,new KeyPos(0.4543f,0.5800f,1f)},
            {0x4c,new KeyPos(0.4997f,0.5800f,1f)},
            {0xba,new KeyPos(0.5450f,0.5800f,1f)},
            {0xde,new KeyPos(0.5903f,0.5800f,1f)},
            {0xd,new KeyPos(0.6697f,0.5800f,2.2f)},
            {0xa0,new KeyPos(0.0849f,0.7200f,2.2f)},
            {0x5a,new KeyPos(0.1597f,0.7200f,1f)},
            {0x58,new KeyPos(0.2050f,0.7200f,1f)},
            {0x43,new KeyPos(0.2503f,0.7200f,1f)},
            {0x56,new KeyPos(0.2957f,0.7200f,1f)},
            {0x42,new KeyPos(0.3410f,0.7200f,1f)},
            {0x4e,new KeyPos(0.3863f,0.7200f,1f)},
            {0x4d,new KeyPos(0.4317f,0.7200f,1f)},
            {0xbc,new KeyPos(0.4770f,0.7200f,1f)},
            {0xbe,new KeyPos(0.5223f,0.7200f,1f)},
            {0xbf,new KeyPos(0.5677f,0.7200f,1f)},
            {0xa1,new KeyPos(0.6606f,0.7200f,2.4f)},
            {0xa2,new KeyPos(0.0622f,0.8700f,1.2f)},
            {0x5b,new KeyPos(0.1211f,0.8700f,1.2f)},
            {0xa4,new KeyPos(0.1801f,0.8700f,1.2f)},
            {0x20,new KeyPos(0.3614f,0.8700f,6f)},
            {0xa5,new KeyPos(0.5291f,0.8700f,1.2f)},
            {0x15,new KeyPos(0.5835f,0.8700f,1.1f)},
            {0x19,new KeyPos(0.1801f,0.8700f,1.2f)},
            {0xa3,new KeyPos(0.6787f,0.8700f,1.2f)},
            {0x26,new KeyPos(0.7975f,0.7200f,1f)},
            {0x28,new KeyPos(0.7975f,0.8700f,1f)},
            {0x25,new KeyPos(0.7584f,0.8700f,1f)},
            {0x27,new KeyPos(0.8366f,0.8700f,1f)},
            {0x2d,new KeyPos(0.7584f,0.2900f,1f)},
            {0x24,new KeyPos(0.7975f,0.2900f,1f)},
            {0x21,new KeyPos(0.8366f,0.2900f,1f)},
            {0x2e,new KeyPos(0.7584f,0.4400f,1f)},
            {0x23,new KeyPos(0.7975f,0.4400f,1f)},
            {0x22,new KeyPos(0.8366f,0.4400f,1f)},
            {0x67,new KeyPos(0.8780f,0.4400f,1f)},
            {0x68,new KeyPos(0.9125f,0.4400f,1f)},
            {0x69,new KeyPos(0.9470f,0.4400f,1f)},
            {0x64,new KeyPos(0.8780f,0.5800f,1f)},
            {0x65,new KeyPos(0.9125f,0.5800f,1f)},
            {0x66,new KeyPos(0.9470f,0.5800f,1f)},
            {0x61,new KeyPos(0.8780f,0.7200f,1f)},
            {0x62,new KeyPos(0.9125f,0.7200f,1f)},
            {0x63,new KeyPos(0.9470f,0.7200f,1f)},
            {0x60,new KeyPos(0.8964f,0.8700f,1.6f)},
            {0x10,new KeyPos(0.0849f,0.72f,2.2f)},
            {0x11,new KeyPos(0.0622f,0.87f,1.2f)},
            {0x12,new KeyPos(0.1801f,0.87f,1.2f)}
        };
        readonly Image scene, mouse, grip, point, up;
        Bitmap cScene; Bitmap cMouse, cGrip, cPoint, cPointF, cUp; float cs = -1;
        readonly Stopwatch sw = Stopwatch.StartNew();
        public long Now { get { return sw.ElapsedMilliseconds; } }
        class Hit { public KeyPos k; public long t; }
        readonly List<Hit> hits = new List<Hit>();
        KeyPos press; bool hasPress; long pressUntil, click;
        float mx = .5f, my = .5f, tx = .5f, ty = .5f;

        static Image Res(string n) { using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(n)) return new Bitmap(Image.FromStream(s)); }
        public Desk() { scene = Res("scene.png"); mouse = Res("mouse.png"); grip = Res("hand_grip.png"); point = Res("hand_point.png"); up = Res("hand_up.png"); }

        public void Key(uint vk)
        {
            KeyPos k; if (!KEYS.TryGetValue(vk, out k)) k = KEYS[0x46];
            hits.Add(new Hit { k = k, t = Now }); if (hits.Count > 12) hits.RemoveAt(0);
            press = k; hasPress = true; pressUntil = Now + 120;
        }
        public void Cursor(float x, float y) { tx = Math.Max(0, Math.Min(1, x)); ty = Math.Max(0, Math.Min(1, y)); }
        public void Click() { click = Now; }

        static PointF Lerp(PointF a, PointF b, float t) { return new PointF(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t); }
        static PointF Quad(PointF[] q, float u, float v) { return Lerp(Lerp(q[0], q[1], u), Lerp(q[3], q[2], u), v); }

        static Bitmap Scaled(Image src, float s, bool flip)
        {
            int w = Math.Max(1, (int)Math.Round(src.Width * s)), h = Math.Max(1, (int)Math.Round(src.Height * s));
            var b = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
            using (var g = Graphics.FromImage(b))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.PixelOffsetMode = PixelOffsetMode.HighQuality; g.CompositingQuality = CompositingQuality.HighQuality;
                if (flip) { g.TranslateTransform(w, 0); g.ScaleTransform(-1, 1); }
                g.DrawImage(src, 0, 0, w, h);
            }
            return b;
        }
        void Cache(float s)
        {
            if (Math.Abs(s - cs) < 1e-4) return; cs = s;
            foreach (var b in new[] { cScene, cMouse, cGrip, cPoint, cPointF, cUp }) if (b != null) b.Dispose();
            cScene = Scaled(scene, s, false); cMouse = Scaled(mouse, s * MOUSE_S, false); cGrip = Scaled(grip, s * GRIP_S, false);
            cPoint = Scaled(point, s * POINT_S, false); cPointF = Scaled(point, s * POINT_S, true); cUp = Scaled(up, s * UP_S, false);
        }
        // 장면좌표 → 픽셀, 기준점(anchor)을 at 에 맞춰 그리고, 다른 기준점(other) 위치를 돌려줌
        static PointF Place(Bitmap b, PointF anchor, bool flip, PointF at, PointF other, out PointF topLeft)
        {
            float ax = (flip ? 1 - anchor.X : anchor.X) * b.Width, ay = anchor.Y * b.Height;
            topLeft = new PointF(at.X - ax, at.Y - ay);
            return new PointF(topLeft.X + (flip ? 1 - other.X : other.X) * b.Width, topLeft.Y + other.Y * b.Height);
        }
        static void Arm(Graphics g, PointF a, PointF b, float bend, float s)
        {
            var c = new PointF((a.X + b.X) / 2, (a.Y + b.Y) / 2 - bend * s);
            PointF c1 = new PointF(a.X + 2f / 3 * (c.X - a.X), a.Y + 2f / 3 * (c.Y - a.Y)), c2 = new PointF(b.X + 2f / 3 * (c.X - b.X), b.Y + 2f / 3 * (c.Y - b.Y));
            using (var p1 = new Pen(Color.FromArgb(17, 17, 17), 40 * s) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (var p2 = new Pen(Color.FromArgb(251, 251, 255), 30 * s) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (var p3 = new Pen(Color.FromArgb(140, 180, 170, 210), 8 * s) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawBezier(p1, a, c1, c2, b); g.DrawBezier(p2, a, c1, c2, b);
                float o = 8 * s; g.DrawBezier(p3, new PointF(a.X, a.Y + o), new PointF(c1.X, c1.Y + o), new PointF(c2.X, c2.Y + o), new PointF(b.X, b.Y + o));
            }
        }
        public void Draw(Graphics g, int W, int H)
        {
            g.Clear(Color.Transparent);
            float s = Math.Min(W / SW, H / SH); Cache(s);
            float ox = (W - SW * s) / 2, oy = (H - SH * s) / 2;
            Func<PointF, PointF> P = p => new PointF(ox + p.X * s, oy + p.Y * s);
            g.SmoothingMode = SmoothingMode.AntiAlias; g.CompositingQuality = CompositingQuality.HighSpeed; g.InterpolationMode = InterpolationMode.NearestNeighbor; g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImageUnscaled(cScene, (int)Math.Round(ox), (int)Math.Round(oy));
            long t = Now;
            // 눌린 키 하늘색
            hits.RemoveAll(h => t - h.t >= 220);
            foreach (var h in hits)
            {
                float a = 1 - (t - h.t) / 220f; var k = h.k;
                float u0 = k.u - KW * k.w / 2, u1 = k.u + KW * k.w / 2, v0 = k.v - .065f, v1 = k.v + .065f;
                var pts = new[] { P(Quad(KB, u0, v0)), P(Quad(KB, u1, v0)), P(Quad(KB, u1, v1)), P(Quad(KB, u0, v1)) };
                using (var br = new SolidBrush(Color.FromArgb((int)(230 * a), 64, 224, 255))) g.FillPolygon(br, pts);
            }
            // 마우스
            mx += (tx - mx) * .35f; my += (ty - my) * .35f;
            var mc = P(Quad(PAD, .2f + .6f * mx, .22f + .56f * my)); float press_ = (t - click) < 120 ? 6 * s : 0;
            PointF tl;
            Place(cMouse, new PointF(.5f, .5f), false, mc, new PointF(.5f, .5f), out tl); g.DrawImageUnscaled(cMouse, (int)tl.X, (int)tl.Y);
            var palm = new PointF(mc.X, mc.Y - 14 * s + press_);
            var gs = Place(cGrip, GRIP_PALM, false, palm, GRIP_SLEEVE, out tl);
            Arm(g, P(SH_MOUSE), gs, 30, s); g.DrawImageUnscaled(cGrip, (int)tl.X, (int)tl.Y);
            // 키보드 손
            if (hasPress && t < pressUntil)
            {
                var kp = P(Quad(KB, press.u, press.v)); bool flip = kp.X > P(SH_KEY).X + 40 * s; var bmp = flip ? cPointF : cPoint;
                var sl = Place(bmp, POINT_TIP, flip, kp, POINT_SLEEVE, out tl); Arm(g, P(SH_KEY), sl, 20, s); g.DrawImageUnscaled(bmp, (int)tl.X, (int)tl.Y);
            }
            else
            {
                float wob = (float)Math.Sin(t / 260.0) * 4; var at = P(new PointF(UP_SLEEVE.X, UP_SLEEVE.Y + wob));
                Place(cUp, UP_SLEEVE_A, false, at, UP_SLEEVE_A, out tl); Arm(g, P(SH_KEY), at, -10, s); g.DrawImageUnscaled(cUp, (int)tl.X, (int)tl.Y);
            }
        }
    }

    class DeskForm : Form
    {
        readonly Desk desk = new Desk();
        readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        readonly NotifyIcon tray = new NotifyIcon();
        readonly ContextMenuStrip menu = new ContextMenuStrip();
        Native.HookProc kbProc; IntPtr kbHook = IntPtr.Zero;
        readonly HashSet<uint> down = new HashSet<uint>();
        float scale = 1f, dpi = 1f; bool lDown;
        const int BASE_W = 420;

        public DeskForm()
        {
            FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; TopMost = true; StartPosition = FormStartPosition.Manual; Text = "타자치는 토끼";
            using (var g = CreateGraphics()) dpi = g.DpiX / 96f;
            var wa = Screen.PrimaryScreen.WorkingArea; var sz = Px();
            Bounds = new Rectangle(wa.Left + (int)(wa.Width * .12), wa.Top + (int)(wa.Height * .18), sz.Width, sz.Height);
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("icon.ico")) tray.Icon = new Icon(s);
            tray.Text = "타자치는 토끼"; tray.Visible = true; tray.ContextMenuStrip = menu;
            BuildMenu();
            timer.Interval = 30; timer.Tick += (o, e) => Tick(); timer.Start();
            Load += (o, e) => { Render(); InstallHook(); };
            MouseDown += (o, e) => { if (e.Button == MouseButtons.Left) { Native.ReleaseCapture(); Native.SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero); } else if (e.Button == MouseButtons.Right) menu.Show(Cursor.Position); };
        }
        Size Px() { int w = (int)Math.Round(BASE_W * scale * dpi); return new Size(w, (int)Math.Round(w * Desk.SH / Desk.SW)); }
        protected override CreateParams CreateParams { get { var cp = base.CreateParams; cp.ExStyle |= 0x80000 | 0x80 | 0x08 | 0x08000000; return cp; } }
        protected override bool ShowWithoutActivation { get { return true; } }

        void BuildMenu()
        {
            menu.Items.Add(new ToolStripMenuItem("타자치는 토끼") { Enabled = false });
            menu.Items.Add(new ToolStripSeparator());
            var size = new ToolStripMenuItem("크기");
            foreach (var p in new[] { Tuple.Create("작게", .7f), Tuple.Create("보통", 1f), Tuple.Create("크게", 1.4f) })
            {
                var it = new ToolStripMenuItem(p.Item1) { Checked = p.Item2 == 1f }; float v = p.Item2;
                it.Click += (o, e) => { foreach (ToolStripMenuItem x in size.DropDownItems) x.Checked = false; it.Checked = true; scale = v; var sz = Px(); Bounds = new Rectangle(Left, Top, sz.Width, sz.Height); Render(); };
                size.DropDownItems.Add(it);
            }
            menu.Items.Add(size);
            var top = new ToolStripMenuItem("항상 위에") { Checked = true, CheckOnClick = true }; top.Click += (o, e) => TopMost = top.Checked; menu.Items.Add(top);
            var home = new ToolStripMenuItem("처음 위치로"); home.Click += (o, e) => { var wa = Screen.PrimaryScreen.WorkingArea; Location = new Point(wa.Left + (int)(wa.Width * .12), wa.Top + (int)(wa.Height * .18)); }; menu.Items.Add(home);
            menu.Items.Add(new ToolStripSeparator());
            var quit = new ToolStripMenuItem("종료"); quit.Click += (o, e) => Close(); menu.Items.Add(quit);
        }
        void InstallHook()
        {
            try { kbProc = KbHook; kbHook = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, kbProc, Native.GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName), 0); } catch { }
        }
        IntPtr KbHook(int nCode, IntPtr w, IntPtr l)
        {
            if (nCode >= 0)
            {
                var k = (Native.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(l, typeof(Native.KBDLLHOOKSTRUCT)); int m = w.ToInt32();
                if (m == Native.WM_KEYDOWN || m == Native.WM_SYSKEYDOWN) { if (down.Add(k.vkCode)) desk.Key(k.vkCode); }
                else if (m == Native.WM_KEYUP || m == Native.WM_SYSKEYUP) down.Remove(k.vkCode);
            }
            return Native.CallNextHookEx(kbHook, nCode, w, l);
        }
        void Tick()
        {
            // 마우스 위치·클릭은 훅 없이 화면 전체 기준으로 읽음
            var vs = SystemInformation.VirtualScreen; var c = Cursor.Position;
            desk.Cursor((c.X - vs.Left) / (float)Math.Max(1, vs.Width), (c.Y - vs.Top) / (float)Math.Max(1, vs.Height));
            bool l = (Native.GetAsyncKeyState(0x01) & 0x8000) != 0 || (Native.GetAsyncKeyState(0x02) & 0x8000) != 0;
            if (l && !lDown) desk.Click(); lDown = l;
            Render();
        }
        void Render()
        {
            if (!IsHandleCreated || !Visible) return;
            using (var bmp = new Bitmap(Width, Height, PixelFormat.Format32bppPArgb))
            {
                using (var g = Graphics.FromImage(bmp)) desk.Draw(g, Width, Height);
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
            finally { Native.ReleaseDC(IntPtr.Zero, screenDc); if (hBmp != IntPtr.Zero) { Native.SelectObject(memDc, old); Native.DeleteObject(hBmp); } Native.DeleteDC(memDc); }
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            tray.Visible = false; if (kbHook != IntPtr.Zero) Native.UnhookWindowsHookEx(kbHook); base.OnFormClosing(e);
        }

        public static void TestRender(string outDir)
        {
            var d = new Desk(); int n = 0;
            Action<string> snap = name => { using (var b = new Bitmap(630, 420, PixelFormat.Format32bppPArgb)) { using (var g = Graphics.FromImage(b)) d.Draw(g, 630, 420); b.Save(Path.Combine(outDir, (n++) + "_" + name + ".png")); } };
            d.Cursor(.1f, .1f); for (int i = 0; i < 30; i++) { using (var b = new Bitmap(630, 420)) using (var g = Graphics.FromImage(b)) d.Draw(g, 630, 420); }
            snap("idle_mouseTL"); d.Key(0x41); snap("key_A"); d.Cursor(.9f, .9f); for (int i = 0; i < 30; i++) { using (var b = new Bitmap(630, 420)) using (var g = Graphics.FromImage(b)) d.Draw(g, 630, 420); }
            d.Key(0x50); snap("key_P_mouseBR"); d.Key(0x20); snap("space");
        }

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 2 && args[0] == "--test-render") { TestRender(args[1]); return; }
            bool created; using (var mx = new Mutex(true, "DeskTyper_Single", out created))
            {
                if (!created) return;
                try { Native.SetProcessDPIAware(); } catch { }
                Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new DeskForm());
            }
        }
    }
}
