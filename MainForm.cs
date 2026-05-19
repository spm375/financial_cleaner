using System.Diagnostics;

namespace FinancialRequestCleaner;

public class MainForm : Form
{
    // ── Palette (matches app.py exactly) ─────────────────────────────────────
    private static readonly Color BG     = ColorTranslator.FromHtml("#F0F2F5");
    private static readonly Color CARD   = Color.White;
    private static readonly Color BORDER = ColorTranslator.FromHtml("#DDE1E7");
    private static readonly Color BLUE   = ColorTranslator.FromHtml("#2563EB");
    private static readonly Color BLUE_H = ColorTranslator.FromHtml("#1D4ED8");
    private static readonly Color GREEN  = ColorTranslator.FromHtml("#16A34A");
    private static readonly Color RED    = ColorTranslator.FromHtml("#DC2626");
    private static readonly Color MUTED  = ColorTranslator.FromHtml("#6B7280");
    private static readonly Color DARK   = ColorTranslator.FromHtml("#111827");
    private static readonly Color DIS_BG = ColorTranslator.FromHtml("#D1D5DB");

    private static readonly string Downloads =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

    private const int W  = 480;   // content width (matches Python)
    private const int PX = 28;    // horizontal padding

    // Controls
    private Label    _fileIcon = null!;
    private Label    _fileName = null!;
    private Label    _fileSub  = null!;
    private Button   _cleanBtn = null!;
    private Label    _openLink = null!;
    private StepRow[] _steps   = null!;

    private string? _csvPath;
    private string? _outPath;

    public MainForm()
    {
        BuildUi();
        CenterToScreen();
    }

    // ── Build UI ──────────────────────────────────────────────────────────────

    private void BuildUi()
    {
        Text            = "Financial Request Cleaner";
        BackColor       = BG;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        Font            = new Font("Segoe UI", 10);

        int y = PX;

        // ── Header ────────────────────────────────────────────────────────────
        var title = AddLabel("Financial Request Cleaner",
            new Font("Segoe UI", 16, FontStyle.Bold), DARK,
            new Rectangle(PX, y, W, 30));
        y += title.Height + 4;

        AddLabel("Finds your CSV in Downloads, cleans it, and saves it back — ready to use.",
            new Font("Segoe UI", 10), MUTED,
            new Rectangle(PX, y, W, 20));
        y += 34;

        // ── File card ─────────────────────────────────────────────────────────
        var fileCard = new BorderPanel(BORDER) { Location = new Point(PX, y), Size = new Size(W, 70) };
        Controls.Add(fileCard);

        _fileIcon = new Label
        {
            Text = "📂", Font = new Font("Segoe UI", 18), BackColor = CARD,
            Size = new Size(44, 44), Location = new Point(14, 13), TextAlign = ContentAlignment.MiddleCenter,
        };
        fileCard.Controls.Add(_fileIcon);

        _fileName = new Label
        {
            Text = "No file selected", Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = MUTED, BackColor = CARD,
            Location = new Point(68, 12), Size = new Size(310, 22), TextAlign = ContentAlignment.MiddleLeft,
        };
        fileCard.Controls.Add(_fileName);

        _fileSub = new Label
        {
            Text = "Click Browse to choose your CSV from Downloads.",
            Font = new Font("Segoe UI", 9), ForeColor = MUTED, BackColor = CARD,
            Location = new Point(68, 36), Size = new Size(310, 20), TextAlign = ContentAlignment.MiddleLeft,
        };
        fileCard.Controls.Add(_fileSub);

        var browse = new Label
        {
            Text = "Browse", Font = new Font("Segoe UI", 10), ForeColor = BLUE, BackColor = CARD,
            Location = new Point(W - 74, 23), Size = new Size(60, 24),
            TextAlign = ContentAlignment.MiddleRight, Cursor = Cursors.Hand,
        };
        browse.Click += (_, _) => Browse();
        fileCard.Controls.Add(browse);

        y += 70 + 16;

        // ── Clean button ──────────────────────────────────────────────────────
        _cleanBtn = new Button
        {
            Text      = "Clean File",
            Font      = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = DIS_BG,
            FlatStyle = FlatStyle.Flat,
            Location  = new Point(PX, y),
            Size      = new Size(W, 48),
            Enabled   = false,
            Cursor    = Cursors.Hand,
        };
        _cleanBtn.FlatAppearance.BorderSize         = 0;
        _cleanBtn.FlatAppearance.MouseOverBackColor = BLUE_H;
        _cleanBtn.Click += (_, _) => StartClean();
        Controls.Add(_cleanBtn);
        y += 48 + 16;

        // ── Steps card ────────────────────────────────────────────────────────
        var stepLabels = new[] {
            "Reading your file",
            "Filtering to unapproved requests",
            "Cleaning up columns",
            "Saving to Downloads",
        };
        _steps = stepLabels.Select(l => new StepRow(l)).ToArray();

        int cardH = _steps.Length * 43 - 1;
        var stepsCard = new BorderPanel(BORDER) { Location = new Point(PX, y), Size = new Size(W, cardH) };
        Controls.Add(stepsCard);

        for (int i = 0; i < _steps.Length; i++)
        {
            _steps[i].Location = new Point(0, i * 43);
            _steps[i].Width    = W;
            stepsCard.Controls.Add(_steps[i]);

            if (i < _steps.Length - 1)
                stepsCard.Controls.Add(new Panel
                {
                    BackColor = BORDER, Location = new Point(0, (i + 1) * 43 - 1), Size = new Size(W, 1),
                });
        }
        y += cardH + 12;

        // ── Open file link (hidden until success) ─────────────────────────────
        _openLink = new Label
        {
            Text      = "Open File  →",
            Font      = new Font("Segoe UI", 11),
            ForeColor = BLUE,
            BackColor = BG,
            Location  = new Point(PX, y),
            Size      = new Size(W, 28),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor    = Cursors.Hand,
            Visible   = false,
        };
        _openLink.Click += (_, _) => OpenFile();
        Controls.Add(_openLink);

        ClientSize = new Size(W + PX * 2, y + 28 + PX);
    }

