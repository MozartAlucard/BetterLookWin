using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BetterLookWin.Core;

/// <summary>
/// Dibuja el menú de la bandeja con la estética de la app: fondo negro, texto claro,
/// selección en violeta, casilla violeta en lo marcado y separadores finos.
/// </summary>
internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    private static readonly Color Background = Color.FromArgb(0x12, 0x12, 0x16);
    private static readonly Color Border = Color.FromArgb(0x2A, 0x2A, 0x33);
    private static readonly Color Text = Color.FromArgb(0xE4, 0xE1, 0xE7);
    private static readonly Color Header = Color.FromArgb(0xA7, 0x8B, 0xFA);
    private static readonly Color Violet = Color.FromArgb(0x8B, 0x5C, 0xF6);
    private static readonly Color VioletDark = Color.FromArgb(0x7C, 0x3A, 0xED);

    public DarkMenuRenderer() : base(new DarkColors())
    {
        RoundedEdges = false;
    }

    /// <summary>Aplica al menú el aspecto oscuro (colores, fuente, márgenes).</summary>
    public static void Apply(ContextMenuStrip menu)
    {
        menu.Renderer = new DarkMenuRenderer();
        menu.BackColor = Background;
        menu.ForeColor = Text;
        menu.Font = new Font("Segoe UI", 9.5f);
        menu.ShowImageMargin = false;
        menu.ShowCheckMargin = true;
        menu.Padding = new Padding(0, 4, 0, 4);
        menu.MinimumSize = new Size(230, 0);

        // Esquinas redondeadas del sistema (Windows 11). En versiones viejas no hace nada.
        menu.Opened += (_, _) => RoundCorners(menu.Handle);
    }

    /// <summary>Da altura cómoda a los elementos del menú (llamar después de llenarlo).</summary>
    public static void StyleItems(ContextMenuStrip menu)
    {
        foreach (ToolStripItem item in menu.Items)
            if (item is ToolStripMenuItem)
                item.Padding = new Padding(6, 5, 6, 5);
    }

    // ---------- Dibujo ----------

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(Background);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(Border);
        var r = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        e.Graphics.DrawRectangle(pen, r);
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        // Sin columna de color aparte: todo el menú es del mismo negro.
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var item = e.Item;
        if (!item.Enabled || (!item.Selected && !item.Pressed)) return;

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(5, 1, item.Width - 10, item.Height - 2);
        using var path = RoundedRect(rect, 4);
        using var fill = new SolidBrush(Color.FromArgb(70, Violet));
        using var line = new Pen(Color.FromArgb(120, Violet));
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(line, path);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = !e.Item.Enabled ? Header : e.Item.Selected ? Color.White : Text;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        using var pen = new Pen(Color.FromArgb(0x24, 0xFF, 0xFF, 0xFF));
        int y = e.Item.Height / 2;
        e.Graphics.DrawLine(pen, 12, y, e.Item.Width - 12, y);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        const int size = 14;
        var area = e.ImageRectangle;
        var box = new Rectangle(
            area.X + (area.Width - size) / 2,
            area.Y + (area.Height - size) / 2,
            size, size);

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (var path = RoundedRect(box, 4))
        using (var fill = new SolidBrush(VioletDark))
        using (var line = new Pen(Violet))
        {
            g.FillPath(fill, path);
            g.DrawPath(line, path);
        }

        using var tick = new Pen(Color.White, 2f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        g.DrawLines(tick, new[]
        {
            new Point(box.X + 3, box.Y + 7),
            new Point(box.X + 6, box.Y + 10),
            new Point(box.X + 11, box.Y + 4)
        });
    }

    private static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        int d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    // ---------- Esquinas redondeadas (Windows 11) ----------

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private static void RoundCorners(IntPtr hwnd)
    {
        try
        {
            int round = 2; // DWMWCP_ROUND
            DwmSetWindowAttribute(hwnd, 33, ref round, sizeof(int));
        }
        catch
        {
        }
    }

    private sealed class DarkColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Background;
        public override Color ImageMarginGradientBegin => Background;
        public override Color ImageMarginGradientMiddle => Background;
        public override Color ImageMarginGradientEnd => Background;
        public override Color MenuBorder => Border;
        public override Color MenuItemBorder => Violet;
        public override Color SeparatorDark => Border;
        public override Color SeparatorLight => Border;
    }
}
