using System;
using System.Drawing;
using System.Windows.Forms;
using SportZoneAdmin.BLL;
using SportZoneAdmin.DTO;

namespace SportZoneAdmin.GUI
{
    /// <summary>UC 1.2.5 - Quản lý tài khoản khách hàng: thêm, sửa, xóa mềm, khôi phục, tìm kiếm</summary>
    public class frmKhachHang : Form
    {
        private readonly KhachHangBLL bll = new KhachHangBLL();
        private readonly DataGridView grid;
        private readonly TextBox txtSearch;
        private readonly CheckBox chkDaXoa;
        private readonly Label lblMa;
        private readonly Label lblTrangThai;
        private readonly TextBox txtHoTen;
        private readonly TextBox txtTDN;
        private readonly TextBox txtMK;
        private readonly Label lblMKHint;
        private readonly TextBox txtSDT;
        private readonly TextBox txtEmail;
        private readonly TextBox txtDiaChi;
        private readonly Button btnDelete;
        private readonly Button btnRestore;
        private int currentId;
        private readonly Font strikeFont = new Font("Segoe UI", 10F, FontStyle.Strikeout);

        public frmKhachHang()
        {
            UiHelper.SetupChild(this, "Quản lý tài khoản khách hàng");

            FlowLayoutPanel toolbar = UiHelper.MakeToolbar();
            txtSearch = UiHelper.AddTool(toolbar, "Tìm (tên, mã, SĐT, email):", new TextBox { Width = 250 });
            chkDaXoa = UiHelper.AddTool(toolbar, null, new CheckBox { Text = "Hiện cả tài khoản đã xóa", AutoSize = true });
            Button btnSearch = UiHelper.AddTool(toolbar, null, UiHelper.MakeButton("Tìm kiếm", UiHelper.Primary, 100));
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Search(true); } };
            btnSearch.Click += (s, e) => Search(true);
            chkDaXoa.CheckedChanged += (s, e) => Search(false);

