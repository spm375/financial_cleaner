using ClosedXML.Excel;
using System.Text;
using System.Text.RegularExpressions;

namespace FinancialRequestCleaner;

public static class SpreadsheetCleaner
{
    // Mirrors the cols_to_drop list from clean_spreadsheet.py
    private static readonly string[] ColsToDrop =
    {
        "Request Id", "Date Approved", "Stage Name", "Request Type", "Status",
        "Approved Amount", "Approved By", "Payee", "Payee Last Name", "Payee Street",
        "Payee Street2", "Payee City", "Payee State", "Payee ZIP", "Description",
        "Comments on Approve/Deny", "Category", "Total of Attached Transactions",
        "Your Email Address:",
        "Please list a detailed description of the purchase request.",
        "Please fill in the following information in order to allow our office",
        "Please upload any supporting documents such as outstanding invoices",
        "Additional files", "Additional files.1",
        "Mileage Reimbursement: Please upload an image showing the route map",
        "Trained Organization Officer Position: - President",
        "Trained Organization Officer Position: - Treasurer",
        "Trained Organization Officer Position: - Vice-President",
        "Trained Organization Officer Position: - Event Coordinator (DUCOM)",
        "Trained Organization Officer Position: - Program/Class Rep (Elkins Park)",
        "Your Contact Number:", "Reimbursement: Drexel ID #",
        "Is this purchase request associated with an event or meeting",
        "Name of event as it is listed on DragonLink",
        "Reimbursement: Name of person being reimbursed",
        "Credit Card Check Out: Date credit card is requested",
        "Credit Card Check Out: Name of person picking up the credit card",
        "Food Orders: Please list the following:",
        "Credit Card Check Out: Email address of person checking out",
        "Purchase Request Additional Notes:",
        "Fund Transfer: Organization/Department name and account number",
        "Reimbursement: Name of Student Life staff member",
        "Mileage Reimbursement: Specify the per mile rate of reimbursement",
        "Reimbursement: Email address of person being reimbursed",
        "Student Life Grad and Undergrad Groups:",
        "Reimbursement: Address for the check to be sent",
        "Event Date (NA if purchase not related to an event):",
        "Promotional Purchase: Please upload design/logo.",
    };

