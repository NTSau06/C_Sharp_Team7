using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SportShopAdmin.BLL;
using SportShopAdmin.DTO;

namespace SportShopAdmin.GUI
{
    /// <summary>UC 1.2.13 - Quản lý mã giảm giá: tạo mới, cập nhật, xóa / vô hiệu hóa, theo dõi lượt dùng</summary>
    public class frmMaGiamGia : Form
    {
        private readonly MaGiamGiaBLL bll = new MaGiamGiaBLL();
        private readonly DataGridView grid;
        private readonly TextBox txtSearch;
        private readonly Label lblMa;
        private readonly TextBox txtCode;
        private readonly NumericUpDown numPT;
        private readonly NumericUpDown numMax;
        private readonly NumericUpDown numMin;
        private readonly DateTimePicker dtBD;
        private readonly DateTimePicker dtKT;
        private readonly NumericUpDown numLuot;
        private readonly Label lblDaDung;
        private readonly CheckBox chkKichHoat;
        private int currentId;

        private readonly ErrorProvider ep;
        private readonly Dictionary<string, Control> errFields;

        public frmMaGiamGia()
        {
            UiHelper.SetupChild(this, "Quản lý mã giảm giá");

            FlowLayoutPanel toolbar = UiHelper.MakeToolbar();
            txtSearch = UiHelper.AddTool(toolbar, "Tìm mã:", new TextBox { Width = 220, CharacterCasing = CharacterCasing.Upper });
            Button btnRefresh = UiHelper.AddTool(toolbar, null, UiHelper.MakeButton("Làm mới", UiHelper.Gray, 100));
            UiHelper.OnTextChangedDebounced(txtSearch, LoadData);
            btnRefresh.Click += (s, e) => LoadData();

            TableLayoutPanel fields;
            FlowLayoutPanel buttons;
            Panel editor = UiHelper.MakeEditor("Thông tin mã giảm giá", out fields, out buttons, 440);
            lblMa = UiHelper.AddField(fields, "Mã:", new Label { Text = "(mới)", AutoSize = true, Font = UiHelper.BoldFont });
            txtCode = UiHelper.AddField(fields, "Code (*):", new TextBox { MaxLength = 30, CharacterCasing = CharacterCasing.Upper });
            numPT = UiHelper.AddField(fields, "% giảm (*):", UiHelper.MakeNumber(1, 100, 1, false));
            numMax = UiHelper.AddField(fields, "Giảm tối đa (đ):", UiHelper.MakeNumber(0, 1000000000, 10000));
            numMin = UiHelper.AddField(fields, "Đơn tối thiểu (đ):", UiHelper.MakeNumber(0, 1000000000, 10000));
            dtBD = UiHelper.AddField(fields, "Ngày bắt đầu:", UiHelper.MakeDate(DateTime.Today, "dd/MM/yyyy HH:mm"));
            dtKT = UiHelper.AddField(fields, "Ngày kết thúc:", UiHelper.MakeDate(DateTime.Today.AddDays(30).AddMinutes(-1), "dd/MM/yyyy HH:mm"));
            numLuot = UiHelper.AddField(fields, "Số lượt tối đa:", UiHelper.MakeNumber(1, 1000000, 1));
            lblDaDung = UiHelper.AddField(fields, "Số lượt đã dùng:", new Label { Text = "0", AutoSize = true, Font = UiHelper.BoldFont });
            chkKichHoat = UiHelper.AddField(fields, "Trạng thái:", new CheckBox { Text = "Kích hoạt", AutoSize = true, Checked = true });
            UiHelper.AddField(fields, "", new Label
            {
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                Text = "Số tiền giảm = min(tạm tính × % giảm, giảm tối đa).\nMã đã dùng trong đơn hàng chỉ có thể vô hiệu hóa."
            });

            ep = UiHelper.MakeErrorProvider(this);
            errFields = new Dictionary<string, Control>
            {
                { "Code", txtCode }, { "PhanTramGiam", numPT }, { "SoTienGiamToiDa", numMax }, { "DonToiThieu", numMin },
                { "NgayKetThuc", dtKT }, { "SoLuotToiDa", numLuot }
            };

            Button btnNew = UiHelper.MakeButton("Thêm mới", UiHelper.Blue, 110);
            Button btnSave = UiHelper.MakeButton("Lưu", UiHelper.Primary, 90);
            Button btnDelete = UiHelper.MakeButton("Xóa / Vô hiệu hóa", UiHelper.Danger, 170);
            buttons.Controls.AddRange(new Control[] { btnNew, btnSave, btnDelete });
            btnNew.Click += (s, e) => ClearEditor();
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;

            grid = UiHelper.MakeGrid();
            grid.SelectionChanged += (s, e) => FillEditor(UiHelper.Selected<MaGiamGiaDTO>(grid));
            grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || grid.Columns[e.ColumnIndex].Name != "TinhTrang") return;
                var m = grid.Rows[e.RowIndex].DataBoundItem as MaGiamGiaDTO;
                if (m == null) return;
                e.CellStyle.ForeColor = m.DangHieuLuc ? UiHelper.Primary : (m.TrangThai ? UiHelper.Orange : UiHelper.Danger);
                e.CellStyle.Font = UiHelper.BoldFont;
            };

