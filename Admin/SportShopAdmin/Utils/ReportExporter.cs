using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;
using System.Windows.Forms;
using SportShopAdmin.BLL;

namespace SportShopAdmin.Utils
{
    /// <summary>
    /// Xuất dữ liệu đang hiển thị trên DataGridView ra file Excel (.xlsx), PDF hoặc CSV.
    /// - Excel: tự tạo file Office Open XML (không cần cài thêm thư viện).
    /// - PDF : in qua máy in ảo "Microsoft Print to PDF" có sẵn trên Windows 10/11.
    /// </summary>
    public static class ReportExporter
    {
        private class Table
        {
            public List<string> Headers = new List<string>();
            public List<float> Weights = new List<float>();
            public List<bool> NumericCols = new List<bool>();
            public List<object[]> RawRows = new List<object[]>();
            public List<string[]> TextRows = new List<string[]>();
        }

        private static Table ReadGrid(DataGridView grid)
        {
            var t = new Table();
            List<DataGridViewColumn> cols = grid.Columns.Cast<DataGridViewColumn>()
                .Where(c => c.Visible).OrderBy(c => c.DisplayIndex).ToList();
            foreach (DataGridViewColumn c in cols)
            {
                t.Headers.Add(c.HeaderText);
                t.Weights.Add(c.Width);
                t.NumericCols.Add(IsNumericType(c.ValueType));
            }
            foreach (DataGridViewRow r in grid.Rows)
            {
                if (r.IsNewRow) continue;
                var raw = new object[cols.Count];
                var text = new string[cols.Count];
                for (int i = 0; i < cols.Count; i++)
                {
                    DataGridViewCell cell = r.Cells[cols[i].Index];
                    raw[i] = cell.Value;
                    object fv = cell.FormattedValue;
                    text[i] = fv == null ? "" : fv.ToString();
                }
                t.RawRows.Add(raw);
                t.TextRows.Add(text);
            }
            if (t.TextRows.Count == 0) throw new BusinessException("Báo cáo trống, không thể xuất file.");
            return t;
        }

        private static bool IsNumericType(Type type)
        {
            if (type == null) return false;
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type == typeof(int) || type == typeof(long) || type == typeof(decimal) || type == typeof(double)
                || type == typeof(float) || type == typeof(short);
        }

