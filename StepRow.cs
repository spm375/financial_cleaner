namespace FinancialRequestCleaner;

public class StepRow : Panel
{
    private static readonly Color Gray  = ColorTranslator.FromHtml("#9CA3AF");
    private static readonly Color Blue  = ColorTranslator.FromHtml("#2563EB");
    private static readonly Color Green = ColorTranslator.FromHtml("#16A34A");
    private static readonly Color Red   = ColorTranslator.FromHtml("#DC2626");
    private static readonly Color Dark  = ColorTranslator.FromHtml("#111827");

    private readonly Label _dot;
    private readonly Label _text;

    public bool IsActive => _dot.Text == "◌";

    public StepRow(string label)
    {
        BackColor = Color.White;
        Height    = 42;

        _dot = new Label
        {
            Text      = "–",
            Font      = new Font("Segoe UI", 13, FontStyle.Regular),
            ForeColor = Gray,
            Size      = new Size(36, 42),
            Location  = new Point(12, 0),
            TextAlign = ContentAlignment.MiddleCenter,
        };

        _text = new Label
        {
            Text      = label,
            Font      = new Font("Segoe UI", 11, FontStyle.Regular),
            ForeColor = Gray,
            Location  = new Point(52, 0),
            Size      = new Size(400, 42),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        Controls.Add(_dot);
        Controls.Add(_text);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        _text.Width = Width - 60;
    }

    public void Pending()
    {
        _dot.Text      = "–";
        _dot.ForeColor = Gray;
        _text.ForeColor = Gray;
    }

    public void Active()
    {
        _dot.Text      = "◌";
        _dot.ForeColor = Blue;
        _text.ForeColor = Dark;
    }

    public void Done(string? newLabel = null)
    {
        if (newLabel != null) _text.Text = newLabel;
        _dot.Text      = "✓";
        _dot.ForeColor = Green;
        _text.ForeColor = Dark;
    }

    public void Error()
    {
        _dot.Text      = "✗";
        _dot.ForeColor = Red;
        _text.ForeColor = Red;
    }
}
