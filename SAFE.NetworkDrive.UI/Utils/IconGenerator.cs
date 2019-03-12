using System.Drawing;
using System.Drawing.Drawing2D;

namespace SAFE.NetworkDrive.UI
{
    internal static class IconGenerator
    {
        readonly static Font _myFont = new Font("Arial Black", 12, FontStyle.Bold, GraphicsUnit.Pixel);

        /// <summary>
        /// This method generates a nice icon for us
        /// </summary>
        /// <param name="c">Some letter to display on the icon</param>
        /// <param name="ballColor">The color of the ball to be drawn</param>
        /// <returns>a new Icon</returns>
        internal static Icon CreateIcon(char c, Color ballColor)
        {
            string letter = c.ToString();
            var bm = new Bitmap(16, 16);

            var g = Graphics.FromImage(bm);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            DrawBall(g, new Rectangle(0, 0, 15, 15), ballColor);

            //g.DrawString(letter, _myFont, Brushes.Black, new Point(4, 4));
            //g.DrawString(letter, _myFont, Brushes.White, new Point(5, 3));

            return Icon.FromHandle(bm.GetHicon());
        }

        static void DrawBall(Graphics g, Rectangle rect, Color c)
        {
            var path = new GraphicsPath();
            path.AddEllipse(rect);

            var pgbrush = new PathGradientBrush(path)
            {
                CenterPoint = new Point((rect.Right - rect.Left) / 3 + rect.Left, (rect.Bottom - rect.Top) / 3 + rect.Top),
                CenterColor = Color.White,
                SurroundColors = new Color[] { c }
            };

            g.FillEllipse(pgbrush, rect);
            g.DrawEllipse(new Pen(c), rect);
        }
    }
}