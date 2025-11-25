using System;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;

namespace WallpaperSync.UI.Components
{
    public class SmoothListBox : ListBox
    {
        private int hoverIndex = -1;

        public SmoothListBox()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            ItemHeight = 32;

            typeof(Control).GetProperty("DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(this, true);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            int index = IndexFromPoint(e.Location);

            if (index != hoverIndex)
            {
                hoverIndex = index;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hoverIndex = -1;
            Invalidate();
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            bool dark = ThemeManager.IsDarkMode();

            e.DrawBackground();

            if (e.Index < 0 || e.Index >= Items.Count)
                return;

            string text = Items[e.Index].ToString();

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            bool isHover = (e.Index == hoverIndex);

            Color backColor;
            Color textColor;

            if (!dark)
            {
                if (isSelected)
                {
                    backColor = Color.FromArgb(225, 240, 255);
                    textColor = Color.Black;
                }
                else if (isHover)
                {
                    backColor = Color.FromArgb(234, 234, 234);
                    textColor = Color.Black;
                }
                else
                {
                    backColor = Color.FromArgb(250, 250, 250);
                    textColor = Color.Black;
                }
            }
            else
            {
                if (isSelected)
                {
                    backColor = Color.FromArgb(20, 50, 70);
                    textColor = Color.White;
                }
                else if (isHover)
                {
                    backColor = Color.FromArgb(35, 35, 35);
                    textColor = Color.White;
                }   
                else
                {
                    backColor = Color.FromArgb(15, 15, 15);
                    textColor = Color.White;
                }
            }


                using (var bg = new SolidBrush(backColor))
                    e.Graphics.FillRectangle(bg, e.Bounds);

            using (var brush = new SolidBrush(textColor))
            {
                var rect = new Rectangle(e.Bounds.X + 20, e.Bounds.Y + 8,
                                         e.Bounds.Width - 20, e.Bounds.Height - 8);

                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                e.Graphics.DrawString(text, Font, brush, rect);
            }

            e.DrawFocusRectangle();
        }
    }
}