    public static void CleanFile(
        string inputCsv,
        string outputXlsx,
        Action<string>? log    = null,
        Action<int, string>? onStep = null)
    {
        log    ??= _ => { };
        onStep ??= (_, _) => { };

        // ── Step 1: Read CSV (skip first 2 rows, row 3 is header) ────────────
        var (headers, rows) = ReadCsv(inputCsv);
        onStep(0, $"Found {rows.Count} requests in your file.");

        // ── Step 2: Filter rows ───────────────────────────────────────────────
        int statusIdx  = FindColumn(headers, "Status");
        int accountIdx = FindColumn(headers, "Account");

        int total = rows.Count;
        if (statusIdx >= 0)
            rows = rows.Where(r => Field(r, statusIdx).Trim().ToLower() == "unapproved").ToList();

        if (accountIdx >= 0)
            rows = rows.Where(r => KeepAccount(Field(r, accountIdx))).ToList();

        log($"Account filter: {rows.Count} rows kept ({total - rows.Count} removed).");
        onStep(1, $"Kept {rows.Count} unapproved requests (removed {total - rows.Count}).");

        // ── Step 3: Drop columns ──────────────────────────────────────────────
        var dropNorm = ColsToDrop.Select(Normalize).ToArray();

        bool ShouldDrop(string col)
        {
            var n = Normalize(col);
            return dropNorm.Any(p => n == p || n.StartsWith(p));
        }

        var keptIdx     = Enumerable.Range(0, headers.Count).Where(i => !ShouldDrop(headers[i])).ToList();
        var keptHeaders = keptIdx.Select(i => headers[i]).ToList();

        // Move "when do you need these items" to end (mirrors Python logic)
        int needLocal = keptHeaders.FindIndex(h => Normalize(h).Contains("when do you need these items"));
        if (needLocal >= 0)
        {
            var h = keptHeaders[needLocal];
            var k = keptIdx[needLocal];
            keptHeaders.RemoveAt(needLocal);
            keptIdx.RemoveAt(needLocal);
            keptHeaders.Add(h);
            keptIdx.Add(k);
        }

        // Sort by "what type of purchase request is this"
        int sortLocal = keptHeaders.FindIndex(h => Normalize(h).Contains("what type of purchase request is this"));
        if (sortLocal >= 0)
        {
            int sortOrig = keptIdx[sortLocal];
            rows = rows.OrderBy(r => Field(r, sortOrig)).ToList();
        }

        onStep(2, $"Cleaned down to {keptHeaders.Count} columns, {rows.Count} rows.");

        // ── Step 4: Save as .xlsx ─────────────────────────────────────────────
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sheet1");

        for (int j = 0; j < keptHeaders.Count; j++)
            ws.Cell(1, j + 1).Value = keptHeaders[j];

        for (int i = 0; i < rows.Count; i++)
            for (int j = 0; j < keptIdx.Count; j++)
                ws.Cell(i + 2, j + 1).Value = Field(rows[i], keptIdx[j]);

        ws.Columns().AdjustToContents();
        wb.SaveAs(outputXlsx);

        onStep(3, $"Saved: {Path.GetFileName(outputXlsx)}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Normalize(string s) =>
        s.Replace("\u00a0", " ").Trim().ToLower();

    private static int FindColumn(List<string> headers, string name) =>
        headers.FindIndex(h => h.Trim().Equals(name, StringComparison.OrdinalIgnoreCase));

    private static string Field(List<string> row, int idx) =>
        idx >= 0 && idx < row.Count ? row[idx] : "";

    // Mirrors the keep_account() lambda: keep if has "TEMP" or has no letters
    private static bool KeepAccount(string val)
    {
        val = val.Trim();
        if (Regex.IsMatch(val, "TEMP", RegexOptions.IgnoreCase)) return true;
        return !Regex.IsMatch(val, "[A-Za-z]");
    }

    // ── CSV parser (handles quoted fields and embedded newlines) ──────────────

    private static (List<string> headers, List<List<string>> rows) ReadCsv(string path)
    {
        var text    = File.ReadAllText(path, Encoding.UTF8);
        var allRows = ParseCsv(text);

        if (allRows.Count < 3)
            throw new InvalidDataException("The CSV must have at least 3 rows (2 header rows + column names).");

        var headers = allRows[2];                   // row index 2 = 3rd row, matching pandas skiprows=2
        var rows    = allRows.Skip(3).ToList();     // data starts at row 4

        return (headers, rows);
    }

    private static List<List<string>> ParseCsv(string text)
    {
        var result = new List<List<string>>();
        int pos = 0, len = text.Length;

        while (pos < len)
        {
            var row = new List<string>();

            while (true)
            {
                string field;

                if (pos < len && text[pos] == '"')
                {
                    // Quoted field — handles embedded commas and newlines
                    pos++;
                    var sb = new StringBuilder();
                    while (pos < len)
                    {
                        if (text[pos] == '"')
                        {
                            if (pos + 1 < len && text[pos + 1] == '"') { sb.Append('"'); pos += 2; }
                            else { pos++; break; }
                        }
                        else sb.Append(text[pos++]);
                    }
                    field = sb.ToString();
                }
                else
                {
                    // Unquoted field
                    var sb = new StringBuilder();
                    while (pos < len && text[pos] != ',' && text[pos] != '\r' && text[pos] != '\n')
                        sb.Append(text[pos++]);
                    field = sb.ToString();
                }

                row.Add(field);

                if (pos >= len || text[pos] == '\r' || text[pos] == '\n')
                {
                    if (pos < len && text[pos] == '\r') pos++;
                    if (pos < len && text[pos] == '\n') pos++;
                    break;
                }
                else
                {
                    pos++; // skip comma
                }
            }

            // Skip blank rows
            if (row.Count > 0 && !(row.Count == 1 && row[0] == ""))
                result.Add(row);
        }

        return result;
    }
}
