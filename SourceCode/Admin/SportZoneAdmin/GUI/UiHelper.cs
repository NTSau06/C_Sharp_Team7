using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using SportZoneAdmin.BLL;
using SportZoneAdmin.DTO;

namespace SportZoneAdmin.GUI
{
    /// <summary>Hàm dựng giao diện và thông báo dùng chung cho các Form</summary>
    public static class UiHelper
    {
        public static readonly Color Primary = Color.FromArgb(0, 122, 89);
        public static readonly Color Blue = Color.FromArgb(13, 110, 253);
        public static readonly Color Danger = Color.FromArgb(220, 53, 69);
        public static readonly Color Gray = Color.FromArgb(108, 117, 125);
        public static readonly Color Orange = Color.FromArgb(232, 115, 12);
        public static readonly Color Purple = Color.FromArgb(111, 66, 193);
        public static readonly Color WarningBack = Color.FromArgb(255, 222, 222);
        public static readonly Color PageBack = Color.FromArgb(240, 242, 245);
        public static readonly Color MenuBack = Color.FromArgb(30, 41, 59);

        public static readonly Font BaseFont = new Font("Segoe UI", 10F);
        public static readonly Font BoldFont = new Font("Segoe UI", 10F, FontStyle.Bold);
        public static readonly Font TitleFont = new Font("Segoe UI", 15F, FontStyle.Bold);
        public static readonly Font BigNumberFont = new Font("Segoe UI", 18F, FontStyle.Bold);

        // ------------------------------------------------------------------ dựng control
        public static void SetupChild(Form f, string title)
        {
            f.Text = title;
            f.Font = BaseFont;
            f.BackColor = Color.White;
            f.FormBorderStyle = FormBorderStyle.None;
        }

        public static Button MakeButton(string text, Color back, int width = 120)
        {
            var b = new Button
            {
                Text = text,
                Width = width,
                Height = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = back,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = BoldFont,
                Margin = new Padding(3, 3, 6, 3),
                UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        public static FlowLayoutPanel MakeToolbar()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(6, 7, 6, 3),
                WrapContents = false,
                BackColor = Color.White
            };
        }

        /// <summary>Thêm (nhãn +) control vào thanh công cụ</summary>
        public static T AddTool<T>(FlowLayoutPanel toolbar, string label, T control) where T : Control
        {
            if (!string.IsNullOrEmpty(label))
                toolbar.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(8, 9, 2, 3) });
            if (!(control is Button)) control.Margin = new Padding(3, 5, 6, 3);
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
                ForeColor = Primary,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };
        }

        /// <summary>Khung nhập liệu bên phải: GroupBox chứa bảng trường (2 cột) và hàng nút</summary>
        public static Panel MakeEditor(string title, out TableLayoutPanel fields, out FlowLayoutPanel buttons, int width = 410)
        {
            var panel = new Panel { Dock = DockStyle.Right, Width = width, Padding = new Padding(10, 4, 6, 6), BackColor = Color.White };
            var group = new GroupBox { Text = title, Dock = DockStyle.Fill, Font = BoldFont, ForeColor = Primary, Padding = new Padding(8) };
            fields = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                Font = BaseFont,
                ForeColor = Color.Black
            };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 12, 0, 0), Font = BaseFont };
            group.Controls.Add(buttons);
            group.Controls.Add(fields); // thêm sau => dock trước => nằm trên cùng
            panel.Controls.Add(group);
            return panel;
        }

        /// <summary>Thêm 1 dòng "Nhãn | Control" vào bảng trường, trả về chính control</summary>
        public static T AddField<T>(TableLayoutPanel table, string label, T control) where T : Control
        {
            int row = table.RowCount;
            table.RowCount = row + 1;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var lbl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top, Margin = new Padding(3, 9, 3, 3) };
            control.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            control.Margin = new Padding(3, 5, 3, 3);
            table.Controls.Add(lbl, 0, row);
            table.Controls.Add(control, 1, row);
            return control;
        }

        public static TextBox MakeMultiline(int height = 70)
        {
            return new TextBox { Multiline = true, Height = height, ScrollBars = ScrollBars.Vertical };
        }

        public static NumericUpDown MakeNumber(decimal min, decimal max, decimal increment, bool thousands = true)
        {
            return new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Increment = increment,
                ThousandsSeparator = thousands,
                TextAlign = HorizontalAlignment.Right
            };
        }

        public static DateTimePicker MakeDate(DateTime value, string format = "dd/MM/yyyy", int width = 125)
        {
            return new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = format, Value = value, Width = width };
        }

        public static ComboBox MakeCombo(int width = 170)
        {
            return new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = width };
        }

        /// <summary>Gọi action sau khi người dùng ngừng gõ 350 ms</summary>
        public static void OnTextChangedDebounced(TextBox textBox, Action action)
        {
            var timer = new System.Windows.Forms.Timer { Interval = 350 };
            timer.Tick += (s, e) => { timer.Stop(); action(); };
            textBox.TextChanged += (s, e) => { timer.Stop(); timer.Start(); };
            textBox.Disposed += (s, e) => timer.Dispose();
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
                BorderStyle = BorderStyle.FixedSingle,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 36,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                GridColor = Color.Gainsboro
            };
            g.ColumnHeadersDefaultCellStyle.BackColor = Primary;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font = BoldFont;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Primary;
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(246, 249, 248);
            g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(178, 223, 206);
            g.DefaultCellStyle.SelectionForeColor = Color.Black;
            g.RowTemplate.Height = 30;
            g.DataBindingComplete += (s, e) => ApplyColumns(g);
            return g;
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
                case TrangThaiDonHang.DaGiao: return Primary;
                case TrangThaiDonHang.GiaoThatBai:
                case TrangThaiDonHang.DaHuy: return Danger;
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
