using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;
using SportZoneAdmin.BLL;
using SportZoneAdmin.DTO;

namespace SportZoneAdmin.GUI
{
    /// <summary>
    /// Hàm dựng giao diện và thông báo dùng chung cho các Form.
    /// Bảng màu theo quy chuẩn giao diện của báo cáo (Bảng 3.19), đồng bộ với ứng dụng Web:
    /// chủ đạo xanh đậm #2C4A6B, nhấn vàng #F4B400, lỗi đỏ #D32F2F, thành công xanh lá #15803D.
    /// </summary>
    public static class UiHelper
    {
        // ------------------------------------------------------------------ bảng màu
        public static readonly Color Primary = Color.FromArgb(44, 74, 107);        // #2C4A6B
        public static readonly Color PrimaryDark = Color.FromArgb(30, 53, 80);     // #1E3550
        public static readonly Color PrimarySoft = Color.FromArgb(232, 238, 245);  // #E8EEF5
        public static readonly Color Accent = Color.FromArgb(244, 180, 0);         // #F4B400
        public static readonly Color Blue = Color.FromArgb(37, 99, 235);
        public static readonly Color Danger = Color.FromArgb(211, 47, 47);         // #D32F2F
        public static readonly Color Success = Color.FromArgb(21, 128, 61);        // #15803D
        public static readonly Color Gray = Color.FromArgb(100, 116, 139);
        public static readonly Color Orange = Color.FromArgb(232, 115, 12);
        public static readonly Color Purple = Color.FromArgb(124, 58, 237);
        public static readonly Color WarningBack = Color.FromArgb(253, 236, 236);
        public static readonly Color PageBack = Color.FromArgb(244, 246, 248);     // #F4F6F8
        public static readonly Color MenuBack = PrimaryDark;
        public static readonly Color Line = Color.FromArgb(226, 232, 240);         // #E2E8F0
        public static readonly Color TextColor = Color.FromArgb(31, 41, 51);       // #1F2933
        public static readonly Color Muted = Color.FromArgb(100, 116, 139);        // #64748B

        public static readonly Font BaseFont = new Font("Segoe UI", 10F);
        public static readonly Font BoldFont = new Font("Segoe UI", 10F, FontStyle.Bold);
        public static readonly Font SmallFont = new Font("Segoe UI", 9F);
        public static readonly Font CardTitleFont = new Font("Segoe UI Semibold", 11F);
        public static readonly Font TitleFont = new Font("Segoe UI", 15F, FontStyle.Bold);
        public static readonly Font BigNumberFont = new Font("Segoe UI", 18F, FontStyle.Bold);

        // ------------------------------------------------------------------ vẽ
        private static readonly Dictionary<float, Font> iconFonts = new Dictionary<float, Font>();

        /// <summary>Font biểu tượng (Segoe MDL2 Assets) theo cỡ chữ, dùng lại cho mọi control</summary>
        public static Font IconFont(float size)
        {
            Font f;
            if (!iconFonts.TryGetValue(size, out f))
            {
                f = new Font("Segoe MDL2 Assets", size);
                iconFonts[size] = f;
            }
            return f;
        }