            TableLayoutPanel fields;
            FlowLayoutPanel buttons;
            Panel editor = UiHelper.MakeEditor("Thông tin tài khoản", out fields, out buttons, 430);
            lblMa = UiHelper.AddField(fields, "Mã khách hàng:", new Label { Text = "(mới)", AutoSize = true, Font = UiHelper.BoldFont });
            lblTrangThai = UiHelper.AddField(fields, "Trạng thái:", new Label { Text = "", AutoSize = true, Font = UiHelper.BoldFont });
            txtHoTen = UiHelper.AddField(fields, "Họ tên (*):", new TextBox { MaxLength = 100 });
            txtTDN = UiHelper.AddField(fields, "Tên đăng nhập (*):", new TextBox { MaxLength = 50 });
            txtMK = UiHelper.AddField(fields, "Mật khẩu:", new TextBox { UseSystemPasswordChar = true, MaxLength = 100 });
            lblMKHint = UiHelper.AddField(fields, "", new Label { AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 8.5F, FontStyle.Italic) });
            txtSDT = UiHelper.AddField(fields, "Số điện thoại:", new TextBox { MaxLength = 11 });
            txtEmail = UiHelper.AddField(fields, "Email (*):", new TextBox { MaxLength = 100 });
            txtDiaChi = UiHelper.AddField(fields, "Địa chỉ:", UiHelper.MakeMultiline(60));
            txtDiaChi.MaxLength = 255;

            Button btnNew = UiHelper.MakeButton("Thêm mới", UiHelper.Blue, 105);
            Button btnSave = UiHelper.MakeButton("Lưu", UiHelper.Primary, 80);
            btnDelete = UiHelper.MakeButton("Xóa", UiHelper.Danger, 80);
            btnRestore = UiHelper.MakeButton("Khôi phục", UiHelper.Orange, 105);
            buttons.Controls.AddRange(new Control[] { btnNew, btnSave, btnDelete, btnRestore });
            btnNew.Click += (s, e) => ClearEditor();
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;
            btnRestore.Click += BtnRestore_Click;

            grid = UiHelper.MakeGrid();
            grid.SelectionChanged += (s, e) => FillEditor(UiHelper.Selected<KhachHangDTO>(grid));
            grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var kh = grid.Rows[e.RowIndex].DataBoundItem as KhachHangDTO;
                if (kh != null && !kh.TrangThai)
                {
                    e.CellStyle.ForeColor = Color.Gray;
                    e.CellStyle.Font = strikeFont;
                }
            };

            Controls.Add(grid);
            Controls.Add(editor);
            Controls.Add(toolbar);
            grid.BringToFront();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ClearEditor();
            Search(false);
        }

        private void Search(bool showNotFound)
        {
            UiHelper.Try(this, () =>
            {
                UiHelper.Bind(grid, bll.Search(txtSearch.Text, chkDaXoa.Checked),
                    "MaKH|Mã||40", "HoTen|Họ tên||130", "TenDangNhap|Tên đăng nhập||100", "SDT|Số điện thoại||90",
                    "Email|Email||150", "DiaChi|Địa chỉ||180", "TrangThaiText|Trạng thái||70");
            });
            if (grid.Rows.Count == 0)
            {
                ClearEditor();
                if (showNotFound) UiHelper.Info(this, "Không tìm thấy khách hàng nào phù hợp.");
            }
        }

        private void FillEditor(KhachHangDTO kh)
        {
            if (kh == null) return;
            currentId = kh.MaKH;
            lblMa.Text = kh.MaKH.ToString();
            lblTrangThai.Text = kh.TrangThaiText;
            lblTrangThai.ForeColor = kh.TrangThai ? UiHelper.Primary : UiHelper.Danger;
            txtHoTen.Text = kh.HoTen;
            txtTDN.Text = kh.TenDangNhap;
            txtMK.Clear();
            lblMKHint.Text = "Để trống nếu không đổi mật khẩu.";
            txtSDT.Text = kh.SDT;
            txtEmail.Text = kh.Email;
            txtDiaChi.Text = kh.DiaChi;
            btnDelete.Enabled = kh.TrangThai;
            btnRestore.Enabled = !kh.TrangThai;
        }

        private void ClearEditor()
        {
            grid.ClearSelection();
            currentId = 0;
            lblMa.Text = "(mới)";
            lblTrangThai.Text = "";
            txtHoTen.Clear();
            txtTDN.Clear();
            txtMK.Clear();
            lblMKHint.Text = "Bắt buộc, tối thiểu 6 ký tự.";
            txtSDT.Clear();
            txtEmail.Clear();
            txtDiaChi.Clear();
            btnDelete.Enabled = false;
            btnRestore.Enabled = false;
            txtHoTen.Focus();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var kh = new KhachHangDTO
            {
                MaKH = currentId,
                HoTen = txtHoTen.Text,
                TenDangNhap = txtTDN.Text,
                SDT = txtSDT.Text,
                Email = txtEmail.Text,
                DiaChi = txtDiaChi.Text,
                TrangThai = true
            };
            bool isNew = currentId == 0;
            int id = 0;
            if (!UiHelper.Try(this, () => id = bll.Save(kh, txtMK.Text))) return;
            UiHelper.Info(this, isNew ? "Tạo tài khoản khách hàng thành công." : "Cập nhật thông tin khách hàng thành công.");
            Search(false);
            SelectRow(id);
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (currentId == 0) { UiHelper.Warn(this, "Hãy chọn tài khoản cần xóa."); return; }
            if (!UiHelper.Confirm(this, "Xóa tài khoản \"" + txtTDN.Text + "\"?\n(Xóa mềm: lịch sử đơn hàng vẫn được giữ lại)")) return;
            int id = currentId;
            if (!UiHelper.Try(this, () => bll.Delete(id))) return;
            UiHelper.Info(this, "Đã xóa tài khoản khách hàng.");
            Search(false);
            SelectRow(id);
        }

        private void BtnRestore_Click(object sender, EventArgs e)
        {
            if (currentId == 0) return;
            int id = currentId;
            if (!UiHelper.Try(this, () => bll.Restore(id))) return;
            UiHelper.Info(this, "Đã khôi phục tài khoản.");
            Search(false);
            SelectRow(id);
        }

        private void SelectRow(int id)
        {
            foreach (DataGridViewRow r in grid.Rows)
            {
                var kh = r.DataBoundItem as KhachHangDTO;
                if (kh != null && kh.MaKH == id)
                {
                    r.Selected = true;
                    grid.CurrentCell = r.Cells[grid.Columns["HoTen"].Index];
                    return;
                }
            }
        }
    }
}
