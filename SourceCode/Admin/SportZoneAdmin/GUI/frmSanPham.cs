using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SportZoneAdmin.BLL;
using SportZoneAdmin.DTO;
using SportZoneAdmin.Utils;

namespace SportZoneAdmin.GUI
{
    /// <summary>UC 1.2.3 - Quản lý sản phẩm: thêm, sửa, xóa, tìm kiếm, lọc theo danh mục</summary>
    public class frmSanPham : Form
    {
        private readonly SanPhamBLL bll = new SanPhamBLL();
        private readonly DanhMucBLL dmBll = new DanhMucBLL();

        private readonly DataGridView grid;
        private readonly TextBox txtSearch;
        private readonly ComboBox cboLocDM;
        private readonly Label lblMa;
        private readonly TextBox txtTen;
        private readonly ComboBox cboDM;
        private readonly NumericUpDown numGia;
        private readonly NumericUpDown numTon;
        private readonly NumericUpDown numMin;
        private readonly TextBox txtMoTa;
        private readonly PictureBox pic;
        private readonly Label lblTonHint;
        private int currentId;
        private string currentImage;
        private bool loading;

        public frmSanPham()
        {
            UiHelper.SetupChild(this, "Quản lý sản phẩm");

            FlowLayoutPanel toolbar = UiHelper.MakeToolbar();
            txtSearch = UiHelper.AddTool(toolbar, "Tìm sản phẩm:", new TextBox { Width = 230 });
            cboLocDM = UiHelper.AddTool(toolbar, "Danh mục:", UiHelper.MakeCombo(200));
            Button btnRefresh = UiHelper.AddTool(toolbar, null, UiHelper.MakeButton("Làm mới", UiHelper.Gray, 100));
            UiHelper.OnTextChangedDebounced(txtSearch, LoadData);
            cboLocDM.SelectedIndexChanged += (s, e) => { if (!loading) LoadData(); };
            btnRefresh.Click += (s, e) => { LoadDanhMuc(); LoadData(); };

            TableLayoutPanel fields;
            FlowLayoutPanel buttons;
            Panel editor = UiHelper.MakeEditor("Thông tin sản phẩm", out fields, out buttons, 430);
            lblMa = UiHelper.AddField(fields, "Mã sản phẩm:", new Label { Text = "(mới)", AutoSize = true, Font = UiHelper.BoldFont });
            txtTen = UiHelper.AddField(fields, "Tên sản phẩm (*):", new TextBox { MaxLength = 200 });
            cboDM = UiHelper.AddField(fields, "Danh mục (*):", UiHelper.MakeCombo());
            numGia = UiHelper.AddField(fields, "Giá bán (đ) (*):", UiHelper.MakeNumber(0, 1000000000, 10000));
            numTon = UiHelper.AddField(fields, "Số lượng tồn:", UiHelper.MakeNumber(0, 1000000, 1));
            lblTonHint = UiHelper.AddField(fields, "", new Label
            {
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                Text = "Sau khi tạo, tồn kho chỉ thay đổi qua Nhập kho\nhoặc khi Xác nhận đơn hàng."
            });
            numMin = UiHelper.AddField(fields, "Mức tồn tối thiểu:", UiHelper.MakeNumber(0, 100000, 1));
            txtMoTa = UiHelper.AddField(fields, "Mô tả:", UiHelper.MakeMultiline(70));

            pic = UiHelper.AddField(fields, "Hình ảnh:", new PictureBox
            {
                Height = 140,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.WhiteSmoke
            });
            var imgButtons = UiHelper.AddField(fields, "", new FlowLayoutPanel { AutoSize = true, WrapContents = false });
            Button btnChonAnh = UiHelper.MakeButton("Chọn ảnh...", UiHelper.Gray, 110);
            Button btnXoaAnh = UiHelper.MakeButton("Bỏ ảnh", UiHelper.Gray, 90);
            imgButtons.Controls.Add(btnChonAnh);
            imgButtons.Controls.Add(btnXoaAnh);
            btnChonAnh.Click += BtnChonAnh_Click;
            btnXoaAnh.Click += (s, e) => SetImage(null);

            Button btnNew = UiHelper.MakeButton("Thêm mới", UiHelper.Blue, 110);
            Button btnSave = UiHelper.MakeButton("Lưu", UiHelper.Primary, 90);
            Button btnDelete = UiHelper.MakeButton("Xóa", UiHelper.Danger, 90);
            buttons.Controls.AddRange(new Control[] { btnNew, btnSave, btnDelete });
            btnNew.Click += (s, e) => ClearEditor();
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;

            grid = UiHelper.MakeGrid();
            grid.SelectionChanged += (s, e) => FillEditor(UiHelper.Selected<SanPhamDTO>(grid));
            grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var sp = grid.Rows[e.RowIndex].DataBoundItem as SanPhamDTO;
                if (sp != null && sp.SapHetHang())
                {
                    e.CellStyle.BackColor = UiHelper.WarningBack;
                    e.CellStyle.ForeColor = Color.DarkRed;
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
            LoadDanhMuc();
            LoadData();
        }

        private void LoadDanhMuc()
        {
            UiHelper.Try(this, () =>
            {
                loading = true;
                List<DanhMucDTO> ds = dmBll.GetAll();
                var loc = new List<DanhMucDTO> { new DanhMucDTO { MaDM = 0, TenDM = "-- Tất cả danh mục --" } };
                loc.AddRange(ds);
                object oldFilter = cboLocDM.SelectedValue;
                cboLocDM.DisplayMember = "TenDM";
                cboLocDM.ValueMember = "MaDM";
                cboLocDM.DataSource = loc;
                if (oldFilter != null) cboLocDM.SelectedValue = oldFilter;

                cboDM.DisplayMember = "TenDM";
                cboDM.ValueMember = "MaDM";
                cboDM.DataSource = new List<DanhMucDTO>(ds);
            });
            loading = false;
        }

        private void LoadData()
        {
            int? maDM = null;
            if (cboLocDM.SelectedValue is int && (int)cboLocDM.SelectedValue > 0) maDM = (int)cboLocDM.SelectedValue;
            UiHelper.Try(this, () =>
            {
                UiHelper.Bind(grid, bll.GetAll(txtSearch.Text, maDM),
                    "MaSP|Mã||40", "TenSP|Tên sản phẩm||200", "TenDM|Danh mục||100", "GiaBan|Giá bán|N0|80",
                    "SoLuongTon|Tồn kho|N0|60", "MucTonToiThieu|Tồn tối thiểu|N0|70", "TinhTrangKho|Tình trạng||80");
            });
            if (grid.Rows.Count == 0) ClearEditor();
        }

        private void FillEditor(SanPhamDTO sp)
        {
            if (sp == null) return;
            currentId = sp.MaSP;
            lblMa.Text = sp.MaSP.ToString();
            txtTen.Text = sp.TenSP;
            if (cboDM.Items.Count > 0) cboDM.SelectedValue = sp.MaDM;
            numGia.Value = Clamp(numGia, sp.GiaBan);
            numTon.Value = Clamp(numTon, sp.SoLuongTon);
            numMin.Value = Clamp(numMin, sp.MucTonToiThieu);
            numTon.Enabled = false;
            txtMoTa.Text = sp.MoTa;
            SetImage(sp.HinhAnh);
        }

        private static decimal Clamp(NumericUpDown n, decimal v)
        {
            return Math.Max(n.Minimum, Math.Min(n.Maximum, v));
        }

        private void ClearEditor()
        {
            grid.ClearSelection();
            currentId = 0;
            lblMa.Text = "(mới)";
            txtTen.Clear();
            if (cboDM.Items.Count > 0) cboDM.SelectedIndex = 0;
            numGia.Value = 0;
            numTon.Value = 0;
            numTon.Enabled = true;
            numMin.Value = 5;
            txtMoTa.Clear();
            SetImage(null);
            txtTen.Focus();
        }

        private void SetImage(string fileName)
        {
            currentImage = string.IsNullOrWhiteSpace(fileName) ? null : fileName;
            Image old = pic.Image;
            pic.Image = ImageHelper.Load(currentImage);
            if (old != null) old.Dispose();
        }

        private void BtnChonAnh_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog { Title = "Chọn ảnh sản phẩm", Filter = "Ảnh (*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp)|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                UiHelper.Try(this, () => SetImage(ImageHelper.Import(dlg.FileName)));
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var sp = new SanPhamDTO
            {
                MaSP = currentId,
                TenSP = txtTen.Text,
                MaDM = cboDM.SelectedValue is int ? (int)cboDM.SelectedValue : 0,
                GiaBan = numGia.Value,
                SoLuongTon = (int)numTon.Value,
                MucTonToiThieu = (int)numMin.Value,
                MoTa = txtMoTa.Text,
                HinhAnh = currentImage
            };
            bool isNew = currentId == 0;
            int id = 0;
            if (!UiHelper.Try(this, () => id = bll.Save(sp))) return;
            UiHelper.Info(this, isNew ? "Đã thêm sản phẩm mới." : "Đã cập nhật sản phẩm.");
            LoadData();
            SelectRow(id);
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (currentId == 0) { UiHelper.Warn(this, "Hãy chọn sản phẩm cần xóa."); return; }
            if (!UiHelper.Confirm(this, "Xóa sản phẩm \"" + txtTen.Text + "\"?")) return;
            if (!UiHelper.Try(this, () => bll.Delete(currentId))) return;
            UiHelper.Info(this, "Đã xóa sản phẩm.");
            LoadData();
        }

        private void SelectRow(int id)
        {
            foreach (DataGridViewRow r in grid.Rows)
            {
                var sp = r.DataBoundItem as SanPhamDTO;
                if (sp != null && sp.MaSP == id)
                {
                    r.Selected = true;
                    grid.CurrentCell = r.Cells[grid.Columns["TenSP"].Index];
                    return;
                }
            }
        }
    }
}