        public static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = Math.Max(1, Math.Min(radius * 2, Math.Min(r.Width, r.Height)));
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>Trộn màu a với b theo tỉ lệ t (0 = a, 1 = b)</summary>
        public static Color Blend(Color a, Color b, double t)
        {
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        public static Color Darken(Color c, double t)
        {
            return Blend(c, Color.Black, t);
        }

        /// <summary>Màu nền thực của control cha (bỏ qua nền trong suốt) để vẽ góc bo tròn</summary>
        public static Color ParentBack(Control c)
        {
            for (Control p = c.Parent; p != null; p = p.Parent)
                if (p.BackColor.A == 255) return p.BackColor;
            return Color.White;
        }

        /// <summary>Gắn sự kiện click cho control và mọi control con (thẻ có thể bấm được)</summary>
        public static void ClickAll(Control root, EventHandler handler)
        {
            root.Cursor = Cursors.Hand;
            root.Click += handler;
            foreach (Control c in root.Controls) ClickAll(c, handler);
        }

        // ------------------------------------------------------------------ dựng control
        public static void SetupChild(Form f, string title)
        {
            f.Text = title;
            f.Font = BaseFont;
            f.ForeColor = TextColor;
            f.BackColor = Color.White;
            f.FormBorderStyle = FormBorderStyle.None;
        }

        /// <summary>Icon tự gắn cho nút theo nội dung chữ</summary>
        private static string GlyphFor(string text)
        {
            string t = (text ?? "").ToLowerInvariant();
            if (t.StartsWith("+") || t.StartsWith("thêm")) return Glyph.Add;
            if (t.StartsWith("lưu") || t.StartsWith("đồng ý")) return Glyph.Save;
            if (t.StartsWith("xóa")) return Glyph.Delete;
            if (t.StartsWith("làm mới")) return Glyph.Refresh;
            if (t.StartsWith("tìm") || t.StartsWith("lọc")) return Glyph.Search;
            if (t.StartsWith("xem")) return Glyph.Eye;
            if (t.StartsWith("xuất") || t == "csv") return Glyph.Download;
            if (t.StartsWith("xác nhận")) return Glyph.Check;
            if (t.StartsWith("hủy")) return Glyph.Cancel;
            if (t.StartsWith("cập nhật") || t.StartsWith("sửa")) return Glyph.Edit;
            if (t.StartsWith("khôi phục")) return Glyph.Undo;
            if (t.StartsWith("đăng nhập")) return Glyph.User;
            if (t.StartsWith("chọn ảnh")) return Glyph.Add;
            return null;
        }

        /// <summary>Nút bo góc có icon. Màu Gray được hiểu là nút phụ (nền trắng, viền xám).</summary>
        public static Button MakeButton(string text, Color back, int width = 120)
        {
            var b = new FlatButton
            {
                Text = text,
                Glyph = GlyphFor(text),
                Outline = back == Gray,
                Width = width,
                Height = 34,
                BackColor = back,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.75F),
                Margin = new Padding(3, 3, 6, 3),
                UseVisualStyleBackColor = false
            };
            return b;
        }

        public static FlowLayoutPanel MakeToolbar()
        {
            var tb = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 56,
                Padding = new Padding(8, 10, 8, 4),
                WrapContents = false,
                BackColor = Color.White
            };
            tb.Paint += (s, e) =>
            {
                using (var p = new Pen(Line)) e.Graphics.DrawLine(p, 0, tb.Height - 1, tb.Width, tb.Height - 1);
            };
            return tb;
        }

        private static void StyleInput(Control c)
        {
            var tb = c as TextBox;
            if (tb != null) tb.BorderStyle = BorderStyle.FixedSingle;
        }

