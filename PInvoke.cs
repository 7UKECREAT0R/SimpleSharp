using System;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Permissions;
using System.Text;

namespace SimpleSharp
{
    public static class PInvoke
    {
        public static readonly byte FULL_BLOCK = (byte)219;

        [StructLayout(LayoutKind.Sequential)]
        public struct MousePoint
        {
            public int X;
            public int Y;

            public MousePoint(int x, int y)
            {
                X = x;
                Y = y;
            }
        }
        [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out MousePoint lpMousePoint);

        [DllImport("user32.dll")]
        private static extern void mouse_event
        (int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
        [Flags]
        public enum MouseEventType
        {
            LeftDown = 0x00000002,
            LeftUp = 0x00000004,
            MiddleDown = 0x00000020,
            MiddleUp = 0x00000040,
            Move = 0x00000001,
            Absolute = 0x00008000,
            RightDown = 0x00000008,
            RightUp = 0x00000010
        }

        const int STD_INPUT_HANDLE = -10;
        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);
        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
        const uint ENABLE_QUICK_EDIT = 0x0040;

        public static void DisableQuickEdit()
        {
            IntPtr consoleHandle = GetStdHandle(STD_INPUT_HANDLE);

            uint consoleMode;
            if (!GetConsoleMode(consoleHandle, out consoleMode))
            {
                return;
            }
            consoleMode &= ~ENABLE_QUICK_EDIT;

            if (!SetConsoleMode(consoleHandle, consoleMode))
            {
                return;
            }
            return;
        }
        public static void EnableQuickEdit()
        {
            IntPtr consoleHandle = GetStdHandle(STD_INPUT_HANDLE);

            uint consoleMode;
            if (!GetConsoleMode(consoleHandle, out consoleMode))
            {
                return;
            }
            consoleMode &= ENABLE_QUICK_EDIT;

            if (!SetConsoleMode(consoleHandle, consoleMode))
            {
                return;
            }
            return;
        }
        public static void SetQuickEditMode(bool mode)
        {
            if (mode)
                EnableQuickEdit();
            else
                DisableQuickEdit();
        }

        public static Keys KeyConvert(ConsoleKey ck)
        {
            return (Keys)Enum.Parse(typeof(Keys), ck.ToString());
        }
        public static ConsoleKey KeyConvert(Keys k)
        {
            return (ConsoleKey)Enum.Parse(typeof(ConsoleKey), k.ToString());
        }
        public static bool IsKeyDown(Keys k)
        {
            short state = GetKeyState(k);
            return Convert.ToBoolean(state & KEY_PRESSED);
        }
        [DllImport("user32.dll")]
        private static extern short GetKeyState(Keys nVirtKey);
        private const int KEY_PRESSED = 0x8000;