            Controls.Add(grid);
            Controls.Add(editor);
            Controls.Add(toolbar);
            grid.BringToFront();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadData();
        }

        private void LoadData()
        {
            UiHelper.Try(this, () =>
            {
                UiHelper.Bind(grid, bll.GetAll(txtSearch.Text),
                    "MaMGG|Mã||35", "Code|Code||80", "PhanTramGiam|% giảm|N0|50", "SoTienGiamToiDa|Giảm tối đa|N0|75",
                    "DonToiThieu|Đơn tối thiểu|N0|75", "NgayBatDau|Bắt đầu|dd/MM/yyyy|70", "NgayKetThuc|Kết thúc|dd/MM/yyyy|70",
                    "SoLuotDaDung|Đã dùng|N0|50", "SoLuotToiDa|Tối đa|N0|50", "TinhTrang|Tình trạng||80");
            });
            if (grid.Rows.Count == 0) ClearEditor();
        }

        private static DateTime ClampDate(DateTimePicker p, DateTime d)
        {
            if (d < p.MinDate) return p.MinDate;
            if (d > p.MaxDate) return p.MaxDate;
            return d;
        }

        private void FillEditor(MaGiamGiaDTO m)
        {
            if (m == null) return;
            currentId = m.MaMGG;
            lblMa.Text = m.MaMGG.ToString();
            txtCode.Text = m.Code;
            numPT.Value = Math.Max(1, Math.Min(100, m.PhanTramGiam));
            numMax.Value = Math.Min(numMax.Maximum, m.SoTienGiamToiDa);
            numMin.Value = Math.Min(numMin.Maximum, m.DonToiThieu);
            dtBD.Value = ClampDate(dtBD, m.NgayBatDau);
            dtKT.Value = ClampDate(dtKT, m.NgayKetThuc);
            numLuot.Value = Math.Max(1, Math.Min(numLuot.Maximum, m.SoLuotToiDa));
            lblDaDung.Text = m.SoLuotDaDung.ToString("N0");
            chkKichHoat.Checked = m.TrangThai;
        }

        private void ClearEditor()
        {
            grid.ClearSelection();
            currentId = 0;
            lblMa.Text = "(mới)";
            txtCode.Clear();
            numPT.Value = 10;
            numMax.Value = 100000;
            numMin.Value = 0;
            dtBD.Value = DateTime.Today;
            dtKT.Value = DateTime.Today.AddDays(30).AddMinutes(-1);
            numLuot.Value = 100;
            lblDaDung.Text = "0";
            chkKichHoat.Checked = true;
            txtCode.Focus();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var m = new MaGiamGiaDTO
            {
                MaMGG = currentId,
                Code = txtCode.Text,
                PhanTramGiam = (int)numPT.Value,
                SoTienGiamToiDa = numMax.Value,
                DonToiThieu = numMin.Value,
                NgayBatDau = dtBD.Value,
                NgayKetThuc = dtKT.Value,
                SoLuotToiDa = (int)numLuot.Value,
                TrangThai = chkKichHoat.Checked
            };
            bool isNew = currentId == 0;
            int id = 0;
            if (!UiHelper.TrySave(this, ep, errFields, () => id = bll.Save(m))) return;
            UiHelper.Info(this, isNew ? "Đã tạo mã giảm giá mới." : "Đã cập nhật mã giảm giá.");
            LoadData();
            SelectRow(id);
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (currentId == 0) { UiHelper.Warn(this, "Hãy chọn mã giảm giá cần xóa."); return; }
            if (!UiHelper.Confirm(this, "Xóa mã \"" + txtCode.Text + "\"?")) return;
            int id = currentId;
            string message = null;
            if (!UiHelper.Try(this, () => message = bll.Delete(id))) return;
            UiHelper.Info(this, message);
            LoadData();
            SelectRow(id);
        }

        private void SelectRow(int id)
        {
            foreach (DataGridViewRow r in grid.Rows)
            {
                var m = r.DataBoundItem as MaGiamGiaDTO;
                if (m != null && m.MaMGG == id)
                {
                    r.Selected = true;
                    grid.CurrentCell = r.Cells[grid.Columns["Code"].Index];
                    return;
                }
            }
        }
    }
}