        /// <summary>Thêm (nhãn +) control vào thanh công cụ</summary>
        public static T AddTool<T>(FlowLayoutPanel toolbar, string label, T control) where T : Control
        {
            if (!string.IsNullOrEmpty(label))
                toolbar.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = Muted, Margin = new Padding(8, 9, 2, 3) });
            if (!(control is Button)) control.Margin = new Padding(3, 5, 6, 3);
            StyleInput(control);
            toolbar.Controls.Add(control);
            return control;
        }

        public static Label MakeTitle(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 42,
                Font = TitleFont,
                ForeColor = PrimaryDark,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };
        }

        /// <summary>Tiêu đề thẻ: chữ đậm màu chủ đạo + đường kẻ mảnh phía dưới</summary>
        public static Label MakeCardHeader(string title, string glyph = null)
        {
            var lbl = new Label
            {
                Text = (glyph != null ? "      " : "") + title,
                Dock = DockStyle.Top,
                Height = 38,
                Font = CardTitleFont,
                ForeColor = PrimaryDark,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.White
            };
            lbl.Paint += (s, e) =>
            {
                if (glyph != null)
                    TextRenderer.DrawText(e.Graphics, glyph, IconFont(12F), new Rectangle(0, 0, 24, lbl.Height - 6), Primary,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);
                using (var p = new Pen(Line)) e.Graphics.DrawLine(p, 0, lbl.Height - 1, lbl.Width, lbl.Height - 1);
            };
            return lbl;
        }

        /// <summary>Thẻ có tiêu đề, nội dung lấp đầy phần còn lại</summary>
        public static CardPanel MakeCard(string title, Control content, string glyph = null)
        {
            var card = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 14) };
            content.Dock = DockStyle.Fill;
            var gap = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.White };
            card.Controls.Add(content);
            card.Controls.Add(gap);
            card.Controls.Add(MakeCardHeader(title, glyph));
            return card;
        }

        /// <summary>Khung nhập liệu bên phải: thẻ bo góc chứa bảng trường (2 cột) và hàng nút</summary>
        public static Panel MakeEditor(string title, out TableLayoutPanel fields, out FlowLayoutPanel buttons, int width = 410)
        {
            var panel = new Panel { Dock = DockStyle.Right, Width = width, Padding = new Padding(12, 10, 8, 8), BackColor = Color.White };
            var card = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(16, 6, 12, 12), AutoScroll = true };
            fields = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                Font = BaseFont,
                ForeColor = TextColor,
                BackColor = Color.White,
                Padding = new Padding(0, 8, 0, 0)
            };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 14, 0, 0), Font = BaseFont, BackColor = Color.White };
            card.Controls.Add(buttons);
            card.Controls.Add(fields);                  // thêm sau => dock trước => nằm trên
            card.Controls.Add(MakeCardHeader(title));   // tiêu đề trên cùng
            panel.Controls.Add(card);
            return panel;
        }

        /// <summary>Thêm 1 dòng "Nhãn | Control" vào bảng trường, trả về chính control</summary>
        public static T AddField<T>(TableLayoutPanel table, string label, T control) where T : Control
        {
            int row = table.RowCount;
            table.RowCount = row + 1;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var lbl = new Label { Text = label, AutoSize = true, ForeColor = Muted, Anchor = AnchorStyles.Left | AnchorStyles.Top, Margin = new Padding(3, 9, 3, 3) };
            control.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            control.Margin = new Padding(3, 5, 3, 3);
            StyleInput(control);
            table.Controls.Add(lbl, 0, row);
            table.Controls.Add(control, 1, row);
            return control;
        }

        public static TextBox MakeMultiline(int height = 70)
        {
            return new TextBox { Multiline = true, Height = height, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.FixedSingle };
        }

        public static NumericUpDown MakeNumber(decimal min, decimal max, decimal increment, bool thousands = true)
        {
            return new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Increment = increment,
                ThousandsSeparator = thousands,
                TextAlign = HorizontalAlignment.Right,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        public static DateTimePicker MakeDate(DateTime value, string format = "dd/MM/yyyy", int width = 125)
        {
            return new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = format, Value = value, Width = width };
        }

        public static ComboBox MakeCombo(int width = 170)
        {
            return new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = width, FlatStyle = FlatStyle.Flat };
        }

        /// <summary>Gọi action sau khi người dùng ngừng gõ 350 ms</summary>
        public static void OnTextChangedDebounced(TextBox textBox, Action action)
        {
            var timer = new System.Windows.Forms.Timer { Interval = 350 };
            timer.Tick += (s, e) => { timer.Stop(); action(); };
            textBox.TextChanged += (s, e) => { timer.Stop(); timer.Start(); };
            textBox.Disposed += (s, e) => timer.Dispose();
        }

        /// <summary>Tab phẳng: tab đang chọn chữ đậm màu chủ đạo, gạch chân màu chủ đạo</summary>
        public static void StyleTabs(TabControl tabs)
        {
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabs.SizeMode = TabSizeMode.Fixed;
            tabs.ItemSize = new Size(210, 38);
            tabs.DrawItem += (s, e) =>
            {
                bool sel = e.Index == tabs.SelectedIndex;
                Rectangle r = tabs.GetTabRect(e.Index);
                e.Graphics.FillRectangle(Brushes.White, r);
                using (var f = new Font("Segoe UI", 10F, sel ? FontStyle.Bold : FontStyle.Regular))
                    TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, f, r, sel ? Primary : Muted,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                if (sel)
                    using (var b = new SolidBrush(Primary)) e.Graphics.FillRectangle(b, r.X + 12, r.Bottom - 3, r.Width - 24, 3);
            };
        }

        /// <summary>
        /// Thẻ số liệu: tiêu đề nhỏ, con số lớn, ô tròn icon màu bên phải, dòng phụ (tùy chọn).
        /// Trả về thẻ; value là nhãn chứa con số để cập nhật sau.
        /// </summary>
        public static CardPanel MakeStatCard(string title, string glyph, Color color, out Label value, string footer = null)
        {
            var card = new CardPanel { Size = new Size(260, 100), Padding = new Padding(16, 12, 12, 10), Margin = new Padding(0, 0, 14, 0) };
            var badge = new IconBadge { Glyph = glyph, Color = color, Dock = DockStyle.Right, Width = 46 };
            var right = new Panel { Dock = DockStyle.Right, Width = 50, BackColor = Color.White };
            right.Controls.Add(badge);
            badge.Dock = DockStyle.Top;
            badge.Height = 46;
            var lblTitle = new Label { Text = title, Dock = DockStyle.Top, Height = 22, ForeColor = Muted, Font = SmallFont, BackColor = Color.White, AutoEllipsis = true };
            value = new Label { Text = "0", Dock = DockStyle.Top, Height = 40, ForeColor = TextColor, Font = BigNumberFont, BackColor = Color.White, AutoEllipsis = true };
            card.Controls.Add(right);
            if (footer != null)
                card.Controls.Add(new Label { Text = footer, Dock = DockStyle.Top, Height = 20, ForeColor = color, Font = SmallFont, BackColor = Color.White });
            card.Controls.Add(value);
            card.Controls.Add(lblTitle);
            // thứ tự dock: tiêu đề trên cùng, rồi con số, rồi dòng phụ
            lblTitle.SendToBack();
            right.SendToBack();
            return card;
        }

        // ------------------------------------------------------------------ DataGridView
        public static DataGridView MakeGrid()
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 40,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                GridColor = Line
            };
            g.ColumnHeadersDefaultCellStyle.BackColor = PrimarySoft;
            g.ColumnHeadersDefaultCellStyle.ForeColor = PrimaryDark;
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.75F);
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = PrimarySoft;
            g.ColumnHeadersDefaultCellStyle.SelectionForeColor = PrimaryDark;
            g.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            g.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
            g.DefaultCellStyle.ForeColor = TextColor;
            g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(214, 226, 240);
            g.DefaultCellStyle.SelectionForeColor = TextColor;
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 252);
            g.RowTemplate.Height = 34;
            g.DataBindingComplete += (s, e) => ApplyColumns(g);
            g.CellPainting += PaintStatusBadge;
            return g;
        }

        /// <summary>Cột trạng thái đơn hàng / tình trạng kho hiển thị dạng nhãn (badge) màu</summary>
        private static void PaintStatusBadge(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var grid = (DataGridView)sender;
            string prop = grid.Columns[e.ColumnIndex].DataPropertyName;
            if (prop != "TrangThai" && prop != "TinhTrangKho") return;
            string text = e.Value as string;
            if (string.IsNullOrEmpty(text)) return;
            Color color = prop == "TinhTrangKho" ? StockColor(text) : StatusColor(text);
            if (color == Color.Black) return;

            e.PaintBackground(e.CellBounds, (e.State & DataGridViewElementStates.Selected) != 0);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Size ts = TextRenderer.MeasureText(g, text, SmallFont);
            int w = Math.Min(ts.Width + 16, e.CellBounds.Width - 8);
            var pill = new Rectangle(e.CellBounds.X + 6, e.CellBounds.Y + (e.CellBounds.Height - 22) / 2, w, 22);
            using (GraphicsPath path = RoundedRect(pill, 11))
            using (var b = new SolidBrush(Blend(color, Color.White, 0.86)))
                g.FillPath(b, path);
            TextRenderer.DrawText(g, text, SmallFont, pill, color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            e.Handled = true;
        }

        /// <summary>
        /// Gán dữ liệu cho lưới. Mỗi spec dạng "TenThuocTinh|Tiêu đề|Định dạng|Độ rộng tương đối".
        /// Cột không có trong spec sẽ bị ẩn.
        /// </summary>
        public static void Bind(DataGridView grid, object data, params string[] spec)
        {
            grid.Tag = spec;
            grid.DataSource = null;
            grid.DataSource = data;
            ApplyColumns(grid);
        }

        private static void ApplyColumns(DataGridView grid)
        {
            var spec = grid.Tag as string[];
            if (spec == null) return;
            foreach (DataGridViewColumn c in grid.Columns) c.Visible = false;
            int order = 0;
            foreach (string s in spec)
            {
                string[] p = s.Split('|');
                if (!grid.Columns.Contains(p[0])) continue;
                DataGridViewColumn col = grid.Columns[p[0]];
                col.Visible = true;
                col.HeaderText = p.Length > 1 ? p[1] : p[0];
                col.DisplayIndex = order++;
                if (p.Length > 2 && p[2] != "")
                {
                    col.DefaultCellStyle.Format = p[2];
                    if (p[2].StartsWith("N")) col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (p.Length > 3 && p[3] != "")
                    col.FillWeight = float.Parse(p[3], CultureInfo.InvariantCulture);
            }
        }

        public static T Selected<T>(DataGridView grid) where T : class
        {
            if (grid.CurrentRow == null || grid.SelectedRows.Count == 0) return null;
            return grid.CurrentRow.DataBoundItem as T;
        }

        public static Color StatusColor(string trangThai)
        {
            switch (trangThai)
            {
                case TrangThaiDonHang.ChoXacNhan: return Orange;
                case TrangThaiDonHang.DaXacNhan: return Blue;
                case TrangThaiDonHang.DangGiao: return Purple;
                case TrangThaiDonHang.DaGiao: return Success;
                case TrangThaiDonHang.GiaoThatBai:
                case TrangThaiDonHang.DaHuy: return Danger;
                default: return Color.Black;
            }
        }

        public static Color StockColor(string tinhTrang)
        {
            switch (tinhTrang)
            {
                case "Hết hàng": return Danger;
                case "Sắp hết hàng": return Orange;
                case "Còn hàng": return Success;
                default: return Color.Black;
            }
        }

        // ------------------------------------------------------------------ thông báo
        public static void Info(IWin32Window owner, string message)
        {
            MessageBox.Show(owner, message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void Warn(IWin32Window owner, string message)
        {
            MessageBox.Show(owner, message, "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static bool Confirm(IWin32Window owner, string message)
        {
            return MessageBox.Show(owner, message, "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        /// <summary>Chạy action, bắt lỗi nghiệp vụ / CSDL và hiển thị thông báo phù hợp</summary>
        /// <summary>
        /// Như Try, nhưng lỗi nghiệp vụ gắn với một ô nhập (BusinessException.Field) được báo ngay cạnh ô đó
        /// bằng ErrorProvider và đưa con trỏ về ô đó (báo cáo mục 2.8.1, UC 1.2.3 - 3a).
        /// fields: ánh xạ tên thuộc tính DTO → control trên form.
        /// </summary>
        public static bool TrySave(IWin32Window owner, ErrorProvider ep, IDictionary<string, Control> fields, Action action)
        {
            foreach (Control c in fields.Values) ep.SetError(c, "");
            try
            {
                action();
                return true;
            }
            catch (BusinessException ex) when (ex.Field != null && fields.ContainsKey(ex.Field))
            {
                Control c = fields[ex.Field];
                ep.SetError(c, ex.Message);
                c.Focus();
                Warn(owner, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                // Các lỗi còn lại xử lý như Try (MessageBox)
                return Try(owner, () => { throw ex; });
            }
        }

        /// <summary>Tạo ErrorProvider dùng chung cho form (biểu tượng lỗi nhấp nháy cạnh ô nhập).</summary>
        public static ErrorProvider MakeErrorProvider(ContainerControl form)
        {
            return new ErrorProvider(form) { BlinkStyle = ErrorBlinkStyle.NeverBlink };
        }

        public static bool Try(IWin32Window owner, Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (BusinessException ex)
            {
                Warn(owner, ex.Message);
            }
            catch (SqlException ex)
            {
                MessageBox.Show(owner, "Lỗi cơ sở dữ liệu:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, "Đã xảy ra lỗi:\n" + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return false;
        }
    }

    /// <summary>Hộp thoại nhập một chuỗi (lý do hủy đơn...)</summary>
    public static class InputDialog
    {
        public static string Show(IWin32Window owner, string title, string prompt, string defaultValue = "", bool multiline = false)
        {
            using (var f = new Form())
            {
                f.Text = title;
                f.Font = UiHelper.BaseFont;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.StartPosition = FormStartPosition.CenterParent;
                f.MinimizeBox = false;
                f.MaximizeBox = false;
                f.ShowInTaskbar = false;
                f.ClientSize = new Size(440, multiline ? 240 : 160);

                var lbl = new Label { Text = prompt, Left = 12, Top = 12, Width = 416, Height = 40 };
                var txt = new TextBox
                {
                    Left = 12,
                    Top = 56,
                    Width = 416,
                    Text = defaultValue,
                    Multiline = multiline,
                    Height = multiline ? 110 : 27,
                    ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None
                };
                var ok = UiHelper.MakeButton("Đồng ý", UiHelper.Primary, 100);
                ok.Left = 220;
                ok.Top = f.ClientSize.Height - 46;
                ok.DialogResult = DialogResult.OK;
                var cancel = UiHelper.MakeButton("Hủy bỏ", UiHelper.Gray, 100);
                cancel.Left = 328;
                cancel.Top = f.ClientSize.Height - 46;
                cancel.DialogResult = DialogResult.Cancel;

                f.Controls.Add(lbl);
                f.Controls.Add(txt);
                f.Controls.Add(ok);
                f.Controls.Add(cancel);
                if (!multiline) f.AcceptButton = ok;
                f.CancelButton = cancel;
                return f.ShowDialog(owner) == DialogResult.OK ? txt.Text.Trim() : null;
            }
        }
    }
}
