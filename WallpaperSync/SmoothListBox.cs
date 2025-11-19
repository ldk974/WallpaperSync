using System;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;

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
        e.DrawBackground();

        if (e.Index < 0 || e.Index >= Items.Count)
            return;

        string text = Items[e.Index].ToString();

        bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        bool isHover = (e.Index == hoverIndex);

        Color backColor;
        Color textColor = Color.White;

        if (isSelected)
            backColor = Color.FromArgb(70, 70, 70);
        else if (isHover)
            backColor = Color.FromArgb(55, 55, 55);
        else
            backColor = Color.FromArgb(40, 40, 40);

        using (var bg = new SolidBrush(backColor))
            e.Graphics.FillRectangle(bg, e.Bounds);

        using (var brush = new SolidBrush(textColor))
        {
            var rect = new Rectangle(e.Bounds.X + 12, e.Bounds.Y + 8,
                                     e.Bounds.Width - 12, e.Bounds.Height - 8);

            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            e.Graphics.DrawString(text, Font, brush, rect);
        }

        e.DrawFocusRectangle();
    }
}