    private Label AddLabel(string text, Font font, Color color, Rectangle bounds)
    {
        var lbl = new Label
        {
            Text      = text,
            Font      = font,
            ForeColor = color,
            BackColor = BG,
            Location  = new Point(bounds.X, bounds.Y),
            Size      = new Size(bounds.Width, bounds.Height),
        };
        Controls.Add(lbl);
        return lbl;
    }

    // ── Browse ────────────────────────────────────────────────────────────────

    private void Browse()
    {
        using var dlg = new OpenFileDialog
        {
            InitialDirectory = Downloads,
            Title  = "Select your CSV file",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        _csvPath = dlg.FileName;
        var dir  = Path.GetDirectoryName(_csvPath) == Downloads ? "Downloads" : Path.GetDirectoryName(_csvPath);

        _fileIcon.Text      = "📄";
        _fileName.Text      = Path.GetFileName(_csvPath);
        _fileName.ForeColor = DARK;
        _fileSub.Text       = $"Selected from {dir}";
        _cleanBtn.Enabled   = true;
        _cleanBtn.BackColor = BLUE;
    }

    // ── Clean ─────────────────────────────────────────────────────────────────

    private void StartClean()
    {
        if (_csvPath == null || !File.Exists(_csvPath))
        {
            _fileName.Text      = "File not found — please browse again.";
            _fileName.ForeColor = RED;
            return;
        }

        _cleanBtn.Enabled   = false;
        _cleanBtn.BackColor = DIS_BG;
        _cleanBtn.Text      = "Cleaning…";
        _openLink.Visible   = false;
        foreach (var s in _steps) s.Pending();
        _steps[0].Active();

        var today = DateTime.Today;
        _outPath  = Path.Combine(Downloads, $"Cleaned Requests - {today:MMMM} {today.Day}, {today.Year}.xlsx");

        var csvPath = _csvPath;
        var outPath = _outPath;

        // Friendly label generators per step (mirrors Python's friendly[] lambdas)
        Func<string, string>[] friendly =
        {
            m => { var p = m.Split(' '); return $"Read {(p.Length > 1 ? p[1] : "?")} requests"; },
            m => { var p = m.Split(' '); return $"Kept {(p.Length > 1 ? p[1] : "?")} unapproved requests"; },
            m => { var p = m.Split(' '); return $"Cleaned to {(p.Length > 3 ? p[3] : "?")} columns"; },
            _ => "Saved to Downloads",
        };

        void OnStep(int n, string msg)
        {
            var label = n < friendly.Length ? friendly[n](msg) : msg;
            Invoke(() => MarkDone(n, label));
        }

        Task.Run(() =>
        {
            try
            {
                SpreadsheetCleaner.CleanFile(csvPath, outPath, log: _ => { }, onStep: OnStep);
                Invoke(OnSuccess);
            }
            catch (Exception ex)
            {
                Invoke(() => OnError(ex.Message));
            }
        });
    }

    private void MarkDone(int n, string label)
    {
        _steps[n].Done(label);
        if (n + 1 < _steps.Length) _steps[n + 1].Active();
    }

    private void OnSuccess()
    {
        _cleanBtn.Enabled   = true;
        _cleanBtn.BackColor = BLUE;
        _cleanBtn.Text      = "Clean File";
        _fileName.Text      = "Done! Ready for the next file.";
        _fileName.ForeColor = GREEN;
        _fileSub.Text       = "Drop a new CSV in Downloads whenever you're ready.";
        _openLink.Visible   = true;
    }

    private void OnError(string msg)
    {
        _cleanBtn.Enabled   = true;
        _cleanBtn.BackColor = BLUE;
        _cleanBtn.Text      = "Clean File";
        foreach (var s in _steps)
            if (s.IsActive) s.Error();
        _fileName.Text      = "Something went wrong.";
        _fileName.ForeColor = RED;
        _fileSub.Text       = msg;
    }

    // ── Open file ─────────────────────────────────────────────────────────────

    private void OpenFile()
    {
        if (_outPath != null && File.Exists(_outPath))
            Process.Start(new ProcessStartInfo(_outPath) { UseShellExecute = true });
    }
}

// ── Helper: panel that draws a 1px border ─────────────────────────────────────

file class BorderPanel(Color borderColor) : Panel
{
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(borderColor);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
}