        // Reading Console Characters
        //(this code is a crapshow trust me dont even try to read it)
        private const int consoleNStd = -11;
        [StructLayout(LayoutKind.Sequential)]
        struct LowLevelCoord { public short X, Y; }
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);
        public static IntPtr _chand = IntPtr.Zero;
        public static IntPtr GetConsoleHandle()
        {
            if (_chand.Equals(IntPtr.Zero))
            {
                _chand = GetStdHandle(consoleNStd);
            }
            return _chand;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadConsoleOutputCharacter
        (IntPtr hConsoleOutput, [Out] StringBuilder lpCharacter,
        uint length, LowLevelCoord bufferCoord, out uint lpNumberOfCharactersRead);
        public static char ReadCharacter(int x, int y)
        {
            IntPtr cHandle = GetConsoleHandle();
            if (cHandle == IntPtr.Zero) return '\0';
            LowLevelCoord pos = new LowLevelCoord()
            {
                X = (short)x,
                Y = (short)y
            };
            StringBuilder res = new StringBuilder(1);
            //int bw = Console.BufferWidth, bh = Console.BufferHeight;
            if (ReadConsoleOutputCharacter(cHandle, res, 1, pos, out _))
            {
                if (res.Length == 0)
                {
                    return ' ';
                }
                return res[0];
            }
            else { return '\0'; }
        }

        // Alternate console writing (not as much of a crapshow but still a crapshow)
        static LowLevelCharInfo[] membuffer = null;
        [StructLayout(LayoutKind.Explicit)]
        public struct LowLevelJoinedChar
        {
            [FieldOffset(0)] public char UnicodeChar;
            [FieldOffset(0)] public byte AsciiChar;
        }
        [StructLayout(LayoutKind.Explicit)]
        public struct LowLevelCharInfo
        {
            [FieldOffset(0)] public LowLevelJoinedChar Char;
            [FieldOffset(2)] public short Attributes;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct LowLevelRect
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct CONSOLE_SCREEN_BUFFER_INFO
        {
            public LowLevelCoord dwSize;
            public LowLevelCoord dwCursorPosition;
            public int wAttributes;
            public LowLevelRect srWindow;
            public LowLevelCoord dwMaximumWindowSize;
        }
        [DllImport("kernel32.dll", EntryPoint = "WriteConsoleOutputA", CharSet = CharSet.Ansi, SetLastError = true)]
        static extern bool WriteConsoleOutput(
                IntPtr hConsoleOutput,
                LowLevelCharInfo[] lpBuffer,
                LowLevelCoord dwBufferSize,
                LowLevelCoord dwBufferCoord,
                ref LowLevelRect lpWriteRegion);

        [DllImport("kernel32.dll", EntryPoint = "FillConsoleOutputCharacter", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int FillConsoleOutputCharacter(int hConsoleOutput, byte cCharacter, int nLength, LowLevelCoord dwWriteCoord, ref int lpNumberOfCharsWritten);

        [DllImport("kernel32.dll", EntryPoint = "GetConsoleScreenBufferInfo", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int GetConsoleScreenBufferInfo(int hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);
        public static LowLevelCharInfo[] _FetchEntireBuffer(int w, int h)
        {
            //Console.WriteLine("Fetching buffer...");
            LowLevelCharInfo[] buffer = new LowLevelCharInfo[w * h];
            int index = -1;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    //Console.WriteLine("perform cRead {0}/{1}", index, w * h);
                    index++;
                    buffer[index].Char.AsciiChar = Convert.ToByte(ReadCharacter(x, y));
                    buffer[index].Attributes = (short)ConsoleColor.White;
                }
            }
            //Console.WriteLine("Fetched buffer.");
            return buffer;
        }

        /// <summary>
        /// Ensure the memory buffer can be written to.
        /// </summary>
        public static void VerifyMemBuffer()
        {
            int w = Console.WindowWidth, h = Console.WindowHeight;
            if (membuffer == null)
            {
                membuffer = _FetchEntireBuffer(w, h);
            }
        }
        public static void ClearMemBuffer()
        {
            Array.Fill(membuffer, new LowLevelCharInfo());
        }
        public static void Clear()
        {
            Console.Clear();
            int w = Console.WindowWidth, h = Console.WindowHeight;
            if (membuffer == null)
            {
                membuffer = _FetchEntireBuffer(w, h);
                return;
            }
            ClearMemBuffer();
        }
        public static int IntLerp(int a, int b, double t)
        {
            return (int)Math.Round(Lerp(a, b, t));
        }
        public static double Lerp(double a, double b, double t)
        {
            return (1 - t) * a + t * b;
        }
        public static double Pythag(double a, double b)
        {
            return Math.Sqrt(Math.Pow(a, 2) + Math.Pow(b, 2));
        }
        public static double Distance(double x1, double y1, double x2, double y2)
        {
            double width, height;
            width = Math.Abs(x1 - x2);
            height = Math.Abs(y1 - y2);
            return Pythag(width, height);
        }
        public static int DistanceInt(int x1, int y1, int x2, int y2)
        {
            return (int)Math.Ceiling(Distance(x1, y1, x2, y2));
        }
        public static void WriteAt(int _x, int _y, object _text)
        {
            string text = _text.ToString();

            int w = Console.WindowWidth;
            VerifyMemBuffer();

            for (int ch = 0; ch < text.Length; ch++)
            {
                int yoffset = _y * w;
                int xoffset = _x;
                int oset = yoffset + xoffset;
                char c = text[ch];
                membuffer[ch + oset].Char.AsciiChar = Convert.ToByte(c);
                membuffer[ch + oset].Attributes = (short)ConsoleColor.White;
            }
            return;
        }
        public static void WriteAt(int _x, int _y, object _text, ConsoleColor _color)
        {
            string text = _text.ToString();

            int w = Console.WindowWidth;
            VerifyMemBuffer();

            for (int ch = 0; ch < text.Length; ch++)
            {
                int yoffset = _y * w;
                int xoffset = _x;
                int oset = yoffset + xoffset;
                char c = text[ch];
                membuffer[ch + oset].Char.AsciiChar = Convert.ToByte(c);
                membuffer[ch + oset].Attributes = (short)_color;
            }
            return;
        }
        public static void WriteLineAt(int x1, int y1, int x2, int y2)
        {
            WriteLineAt(x1, y1, x2, y2, ConsoleColor.White);
        }
        public static void WriteLineAt(int x1, int y1, int x2, int y2, ConsoleColor color)
        {
            VerifyMemBuffer();
            int w = Console.WindowWidth;

            int passes = DistanceInt(x1, y1, x2, y2);
            double _t = 1.0 / (double)passes;
            for (int i = 0; i <= passes; i++)
            {
                double t = _t * (double)i;
                int x = IntLerp(x1, x2, t);
                int y = IntLerp(y1, y2, t);
                int yoffset = y * w;
                int xoffset = x;
                int oset = yoffset + xoffset;
                membuffer[oset].Char.AsciiChar = FULL_BLOCK;
                membuffer[oset].Attributes = (short)color;
            }
        }
        public static void WriteRectangleAt(int x1, int y1, int x2, int y2)
        {
            WriteRectangleAt(x1, y1, x2, y2, ConsoleColor.White);
        }
        public static void WriteRectangleAt(int x1, int y1, int x2, int y2, ConsoleColor color)
        {
            VerifyMemBuffer();

            // just realized i could have used ref but eh
            ArrangeMinMaxCoordinates(x1, y1, x2, y2, out x1, out y1, out x2, out y2);
            int w = Console.WindowWidth;

            for (int x = x1; x <= x2; x++)
            {
                for (int y = y1; y <= y2; y++)
                {
                    int yoffset = y * w;
                    int xoffset = x;
                    int oset = yoffset + xoffset;
                    membuffer[oset].Char.AsciiChar = FULL_BLOCK;
                    membuffer[oset].Attributes = (short)color;
                }
            }
        }
        public static void WriteHollowRectangleAt(int x1, int y1, int x2, int y2)
        {
            WriteHollowRectangleAt(x1, y1, x2, y2, ConsoleColor.White);
        }
        public static void WriteHollowRectangleAt(int x1, int y1, int x2, int y2, ConsoleColor color)
        {
            VerifyMemBuffer();
            ArrangeMinMaxCoordinates(x1, y1, x2, y2, out x1, out y1, out x2, out y2);

            WriteLineAt(x1, y1, x2, y1, color);
            WriteLineAt(x1, y1, x1, y2, color);
            WriteLineAt(x1, y2, x2, y2, color);
            WriteLineAt(x2, y1, x2, y2, color);
        }

        /// <summary>
        /// Arranges coordinates so that x1 and y1 are the top-left coordinates.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        public static void ArrangeMinMaxCoordinates(int x1, int y1, int x2, int y2,
            out int ox1, out int oy1, out int ox2, out int oy2)
        {
            int maxX, maxY, minX, minY;
            maxX = Math.Max(x1, x2);
            minX = Math.Min(x1, x2);
            maxY = Math.Max(y1, y2);
            minY = Math.Min(y1, y2);

            ox1 = minX;
            oy1 = minY;
            ox2 = maxX;
            oy2 = maxY;
            return;
        }

        public static void RenderWLQUEUE()
        {
            IntPtr cHandle = GetConsoleHandle();

            int w = Console.WindowWidth, h = Console.WindowHeight;
            LowLevelCoord sz = new LowLevelCoord() { X = (short)w, Y = (short)h };

            LowLevelRect zn = new LowLevelRect()
            {
                Left = 0,
                Top = 0,
                Right = (short)w,
                Bottom = (short)h
            };
            LowLevelCharInfo[] cbuffer = membuffer;
            WriteConsoleOutput(cHandle, cbuffer, sz,
                new LowLevelCoord() { X = 0, Y = 0 }, ref zn);
            ClearMemBuffer();
        }

        //static CONSOLE_SCREEN_BUFFER_INFO? bufferInfo;
        /*static void PInvokeClear()
        {
            int written = 0;
            IntPtr cHandle = GetConsoleHandle();

            if (!bufferInfo.HasValue)
            {
                var tempbf = new CONSOLE_SCREEN_BUFFER_INFO();
                GetConsoleScreenBufferInfo(cHandle.ToInt32(), ref tempbf);
                bufferInfo = tempbf;
            }

            LowLevelCoord home; home.X = home.Y = 0;
            var inf = bufferInfo.Value;
            LowLevelCoord sz = inf.dwSize;

            FillConsoleOutputCharacter(cHandle.ToInt32(),
                (byte)32, sz.X * sz.Y, home, ref written);
            return;
        }*/
        // ---------------------------------
        // Not P/Invoke but fits here because of its unlikely use-cases
        public static void CMD(string c)
        {
            Process.Start("cmd.exe", c).WaitForExit();
        }
        public static void SendKeysInternal(string keys)
        {
            SendKeysReImplementation.Send(keys);
        }

        public static void SendMouseEventInternal(MouseEventType flag, MousePoint point)
        {
            mouse_event((int)flag,
                point.X, point.Y, 0, 0);
        }
        public static void SendMouseEventInternalFromCommand(MouseEventType flag)
        {
            MousePoint point;
            var success = GetCursorPos(out point);
            if (!success) return;

            mouse_event((int)flag,
                point.X, point.Y, 0, 0);
        }
        public static void SendMouseEventInternalFromCommand(MouseEventType flag, int x, int y)
        {
            SendMouseEventInternal(flag, new MousePoint(x, y));
        }
        public static void SendLeftClickInternal()
        {
            MousePoint point;
            var success = GetCursorPos(out point);
            if (!success) return;

            SendMouseEventInternal(MouseEventType.LeftDown, point);
            SendMouseEventInternal(MouseEventType.LeftUp, point);
        }
        public static void SendRightClickInternal()
        {
            MousePoint point;
            var success = GetCursorPos(out point);
            if (!success) return;

            SendMouseEventInternal(MouseEventType.RightDown, point);
            SendMouseEventInternal(MouseEventType.RightUp, point);
        }
        public static void SendMiddleClickInternal()
        {
            MousePoint point;
            var success = GetCursorPos(out point);
            if (!success) return;

            SendMouseEventInternal(MouseEventType.MiddleDown, point);
            SendMouseEventInternal(MouseEventType.MiddleUp, point);
        }
        public static int GetMousePositionOnAxis(int axis)
        {
            // 0 = x, 1 = y
            MousePoint point;
            var success = GetCursorPos(out point);
            if (!success) return 0;

            if (axis == 0)
                return point.X;
            else
                return point.Y;
        }
        public static void SetMousePositionInternal(int x, int y)
        {
            SetCursorPos(x, y);
        }
    }
    public enum Keys
    {
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.KeyCode"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The bit mask to extract a key code from a key value.
        ///       
        ///    </para>
        /// </devdoc>
        KeyCode = 0x0000FFFF,

        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Modifiers"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The bit mask to extract modifiers from a key value.
        ///       
        ///    </para>
        /// </devdoc>
        Modifiers = unchecked((int)0xFFFF0000),

        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.None"]/*' />
        /// <devdoc>
        ///    <para>
        ///       No key pressed.
        ///    </para>
        /// </devdoc>
        None = 0x00,

        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.LButton"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The left mouse button.
        ///       
        ///    </para>
        /// </devdoc>
        LButton = 0x01,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.RButton"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The right mouse button.
        ///    </para>
        /// </devdoc>
        RButton = 0x02,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Cancel"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The CANCEL key.
        ///    </para>
        /// </devdoc>
        Cancel = 0x03,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.MButton"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The middle mouse button (three-button mouse).
        ///    </para>
        /// </devdoc>
        MButton = 0x04,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.XButton1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The first x mouse button (five-button mouse).
        ///    </para>
        /// </devdoc>
        XButton1 = 0x05,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.XButton2"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The second x mouse button (five-button mouse).
        ///    </para>
        /// </devdoc>
        XButton2 = 0x06,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Back"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The BACKSPACE key.
        ///    </para>
        /// </devdoc>
        Back = 0x08,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Tab"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The TAB key.
        ///    </para>
        /// </devdoc>
        Tab = 0x09,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.LineFeed"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The CLEAR key.
        ///    </para>
        /// </devdoc>
        LineFeed = 0x0A,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Clear"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The CLEAR key.
        ///    </para>
        /// </devdoc>
        Clear = 0x0C,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Return"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The RETURN key.
        ///
        ///    </para>
        /// </devdoc>
        Return = 0x0D,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Enter"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The ENTER key.
        ///       
        ///    </para>
        /// </devdoc>
        Enter = Return,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.ShiftKey"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The SHIFT key.
        ///    </para>
        /// </devdoc>
        ShiftKey = 0x10,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.ControlKey"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The CTRL key.
        ///    </para>
        /// </devdoc>
        ControlKey = 0x11,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Menu"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The ALT key.
        ///    </para>
        /// </devdoc>
        Menu = 0x12,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Pause"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The PAUSE key.
        ///    </para>
        /// </devdoc>
        Pause = 0x13,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Capital"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The CAPS LOCK key.
        ///
        ///    </para>
        /// </devdoc>
        Capital = 0x14,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.CapsLock"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The CAPS LOCK key.
        ///    </para>
        /// </devdoc>
        CapsLock = 0x14,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Kana"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The IME Kana mode key.
        ///    </para>
        /// </devdoc>
        KanaMode = 0x15,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.HanguelMode"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The IME Hanguel mode key.
        ///    </para>
        /// </devdoc>
        HanguelMode = 0x15,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.HangulMode"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The IME Hangul mode key.
        ///    </para>
        /// </devdoc>
        HangulMode = 0x15,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.JunjaMode"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The IME Junja mode key.
        ///    </para>
        /// </devdoc>
        JunjaMode = 0x17,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.FinalMode"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The IME Final mode key.
        ///    </para>
        /// </devdoc>
        FinalMode = 0x18,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.HanjaMode"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The IME Hanja mode key.
        ///    </para>
        /// </devdoc>
        HanjaMode = 0x19,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.KanjiMode"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The IME Kanji mode key.
        ///    </para>
        /// </devdoc>
        KanjiMode = 0x19,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Escape"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The ESC key.
        ///    </para>
        /// </devdoc>
        Escape = 0x1B,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.IMEConvert"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The IME Convert key.
        ///    </para>
        /// </devdoc>
        IMEConvert = 0x1C,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.IMENonconvert"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The IME NonConvert key.
        ///    </para>
        /// </devdoc>
        IMENonconvert = 0x1D,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.IMEAccept"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The IME Accept key.
        ///    </para>
        /// </devdoc>
        IMEAccept = 0x1E,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.IMEAceept"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The IME Accept key.
        ///    </para>
        /// </devdoc>
        IMEAceept = IMEAccept,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.IMEModeChange"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The IME Mode change request.
        ///    </para>
        /// </devdoc>
        IMEModeChange = 0x1F,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Space"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The SPACEBAR key.
        ///    </para>
        /// </devdoc>
        Space = 0x20,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Prior"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The PAGE UP key.
        ///    </para>
        /// </devdoc>
        Prior = 0x21,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.PageUp"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The PAGE UP key.
        ///    </para>
        /// </devdoc>
        PageUp = Prior,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Next"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The PAGE DOWN key.
        ///    </para>
        /// </devdoc>
        Next = 0x22,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.PageDown"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The PAGE DOWN key.
        ///    </para>
        /// </devdoc>
        PageDown = Next,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.End"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The END key.
        ///    </para>
        /// </devdoc>
        End = 0x23,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Home"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The HOME key.
        ///    </para>
        /// </devdoc>
        Home = 0x24,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Left"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The LEFT ARROW key.
        ///    </para>
        /// </devdoc>
        LeftArrow = 0x25,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Up"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The UP ARROW key.
        ///    </para>
        /// </devdoc>
        UpArrow = 0x26,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Right"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The RIGHT ARROW key.
        ///    </para>
        /// </devdoc>
        RightArrow = 0x27,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Down"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The DOWN ARROW key.
        ///    </para>
        /// </devdoc>
        DownArrow = 0x28,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Select"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The SELECT key.
        ///    </para>
        /// </devdoc>
        Select = 0x29,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Print"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The PRINT key.
        ///    </para>
        /// </devdoc>
        Print = 0x2A,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Execute"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The EXECUTE key.
        ///    </para>
        /// </devdoc>
        Execute = 0x2B,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Snapshot"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The PRINT SCREEN key.
        ///
        ///    </para>
        /// </devdoc>
        Snapshot = 0x2C,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.PrintScreen"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The PRINT SCREEN key.
        ///    </para>
        /// </devdoc>
        PrintScreen = Snapshot,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Insert"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The INS key.
        ///    </para>
        /// </devdoc>
        Insert = 0x2D,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Delete"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The DEL key.
        ///    </para>
        /// </devdoc>
        Delete = 0x2E,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Help"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The HELP key.
        ///    </para>
        /// </devdoc>
        Help = 0x2F,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.D0"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The 0 key.
        ///    </para>
        /// </devdoc>
        D0 = 0x30, // 0
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.D1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The 1 key.
        ///    </para>
        /// </devdoc>
        D1 = 0x31, // 1
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.D2"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The 2 key.
        ///    </para>
        /// </devdoc>
        D2 = 0x32, // 2
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.D3"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The 3 key.
        ///    </para>
        /// </devdoc>
        D3 = 0x33, // 3
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.D4"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The 4 key.
        ///    </para>
        /// </devdoc>
        D4 = 0x34, // 4
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.D5"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The 5 key.
        ///    </para>
        /// </devdoc>
        D5 = 0x35, // 5
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.D6"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The 6 key.
        ///    </para>
        /// </devdoc>
        D6 = 0x36, // 6
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.D7"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The 7 key.
        ///    </para>
        /// </devdoc>
        D7 = 0x37, // 7
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.D8"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The 8 key.
        ///    </para>
        /// </devdoc>
        D8 = 0x38, // 8
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.D9"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The 9 key.
        ///    </para>
        /// </devdoc>
        D9 = 0x39, // 9
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.A"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The A key.
        ///    </para>
        /// </devdoc>
        A = 0x41,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.B"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The B key.
        ///    </para>
        /// </devdoc>
        B = 0x42,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.C"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The C key.
        ///    </para>
        /// </devdoc>
        C = 0x43,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.D"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The D key.
        ///    </para>
        /// </devdoc>
        D = 0x44,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.E"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The E key.
        ///    </para>
        /// </devdoc>
        E = 0x45,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.F"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The F key.
        ///    </para>
        /// </devdoc>
        F = 0x46,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.G"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The G key.
        ///    </para>
        /// </devdoc>
        G = 0x47,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.H"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The H key.
        ///    </para>
        /// </devdoc>
        H = 0x48,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.I"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The I key.
        ///    </para>
        /// </devdoc>
        I = 0x49,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.J"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The J key.
        ///    </para>
        /// </devdoc>
        J = 0x4A,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.K"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The K key.
        ///    </para>
        /// </devdoc>
        K = 0x4B,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.L"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The L key.
        ///    </para>
        /// </devdoc>
        L = 0x4C,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.M"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The M key.
        ///    </para>
        /// </devdoc>
        M = 0x4D,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.N"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The N key.
        ///    </para>
        /// </devdoc>
        N = 0x4E,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.O"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The O key.
        ///    </para>
        /// </devdoc>
        O = 0x4F,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.P"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The P key.
        ///    </para>
        /// </devdoc>
        P = 0x50,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Q"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Q key.
        ///    </para>
        /// </devdoc>
        Q = 0x51,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.R"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The R key.
        ///    </para>
        /// </devdoc>
        R = 0x52,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.S"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The S key.
        ///    </para>
        /// </devdoc>
        S = 0x53,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.T"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The T key.
        ///    </para>
        /// </devdoc>
        T = 0x54,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.U"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The U key.
        ///    </para>
        /// </devdoc>
        U = 0x55,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.V"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The V key.
        ///    </para>
        /// </devdoc>
        V = 0x56,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.W"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The W key.
        ///    </para>
        /// </devdoc>
        W = 0x57,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.X"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The X key.
        ///    </para>
        /// </devdoc>
        X = 0x58,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Y"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Y key.
        ///    </para>
        /// </devdoc>
        Y = 0x59,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Z"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Z key.
        ///    </para>
        /// </devdoc>
        Z = 0x5A,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.LWin"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The left Windows logo key (Microsoft Natural Keyboard).
        ///    </para>
        /// </devdoc>
        LWin = 0x5B,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.RWin"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The right Windows logo key (Microsoft Natural Keyboard).
        ///    </para>
        /// </devdoc>
        RWin = 0x5C,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Apps"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Application key (Microsoft Natural Keyboard).
        ///    </para>
        /// </devdoc>
        Apps = 0x5D,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Sleep"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Computer Sleep key.
        ///    </para>
        /// </devdoc>
        Sleep = 0x5F,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.NumPad0"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The 0 key on the numeric keypad.
        ///    </para>
        /// </devdoc>
        // PM team has reviewed and decided on naming changes already
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        NumPad0 = 0x60,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.NumPad1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The 1 key on the numeric keypad.
        ///    </para>
        /// </devdoc>
        // PM team has reviewed and decided on naming changes already
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        NumPad1 = 0x61,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.NumPad2"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The 2 key on the numeric keypad.
        ///    </para>
        /// </devdoc>
        // PM team has reviewed and decided on naming changes already
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        NumPad2 = 0x62,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.NumPad3"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The 3 key on the numeric keypad.
        ///    </para>
        /// </devdoc>
        // PM team has reviewed and decided on naming changes already
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        NumPad3 = 0x63,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.NumPad4"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The 4 key on the numeric keypad.
        ///    </para>
        /// </devdoc>
        // PM team has reviewed and decided on naming changes already
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        NumPad4 = 0x64,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.NumPad5"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The 5 key on the numeric keypad.
        ///    </para>
        /// </devdoc>
        // PM team has reviewed and decided on naming changes already
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        NumPad5 = 0x65,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.NumPad6"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The 6 key on the numeric keypad.
        ///    </para>
        /// </devdoc>
        // PM team has reviewed and decided on naming changes already
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        NumPad6 = 0x66,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.NumPad7"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The 7 key on the numeric keypad.
        ///    </para>
        /// </devdoc>
        // PM team has reviewed and decided on naming changes already
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        NumPad7 = 0x67,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.NumPad8"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The 8 key on the numeric keypad.
        ///    </para>
        /// </devdoc>
        // PM team has reviewed and decided on naming changes already
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        NumPad8 = 0x68,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.NumPad9"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The 9 key on the numeric keypad.
        ///    </para>
        /// </devdoc>
        // PM team has reviewed and decided on naming changes already
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        NumPad9 = 0x69,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Multiply"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Multiply key.
        ///    </para>
        /// </devdoc>
        Multiply = 0x6A,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Add"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Add key.
        ///    </para>
        /// </devdoc>
        Add = 0x6B,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Separator"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Separator key.
        ///    </para>
        /// </devdoc>
        Separator = 0x6C,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Subtract"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Subtract key.
        ///    </para>
        /// </devdoc>
        Subtract = 0x6D,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Decimal"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Decimal key.
        ///    </para>
        /// </devdoc>
        Decimal = 0x6E,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Divide"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Divide key.
        ///    </para>
        /// </devdoc>
        Divide = 0x6F,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.F1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The F1 key.
        ///    </para>
        /// </devdoc>
        F1 = 0x70,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.F2"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The F2 key.
        ///    </para>
        /// </devdoc>
        F2 = 0x71,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.F3"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The F3 key.
        ///    </para>
        /// </devdoc>
        F3 = 0x72,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.F4"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The F4 key.
        ///    </para>
        /// </devdoc>
        F4 = 0x73,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.F5"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The F5 key.
        ///    </para>
        /// </devdoc>
        F5 = 0x74,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.F6"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The F6 key.
        ///    </para>
        /// </devdoc>
        F6 = 0x75,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.F7"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The F7 key.
        ///    </para>
        /// </devdoc>
        F7 = 0x76,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.F8"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The F8 key.
        ///    </para>
        /// </devdoc>
        F8 = 0x77,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.F9"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The F9 key.
        ///    </para>
        /// </devdoc>
        F9 = 0x78,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.F10"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The F10 key.
        ///    </para>
        /// </devdoc>
        F10 = 0x79,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.F11"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The F11 key.
        ///    </para>
        /// </devdoc>
        F11 = 0x7A,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.F12"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The F12 key.
        ///    </para>
        /// </devdoc>
        F12 = 0x7B,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.F13"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The F13 key.
        ///    </para>
        /// </devdoc>
        F13 = 0x7C,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.F14"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The F14 key.
        ///    </para>
        /// </devdoc>
        F14 = 0x7D,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.F15"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The F15 key.
        ///    </para>
        /// </devdoc>
        F15 = 0x7E,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.F16"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The F16 key.
        ///    </para>
        /// </devdoc>
        F16 = 0x7F,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.F17"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The F17 key.
        ///    </para>
        /// </devdoc>
        F17 = 0x80,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.F18"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The F18 key.
        ///    </para>
        /// </devdoc>
        F18 = 0x81,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.F19"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The F19 key.
        ///    </para>
        /// </devdoc>
        F19 = 0x82,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.F20"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The F20 key.
        ///    </para>
        /// </devdoc>
        F20 = 0x83,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.F21"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The F21 key.
        ///    </para>
        /// </devdoc>
        F21 = 0x84,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.F22"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The F22 key.
        ///    </para>
        /// </devdoc>
        F22 = 0x85,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.F23"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The F23 key.
        ///    </para>
        /// </devdoc>
        F23 = 0x86,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.F24"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The F24 key.
        ///    </para>
        /// </devdoc>
        F24 = 0x87,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.NumLock"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The NUM LOCK key.
        ///    </para>
        /// </devdoc>
        // PM team has reviewed and decided on naming changes already
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        NumLock = 0x90,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Scroll"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The SCROLL LOCK key.
        ///    </para>
        /// </devdoc>
        Scroll = 0x91,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.LShiftKey"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The left SHIFT key.
        ///    </para>
        /// </devdoc>
        LShiftKey = 0xA0,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.RShiftKey"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The right SHIFT key.
        ///    </para>
        /// </devdoc>
        RShiftKey = 0xA1,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.LControlKey"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The left CTRL key.
        ///    </para>
        /// </devdoc>
        LControlKey = 0xA2,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.RControlKey"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The right CTRL key.
        ///    </para>
        /// </devdoc>
        RControlKey = 0xA3,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.LMenu"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The left ALT key.
        ///    </para>
        /// </devdoc>
        LMenu = 0xA4,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.RMenu"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The right ALT key.
        ///    </para>
        /// </devdoc>
        RMenu = 0xA5,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.BrowserBack"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Browser Back key.
        ///    </para>
        /// </devdoc>
        BrowserBack = 0xA6,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.BrowserForward"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Browser Forward key.
        ///    </para>
        /// </devdoc>
        BrowserForward = 0xA7,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.BrowserRefresh"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Browser Refresh key.
        ///    </para>
        /// </devdoc>
        BrowserRefresh = 0xA8,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.BrowserStop"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Browser Stop key.
        ///    </para>
        /// </devdoc>
        BrowserStop = 0xA9,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.BrowserSearch"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Browser Search key.
        ///    </para>
        /// </devdoc>
        BrowserSearch = 0xAA,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.BrowserFavorites"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Browser Favorites key.
        ///    </para>
        /// </devdoc>
        BrowserFavorites = 0xAB,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.BrowserHome"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Browser Home key.
        ///    </para>
        /// </devdoc>
        BrowserHome = 0xAC,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.VolumeMute"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Volume Mute key.
        ///    </para>
        /// </devdoc>
        VolumeMute = 0xAD,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.VolumeDown"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Volume Down key.
        ///    </para>
        /// </devdoc>
        VolumeDown = 0xAE,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.VolumeUp"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Volume Up key.
        ///    </para>
        /// </devdoc>
        VolumeUp = 0xAF,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.MediaNextTrack"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Media Next Track key.
        ///    </para>
        /// </devdoc>
        MediaNextTrack = 0xB0,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.MediaPreviousTrack"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Media Previous Track key.
        ///    </para>
        /// </devdoc>
        MediaPreviousTrack = 0xB1,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.MediaStop"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Media Stop key.
        ///    </para>
        /// </devdoc>
        MediaStop = 0xB2,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.MediaPlayPause"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Media Play Pause key.
        ///    </para>
        /// </devdoc>
        MediaPlayPause = 0xB3,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.LaunchMail"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Launch Mail key.
        ///    </para>
        /// </devdoc>
        LaunchMail = 0xB4,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.SelectMedia"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Select Media key.
        ///    </para>
        /// </devdoc>
        SelectMedia = 0xB5,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.LaunchApplication1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Launch Application1 key.
        ///    </para>
        /// </devdoc>
        LaunchApplication1 = 0xB6,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.LaunchApplication2"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Launch Application2 key.
        ///    </para>
        /// </devdoc>
        LaunchApplication2 = 0xB7,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.OemSemicolon"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Oem Semicolon key.
        ///    </para>
        /// </devdoc>
        OemSemicolon = 0xBA,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Oem1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Oem 1 key.
        ///    </para>
        /// </devdoc>
        Oem1 = OemSemicolon,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Oemplus"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Oem plus key.
        ///    </para>
        /// </devdoc>
        Oemplus = 0xBB,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Oemcomma"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Oem comma key.
        ///    </para>
        /// </devdoc>
        Oemcomma = 0xBC,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.OemMinus"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Oem Minus key.
        ///    </para>
        /// </devdoc>
        OemMinus = 0xBD,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.OemPeriod"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Oem Period key.
        ///    </para>
        /// </devdoc>
        OemPeriod = 0xBE,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.OemQuestion"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Oem Question key.
        ///    </para>
        /// </devdoc>
        OemQuestion = 0xBF,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Oem2"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Oem 2 key.
        ///    </para>
        /// </devdoc>
        Oem2 = OemQuestion,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Oemtilde"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Oem tilde key.
        ///    </para>
        /// </devdoc>
        Oemtilde = 0xC0,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Oem3"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Oem 3 key.
        ///    </para>
        /// </devdoc>
        Oem3 = Oemtilde,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.OemOpenBrackets"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Oem Open Brackets key.
        ///    </para>
        /// </devdoc>
        OemOpenBrackets = 0xDB,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Oem4"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Oem 4 key.
        ///    </para>
        /// </devdoc>
        Oem4 = OemOpenBrackets,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.OemPipe"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Oem Pipe key.
        ///    </para>
        /// </devdoc>
        OemPipe = 0xDC,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Oem5"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Oem 5 key.
        ///    </para>
        /// </devdoc>
        Oem5 = OemPipe,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.OemCloseBrackets"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Oem Close Brackets key.
        ///    </para>
        /// </devdoc>
        OemCloseBrackets = 0xDD,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Oem6"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Oem 6 key.
        ///    </para>
        /// </devdoc>
        Oem6 = OemCloseBrackets,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.OemQuotes"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Oem Quotes key.
        ///    </para>
        /// </devdoc>
        OemQuotes = 0xDE,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Oem7"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Oem 7 key.
        ///    </para>
        /// </devdoc>
        Oem7 = OemQuotes,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Oem8"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Oem8 key.
        ///    </para>
        /// </devdoc>
        Oem8 = 0xDF,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.OemBackslash"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Oem Backslash key.
        ///    </para>
        /// </devdoc>
        OemBackslash = 0xE2,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Oem102"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Oem 102 key.
        ///    </para>
        /// </devdoc>
        Oem102 = OemBackslash,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.ProcessKey"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The PROCESS KEY key.
        ///    </para>
        /// </devdoc>
        ProcessKey = 0xE5,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Packet"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The Packet KEY key.
        ///    </para>
        /// </devdoc>
        Packet = 0xE7,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Attn"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The ATTN key.
        ///    </para>
        /// </devdoc>
        Attn = 0xF6,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Crsel"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The CRSEL key.
        ///    </para>
        /// </devdoc>
        Crsel = 0xF7,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Exsel"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The EXSEL key.
        ///    </para>
        /// </devdoc>
        Exsel = 0xF8,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.EraseEof"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The ERASE EOF key.
        ///    </para>
        /// </devdoc>
        EraseEof = 0xF9,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Play"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The PLAY key.
        ///    </para>
        /// </devdoc>
        Play = 0xFA,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Zoom"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The ZOOM key.
        ///    </para>
        /// </devdoc>
        Zoom = 0xFB,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.NoName"]/*' />
        /// <devdoc>
        ///    <para>
        ///       A constant reserved for future use.
        ///    </para>
        /// </devdoc>
        NoName = 0xFC,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Pa1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The PA1 key.
        ///    </para>
        /// </devdoc>
        Pa1 = 0xFD,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.OemClear"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The CLEAR key.
        ///    </para>
        /// </devdoc>
        OemClear = 0xFE,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Shift"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The SHIFT modifier key.
        ///    </para>
        /// </devdoc>
        Shift = 0x00010000,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Control"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The
        ///       CTRL modifier key.
        ///
        ///    </para>
        /// </devdoc>
        Control = 0x00020000,
        /// <include file='doc\Keys.uex' path='docs/doc[@for="Keys.Alt"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The ALT modifier key.
        ///
        ///    </para>
        /// </devdoc>
        Alt = 0x00040000,
    }
    class KeywordVk
    {
        internal string keyword;
        internal int vk;

        public KeywordVk(string key, int v)
        {
            keyword = key;
            vk = v;
        }
    }
    [ComVisible(true)]
    [Serializable]

    public class Queue : ICollection, ICloneable
    {
        private Object[] _array;
        private int _head;       // First valid element in the queue
        private int _tail;       // Last valid element in the queue
        private int _size;       // Number of elements.
        private int _growFactor; // 100 == 1.0, 130 == 1.3, 200 == 2.0
        private int _version;
        [NonSerialized]
        private Object _syncRoot;

        private const int _MinimumGrow = 4;
        //private const int _ShrinkThreshold = 32;

        // Creates a queue with room for capacity objects. The default initial
        // capacity and grow factor are used.
        public Queue()
            : this(32, (float)2.0)
        {
        }

        // Creates a queue with room for capacity objects. The default grow factor
        // is used.
        //
        public Queue(int capacity)
            : this(capacity, (float)2.0)
        {
        }

        // Creates a queue with room for capacity objects. When full, the new
        // capacity is set to the old capacity * growFactor.
        //
        public Queue(int capacity, float growFactor)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException("ArgumentOutOfRange_NeedNonNegNum");
            if (!(growFactor >= 1.0 && growFactor <= 10.0))
                throw new ArgumentOutOfRangeException("ArgumentOutOfRange_QueueGrowFactor");

            _array = new object[capacity];
            _head = 0;
            _tail = 0;
            _size = 0;
            _growFactor = (int)(growFactor * 100);
        }

        // Fills a Queue with the elements of an ICollection.  Uses the enumerator
        // to get each of the elements.
        //
        public Queue(ICollection col) : this((col == null ? 32 : col.Count))
        {
            if (col == null)
                throw new ArgumentNullException("col");
            IEnumerator en = col.GetEnumerator();
            while (en.MoveNext())
                Enqueue(en.Current);
        }


        public virtual int Count
        {
            get { return _size; }
        }

        public virtual Object Clone()
        {
            Queue q = new Queue(_size);
            q._size = _size;

            int numToCopy = _size;
            int firstPart = (_array.Length - _head < numToCopy) ? _array.Length - _head : numToCopy;
            Array.Copy(_array, _head, q._array, 0, firstPart);
            numToCopy -= firstPart;
            if (numToCopy > 0)
                Array.Copy(_array, 0, q._array, _array.Length - _head, numToCopy);

            q._version = _version;
            return q;
        }

        public virtual bool IsSynchronized
        {
            get { return false; }
        }

        public virtual Object SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    System.Threading.Interlocked.CompareExchange(ref _syncRoot, new Object(), null);
                }
                return _syncRoot;
            }
        }

        // Removes all Objects from the queue.
        public virtual void Clear()
        {
            if (_head < _tail)
                Array.Clear(_array, _head, _size);
            else
            {
                Array.Clear(_array, _head, _array.Length - _head);
                Array.Clear(_array, 0, _tail);
            }

            _head = 0;
            _tail = 0;
            _size = 0;
            _version++;
        }

        // CopyTo copies a collection into an Array, starting at a particular
        // index into the array.
        // 
        public virtual void CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (array.Rank != 1)
                throw new ArgumentException("rank");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            int arrayLen = array.Length;
            if (arrayLen - index < _size)
                throw new ArgumentException("InvalidOffLen");

            int numToCopy = _size;
            if (numToCopy == 0)
                return;
            int firstPart = (_array.Length - _head < numToCopy) ? _array.Length - _head : numToCopy;
            Array.Copy(_array, _head, array, index, firstPart);
            numToCopy -= firstPart;
            if (numToCopy > 0)
                Array.Copy(_array, 0, array, index + _array.Length - _head, numToCopy);
        }

        // Adds obj to the tail of the queue.
        //
        public virtual void Enqueue(object obj)
        {
            if (_size == _array.Length)
            {
                int newcapacity = (int)((long)_array.Length * (long)_growFactor / 100);
                if (newcapacity < _array.Length + _MinimumGrow)
                {
                    newcapacity = _array.Length + _MinimumGrow;
                }
                SetCapacity(newcapacity);
            }

            _array[_tail] = obj;
            _tail = (_tail + 1) % _array.Length;
            _size++;
            _version++;
        }

        // GetEnumerator returns an IEnumerator over this Queue.  This
        // Enumerator will support removing.
        // 

        // Removes the object at the head of the queue and returns it. If the queue
        // is empty, this method simply returns null.
        public virtual object Dequeue()
        {
            if (Count == 0)
                throw new InvalidOperationException("Dequeue Error");

            object removed = _array[_head];
            _array[_head] = null;
            _head = (_head + 1) % _array.Length;
            _size--;
            _version++;
            return removed;
        }

        // Returns the object at the head of the queue. The object remains in the
        // queue. If the queue is empty, this method throws an 
        // InvalidOperationException.
        public virtual object Peek()
        {
            if (Count == 0)
                throw new InvalidOperationException("Peek Error");

            return _array[_head];
        }

        // Returns a synchronized Queue.  Returns a synchronized wrapper
        // class around the queue - the caller must not use references to the
        // original queue.
        // 
        [HostProtection(Synchronization = true)]
        public static Queue Synchronized(Queue queue)
        {
            if (queue == null)
                throw new ArgumentNullException("queue");
            return new SynchronizedQueue(queue);
        }

        // Returns true if the queue contains at least one object equal to obj.
        // Equality is determined using obj.Equals().
        //
        // Exceptions: ArgumentNullException if obj == null.
        public virtual bool Contains(Object obj)
        {
            int index = _head;
            int count = _size;

            while (count-- > 0)
            {
                if (obj == null)
                {
                    if (_array[index] == null)
                        return true;
                }
                else if (_array[index] != null && _array[index].Equals(obj))
                {
                    return true;
                }
                index = (index + 1) % _array.Length;
            }

            return false;
        }

        internal Object GetElement(int i)
        {
            return _array[(_head + i) % _array.Length];
        }

        // Iterates over the objects in the queue, returning an array of the
        // objects in the Queue, or an empty array if the queue is empty.
        // The order of elements in the array is first in to last in, the same
        // order produced by successive calls to Dequeue.
        public virtual Object[] ToArray()
        {
            Object[] arr = new Object[_size];
            if (_size == 0)
                return arr;

            if (_head < _tail)
            {
                Array.Copy(_array, _head, arr, 0, _size);
            }
            else
            {
                Array.Copy(_array, _head, arr, 0, _array.Length - _head);
                Array.Copy(_array, 0, arr, _array.Length - _head, _tail);
            }

            return arr;
        }


        // PRIVATE Grows or shrinks the buffer to hold capacity objects. Capacity
        // must be >= _size.
        private void SetCapacity(int capacity)
        {
            Object[] newarray = new Object[capacity];
            if (_size > 0)
            {
                if (_head < _tail)
                {
                    Array.Copy(_array, _head, newarray, 0, _size);
                }
                else
                {
                    Array.Copy(_array, _head, newarray, 0, _array.Length - _head);
                    Array.Copy(_array, 0, newarray, _array.Length - _head, _tail);
                }
            }

            _array = newarray;
            _head = 0;
            _tail = (_size == capacity) ? 0 : _size;
            _version++;
        }

        public virtual void TrimToSize()
        {
            SetCapacity(_size);
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }


        // Implements a synchronization wrapper around a queue.
        [Serializable]
        private class SynchronizedQueue : Queue
        {
            private Queue _q;
            private Object root;

            internal SynchronizedQueue(Queue q)
            {
                this._q = q;
                root = _q.SyncRoot;
            }

            public override bool IsSynchronized
            {
                get { return true; }
            }

            public override Object SyncRoot
            {
                get
                {
                    return root;
                }
            }

            public override int Count
            {
                get
                {
                    lock (root)
                    {
                        return _q.Count;
                    }
                }
            }

            public override void Clear()
            {
                lock (root)
                {
                    _q.Clear();
                }
            }

            public override Object Clone()
            {
                lock (root)
                {
                    return new SynchronizedQueue((Queue)_q.Clone());
                }
            }

            public override bool Contains(Object obj)
            {
                lock (root)
                {
                    return _q.Contains(obj);
                }
            }

            public override void CopyTo(Array array, int arrayIndex)
            {
                lock (root)
                {
                    _q.CopyTo(array, arrayIndex);
                }
            }

            public override void Enqueue(Object value)
            {
                lock (root)
                {
                    _q.Enqueue(value);
                }
            }

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Thread safety problems with precondition - can't express the precondition as of Dev10.
            public override Object Dequeue()
            {
                lock (root)
                {
                    return _q.Dequeue();
                }
            }

            /*public override IEnumerator GetEnumerator()
            {
                lock (root)
                {
                    return _q.GetEnumerator();
                }
            }*/

            [SuppressMessage("Microsoft.Contracts", "CC1055")]  // Thread safety problems with precondition - can't express the precondition as of Dev10.
            public override Object Peek()
            {
                lock (root)
                {
                    return _q.Peek();
                }
            }

            public override Object[] ToArray()
            {
                lock (root)
                {
                    return _q.ToArray();
                }
            }

            public override void TrimToSize()
            {
                lock (root)
                {
                    _q.TrimToSize();
                }
            }
        }
    }
    class SKEvent
    {
        internal int wm;
        internal int paramL;
        internal int paramH;
        internal IntPtr hwnd;

        public SKEvent(int a, int b, bool c, IntPtr hwnd)
        {
            wm = a;
            paramL = b;
            paramH = c ? 1 : 0;
            this.hwnd = hwnd;
        }

        public SKEvent(int a, int b, int c, IntPtr hwnd)
        {
            wm = a;
            paramL = b;
            paramH = c;
            this.hwnd = hwnd;
        }
    }
    static class SendKeysReImplementation
    {
        public static void Send(string keys)
        {
            Send(keys, null, false);
        }


        internal static bool stopHook;
        private const int UNKNOWN_GROUPING = 10;
        private static bool fStartNewChar;
        private static KeywordVk[] keywords = new KeywordVk[] {
            new KeywordVk("ENTER",      (int)Keys.Return),
            new KeywordVk("TAB",        (int)Keys.Tab),
            new KeywordVk("ESC",        (int)Keys.Escape),
            new KeywordVk("ESCAPE",     (int)Keys.Escape),
            new KeywordVk("HOME",       (int)Keys.Home),
            new KeywordVk("END",        (int)Keys.End),
            new KeywordVk("LEFT",       (int)Keys.LeftArrow),
            new KeywordVk("RIGHT",      (int)Keys.RightArrow),
            new KeywordVk("UP",         (int)Keys.UpArrow),
            new KeywordVk("DOWN",       (int)Keys.DownArrow),
            new KeywordVk("PGUP",       (int)Keys.Prior),
            new KeywordVk("PGDN",       (int)Keys.Next),
            new KeywordVk("NUMLOCK",    (int)Keys.NumLock),
            new KeywordVk("SCROLLLOCK", (int)Keys.Scroll),
            new KeywordVk("PRTSC",      (int)Keys.PrintScreen),
            new KeywordVk("BREAK",      (int)Keys.Cancel),
            new KeywordVk("BACKSPACE",  (int)Keys.Back),
            new KeywordVk("BKSP",       (int)Keys.Back),
            new KeywordVk("BS",         (int)Keys.Back),
            new KeywordVk("CLEAR",      (int)Keys.Clear),
            new KeywordVk("CAPSLOCK",   (int)Keys.CapsLock),
            new KeywordVk("INS",        (int)Keys.Insert),
            new KeywordVk("INSERT",     (int)Keys.Insert),
            new KeywordVk("DEL",        (int)Keys.Delete),
            new KeywordVk("DELETE",     (int)Keys.Delete),
            new KeywordVk("HELP",       (int)Keys.Help),
            new KeywordVk("F1",         (int)Keys.F1),
            new KeywordVk("F2",         (int)Keys.F2),
            new KeywordVk("F3",         (int)Keys.F3),
            new KeywordVk("F4",         (int)Keys.F4),
            new KeywordVk("F5",         (int)Keys.F5),
            new KeywordVk("F6",         (int)Keys.F6),
            new KeywordVk("F7",         (int)Keys.F7),
            new KeywordVk("F8",         (int)Keys.F8),
            new KeywordVk("F9",         (int)Keys.F9),
            new KeywordVk("F10",        (int)Keys.F10),
            new KeywordVk("F11",        (int)Keys.F11),
            new KeywordVk("F12",        (int)Keys.F12),
            new KeywordVk("F13",        (int)Keys.F13),
            new KeywordVk("F14",        (int)Keys.F14),
            new KeywordVk("F15",        (int)Keys.F15),
            new KeywordVk("F16",        (int)Keys.F16),
            new KeywordVk("MULTIPLY",   (int)Keys.Multiply),
            new KeywordVk("ADD",        (int)Keys.Add),
            new KeywordVk("SUBTRACT",   (int)Keys.Subtract),
            new KeywordVk("DIVIDE",     (int)Keys.Divide),
            new KeywordVk("+",          (int)Keys.Add),
            new KeywordVk("%",          (int)(Keys.D5 | Keys.Shift)),
            new KeywordVk("^",          (int)(Keys.D6 | Keys.Shift)),
        };

        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_CHAR = 0x0102;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private const int HAVESHIFT = 0;
        private const int HAVECTRL = 1;
        private const int HAVEALT = 2;
        private const int SHIFTKEYSCAN = 0x0100;
        private const int CTRLKEYSCAN = 0x0200;
        private const int ALTKEYSCAN = 0x0400;
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern short VkKeyScan(char ch);
        [DllImport("user32.dll")]
        [ResourceExposure(ResourceScope.None)]
        public static extern int OemKeyScan(short wAsciiVal);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetWindowsHookEx(HookType hookType, HookProc lpfn, HandleRef hMod, uint dwThreadId);
        [DllImport("coredll.dll", EntryPoint = "GetModuleHandleW", SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string moduleName);
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool UnhookWindowsHookEx(HandleRef hhk);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetKeyboardState(byte[] lpKeyState);
        [DllImport("user32.dll")]
        static extern bool SetKeyboardState(byte[] lpKeyState);
        [DllImport("user32.dll")]
        static extern bool BlockInput(bool fBlockIt);
        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs,
           [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs,
           int cbSize);
        public enum HookType : int
        {
            WH_JOURNALRECORD = 0,
            WH_JOURNALPLAYBACK = 1,
            WH_KEYBOARD = 2,
            WH_GETMESSAGE = 3,
            WH_CALLWNDPROC = 4,
            WH_CBT = 5,
            WH_SYSMSGFILTER = 6,
            WH_MOUSE = 7,
            WH_HARDWARE = 8,
            WH_DEBUG = 9,
            WH_SHELL = 10,
            WH_FOREGROUNDIDLE = 11,
            WH_CALLWNDPROCRET = 12,
            WH_KEYBOARD_LL = 13,
            WH_MOUSE_LL = 14
        }


        internal static Queue events;
        internal static void UninstallJournalingHook()
        {
            if (hhook != IntPtr.Zero)
            {
                stopHook = false;
                if (events != null)
                {
                    events.Clear();
                }
                UnhookWindowsHookEx(new HandleRef(null, hhook));
                hhook = IntPtr.Zero;
            }
        }
        private static void AddEvent(SKEvent skevent)
        {
            if (events == null)
            {
                events = new Queue();
            }
            events.Enqueue(skevent);
        }
        private static void AddMsgsForVK(int vk, int repeat, bool altnoctrldown, IntPtr hwnd)
        {
            for (int i = 0; i < repeat; i++)
            {
                AddEvent(new SKEvent(altnoctrldown ? WM_SYSKEYDOWN : WM_KEYDOWN, vk, fStartNewChar, hwnd));
                // fStartNewChar = false; 
                AddEvent(new SKEvent(altnoctrldown ? WM_SYSKEYUP : WM_KEYUP, vk, fStartNewChar, hwnd));
            }
        }
        private static void CancelMods(int[] haveKeys, int level, IntPtr hwnd)
        {
            if (haveKeys[HAVESHIFT] == level)
            {
                AddEvent(new SKEvent(WM_KEYUP, (int)Keys.ShiftKey, false, hwnd));
                haveKeys[HAVESHIFT] = 0;
            }
            if (haveKeys[HAVECTRL] == level)
            {
                AddEvent(new SKEvent(WM_KEYUP, (int)Keys.ControlKey, false, hwnd));
                haveKeys[HAVECTRL] = 0;
            }
            if (haveKeys[HAVEALT] == level)
            {
                AddEvent(new SKEvent(WM_SYSKEYUP, (int)Keys.Menu, false, hwnd));
                haveKeys[HAVEALT] = 0;
            }
        }
        private static bool AddSimpleKey(char character, int repeat, IntPtr hwnd, int[] haveKeys, bool fStartNewChar, int cGrp)
        {
            int vk = VkKeyScan(character);

            if (vk != -1)
            {
                if (haveKeys[HAVESHIFT] == 0 && (vk & SHIFTKEYSCAN) != 0)
                {
                    AddEvent(new SKEvent(WM_KEYDOWN, (int)Keys.ShiftKey, fStartNewChar, hwnd));
                    fStartNewChar = false;
                    haveKeys[HAVESHIFT] = UNKNOWN_GROUPING;
                }

                if (haveKeys[HAVECTRL] == 0 && (vk & CTRLKEYSCAN) != 0)
                {
                    AddEvent(new SKEvent(WM_KEYDOWN, (int)Keys.ControlKey, fStartNewChar, hwnd));
                    fStartNewChar = false;
                    haveKeys[HAVECTRL] = UNKNOWN_GROUPING;
                }

                if (haveKeys[HAVEALT] == 0 && (vk & ALTKEYSCAN) != 0)
                {
                    AddEvent(new SKEvent(WM_KEYDOWN, (int)Keys.Menu, fStartNewChar, hwnd));
                    fStartNewChar = false;
                    haveKeys[HAVEALT] = UNKNOWN_GROUPING;
                }

                AddMsgsForVK(vk & 0xff, repeat, haveKeys[HAVEALT] > 0 && haveKeys[HAVECTRL] == 0, hwnd);
                CancelMods(haveKeys, UNKNOWN_GROUPING, hwnd);
            }
            else
            {
                int oemVal = OemKeyScan((short)(0xFF & (int)character));
                for (int i = 0; i < repeat; i++)
                {
                    AddEvent(new SKEvent(WM_CHAR, character, (int)(oemVal & 0xFFFF), hwnd));
                }
            }

            if (cGrp != 0) fStartNewChar = true;
            return fStartNewChar;
        }
        private static void ParseKeys(string keys, IntPtr hwnd)
        {
            int i = 0;
            // these four variables are used for grouping 
            int[] haveKeys = new int[] { 0, 0, 0 }; // shift, ctrl, alt
            int cGrp = 0;

            // fStartNewChar indicates that the next msg will be the first
            // of a char or char group.  This is needed for IntraApp Nesting 
            // of SendKeys.
            //
            fStartNewChar = true;

            // okay, start whipping through the characters one at a time.
            // 
            int keysLen = keys.Length;
            while (i < keysLen)
            {
                int repeat = 1;
                char ch = keys[i];
                int vk = 0;

                switch (ch)
                {
                    case '}':
                        // if these appear at this point they are out of 
                        // context, so return an error.  KeyStart processes 
                        // ochKeys up to the appropriate KeyEnd.
                        throw new ArgumentException("SendKeysError");

                    case '{':
                        int j = i + 1;

                        // There's a unique class of strings of the form "{} n}" where 
                        // n is an integer - in this case we want to send n copies of the '}' character. 
                        // Here we test for the possibility of this class of problems, and skip the
                        // first '}' in the string if necessary. 
                        //
                        if (j + 1 < keysLen && keys[j] == '}')
                        {
                            // Scan for the final '}' character
                            int final = j + 1;
                            while (final < keysLen && keys[final] != '}')
                            {
                                final++;
                            }
                            if (final < keysLen)
                            {
                                // Found the special case, so skip the first '}' in the string. 
                                // The remainder of the code will attempt to find the repeat count.
                                j++;
                            }
                        }

                        // okay, we're in a {<keyword>...} situation.  look for the keyword 
                        // 
                        while (j < keysLen && keys[j] != '}'
                               && !Char.IsWhiteSpace(keys[j]))
                        {
                            j++;
                        }

                        if (j >= keysLen)
                        {
                            throw new ArgumentException("SendKeysError");
                        }

                        // okay, have our KEYWORD.  verify it's one we know about
                        // 
                        string keyName = keys.Substring(i + 1, j - (i + 1));

                        // see if we have a space, which would mean a repeat count.
                        // 
                        if (char.IsWhiteSpace(keys[j]))
                        {
                            int digit;
                            while (j < keysLen && char.IsWhiteSpace(keys[j]))
                            {
                                j++;
                            }

                            if (j >= keysLen)
                            {
                                throw new ArgumentException("SendKeysError");
                            }

                            if (char.IsDigit(keys[j]))
                            {
                                digit = j;
                                while (j < keysLen && Char.IsDigit(keys[j]))
                                {
                                    j++;
                                }
                                repeat = Int32.Parse(keys.Substring(digit, j - digit), CultureInfo.InvariantCulture);
                            }
                        }

                        if (j >= keysLen)
                        {
                            throw new ArgumentException("SendKeysError");
                        }
                        if (keys[j] != '}')
                        {
                            throw new ArgumentException("SendKeysError");
                        }

                        vk = MatchKeyword(keyName);
                        if (vk != -1)
                        {
                            // Unlike AddSimpleKey, the bit mask uses Keys, rather than scan keys 
                            if (haveKeys[HAVESHIFT] == 0 && (vk & (int)Keys.Shift) != 0)
                            {
                                AddEvent(new SKEvent(WM_KEYDOWN, (int)Keys.ShiftKey, fStartNewChar, hwnd));
                                fStartNewChar = false;
                                haveKeys[HAVESHIFT] = UNKNOWN_GROUPING;
                            }

                            if (haveKeys[HAVECTRL] == 0 && (vk & (int)Keys.Control) != 0)
                            {
                                AddEvent(new SKEvent(WM_KEYDOWN, (int)Keys.ControlKey, fStartNewChar, hwnd));
                                fStartNewChar = false;
                                haveKeys[HAVECTRL] = UNKNOWN_GROUPING;
                            }

                            if (haveKeys[HAVEALT] == 0 && (vk & (int)Keys.Alt) != 0)
                            {
                                AddEvent(new SKEvent(WM_KEYDOWN, (int)Keys.Menu, fStartNewChar, hwnd));
                                fStartNewChar = false;
                                haveKeys[HAVEALT] = UNKNOWN_GROUPING;
                            }
                            AddMsgsForVK(vk, repeat, haveKeys[HAVEALT] > 0 && haveKeys[HAVECTRL] == 0, hwnd);
                            CancelMods(haveKeys, UNKNOWN_GROUPING, hwnd);
                        }
                        else if (keyName.Length == 1)
                        {
                            fStartNewChar = AddSimpleKey(keyName[0], repeat, hwnd, haveKeys, fStartNewChar, cGrp);
                        }
                        else
                        {
                            throw new ArgumentException("SendKeysError");
                        }

                        // don't forget to position ourselves at the end of the {...} group 
                        i = j;
                        break;

                    case '+':
                        if (haveKeys[HAVESHIFT] != 0) throw new ArgumentException("SendKeysError");

                        AddEvent(new SKEvent(WM_KEYDOWN, (int)Keys.ShiftKey, fStartNewChar, hwnd));
                        fStartNewChar = false;
                        haveKeys[HAVESHIFT] = UNKNOWN_GROUPING;
                        break;

                    case '^':
                        if (haveKeys[HAVECTRL] != 0) throw new ArgumentException("SendKeysError");

                        AddEvent(new SKEvent(WM_KEYDOWN, (int)Keys.ControlKey, fStartNewChar, hwnd));
                        fStartNewChar = false;
                        haveKeys[HAVECTRL] = UNKNOWN_GROUPING;
                        break;

                    case '%':
                        if (haveKeys[HAVEALT] != 0) throw new ArgumentException("SendKeysError");

                        AddEvent(new SKEvent((haveKeys[HAVECTRL] != 0) ? WM_KEYDOWN : WM_SYSKEYDOWN,
                                             (int)Keys.Menu, fStartNewChar, hwnd));
                        fStartNewChar = false;
                        haveKeys[HAVEALT] = UNKNOWN_GROUPING;
                        break;

                    case '(':
                        cGrp++;
                        if (cGrp > 3) throw new ArgumentException("SendKeysError");

                        if (haveKeys[HAVESHIFT] == UNKNOWN_GROUPING) haveKeys[HAVESHIFT] = cGrp;
                        if (haveKeys[HAVECTRL] == UNKNOWN_GROUPING) haveKeys[HAVECTRL] = cGrp;
                        if (haveKeys[HAVEALT] == UNKNOWN_GROUPING) haveKeys[HAVEALT] = cGrp;
                        break;

                    case ')':
                        if (cGrp < 1) throw new ArgumentException("SendKeysError");
                        CancelMods(haveKeys, cGrp, hwnd);
                        cGrp--;
                        if (cGrp == 0) fStartNewChar = true;
                        break;

                    case '~':
                        vk = (int)Keys.Return;
                        AddMsgsForVK(vk, repeat, haveKeys[HAVEALT] > 0 && haveKeys[HAVECTRL] == 0, hwnd);
                        break;

                    default:
                        fStartNewChar = AddSimpleKey(keys[i], repeat, hwnd, haveKeys, fStartNewChar, cGrp);
                        break;
                }
                i++;
            }

            if (cGrp != 0)
                throw new ArgumentException("SendKeysError");

            CancelMods(haveKeys, UNKNOWN_GROUPING, hwnd);
        }
        private static int MatchKeyword(string keyword)
        {
            for (int i = 0; i < keywords.Length; i++)
                if (string.Equals(keywords[i].keyword, keyword, StringComparison.OrdinalIgnoreCase))
                    return keywords[i].vk;

            return -1;
        }
        private enum SendMethodTypes
        {
            Default = 1,
            JournalHook = 2,
            SendInput = 3
        }

        // Hooking API
        public delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);
        internal static IntPtr hhook;
        internal static HookProc hook;
        private static SendMethodTypes? sendMethod = null;
        private static bool? hookSupported = null;
        private static bool capslockChanged;
        private static bool numlockChanged;
        private static bool scrollLockChanged;
        private static bool kanaChanged;
        static SendKeysReImplementation()
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnThreadExit);
        }
        private static void OnThreadExit(object sender, EventArgs e)
        {
            try
            {
                UninstallJournalingHook();
            }
            catch { }
        }
        private static void InstallHook()
        {
            if (hhook == IntPtr.Zero)
            {
                hook = new HookProc(new SendKeysHookProc().Callback);
                stopHook = false;
                hhook = SetWindowsHookEx(HookType.WH_JOURNALPLAYBACK, hook, new HandleRef(null, GetModuleHandle(null)), 0);
                if (hhook == IntPtr.Zero)
                    throw new Exception("hhook is invalid.");
            }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private static void LoadSendMethodFromConfig()
        {
            if (!sendMethod.HasValue)
            {
                sendMethod = SendMethodTypes.Default;
            }
        }
        private class ControlPlaceholder { public IntPtr Handle = IntPtr.Zero; }
        private static byte[] GetKeyboardState()
        {
            byte[] keystate = new byte[256];
            GetKeyboardState(keystate);
            return keystate;
        }
        private static IntPtr EmptyHookCallback(int code, IntPtr wparam, IntPtr lparam)
        {
            return IntPtr.Zero;
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private static void TestHook()
        {
            hookSupported = false;
            try
            {

                HookProc hookProc = new HookProc(EmptyHookCallback);
                IntPtr hookHandle = SetWindowsHookEx(HookType.WH_JOURNALPLAYBACK,
                                                 hookProc,
                                                 new HandleRef(null, GetModuleHandle(null)),
                                                 0);

                hookSupported = (hookHandle != IntPtr.Zero);

                if (hookHandle != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(new HandleRef(null, hookHandle));
                }
            }
            catch { } // ignore any exceptions to keep existing SendKeys behavior
        }
        private static void SetKeyboardStateBridge(byte[] keystate)
        {
            SetKeyboardState(keystate);
        }
        private static void ClearKeyboardState()
        {

            byte[] keystate = GetKeyboardState();

            keystate[(int)Keys.CapsLock] = 0;
            keystate[(int)Keys.NumLock] = 0;
            keystate[(int)Keys.Scroll] = 0;

            SetKeyboardStateBridge(keystate);
        }
        private static void Send(string keys, ControlPlaceholder control, bool wait)
        {
            //Debug.WriteLineIf(IntSecurity.SecurityDemand.TraceVerbose, "UnmanagedCode Demanded");
            //IntSecurity.UnmanagedCode.Demand(); POTENTIAL ISSUE

            if (keys == null || keys.Length == 0) return;

            // Cancelled because the MessageLoop is null in a console application.
            /*if (!wait && !Application.MessageLoop)
            {
                throw new InvalidOperationException(SR.GetString(SR.SendKeysNoMessageLoop));
            }*/

            // For SendInput only, see AddCancelModifiersForPreviousEvents for details
            Queue previousEvents = null;
            if ((events != null) && (events.Count != 0))
            {
                previousEvents = (Queue)events.Clone();
            }

            // generate the list of events that we're going to fire off with the hook
            // 
            ParseKeys(keys, (control != null) ? control.Handle : IntPtr.Zero);


            // if there weren't any events posted as a result, we're done!
            // 
            if (events == null) return;

            LoadSendMethodFromConfig();

            byte[] oldstate = GetKeyboardState();

            if (sendMethod.Value != SendMethodTypes.SendInput)
            {
                if (!hookSupported.HasValue &&
                    sendMethod.Value == SendMethodTypes.Default)
                {
                    // We don't know if the JournalHook will work, test it out
                    // so we know whether or not to call ClearKeyboardState.  ClearKeyboardState 
                    // does nothing for JournalHooks but inversely affects SendInput
                    TestHook();
                }
                if (sendMethod.Value == SendMethodTypes.JournalHook ||
                    hookSupported.Value)
                {
                    ClearKeyboardState();
                    InstallHook();
                    SetKeyboardStateBridge(oldstate);
                }
            }

            if (sendMethod.Value == SendMethodTypes.SendInput ||
                (sendMethod.Value == SendMethodTypes.Default && !hookSupported.Value))
            {
                // either SendInput is configured or JournalHooks failed by default, call SendInput
                SendInput(oldstate, previousEvents);
            }
        }
        private static void ClearGlobalKeys()
        {
            capslockChanged = false;
            numlockChanged = false;
            scrollLockChanged = false;
            kanaChanged = false;
        }
        private static void AddCancelModifiersForPreviousEvents(Queue previousEvents)
        {
            if (previousEvents == null)
            {
                return;
            }

            bool shift = false;
            bool ctrl = false;
            bool alt = false;
            while (previousEvents.Count > 0)
            {
                SKEvent skEvent = (SKEvent)previousEvents.Dequeue();

                bool isOn;
                if ((skEvent.wm == WM_KEYUP) ||
                    (skEvent.wm == WM_SYSKEYUP))
                {
                    isOn = false;
                }
                else if ((skEvent.wm == WM_KEYDOWN) ||
                         (skEvent.wm == WM_SYSKEYDOWN))
                {
                    isOn = true;
                }
                else
                {
                    continue;
                }

                if (skEvent.paramL == (int)Keys.ShiftKey)
                {
                    shift = isOn;
                }
                else if (skEvent.paramL == (int)Keys.ControlKey)
                {
                    ctrl = isOn;
                }
                else if (skEvent.paramL == (int)Keys.Menu)
                {
                    alt = isOn;
                }
            }

            if (shift)
            {
                AddEvent(new SKEvent(WM_KEYUP, (int)Keys.ShiftKey, false, IntPtr.Zero));
            }
            else if (ctrl)
            {
                AddEvent(new SKEvent(WM_KEYUP, (int)Keys.ControlKey, false, IntPtr.Zero));
            }
            else if (alt)
            {
                AddEvent(new SKEvent(WM_SYSKEYUP, (int)Keys.Menu, false, IntPtr.Zero));
            }
        }
        private static bool IsExtendedKey(SKEvent skEvent)
        {
            return (skEvent.paramL == (int)VirtualKeyShort.UP) ||
                   (skEvent.paramL == (int)VirtualKeyShort.DOWN) ||
                   (skEvent.paramL == (int)VirtualKeyShort.LEFT) ||
                   (skEvent.paramL == (int)VirtualKeyShort.RIGHT) ||
                   (skEvent.paramL == (int)VirtualKeyShort.PRIOR) ||
                   (skEvent.paramL == (int)VirtualKeyShort.NEXT) ||
                   (skEvent.paramL == (int)VirtualKeyShort.HOME) ||
                   (skEvent.paramL == (int)VirtualKeyShort.END) ||
                   (skEvent.paramL == (int)VirtualKeyShort.INSERT) ||
                   (skEvent.paramL == (int)VirtualKeyShort.DELETE);
        }
        private static void SendInput(byte[] oldKeyboardState, Queue previousEvents)
        {
            // Should be a No-Opt most of the time 
            AddCancelModifiersForPreviousEvents(previousEvents);

            // SKEvents are sent as sent as 1 or 2 inputs
            // currentInput[0] represents the SKEvent 
            // currentInput[1] is a KeyUp to prevent all identical WM_CHARs to be sent as one message
            INPUT[] currentInput = new INPUT[2];

            // all events are Keyboard events
            currentInput[0].type = (uint)INPUT_TYPE.INPUT_KEYBOARD;
            currentInput[1].type = (uint)INPUT_TYPE.INPUT_KEYBOARD;

            //uint KEYEVENTF_KEYUP = 0x0002;
            //uint KEYEVENTF_UNICODE = 0x0004;

            // set KeyUp values for currentInput[1]
            currentInput[1].U.ki.wVk = (short)0;
            currentInput[1].U.ki.dwFlags = KEYEVENTF.UNICODE | KEYEVENTF.KEYUP;

            // initialize unused members 
            currentInput[0].U.ki.dwExtraInfo = UIntPtr.Zero;
            currentInput[0].U.ki.time = 0;
            currentInput[1].U.ki.dwExtraInfo = UIntPtr.Zero;
            currentInput[1].U.ki.time = 0;

            // send each of our SKEvents using SendInput 
            int INPUTSize = Marshal.SizeOf(typeof(INPUT));

            // need these outside the lock below 
            uint eventsSent = 0;
            int eventsTotal;

            // A lock here will allow multiple threads to SendInput at the same time.
            // This mimics the JournalHook method of using the message loop to mitigate
            // threading issues. There is still a theoretical thread issue with adding 
            // to the events Queue (both JournalHook and SendInput), but we do not want
            // to alter the timings of the existing shipped behavior. I did not run into 
            // problems with 2 threads on a multiproc machine so it should be consistent.
            lock (events.SyncRoot)
            {
                // block keyboard and mouse input events from reaching applications.
                bool blockInputSuccess = BlockInput(true);

                try
                {
                    eventsTotal = events.Count;
                    ClearGlobalKeys();

                    for (int i = 0; i < eventsTotal; i++)
                    {
                        SKEvent skEvent = (SKEvent)events.Dequeue();

                        currentInput[0].U.ki.dwFlags = 0;

                        if (skEvent.wm == WM_CHAR)
                        {
                            // for WM_CHAR, send a KEYEVENTF_UNICODE instead of a Keyboard event
                            // to support extended ascii characters with no keyboard equivalent. 
                            // send currentInput[1] in this case
                            currentInput[0].U.ki.wVk = 0;
                            currentInput[0].U.ki.wScan = (ScanCodeShort)skEvent.paramL;
                            currentInput[0].U.ki.dwFlags = KEYEVENTF.UNICODE;
                            currentInput[1].U.ki.wScan = (ScanCodeShort)skEvent.paramL;

                            // call SendInput, increment the eventsSent but subtract 1 for the extra one sent 
                            eventsSent += SendInput(2, currentInput, INPUTSize) - 1;
                        }
                        else
                        {
                            // just need to send currentInput[0] for skEvent
                            currentInput[0].U.ki.wScan = 0;

                            // add KeyUp flag if we have a KeyUp 
                            if (skEvent.wm == WM_KEYUP || skEvent.wm == WM_SYSKEYUP)
                            {
                                currentInput[0].U.ki.dwFlags |= KEYEVENTF.KEYUP;
                            }

                            // Sets KEYEVENTF_EXTENDEDKEY flag if necessary
                            if (IsExtendedKey(skEvent))
                            {
                                currentInput[0].U.ki.dwFlags |= KEYEVENTF.EXTENDEDKEY;
                            }

                            currentInput[0].U.ki.wVk = (VirtualKeyShort)skEvent.paramL;

                            // send only currentInput[0]
                            eventsSent += SendInput(1, currentInput, INPUTSize);

                            CheckGlobalKeys(skEvent);
                        }

                        // We need this slight delay here for Alt-Tab to work on Vista when the Aero theme
                        // is running.  See DevDiv bugs 23355.  Although this does not look good, a delay 
                        // here actually more closely resembles the old JournalHook that processes each
                        // event individually in the hook callback.
                        System.Threading.Thread.Sleep(1);
                    }

                    // reset the keyboard back to what it was before inputs were sent, SendInupt modifies 
                    // the global lights on the keyboard (caps, scroll..) so we need to call it again to 
                    // undo those changes
                    ResetKeyboardUsingSendInput(INPUTSize);
                }
                finally
                {
                    SetKeyboardState(oldKeyboardState);

                    // unblock input if it was previously blocked 
                    if (blockInputSuccess)
                    {
                        BlockInput(false);
                    }
                }
            }

            // check to see if we sent the number of events we're supposed to
            if (eventsSent != eventsTotal)
            {
                // calls Marshal.GetLastWin32Error and sets it in the exception
                throw new Exception();
            }
        }
        private static void ResetKeyboardUsingSendInput(int INPUTSize)
        {
            // if the new state is the same, we don't need to do anything 
            if (!(capslockChanged || numlockChanged || scrollLockChanged || kanaChanged))
            {
                return;
            }

            // INPUT struct for resetting the keyboard
            INPUT[] keyboardInput = new INPUT[2];

            keyboardInput[0].type = (uint)INPUT_TYPE.INPUT_KEYBOARD;
            keyboardInput[0].U.ki.dwFlags = 0;

            keyboardInput[1].type = (uint)INPUT_TYPE.INPUT_KEYBOARD;
            keyboardInput[1].U.ki.dwFlags = KEYEVENTF.KEYUP;

            // SendInputs to turn on or off these keys.  Inputs are pairs because KeyUp is sent for each one
            if (capslockChanged)
            {
                keyboardInput[0].U.ki.wVk = VirtualKeyShort.CAPITAL;
                keyboardInput[1].U.ki.wVk = VirtualKeyShort.CAPITAL;
                SendInput(2, keyboardInput, INPUTSize);
            }

            if (numlockChanged)
            {
                keyboardInput[0].U.ki.wVk = VirtualKeyShort.NUMLOCK;
                keyboardInput[1].U.ki.wVk = VirtualKeyShort.NUMLOCK;
                SendInput(2, keyboardInput, INPUTSize);
            }

            if (scrollLockChanged)
            {
                keyboardInput[0].U.ki.wVk = VirtualKeyShort.SCROLL;
                keyboardInput[1].U.ki.wVk = VirtualKeyShort.SCROLL;
                SendInput(2, keyboardInput, INPUTSize);
            }

            if (kanaChanged)
            {
                keyboardInput[0].U.ki.wVk = VirtualKeyShort.KANA;
                keyboardInput[1].U.ki.wVk = VirtualKeyShort.KANA;
                SendInput(2, keyboardInput, INPUTSize);
            }
        }
        private static void CheckGlobalKeys(SKEvent skEvent)
        {
            if (skEvent.wm == WM_KEYDOWN)
            {
                switch (skEvent.paramL)
                {
                    case (int)Keys.CapsLock:
                        capslockChanged = !capslockChanged;
                        break;
                    case (int)Keys.NumLock:
                        numlockChanged = !numlockChanged;
                        break;
                    case (int)Keys.Scroll:
                        scrollLockChanged = !scrollLockChanged;
                        break;
                    case (int)Keys.KanaMode:
                        kanaChanged = !kanaChanged;
                        break;
                }
            }
        }

    }
    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        internal uint type;
        internal INPUTUNION U;
        internal static int Size
        {
            get { return Marshal.SizeOf(typeof(INPUT)); }
        }
    }
    [StructLayout(LayoutKind.Explicit)]
    internal struct INPUTUNION
    {
        [FieldOffset(0)]
        internal MOUSEINPUT mi;
        [FieldOffset(0)]
        internal KEYBDINPUT ki;
        [FieldOffset(0)]
        internal HARDWAREINPUT hi;
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct MOUSEINPUT
    {
        internal int dx;
        internal int dy;
        internal int mouseData;
        internal MOUSEEVENTF dwFlags;
        internal uint time;
        internal UIntPtr dwExtraInfo;
    }
    [Flags]
    internal enum MOUSEEVENTF : uint
    {
        ABSOLUTE = 0x8000,
        HWHEEL = 0x01000,
        MOVE = 0x0001,
        MOVE_NOCOALESCE = 0x2000,
        LEFTDOWN = 0x0002,
        LEFTUP = 0x0004,
        RIGHTDOWN = 0x0008,
        RIGHTUP = 0x0010,
        MIDDLEDOWN = 0x0020,
        MIDDLEUP = 0x0040,
        VIRTUALDESK = 0x4000,
        WHEEL = 0x0800,
        XDOWN = 0x0080,
        XUP = 0x0100
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct KEYBDINPUT
    {
        internal VirtualKeyShort wVk;
        internal ScanCodeShort wScan;
        internal KEYEVENTF dwFlags;
        internal int time;
        internal UIntPtr dwExtraInfo;
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct HARDWAREINPUT
    {
        internal int uMsg;
        internal short wParamL;
        internal short wParamH;
    }
    [Flags]
    internal enum KEYEVENTF : uint
    {
        EXTENDEDKEY = 0x0001,
        KEYUP = 0x0002,
        SCANCODE = 0x0008,
        UNICODE = 0x0004
    }
    internal enum VirtualKeyShort : short
    {
        ///<summary>
        ///Left mouse button
        ///</summary>
        LBUTTON = 0x01,
        ///<summary>
        ///Right mouse button
        ///</summary>
        RBUTTON = 0x02,
        ///<summary>
        ///Control-break processing
        ///</summary>
        CANCEL = 0x03,
        ///<summary>
        ///Middle mouse button (three-button mouse)
        ///</summary>
        MBUTTON = 0x04,
        ///<summary>
        ///Windows 2000/XP: X1 mouse button
        ///</summary>
        XBUTTON1 = 0x05,
        ///<summary>
        ///Windows 2000/XP: X2 mouse button
        ///</summary>
        XBUTTON2 = 0x06,
        ///<summary>
        ///BACKSPACE key
        ///</summary>
        BACK = 0x08,
        ///<summary>
        ///TAB key
        ///</summary>
        TAB = 0x09,
        ///<summary>
        ///CLEAR key
        ///</summary>
        CLEAR = 0x0C,
        ///<summary>
        ///ENTER key
        ///</summary>
        RETURN = 0x0D,
        ///<summary>
        ///SHIFT key
        ///</summary>
        SHIFT = 0x10,
        ///<summary>
        ///CTRL key
        ///</summary>
        CONTROL = 0x11,
        ///<summary>
        ///ALT key
        ///</summary>
        MENU = 0x12,
        ///<summary>
        ///PAUSE key
        ///</summary>
        PAUSE = 0x13,
        ///<summary>
        ///CAPS LOCK key
        ///</summary>
        CAPITAL = 0x14,
        ///<summary>
        ///Input Method Editor (IME) Kana mode
        ///</summary>
        KANA = 0x15,
        ///<summary>
        ///IME Hangul mode
        ///</summary>
        HANGUL = 0x15,
        ///<summary>
        ///IME Junja mode
        ///</summary>
        JUNJA = 0x17,
        ///<summary>
        ///IME final mode
        ///</summary>
        FINAL = 0x18,
        ///<summary>
        ///IME Hanja mode
        ///</summary>
        HANJA = 0x19,
        ///<summary>
        ///IME Kanji mode
        ///</summary>
        KANJI = 0x19,
        ///<summary>
        ///ESC key
        ///</summary>
        ESCAPE = 0x1B,
        ///<summary>
        ///IME convert
        ///</summary>
        CONVERT = 0x1C,
        ///<summary>
        ///IME nonconvert
        ///</summary>
        NONCONVERT = 0x1D,
        ///<summary>
        ///IME accept
        ///</summary>
        ACCEPT = 0x1E,
        ///<summary>
        ///IME mode change request
        ///</summary>
        MODECHANGE = 0x1F,
        ///<summary>
        ///SPACEBAR
        ///</summary>
        SPACE = 0x20,
        ///<summary>
        ///PAGE UP key
        ///</summary>
        PRIOR = 0x21,
        ///<summary>
        ///PAGE DOWN key
        ///</summary>
        NEXT = 0x22,
        ///<summary>
        ///END key
        ///</summary>
        END = 0x23,
        ///<summary>
        ///HOME key
        ///</summary>
        HOME = 0x24,
        ///<summary>
        ///LEFT ARROW key
        ///</summary>
        LEFT = 0x25,
        ///<summary>
        ///UP ARROW key
        ///</summary>
        UP = 0x26,
        ///<summary>
        ///RIGHT ARROW key
        ///</summary>
        RIGHT = 0x27,
        ///<summary>
        ///DOWN ARROW key
        ///</summary>
        DOWN = 0x28,
        ///<summary>
        ///SELECT key
        ///</summary>
        SELECT = 0x29,
        ///<summary>
        ///PRINT key
        ///</summary>
        PRINT = 0x2A,
        ///<summary>
        ///EXECUTE key
        ///</summary>
        EXECUTE = 0x2B,
        ///<summary>
        ///PRINT SCREEN key
        ///</summary>
        SNAPSHOT = 0x2C,
        ///<summary>
        ///INS key
        ///</summary>
        INSERT = 0x2D,
        ///<summary>
        ///DEL key
        ///</summary>
        DELETE = 0x2E,
        ///<summary>
        ///HELP key
        ///</summary>
        HELP = 0x2F,
        ///<summary>
        ///0 key
        ///</summary>
        KEY_0 = 0x30,
        ///<summary>
        ///1 key
        ///</summary>
        KEY_1 = 0x31,
        ///<summary>
        ///2 key
        ///</summary>
        KEY_2 = 0x32,
        ///<summary>
        ///3 key
        ///</summary>
        KEY_3 = 0x33,
        ///<summary>
        ///4 key
        ///</summary>
        KEY_4 = 0x34,
        ///<summary>
        ///5 key
        ///</summary>
        KEY_5 = 0x35,
        ///<summary>
        ///6 key
        ///</summary>
        KEY_6 = 0x36,
        ///<summary>
        ///7 key
        ///</summary>
        KEY_7 = 0x37,
        ///<summary>
        ///8 key
        ///</summary>
        KEY_8 = 0x38,
        ///<summary>
        ///9 key
        ///</summary>
        KEY_9 = 0x39,
        ///<summary>
        ///A key
        ///</summary>
        KEY_A = 0x41,
        ///<summary>
        ///B key
        ///</summary>
        KEY_B = 0x42,
        ///<summary>
        ///C key
        ///</summary>
        KEY_C = 0x43,
        ///<summary>
        ///D key
        ///</summary>
        KEY_D = 0x44,
        ///<summary>
        ///E key
        ///</summary>
        KEY_E = 0x45,
        ///<summary>
        ///F key
        ///</summary>
        KEY_F = 0x46,
        ///<summary>
        ///G key
        ///</summary>
        KEY_G = 0x47,
        ///<summary>
        ///H key
        ///</summary>
        KEY_H = 0x48,
        ///<summary>
        ///I key
        ///</summary>
        KEY_I = 0x49,
        ///<summary>
        ///J key
        ///</summary>
        KEY_J = 0x4A,
        ///<summary>
        ///K key
        ///</summary>
        KEY_K = 0x4B,
        ///<summary>
        ///L key
        ///</summary>
        KEY_L = 0x4C,
        ///<summary>
        ///M key
        ///</summary>
        KEY_M = 0x4D,
        ///<summary>
        ///N key
        ///</summary>
        KEY_N = 0x4E,
        ///<summary>
        ///O key
        ///</summary>
        KEY_O = 0x4F,
        ///<summary>
        ///P key
        ///</summary>
        KEY_P = 0x50,
        ///<summary>
        ///Q key
        ///</summary>
        KEY_Q = 0x51,
        ///<summary>
        ///R key
        ///</summary>
        KEY_R = 0x52,
        ///<summary>
        ///S key
        ///</summary>
        KEY_S = 0x53,
        ///<summary>
        ///T key
        ///</summary>
        KEY_T = 0x54,
        ///<summary>
        ///U key
        ///</summary>
        KEY_U = 0x55,
        ///<summary>
        ///V key
        ///</summary>
        KEY_V = 0x56,
        ///<summary>
        ///W key
        ///</summary>
        KEY_W = 0x57,
        ///<summary>
        ///X key
        ///</summary>
        KEY_X = 0x58,
        ///<summary>
        ///Y key
        ///</summary>
        KEY_Y = 0x59,
        ///<summary>
        ///Z key
        ///</summary>
        KEY_Z = 0x5A,
        ///<summary>
        ///Left Windows key (Microsoft Natural keyboard)
        ///</summary>
        LWIN = 0x5B,
        ///<summary>
        ///Right Windows key (Natural keyboard)
        ///</summary>
        RWIN = 0x5C,
        ///<summary>
        ///Applications key (Natural keyboard)
        ///</summary>
        APPS = 0x5D,
        ///<summary>
        ///Computer Sleep key
        ///</summary>
        SLEEP = 0x5F,
        ///<summary>
        ///Numeric keypad 0 key
        ///</summary>
        NUMPAD0 = 0x60,
        ///<summary>
        ///Numeric keypad 1 key
        ///</summary>
        NUMPAD1 = 0x61,
        ///<summary>
        ///Numeric keypad 2 key
        ///</summary>
        NUMPAD2 = 0x62,
        ///<summary>
        ///Numeric keypad 3 key
        ///</summary>
        NUMPAD3 = 0x63,
        ///<summary>
        ///Numeric keypad 4 key
        ///</summary>
        NUMPAD4 = 0x64,
        ///<summary>
        ///Numeric keypad 5 key
        ///</summary>
        NUMPAD5 = 0x65,
        ///<summary>
        ///Numeric keypad 6 key
        ///</summary>
        NUMPAD6 = 0x66,
        ///<summary>
        ///Numeric keypad 7 key
        ///</summary>
        NUMPAD7 = 0x67,
        ///<summary>
        ///Numeric keypad 8 key
        ///</summary>
        NUMPAD8 = 0x68,
        ///<summary>
        ///Numeric keypad 9 key
        ///</summary>
        NUMPAD9 = 0x69,
        ///<summary>
        ///Multiply key
        ///</summary>
        MULTIPLY = 0x6A,
        ///<summary>
        ///Add key
        ///</summary>
        ADD = 0x6B,
        ///<summary>
        ///Separator key
        ///</summary>
        SEPARATOR = 0x6C,
        ///<summary>
        ///Subtract key
        ///</summary>
        SUBTRACT = 0x6D,
        ///<summary>
        ///Decimal key
        ///</summary>
        DECIMAL = 0x6E,
        ///<summary>
        ///Divide key
        ///</summary>
        DIVIDE = 0x6F,
        ///<summary>
        ///F1 key
        ///</summary>
        F1 = 0x70,
        ///<summary>
        ///F2 key
        ///</summary>
        F2 = 0x71,
        ///<summary>
        ///F3 key
        ///</summary>
        F3 = 0x72,
        ///<summary>
        ///F4 key
        ///</summary>
        F4 = 0x73,
        ///<summary>
        ///F5 key
        ///</summary>
        F5 = 0x74,
        ///<summary>
        ///F6 key
        ///</summary>
        F6 = 0x75,
        ///<summary>
        ///F7 key
        ///</summary>
        F7 = 0x76,
        ///<summary>
        ///F8 key
        ///</summary>
        F8 = 0x77,
        ///<summary>
        ///F9 key
        ///</summary>
        F9 = 0x78,
        ///<summary>
        ///F10 key
        ///</summary>
        F10 = 0x79,
        ///<summary>
        ///F11 key
        ///</summary>
        F11 = 0x7A,
        ///<summary>
        ///F12 key
        ///</summary>
        F12 = 0x7B,
        ///<summary>
        ///F13 key
        ///</summary>
        F13 = 0x7C,
        ///<summary>
        ///F14 key
        ///</summary>
        F14 = 0x7D,
        ///<summary>
        ///F15 key
        ///</summary>
        F15 = 0x7E,
        ///<summary>
        ///F16 key
        ///</summary>
        F16 = 0x7F,
        ///<summary>
        ///F17 key  
        ///</summary>
        F17 = 0x80,
        ///<summary>
        ///F18 key  
        ///</summary>
        F18 = 0x81,
        ///<summary>
        ///F19 key  
        ///</summary>
        F19 = 0x82,
        ///<summary>
        ///F20 key  
        ///</summary>
        F20 = 0x83,
        ///<summary>
        ///F21 key  
        ///</summary>
        F21 = 0x84,
        ///<summary>
        ///F22 key, (PPC only) Key used to lock device.
        ///</summary>
        F22 = 0x85,
        ///<summary>
        ///F23 key  
        ///</summary>
        F23 = 0x86,
        ///<summary>
        ///F24 key  
        ///</summary>
        F24 = 0x87,
        ///<summary>
        ///NUM LOCK key
        ///</summary>
        NUMLOCK = 0x90,
        ///<summary>
        ///SCROLL LOCK key
        ///</summary>
        SCROLL = 0x91,
        ///<summary>
        ///Left SHIFT key
        ///</summary>
        LSHIFT = 0xA0,
        ///<summary>
        ///Right SHIFT key
        ///</summary>
        RSHIFT = 0xA1,
        ///<summary>
        ///Left CONTROL key
        ///</summary>
        LCONTROL = 0xA2,
        ///<summary>
        ///Right CONTROL key
        ///</summary>
        RCONTROL = 0xA3,
        ///<summary>
        ///Left MENU key
        ///</summary>
        LMENU = 0xA4,
        ///<summary>
        ///Right MENU key
        ///</summary>
        RMENU = 0xA5,
        ///<summary>
        ///Windows 2000/XP: Browser Back key
        ///</summary>
        BROWSER_BACK = 0xA6,
        ///<summary>
        ///Windows 2000/XP: Browser Forward key
        ///</summary>
        BROWSER_FORWARD = 0xA7,
        ///<summary>
        ///Windows 2000/XP: Browser Refresh key
        ///</summary>
        BROWSER_REFRESH = 0xA8,
        ///<summary>
        ///Windows 2000/XP: Browser Stop key
        ///</summary>
        BROWSER_STOP = 0xA9,
        ///<summary>
        ///Windows 2000/XP: Browser Search key
        ///</summary>
        BROWSER_SEARCH = 0xAA,
        ///<summary>
        ///Windows 2000/XP: Browser Favorites key
        ///</summary>
        BROWSER_FAVORITES = 0xAB,
        ///<summary>
        ///Windows 2000/XP: Browser Start and Home key
        ///</summary>
        BROWSER_HOME = 0xAC,
        ///<summary>
        ///Windows 2000/XP: Volume Mute key
        ///</summary>
        VOLUME_MUTE = 0xAD,
        ///<summary>
        ///Windows 2000/XP: Volume Down key
        ///</summary>
        VOLUME_DOWN = 0xAE,
        ///<summary>
        ///Windows 2000/XP: Volume Up key
        ///</summary>
        VOLUME_UP = 0xAF,
        ///<summary>
        ///Windows 2000/XP: Next Track key
        ///</summary>
        MEDIA_NEXT_TRACK = 0xB0,
        ///<summary>
        ///Windows 2000/XP: Previous Track key
        ///</summary>
        MEDIA_PREV_TRACK = 0xB1,
        ///<summary>
        ///Windows 2000/XP: Stop Media key
        ///</summary>
        MEDIA_STOP = 0xB2,
        ///<summary>
        ///Windows 2000/XP: Play/Pause Media key
        ///</summary>
        MEDIA_PLAY_PAUSE = 0xB3,
        ///<summary>
        ///Windows 2000/XP: Start Mail key
        ///</summary>
        LAUNCH_MAIL = 0xB4,
        ///<summary>
        ///Windows 2000/XP: Select Media key
        ///</summary>
        LAUNCH_MEDIA_SELECT = 0xB5,
        ///<summary>
        ///Windows 2000/XP: Start Application 1 key
        ///</summary>
        LAUNCH_APP1 = 0xB6,
        ///<summary>
        ///Windows 2000/XP: Start Application 2 key
        ///</summary>
        LAUNCH_APP2 = 0xB7,
        ///<summary>
        ///Used for miscellaneous characters; it can vary by keyboard.
        ///</summary>
        OEM_1 = 0xBA,
        ///<summary>
        ///Windows 2000/XP: For any country/region, the '+' key
        ///</summary>
        OEM_PLUS = 0xBB,
        ///<summary>
        ///Windows 2000/XP: For any country/region, the ',' key
        ///</summary>
        OEM_COMMA = 0xBC,
        ///<summary>
        ///Windows 2000/XP: For any country/region, the '-' key
        ///</summary>
        OEM_MINUS = 0xBD,
        ///<summary>
        ///Windows 2000/XP: For any country/region, the '.' key
        ///</summary>
        OEM_PERIOD = 0xBE,
        ///<summary>
        ///Used for miscellaneous characters; it can vary by keyboard.
        ///</summary>
        OEM_2 = 0xBF,
        ///<summary>
        ///Used for miscellaneous characters; it can vary by keyboard.
        ///</summary>
        OEM_3 = 0xC0,
        ///<summary>
        ///Used for miscellaneous characters; it can vary by keyboard.
        ///</summary>
        OEM_4 = 0xDB,
        ///<summary>
        ///Used for miscellaneous characters; it can vary by keyboard.
        ///</summary>
        OEM_5 = 0xDC,
        ///<summary>
        ///Used for miscellaneous characters; it can vary by keyboard.
        ///</summary>
        OEM_6 = 0xDD,
        ///<summary>
        ///Used for miscellaneous characters; it can vary by keyboard.
        ///</summary>
        OEM_7 = 0xDE,
        ///<summary>
        ///Used for miscellaneous characters; it can vary by keyboard.
        ///</summary>
        OEM_8 = 0xDF,
        ///<summary>
        ///Windows 2000/XP: Either the angle bracket key or the backslash key on the RT 102-key keyboard
        ///</summary>
        OEM_102 = 0xE2,
        ///<summary>
        ///Windows 95/98/Me, Windows NT 4.0, Windows 2000/XP: IME PROCESS key
        ///</summary>
        PROCESSKEY = 0xE5,
        ///<summary>
        ///Windows 2000/XP: Used to pass Unicode characters as if they were keystrokes.
        ///The VK_PACKET key is the low word of a 32-bit Virtual Key value used for non-keyboard input methods. For more information,
        ///see Remark in KEYBDINPUT, SendInput, WM_KEYDOWN, and WM_KEYUP
        ///</summary>
        PACKET = 0xE7,
        ///<summary>
        ///Attn key
        ///</summary>
        ATTN = 0xF6,
        ///<summary>
        ///CrSel key
        ///</summary>
        CRSEL = 0xF7,
        ///<summary>
        ///ExSel key
        ///</summary>
        EXSEL = 0xF8,
        ///<summary>
        ///Erase EOF key
        ///</summary>
        EREOF = 0xF9,
        ///<summary>
        ///Play key
        ///</summary>
        PLAY = 0xFA,
        ///<summary>
        ///Zoom key
        ///</summary>
        ZOOM = 0xFB,
        ///<summary>
        ///Reserved
        ///</summary>
        NONAME = 0xFC,
        ///<summary>
        ///PA1 key
        ///</summary>
        PA1 = 0xFD,
        ///<summary>
        ///Clear key
        ///</summary>
        OEM_CLEAR = 0xFE
    }
    internal enum ScanCodeShort : short
    {
        LBUTTON = 0,
        RBUTTON = 0,
        CANCEL = 70,
        MBUTTON = 0,
        XBUTTON1 = 0,
        XBUTTON2 = 0,
        BACK = 14,
        TAB = 15,
        CLEAR = 76,
        RETURN = 28,
        SHIFT = 42,
        CONTROL = 29,
        MENU = 56,
        PAUSE = 0,
        CAPITAL = 58,
        KANA = 0,
        HANGUL = 0,
        JUNJA = 0,
        FINAL = 0,
        HANJA = 0,
        KANJI = 0,
        ESCAPE = 1,
        CONVERT = 0,
        NONCONVERT = 0,
        ACCEPT = 0,
        MODECHANGE = 0,
        SPACE = 57,
        PRIOR = 73,
        NEXT = 81,
        END = 79,
        HOME = 71,
        LEFT = 75,
        UP = 72,
        RIGHT = 77,
        DOWN = 80,
        SELECT = 0,
        PRINT = 0,
        EXECUTE = 0,
        SNAPSHOT = 84,
        INSERT = 82,
        DELETE = 83,
        HELP = 99,
        KEY_0 = 11,
        KEY_1 = 2,
        KEY_2 = 3,
        KEY_3 = 4,
        KEY_4 = 5,
        KEY_5 = 6,
        KEY_6 = 7,
        KEY_7 = 8,
        KEY_8 = 9,
        KEY_9 = 10,
        KEY_A = 30,
        KEY_B = 48,
        KEY_C = 46,
        KEY_D = 32,
        KEY_E = 18,
        KEY_F = 33,
        KEY_G = 34,
        KEY_H = 35,
        KEY_I = 23,
        KEY_J = 36,
        KEY_K = 37,
        KEY_L = 38,
        KEY_M = 50,
        KEY_N = 49,
        KEY_O = 24,
        KEY_P = 25,
        KEY_Q = 16,
        KEY_R = 19,
        KEY_S = 31,
        KEY_T = 20,
        KEY_U = 22,
        KEY_V = 47,
        KEY_W = 17,
        KEY_X = 45,
        KEY_Y = 21,
        KEY_Z = 44,
        LWIN = 91,
        RWIN = 92,
        APPS = 93,
        SLEEP = 95,
        NUMPAD0 = 82,
        NUMPAD1 = 79,
        NUMPAD2 = 80,
        NUMPAD3 = 81,
        NUMPAD4 = 75,
        NUMPAD5 = 76,
        NUMPAD6 = 77,
        NUMPAD7 = 71,
        NUMPAD8 = 72,
        NUMPAD9 = 73,
        MULTIPLY = 55,
        ADD = 78,
        SEPARATOR = 0,
        SUBTRACT = 74,
        DECIMAL = 83,
        DIVIDE = 53,
        F1 = 59,
        F2 = 60,
        F3 = 61,
        F4 = 62,
        F5 = 63,
        F6 = 64,
        F7 = 65,
        F8 = 66,
        F9 = 67,
        F10 = 68,
        F11 = 87,
        F12 = 88,
        F13 = 100,
        F14 = 101,
        F15 = 102,
        F16 = 103,
        F17 = 104,
        F18 = 105,
        F19 = 106,
        F20 = 107,
        F21 = 108,
        F22 = 109,
        F23 = 110,
        F24 = 118,
        NUMLOCK = 69,
        SCROLL = 70,
        LSHIFT = 42,
        RSHIFT = 54,
        LCONTROL = 29,
        RCONTROL = 29,
        LMENU = 56,
        RMENU = 56,
        BROWSER_BACK = 106,
        BROWSER_FORWARD = 105,
        BROWSER_REFRESH = 103,
        BROWSER_STOP = 104,
        BROWSER_SEARCH = 101,
        BROWSER_FAVORITES = 102,
        BROWSER_HOME = 50,
        VOLUME_MUTE = 32,
        VOLUME_DOWN = 46,
        VOLUME_UP = 48,
        MEDIA_NEXT_TRACK = 25,
        MEDIA_PREV_TRACK = 16,
        MEDIA_STOP = 36,
        MEDIA_PLAY_PAUSE = 34,
        LAUNCH_MAIL = 108,
        LAUNCH_MEDIA_SELECT = 109,
        LAUNCH_APP1 = 107,
        LAUNCH_APP2 = 33,
        OEM_1 = 39,
        OEM_PLUS = 13,
        OEM_COMMA = 51,
        OEM_MINUS = 12,
        OEM_PERIOD = 52,
        OEM_2 = 53,
        OEM_3 = 41,
        OEM_4 = 26,
        OEM_5 = 43,
        OEM_6 = 27,
        OEM_7 = 40,
        OEM_8 = 0,
        OEM_102 = 86,
        PROCESSKEY = 0,
        PACKET = 0,
        ATTN = 0,
        CRSEL = 0,
        EXSEL = 0,
        EREOF = 93,
        PLAY = 0,
        ZOOM = 98,
        NONAME = 0,
        PA1 = 0,
        OEM_CLEAR = 0,
    }
    internal enum INPUT_TYPE : uint
    {
        INPUT_MOUSE = 0,
        INPUT_KEYBOARD = 1,
        INPUT_HARDWARE = 2
    }
    public class SendKeysHookProc
    {
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);
        [DllImport("kernel32.dll")]
        static extern uint GetTickCount();
        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(HandleRef hhk, int nCode, IntPtr wParam, IntPtr lParam);
        struct EVENTMSG
        {
            public int message;
            public int paramL;
            public int paramH;
            public int time;
            public IntPtr hwnd;
        }
        private bool gotNextEvent = false;

        public virtual IntPtr Callback(int code, IntPtr wparam, IntPtr lparam)
        {
            EVENTMSG eventmsg = (EVENTMSG)Marshal.PtrToStructure(lparam, typeof(EVENTMSG));


            if (GetAsyncKeyState((int)Keys.Pause) != 0)
            {
                SendKeysReImplementation.stopHook = true;
            }

            //
            switch (code)
            {
                case 2: // HC_SKIP

                    if (!gotNextEvent)
                    {
                        break;
                    }

                    if (SendKeysReImplementation.events != null && SendKeysReImplementation.events.Count > 0)
                    {
                        SendKeysReImplementation.events.Dequeue();
                    }
                    SendKeysReImplementation.stopHook = SendKeysReImplementation.events == null || SendKeysReImplementation.events.Count == 0;
                    break;

                case 1: // HC_GETNEXT

                    gotNextEvent = true;

#if DEBUG
                    Debug.Assert(SendKeysReImplementation.events != null && SendKeysReImplementation.events.Count > 0 && !SendKeysReImplementation.stopHook, "HC_GETNEXT when queue is empty!");
#endif

                    SKEvent evt = (SKEvent)SendKeysReImplementation.events.Peek();
                    eventmsg.message = evt.wm;
                    eventmsg.paramL = evt.paramL;
                    eventmsg.paramH = evt.paramH;
                    eventmsg.hwnd = evt.hwnd;
                    eventmsg.time = Convert.ToInt32(GetTickCount());
                    Marshal.StructureToPtr(eventmsg, lparam, true);
                    break;

                default:
                    if (code < 0)
                        CallNextHookEx(new HandleRef(null, SendKeysReImplementation.hhook), code, wparam, lparam);
                    break;
            }

            if (SendKeysReImplementation.stopHook)
            {
                SendKeysReImplementation.UninstallJournalingHook();
                gotNextEvent = false;
            }
            return IntPtr.Zero;
        }
    }
}