        // ================================================================ CSV
        public static void ExportCsv(string path, string title, string subtitle, DataGridView grid)
        {
            Table t = ReadGrid(grid);
            var sb = new StringBuilder();
            sb.AppendLine(Csv(title));
            if (!string.IsNullOrEmpty(subtitle)) sb.AppendLine(Csv(subtitle));
            sb.AppendLine();
            sb.AppendLine(string.Join(",", t.Headers.Select(Csv)));
            for (int r = 0; r < t.RawRows.Count; r++)
            {
                var cells = new string[t.Headers.Count];
                for (int c = 0; c < cells.Length; c++)
                {
                    object v = t.RawRows[r][c];
                    cells[c] = t.NumericCols[c] && v != null && v != DBNull.Value
                        ? Convert.ToString(v, CultureInfo.InvariantCulture)
                        : Csv(t.TextRows[r][c]);
                }
                sb.AppendLine(string.Join(",", cells));
            }
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        private static string Csv(string s)
        {
            s = s ?? "";
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        // ================================================================ EXCEL (.xlsx)
        public static void ExportExcel(string path, string title, string subtitle, DataGridView grid)
        {
            Table t = ReadGrid(grid);
            int n = t.Headers.Count;

            var sheet = new StringBuilder();
            sheet.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sheet.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            sheet.Append("<cols>");
            for (int c = 0; c < n; c++)
            {
                int width = t.NumericCols[c] ? 18 : 32;
                sheet.Append("<col min=\"" + (c + 1) + "\" max=\"" + (c + 1) + "\" width=\"" + width + "\" customWidth=\"1\"/>");
            }
            sheet.Append("</cols><sheetData>");
            int row = 1;
            sheet.Append(RowXml(row, new[] { InlineCell("A" + row, title, 1) }));
            row++;
            if (!string.IsNullOrEmpty(subtitle))
            {
                sheet.Append(RowXml(row, new[] { InlineCell("A" + row, subtitle, 0) }));
                row++;
            }
            sheet.Append(RowXml(row, new[] { InlineCell("A" + row, "Ngày xuất: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), 0) }));
            row += 2; // chừa 1 dòng trống

            var header = new List<string>();
            for (int c = 0; c < n; c++) header.Add(InlineCell(ColName(c) + row, t.Headers[c], 2));
            sheet.Append(RowXml(row++, header));

            for (int r = 0; r < t.RawRows.Count; r++)
            {
                var cells = new List<string>();
                for (int c = 0; c < n; c++)
                {
                    string refName = ColName(c) + row;
                    object v = t.RawRows[r][c];
                    if (t.NumericCols[c] && v != null && v != DBNull.Value)
                        cells.Add("<c r=\"" + refName + "\" s=\"4\"><v>" + Convert.ToString(v, CultureInfo.InvariantCulture) + "</v></c>");
                    else
                        cells.Add(InlineCell(refName, t.TextRows[r][c], 3));
                }
                sheet.Append(RowXml(row++, cells));
            }
            sheet.Append("</sheetData></worksheet>");

            if (File.Exists(path)) File.Delete(path);
            using (FileStream fs = new FileStream(path, FileMode.Create))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                AddEntry(zip, "[Content_Types].xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                    "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                    "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                    "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                    "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                    "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
                    "</Types>");
                AddEntry(zip, "_rels/.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                    "</Relationships>");
                AddEntry(zip, "xl/workbook.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
                    "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                    "<sheets><sheet name=\"BaoCao\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
                AddEntry(zip, "xl/_rels/workbook.xml.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                    "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
                    "</Relationships>");
                AddEntry(zip, "xl/styles.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                    "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                    "<numFmts count=\"1\"><numFmt numFmtId=\"164\" formatCode=\"#,##0\"/></numFmts>" +
                    "<fonts count=\"3\">" +
                    "<font><sz val=\"11\"/><name val=\"Calibri\"/></font>" +
                    "<font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font>" +
                    "<font><b/><sz val=\"14\"/><name val=\"Calibri\"/></font>" +
                    "</fonts>" +
                    "<fills count=\"3\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill>" +
                    "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFD9EAD3\"/><bgColor indexed=\"64\"/></patternFill></fill></fills>" +
                    "<borders count=\"2\"><border><left/><right/><top/><bottom/><diagonal/></border>" +
                    "<border><left style=\"thin\"><color auto=\"1\"/></left><right style=\"thin\"><color auto=\"1\"/></right>" +
                    "<top style=\"thin\"><color auto=\"1\"/></top><bottom style=\"thin\"><color auto=\"1\"/></bottom><diagonal/></border></borders>" +
                    "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
                    "<cellXfs count=\"5\">" +
                    "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
                    "<xf numFmtId=\"0\" fontId=\"2\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>" +
                    "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\"/>" +
                    "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyBorder=\"1\"/>" +
                    "<xf numFmtId=\"164\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyNumberFormat=\"1\" applyBorder=\"1\"/>" +
                    "</cellXfs>" +
                    "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>" +
                    "</styleSheet>");
                AddEntry(zip, "xl/worksheets/sheet1.xml", sheet.ToString());
            }
        }

        private static string RowXml(int row, IEnumerable<string> cells)
        {
            return "<row r=\"" + row + "\">" + string.Concat(cells) + "</row>";
        }

        private static string InlineCell(string refName, string text, int style)
        {
            return "<c r=\"" + refName + "\" t=\"inlineStr\" s=\"" + style + "\"><is><t xml:space=\"preserve\">" +
                   SecurityElement.Escape(text ?? "") + "</t></is></c>";
        }

        private static string ColName(int index)
        {
            string name = "";
            index++;
            while (index > 0)
            {
                int m = (index - 1) % 26;
                name = (char)('A' + m) + name;
                index = (index - 1) / 26;
            }
            return name;
        }

        private static void AddEntry(ZipArchive zip, string name, string content)
        {
            ZipArchiveEntry entry = zip.CreateEntry(name);
            using (var w = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
            {
                w.Write(content);
            }
        }

        // ================================================================ PDF
        public const string PdfPrinter = "Microsoft Print to PDF";

        public static void ExportPdf(string path, string title, string subtitle, DataGridView grid)
        {
            Table t = ReadGrid(grid);
            bool hasPdfPrinter = PrinterSettings.InstalledPrinters.Cast<string>()
                .Any(p => string.Equals(p, PdfPrinter, StringComparison.OrdinalIgnoreCase));
            if (!hasPdfPrinter)
                throw new BusinessException("Không tìm thấy máy in ảo \"Microsoft Print to PDF\". " +
                    "Hãy bật nó trong Windows Features hoặc xuất ra Excel.");

            if (File.Exists(path)) File.Delete(path);
            int n = t.Headers.Count;
            float totalWeight = t.Weights.Sum();
            int rowIndex = 0;
            int page = 0;

            using (var titleFont = new Font("Arial", 15F, FontStyle.Bold))
            using (var normalFont = new Font("Arial", 9F))
            using (var headerFont = new Font("Arial", 9F, FontStyle.Bold))
            using (var headerBrush = new SolidBrush(Color.FromArgb(217, 234, 211)))
            using (var doc = new PrintDocument())
            {
                doc.DocumentName = title;
                doc.PrinterSettings.PrinterName = PdfPrinter;
                doc.PrinterSettings.PrintToFile = true;
                doc.PrinterSettings.PrintFileName = path;
                doc.DefaultPageSettings.Landscape = n > 5;
                doc.DefaultPageSettings.Margins = new Margins(40, 40, 50, 50);
                doc.PrintController = new StandardPrintController();

                doc.PrintPage += (s, e) =>
                {
                    Graphics g = e.Graphics;
                    Rectangle b = e.MarginBounds;
                    float y = b.Top;
                    page++;

                    if (page == 1)
                    {
                        g.DrawString(title, titleFont, Brushes.Black, b.Left, y);
                        y += titleFont.GetHeight(g) + 4;
                        if (!string.IsNullOrEmpty(subtitle))
                        {
                            g.DrawString(subtitle, normalFont, Brushes.Black, b.Left, y);
                            y += normalFont.GetHeight(g) + 2;
                        }
                        g.DrawString("Ngày xuất: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), normalFont, Brushes.Gray, b.Left, y);
                        y += normalFont.GetHeight(g) + 10;
                    }

                    float rowH = normalFont.GetHeight(g) + 8;
                    var widths = new float[n];
                    for (int c = 0; c < n; c++) widths[c] = b.Width * t.Weights[c] / totalWeight;

                    // tiêu đề cột
                    float x = b.Left;
                    for (int c = 0; c < n; c++)
                    {
                        var rect = new RectangleF(x, y, widths[c], rowH);
                        g.FillRectangle(headerBrush, rect);
                        g.DrawRectangle(Pens.Gray, rect.X, rect.Y, rect.Width, rect.Height);
                        DrawCell(g, t.Headers[c], headerFont, rect, false);
                        x += widths[c];
                    }
                    y += rowH;

                    // dữ liệu
                    while (rowIndex < t.TextRows.Count && y + rowH <= b.Bottom - 20)
                    {
                        x = b.Left;
                        for (int c = 0; c < n; c++)
                        {
                            var rect = new RectangleF(x, y, widths[c], rowH);
                            g.DrawRectangle(Pens.LightGray, rect.X, rect.Y, rect.Width, rect.Height);
                            DrawCell(g, t.TextRows[rowIndex][c], normalFont, rect, t.NumericCols[c]);
                            x += widths[c];
                        }
                        y += rowH;
                        rowIndex++;
                    }

                    g.DrawString("Trang " + page, normalFont, Brushes.Gray, b.Right - 60, b.Bottom);
                    e.HasMorePages = rowIndex < t.TextRows.Count;
                };
                doc.Print();
            }
        }

        private static void DrawCell(Graphics g, string text, Font font, RectangleF rect, bool alignRight)
        {
            using (var sf = new StringFormat
            {
                LineAlignment = StringAlignment.Center,
                Alignment = alignRight ? StringAlignment.Far : StringAlignment.Near,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            })
            {
                var inner = new RectangleF(rect.X + 4, rect.Y, rect.Width - 8, rect.Height);
                g.DrawString(text ?? "", font, Brushes.Black, inner, sf);
            }
        }
    }
}
