using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace CriminalCaseBot
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        Bitmap bg;

        static Rectangle FindWhite(Bitmap b, int xs=0, int ys=0, int xe=0, int ye=0)
        {
            if (xe == 0) xe = b.Width;
            if (ye == 0) ye = b.Height;

            Rectangle rv = new Rectangle(2800, 2800, -1, -1);

            unsafe
            {
              //  var l = b.LockBits(new Rectangle(0,0,b.Width,b.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                //int* p=(int*)l.Scan0;
                for (int y = ys; y < ye; y++)
                {
                    for (int x = xs; x < xe; x++)
                    {
                        Color cc = /*Color.FromArgb(p[x + y*l.Stride/4]);*/ b.GetPixel(x, y);
                        if ((cc.R + cc.G + cc.B) >= 700)
                        {
                            rv.X = Math.Min(rv.X, x);
                            rv.Y = Math.Min(rv.Y, y);
                            rv.Width = Math.Max(rv.Width, x);
                            rv.Height = Math.Max(rv.Height, y);
                        }
                    }
                }
               // b.UnlockBits(l);
            }

            rv.Width -= rv.X-1;
            rv.Height -= rv.Y - 1;
            return rv;
        }

        static public Bitmap CopyWhite(Bitmap inb, int xs=0, int ys=0, int ws=0, int hs=0)
        {
            Rectangle w = FindWhite(inb, xs, ys, xs + ws, hs + ys);
            // Create the new bitmap and associated graphics object
            if (w.Width < 0 || w.Height < 0)
            {
                w.X = 0;
                w.Y = 0;
                w.Width = 1;
                w.Height = 1;
            }

            Bitmap bmp = new Bitmap(w.Width, w.Height);

            Graphics g = Graphics.FromImage(bmp);
            if (ws == 0) ws = w.Width;  else ws += xs;
            if (hs == 0) hs = w.Height; else hs += ys;
            
            
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color cc = inb.GetPixel(x + w.X, y + w.Y);

                    unchecked
                    {
                        if ((cc.R + cc.G + cc.B) < 700)
                            cc = Color.FromArgb((int)0xFF000000);
                        else
                            cc = Color.FromArgb((int)0xFFFFFFFF);
                    }

                    bmp.SetPixel(x, y, cc);
                }
            }

            // Clean up
            g.Dispose();

            return bmp;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

            b1.Tag = pb1; pb1.Tag = tb1; tb1.Tag = b1;
            b2.Tag = pb2; pb2.Tag = tb2; tb2.Tag = b2;
            b3.Tag = pb3; pb3.Tag = tb3; tb3.Tag = b3;
            b4.Tag = pb4; pb4.Tag = tb4; tb4.Tag = b4;
            b5.Tag = pb5; pb5.Tag = tb5; tb5.Tag = b5;
            b6.Tag = pb6; pb6.Tag = tb6; tb6.Tag = b6;

            //LoadLv("85ab8136328e9398a1b51de74fe34084");

        }


        // make a hex string of the hash for display or whatever
        //this is wealy unique
        public static string ImageHash(Bitmap image)
        {
            StringBuilder sb = new StringBuilder();
            if (image.Width > 3)
            {
                for (int i = 0; i < image.Height; i++)
                {
                    sb.Append((image.GetPixel(0, i).R / 255) * 2 + (image.GetPixel(3, i).R / 255) * 1);
                }
            }

            return image.Width + "x" + image.Height + "&" + sb.ToString();
        }

        // make a hex string of the hash for display or whatever
        //this is strongly unique
        public static string ImageHash_uniq(Image image)
        {
            if (image == null)
                return "????????????????";

            byte[] bytes = null;
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Gif); // gif works fine
                bytes = ms.ToArray();
            }

            // hash the bytes
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] hash = md5.ComputeHash(bytes);

            //stringify
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2").ToLower());
            }

            return sb.ToString();
        }

        static public Bitmap Copy(Bitmap srcBitmap, int x, int y, int w, int h)
        {
            Bitmap bmp = new Bitmap(w, h);
            Graphics g = Graphics.FromImage(bmp);

            Rectangle section = new Rectangle(x, y, w, h);

            g.DrawImage(srcBitmap, 0, 0, section, GraphicsUnit.Pixel);

            g.Dispose();

            return bmp;
        }

        static public string CopyHash(Bitmap srcBitmap, Bitmap dstBitmap, int x, int y)
        {
            Graphics g = Graphics.FromImage(dstBitmap);

            Rectangle section = new Rectangle(x, y, dstBitmap.Width, dstBitmap.Height);

            g.DrawImage(srcBitmap, 0, 0, section, GraphicsUnit.Pixel);

            g.Dispose();

            return ImageHash(dstBitmap);
        }

        Dictionary<string, Dictionary<string, Point>> itemlist = new Dictionary<string, Dictionary<string, Point>>();

        Point HashPoint(Bitmap img)
        {
            string ihsh = ImageHash(img);
            string ihsh_uniq = ImageHash_uniq(img);
            if (itemlist.ContainsKey(ihsh) &&
                (itemlist[ihsh].Count == 1 || itemlist[ihsh].ContainsKey(ihsh_uniq)) )
                if (itemlist[ihsh].Count == 1)
                    return itemlist[ihsh].First().Value;
                else
                    return itemlist[ihsh][ihsh_uniq];
            else
                return new Point(0, 0);
        }

        void AddHashPoint(string file, Point p)
        {
            var lf = new ListTag(file);

            if (!itemlist.ContainsKey(lf.hash)) itemlist[lf.hash] = new Dictionary<string, Point>();
            itemlist[lf.hash][lf.hash_uniq] = p;
            lstTags.Items.Add(lf);
        }

        static string spath = Environment.CurrentDirectory + @"\stored\";
        string FileFromHash(string hash, int id)
        {
            var rv = Directory.GetFiles(spath, Text + "." + id + "_" + hash + "*_.*");

            if (rv.Length > 0) 
                return rv[0];
            else
                return null;
        }

        string GetPath(Bitmap img, int id, Point p)
        {
            return GetPath(img, id, p.ToString());
        }

        string GetPath(Bitmap img, int id, string p)
        {

            string basename = "_" + ImageHash(img) + "_" + p.ToString() + "_." + (id == 1 ? "jpg" : "png");

            return spath + this.Text + "+" + id + basename;
        }

        void NewHashPoint(Bitmap img, Point p, Bitmap bg, Point pc)
        {
            string fname = GetPath(img, 0, p);
            string fname2 = GetPath(img, 1, p);

            img.Save(fname);
            var b = Copy(bg, pc.X - 16, pc.Y - 16, 32, 32);
            b.Save(fname2);
            b.Dispose();

            AddHashPoint(GetPath(img,0,p),p);
        }

        bool ClickHash(Bitmap img)
        {
            var s = HashPoint(img);

            if (!s.IsEmpty)
                ScreenCapture.User32.LeftClick(s.X + ax, s.Y + ay);

           return !s.IsEmpty;
        }

        void HandleHash(PictureBox pbx, Bitmap img, int x, int y)
        {
            TextBox tbx = (TextBox)pbx.Tag;
            Button bx = (Button)tbx.Tag;

            pbx.Image = CopyWhite(img,x,y,150,24); // Copy(img, x , y , 150, 24);
            tbx.Text = ImageHash((Bitmap)pbx.Image);
            bx.Text = HashPoint((Bitmap)pbx.Image).ToString();

            tbx.BackColor = HashPoint((Bitmap)pbx.Image).IsEmpty ? Color.Red : Color.Green;
        }

        bool found;

        class ListTag
        {
            public string hash;
            public string hash_uniq;
            public string path_tag;
            public string path_img;

            public ListTag(string file_tag)
            {
                using (var b = (Bitmap)Bitmap.FromFile(file_tag))
                {
                    hash = MainForm.ImageHash(b);
                    hash_uniq = MainForm.ImageHash_uniq(b);
                    path_tag = file_tag;
                    path_img = file_tag.Replace("+0_", "+1_");
                }
            }

            public override string ToString() { return hash; }
        }

        int ax, ay, cx, cy;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (found)
            {
                if (cbCheat.Checked)
                {

                    //Execute ?

                    Point op = Cursor.Position;

                    bool r = false;
                    bool b;
                    b = ClickHash((Bitmap)pb1.Image);
                    if (b) { Thread.Sleep(b ? 140 : 100); r |= b; }
                    b = ClickHash((Bitmap)pb2.Image);
                    if (b) { Thread.Sleep(b ? 140 : 100); r |= b; }
                    b = ClickHash((Bitmap)pb3.Image);
                    if (b) { Thread.Sleep(b ? 140 : 100); r |= b; }
                    b = ClickHash((Bitmap)pb4.Image);
                    if (b) { Thread.Sleep(b ? 140 : 100); r |= b; }
                    b = ClickHash((Bitmap)pb5.Image);
                    if (b) { Thread.Sleep(b ? 140 : 100); r |= b; }
                    b = ClickHash((Bitmap)pb6.Image);
                    if (b) { Thread.Sleep(b ? 140 : 100); r |= b; }

                    if (r)
                        Cursor.Position = op;
                }
                found = false;
            }
            else
            {
                ScreenCapture sc = new ScreenCapture();
                if (bg != null)
                    bg.Dispose();

                Bitmap img = sc.CaptureScreen();
                bg = img;

                //Anchor to a specific color to compute coordinates -- not terribly accurate, but it mostly works
                uint acc = (uint)img.GetPixel(cx, cy).ToArgb();
                uint magic = 0xFF478E98; //0xFF4C929B

                var b = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
                unsafe
                {
                    uint* pixels = (uint*)b.Scan0;
                    for (int y = 0; y < img.Height - 16; y++)
                        for (int x = 0; x < img.Width - 16; x++)
                        {
                            if (cx != 0 && cy != 0 && (uint)acc == magic)
                            {
                                x = cx;
                                y = cy;
                            }


                            if ((uint)pixels[x + y * b.Stride / 4] == magic)
                            {
                                cx = x; cy = y;
                                img.UnlockBits(b);
                                y += 36;

                                ax = x;
                                ay = y;


                                found = true;

                                Image old2 = this.pbLevelId.Image;
                                pbLevelId.Image = Copy(img, x + 32, y - 128, 64, 64);
                                if (old2 != null)
                                    old2.Dispose();

                                try
                                {
                                    HandleHash(pb1, img, x + 28, y - 28);
                                    HandleHash(pb2, img, x + 190, y - 28);
                                    HandleHash(pb3, img, x + 350, y - 28);
                                    HandleHash(pb4, img, x + 28, y + 14);
                                    HandleHash(pb5, img, x + 190, y + 14);
                                    HandleHash(pb6, img, x + 350, y + 14);
                                }
                                catch (Exception)
                                {
                                    found = false;
                                }






                                goto nomore;
                            }
                        }
                }
                img.UnlockBits(b);
            }
            
        nomore:
            Text = Text;
        }

        private void LoadLv(string nlvl)
        {
            Text = nlvl;
            lstTags.Items.Clear();
            itemlist.Clear();
            var r = Directory.GetFiles(spath, nlvl + "+redir+*", SearchOption.AllDirectories);
            if (r.Length > 0)
                nlvl = r[0].Split('+')[2];

            var f = Directory.GetFiles(spath, nlvl + "+0_*.*", SearchOption.AllDirectories);

            foreach (var sF in f)
            {
                FileInfo fi = new FileInfo(sF);
                string s = fi.Name;
                var p = s.Split('_');
                var xy = p[2].Split(',').Select(sx => Int32.Parse(sx.Split('=')[1].Replace("}", ""))).ToArray();

                AddHashPoint(sF, new Point(xy[0], xy[1]));
            }
        }

        private void b1_MouseUp(object sender, MouseEventArgs e)
        {
            Point pc=Cursor.Position;
            Point p=pc;
            p.X-=ax;
            p.Y-=ay;
            Button bx;
            PictureBox px;
            TextBox tbx;

            if (sender is Button)
            {
                bx = ((Button)sender);
                px = ((PictureBox)bx.Tag);
                tbx = ((TextBox)px.Tag);
            }
            else
            {
                px = ((PictureBox)sender);
                tbx = ((TextBox)px.Tag);
                bx = ((Button)tbx.Tag);
            }

            bx.Text = p.ToString();

            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                NewHashPoint((Bitmap)px.Image, p, bg, pc);
            }
            else if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                ClickHash((Bitmap)px.Image);
            }
        }


        private void btnStart_Click(object sender, EventArgs e)
        {
            timer1.Enabled = !timer1.Enabled;
            if (timer1.Enabled)
                btnStart.Text = "STOP";
            else
                btnStart.Text = "START";
        }


        private void btnLoad_Click(object sender, EventArgs e)
        {
            string nlvl = ImageHash_uniq(pbLevelId.Image);
            if (nlvl != Text)
            {
                LoadLv(nlvl);
            }
        }

        private void tbPath_TextChanged(object sender, EventArgs e)
        {
            spath = tbPath.Text;
        }

        private void btnRemoveTag_Click(object sender, EventArgs e)
        {
            ListTag l = lstTags.SelectedItem as ListTag;

            if (l != null)
            {
                lstTags.SelectedItem = null;
                File.Delete(l.path_tag);
                File.Delete(l.path_img);
            }
        }

        private void lstTags_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListTag l = lstTags.SelectedItem as ListTag;

            if (l != null)
            {
                try
                {
                    pbTagPreview.Image = Image.FromFile(l.path_tag);
                    pbTagImage.Image = Image.FromFile(l.path_img);
                    return;
                }
                catch (Exception)
                {
                }
            }

            pbTagPreview.Image = null;
            pbTagImage.Image = null;
        }

    }

    /// <summary>
    /// Provides functions to capture the entire screen, or a particular window, and save it to a file.
    /// </summary>
    public class ScreenCapture
    {
        /// <summary>
        /// Creates an Image object containing a screen shot of the entire desktop
        /// </summary>
        /// <returns></returns>
        public Bitmap CaptureScreen()
        {
            return CaptureWindow(User32.GetDesktopWindow());
        }

        public Bitmap CaptureWindow()
        {
            User32.POINT p;
            User32.GetCursorPos(out p);
            
            return CaptureWindow(User32.WindowFromPoint(p));
        }
        /// <summary>
        /// Creates an Image object containing a screen shot of a specific window
        /// </summary>
        /// <param name="handle">The handle to the window. (In windows forms, this is obtained by the Handle property)</param>
        /// <returns></returns>
        public Bitmap CaptureWindow(IntPtr handle)
        {
            // get te hDC of the target window
            IntPtr hdcSrc = User32.GetWindowDC(handle);
            // get the size
            User32.RECT windowRect = new User32.RECT();
            User32.GetWindowRect(handle, ref windowRect);
            int width = windowRect.right - windowRect.left;
            int height = windowRect.bottom - windowRect.top;
            // create a device context we can copy to
            IntPtr hdcDest = GDI32.CreateCompatibleDC(hdcSrc);
            // create a bitmap we can copy it to,
            // using GetDeviceCaps to get the width/height
            IntPtr hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, width, height);
            // select the bitmap object
            IntPtr hOld = GDI32.SelectObject(hdcDest, hBitmap);
            // bitblt over
            GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, GDI32.SRCCOPY);
            // restore selection
            GDI32.SelectObject(hdcDest, hOld);
            // clean up 
            GDI32.DeleteDC(hdcDest);
            User32.ReleaseDC(handle, hdcSrc);
            // get a .NET image object for it
            Bitmap img = Image.FromHbitmap(hBitmap);
            // free up the Bitmap object
            GDI32.DeleteObject(hBitmap);
            return img;
        }
        /// <summary>
        /// Captures a screen shot of a specific window, and saves it to a file
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="filename"></param>
        /// <param name="format"></param>
        public void CaptureWindowToFile(IntPtr handle, string filename, ImageFormat format)
        {
            Image img = CaptureWindow(handle);
            img.Save(filename, format);
        }
        /// <summary>
        /// Captures a screen shot of the entire desktop, and saves it to a file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="format"></param>
        public void CaptureScreenToFile(string filename, ImageFormat format)
        {
            Image img = CaptureScreen();
            img.Save(filename, format);
        }

        /// <summary>
        /// Helper class containing Gdi32 API functions
        /// </summary>
        public class GDI32
        {

            public const int SRCCOPY = 0x00CC0020; // BitBlt dwRop parameter
            [DllImport("gdi32.dll")]
            public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest,
                int nWidth, int nHeight, IntPtr hObjectSource,
                int nXSrc, int nYSrc, int dwRop);
            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth,
                int nHeight);
            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleDC(IntPtr hDC);
            [DllImport("gdi32.dll")]
            public static extern bool DeleteDC(IntPtr hDC);
            [DllImport("gdi32.dll")]
            public static extern bool DeleteObject(IntPtr hObject);
            [DllImport("gdi32.dll")]
            public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
        }

        /// <summary>
        /// Helper class containing User32 API functions
        /// </summary>
        public class User32
        {
            [DllImport("user32.dll")]
            static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData,
               UIntPtr dwExtraInfo);
            [Flags]
            public enum MouseEventFlags
            {
                LEFTDOWN = 0x00000002,
                LEFTUP = 0x00000004,
                MIDDLEDOWN = 0x00000020,
                MIDDLEUP = 0x00000040,
                MOVE = 0x00000001,
                ABSOLUTE = 0x00008000,
                RIGHTDOWN = 0x00000008,
                RIGHTUP = 0x00000010
            }

            public static void LeftClick(int x, int y)
            {
                Cursor.Position = new System.Drawing.Point(x, y);
                mouse_event((uint)(MouseEventFlags.LEFTDOWN), 0, 0, 0, UIntPtr.Zero);
                mouse_event((uint)(MouseEventFlags.LEFTUP), 0, 0, 0, UIntPtr.Zero);
            }
            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int left;
                public int top;
                public int right;
                public int bottom;
            }
            [DllImport("user32.dll")]
            public static extern IntPtr GetDesktopWindow();
            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowDC(IntPtr hWnd);
            [DllImport("user32.dll")]
            public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);
            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowRect(IntPtr hWnd, ref RECT rect);

            [DllImport("user32.dll")]
            public static extern IntPtr WindowFromPoint(POINT Point);

            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern uint GetWindowModuleFileName(IntPtr hwnd,
                StringBuilder lpszFileName, uint cchFileNameMax);

            [DllImport("user32.dll")]
            public static extern bool GetCursorPos(out POINT lpPoint);


            [StructLayout(LayoutKind.Sequential)]
            public struct POINT
            {
                public int X;
                public int Y;

                public POINT(int x, int y)
                {
                    this.X = x;
                    this.Y = y;
                }

                public static implicit operator System.Drawing.Point(POINT p)
                {
                    return new System.Drawing.Point(p.X, p.Y);
                }

                public static implicit operator POINT(System.Drawing.Point p)
                {
                    return new POINT(p.X, p.Y);
                }
            }
        }
    }
}
