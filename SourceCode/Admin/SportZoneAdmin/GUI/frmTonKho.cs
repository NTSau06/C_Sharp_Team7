using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SportZoneAdmin.BLL;
using SportZoneAdmin.DTO;
using SportZoneAdmin.Utils;

namespace SportZoneAdmin.GUI
{
    /// <summary>
    /// UC 1.2.12 - Quản lý tồn kho: xem tồn hiện tại + cảnh báo sắp hết, lập phiếu nhập,
    /// xem phiếu nhập và theo dõi lịch sử nhập / xuất
    /// </summary>
    public class frmTonKho : Form
    {
        private readonly KhoBLL bll = new KhoBLL();

        // tab tồn kho
        private readonly DataGridView gridTon;
        private readonly TextBox txtSearch;
        private readonly CheckBox chkSapHet;
        private readonly Label lblTonSummary;
        // tab phiếu nhập
        private readonly DataGridView gridPN;
        private readonly DataGridView gridCTPN;
        private readonly DateTimePicker pnFrom;
        private readonly DateTimePicker pnTo;
        // tab lịch sử
        private readonly DataGridView gridLS;
        private readonly DateTimePicker lsFrom;
        private readonly DateTimePicker lsTo;
        private readonly TextBox txtLsSP;
        private readonly Label lblLsSummary;

        private readonly bool moNhapKho;
        private TabPage tabPhieuNhap;
        private TabControl tabs;

        public frmTonKho() : this(false) { }

        /// <param name="moNhapKho">true: mở từ menu "Nhập kho" - chuyển sang tab Phiếu nhập và mở ngay form lập phiếu (frmNhapKho)</param>
        public frmTonKho(bool moNhapKho)
        {
            this.moNhapKho = moNhapKho;
            UiHelper.SetupChild(this, moNhapKho ? "Nhập kho" : "Quản lý tồn kho");
            tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(16, 6) };

            // ================= TAB 1: TỒN KHO HIỆN TẠI
            var tabTon = new TabPage("Tồn kho hiện tại") { BackColor = Color.White };
            FlowLayoutPanel tb1 = UiHelper.MakeToolbar();
            txtSearch = UiHelper.AddTool(tb1, "Tìm sản phẩm:", new TextBox { Width = 220 });
            chkSapHet = UiHelper.AddTool(tb1, null, new CheckBox { Text = "Chỉ hiện sản phẩm sắp hết hàng", AutoSize = true });
            Button btnNhap = UiHelper.AddTool(tb1, null, UiHelper.MakeButton("+ Lập phiếu nhập kho", UiHelper.Primary, 190));
            Button btnRefresh = UiHelper.AddTool(tb1, null, UiHelper.MakeButton("Làm mới", UiHelper.Gray, 100));
            UiHelper.OnTextChangedDebounced(txtSearch, LoadTonKho);
            chkSapHet.CheckedChanged += (s, e) => LoadTonKho();
            btnNhap.Click += (s, e) => LapPhieuNhap();
            btnRefresh.Click += (s, e) => LoadTonKho();

            lblTonSummary = new Label { Dock = DockStyle.Bottom, Height = 30, ForeColor = Color.DimGray, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0) };
            gridTon = UiHelper.MakeGrid();
            gridTon.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var sp = gridTon.Rows[e.RowIndex].DataBoundItem as SanPhamDTO;
                if (sp != null && sp.SapHetHang())
                {
                    e.CellStyle.BackColor = UiHelper.WarningBack;
                    e.CellStyle.ForeColor = Color.DarkRed;
                }
            };
            gridTon.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) LapPhieuNhap(); };
            tabTon.Controls.Add(gridTon);
            tabTon.Controls.Add(lblTonSummary);
            tabTon.Controls.Add(tb1);
            gridTon.BringToFront();

            // ================= TAB 2: PHIẾU NHẬP
            var tabPN = new TabPage("Phiếu nhập kho") { BackColor = Color.White };
            FlowLayoutPanel tb2 = UiHelper.MakeToolbar();
            pnFrom = UiHelper.AddTool(tb2, "Từ ngày:", UiHelper.MakeDate(DateTime.Today.AddMonths(-12)));
            pnTo = UiHelper.AddTool(tb2, "Đến ngày:", UiHelper.MakeDate(DateTime.Today));
            Button btnXemPN = UiHelper.AddTool(tb2, null, UiHelper.MakeButton("Xem", UiHelper.Primary, 80));
            btnXemPN.Click += (s, e) => LoadPhieuNhap();

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterWidth = 6 };
            gridPN = UiHelper.MakeGrid();
            gridCTPN = UiHelper.MakeGrid();
            gridPN.SelectionChanged += (s, e) => LoadChiTietPhieuNhap();
            split.Panel1.Controls.Add(gridPN);
            var lblCT = new Label { Text = "Chi tiết phiếu nhập", Dock = DockStyle.Top, Height = 26, Font = UiHelper.BoldFont, ForeColor = UiHelper.Primary };
            split.Panel2.Controls.Add(gridCTPN);
            split.Panel2.Controls.Add(lblCT);
            gridCTPN.BringToFront();
            tabPN.Controls.Add(split);
            tabPN.Controls.Add(tb2);
            split.BringToFront();

            // ================= TAB 3: LỊCH SỬ NHẬP / XUẤT
            var tabLS = new TabPage("Lịch sử nhập / xuất") { BackColor = Color.White };
            FlowLayoutPanel tb3 = UiHelper.MakeToolbar();
            lsFrom = UiHelper.AddTool(tb3, "Từ ngày:", UiHelper.MakeDate(DateTime.Today.AddMonths(-3)));
            lsTo = UiHelper.AddTool(tb3, "Đến ngày:", UiHelper.MakeDate(DateTime.Today));
            txtLsSP = UiHelper.AddTool(tb3, "Sản phẩm:", new TextBox { Width = 200 });
            Button btnXemLS = UiHelper.AddTool(tb3, null, UiHelper.MakeButton("Xem", UiHelper.Primary, 80));
            btnXemLS.Click += (s, e) => LoadLichSu();
            txtLsSP.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadLichSu(); } };
            lblLsSummary = new Label { Dock = DockStyle.Bottom, Height = 30, ForeColor = Color.DimGray, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 0, 0) };
            gridLS = UiHelper.MakeGrid();
            gridLS.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || gridLS.Columns[e.ColumnIndex].Name != "Loai") return;
                var ls = gridLS.Rows[e.RowIndex].DataBoundItem as LichSuKhoDTO;
                if (ls != null) e.CellStyle.ForeColor = ls.Loai == "Nhập kho" ? UiHelper.Primary : UiHelper.Blue;
            };
            tabLS.Controls.Add(gridLS);
            tabLS.Controls.Add(lblLsSummary);
            tabLS.Controls.Add(tb3);
            gridLS.BringToFront();

            tabs.TabPages.Add(tabTon);
            tabs.TabPages.Add(tabPN);
            tabPhieuNhap = tabPN;
            tabs.TabPages.Add(tabLS);
            tabs.SelectedIndexChanged += (s, e) =>
            {
                if (tabs.SelectedTab == tabPN && gridPN.DataSource == null) LoadPhieuNhap();
                if (tabs.SelectedTab == tabLS && gridLS.DataSource == null) LoadLichSu();
            };
            UiHelper.StyleTabs(tabs);
            Controls.Add(tabs);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadTonKho();
            if (moNhapKho)
            {
                tabs.SelectedTab = tabPhieuNhap;
                BeginInvoke((Action)LapPhieuNhap);
            }
        }

        private void LoadTonKho()
        {
            List<SanPhamDTO> list = null;
            UiHelper.Try(this, () =>
            {
                list = bll.GetTonKho(txtSearch.Text, chkSapHet.Checked);
                UiHelper.Bind(gridTon, list,
                    "MaSP|Mã||40", "TenSP|Sản phẩm||220", "TenDM|Danh mục||100", "SoLuongTon|Tồn hiện tại|N0|70",
                    "MucTonToiThieu|Mức tối thiểu|N0|70", "GiaBan|Giá bán|N0|80", "TinhTrangKho|Tình trạng||80");
            });
            if (list != null)
                lblTonSummary.Text = list.Count + " sản phẩm  |  Tổng số lượng tồn: " + list.Sum(x => x.SoLuongTon).ToString("N0") +
                                     "  |  Dòng tô đỏ: tồn kho ≤ mức tối thiểu (" + list.Count(x => x.SapHetHang()) + " sản phẩm). Nhấp đúp để nhập hàng.";
        }

        private void LapPhieuNhap()
        {
            SanPhamDTO sp = UiHelper.Selected<SanPhamDTO>(gridTon);
            using (var f = new frmNhapKho(sp == null ? 0 : sp.MaSP))
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                {
                    LoadTonKho();
                    if (gridPN.DataSource != null) LoadPhieuNhap();
                    gridLS.DataSource = null;
                }
            }
        }

        private void LoadPhieuNhap()
        {
            UiHelper.Try(this, () =>
            {
                UiHelper.Bind(gridPN, bll.GetPhieuNhap(pnFrom.Value, pnTo.Value),
                    "MaPN|Mã phiếu||50", "NgayNhap|Ngày nhập|dd/MM/yyyy HH:mm|100", "TongTienNhap|Tổng tiền nhập|N0|90",
                    "TenQTV|Người lập||120", "GhiChu|Ghi chú||220");
            });
            LoadChiTietPhieuNhap();
        }

        private void LoadChiTietPhieuNhap()
        {
            PhieuNhapKhoDTO pn = UiHelper.Selected<PhieuNhapKhoDTO>(gridPN);
            List<ChiTietPhieuNhapDTO> items = new List<ChiTietPhieuNhapDTO>();
            if (pn != null) UiHelper.Try(this, () => items = bll.GetChiTietPhieuNhap(pn.MaPN));
            UiHelper.Bind(gridCTPN, items,
                "MaSP|Mã SP||50", "TenSP|Sản phẩm||250", "SoLuongNhap|Số lượng nhập|N0|80", "GiaNhap|Giá nhập|N0|80", "ThanhTien|Thành tiền|N0|90");
        }

        private void LoadLichSu()
        {
            List<LichSuKhoDTO> list = null;
            UiHelper.Try(this, () =>
            {
                list = bll.GetLichSu(lsFrom.Value, lsTo.Value, txtLsSP.Text);
                UiHelper.Bind(gridLS, list,
                    "Ngay|Ngày|dd/MM/yyyy HH:mm|90", "Loai|Loại||60", "ChungTu|Chứng từ||60", "TenSP|Sản phẩm||220",
                    "SoLuong|Số lượng|N0|60", "DonGia|Đơn giá|N0|80", "ThanhTien|Thành tiền|N0|90");
            });
            if (list != null)
                lblLsSummary.Text = "Tổng nhập: " + list.Where(x => x.Loai == "Nhập kho").Sum(x => x.SoLuong).ToString("N0") +
                                    "  |  Tổng xuất: " + list.Where(x => x.Loai != "Nhập kho").Sum(x => x.SoLuong).ToString("N0") +
                                    "  (Xuất = đơn hàng đã xác nhận / đang giao / đã giao)";
        }
    }

    /// <summary>UC 1.2.12 - Lập phiếu nhập kho (cộng dồn tồn kho trong transaction)</summary>
    public class frmNhapKho : Form
    {
        private readonly BindingList<ChiTietPhieuNhapDTO> lines = new BindingList<ChiTietPhieuNhapDTO>();
        private readonly ComboBox cboSP;
        private readonly NumericUpDown numSL;
        private readonly NumericUpDown numGia;
        private readonly DataGridView grid;
        private readonly TextBox txtGhiChu;
        private readonly Label lblTong;
        private List<SanPhamDTO> products = new List<SanPhamDTO>();

        public frmNhapKho(int maSPChon)
        {
            Text = "Lập phiếu nhập kho";
            Font = UiHelper.BaseFont;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(920, 620);
            MinimumSize = new Size(800, 500);
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.White;

            FlowLayoutPanel tb = UiHelper.MakeToolbar();
            tb.Height = 54;
            cboSP = UiHelper.AddTool(tb, "Sản phẩm:", new ComboBox
            {
                Width = 320,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            });
            numSL = UiHelper.AddTool(tb, "SL:", UiHelper.MakeNumber(1, 100000, 1));
            numSL.Width = 80;
            numGia = UiHelper.AddTool(tb, "Giá nhập:", UiHelper.MakeNumber(0, 1000000000, 1000));
            numGia.Width = 120;
            Button btnAdd = UiHelper.AddTool(tb, null, UiHelper.MakeButton("Thêm dòng", UiHelper.Blue, 110));
            Button btnRemove = UiHelper.AddTool(tb, null, UiHelper.MakeButton("Xóa dòng", UiHelper.Danger, 100));
            btnAdd.Click += (s, e) => AddLine();
            btnRemove.Click += (s, e) =>
            {
                ChiTietPhieuNhapDTO ct = UiHelper.Selected<ChiTietPhieuNhapDTO>(grid);
                if (ct != null) lines.Remove(ct);
                UpdateTotal();
            };
            cboSP.SelectedIndexChanged += (s, e) =>
            {
                var sp = cboSP.SelectedItem as SanPhamDTO;
                if (sp != null) numGia.Value = Math.Min(numGia.Maximum, Math.Round(sp.GiaBan * 0.7m / 1000m) * 1000m);
            };

            grid = UiHelper.MakeGrid();
            UiHelper.Bind(grid, lines,
                "MaSP|Mã SP||50", "TenSP|Sản phẩm||260", "SoLuongNhap|Số lượng|N0|70", "GiaNhap|Giá nhập|N0|90", "ThanhTien|Thành tiền|N0|100");

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 120, Padding = new Padding(10) };
            var lblGhiChu = new Label { Text = "Ghi chú:", Left = 10, Top = 12, AutoSize = true };
            txtGhiChu = new TextBox { Left = 80, Top = 8, Width = 480, Height = 60, Multiline = true, MaxLength = 255 };
            lblTong = new Label { Left = 580, Top = 10, Width = 300, Height = 30, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = UiHelper.Primary, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            Button btnSave = UiHelper.MakeButton("Lưu phiếu nhập", UiHelper.Primary, 150);
            Button btnCancel = UiHelper.MakeButton("Hủy", UiHelper.Gray, 90);
            btnSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSave.Location = new Point(620, 74);
            btnCancel.Location = new Point(780, 74);
            bottom.Controls.AddRange(new Control[] { lblGhiChu, txtGhiChu, lblTong, btnSave, btnCancel });
            btnSave.Click += (s, e) => Save();
            btnCancel.Click += (s, e) =>
            {
                if (lines.Count > 0 && !UiHelper.Confirm(this, "Hủy phiếu nhập đang lập? Dữ liệu chưa được lưu.")) return;
                DialogResult = DialogResult.Cancel;
                Close();
            };

            var gridPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 0, 10, 0) };
            gridPanel.Controls.Add(grid);
            Controls.Add(gridPanel);
            Controls.Add(bottom);
            Controls.Add(tb);
            gridPanel.BringToFront();

            Load += (s, e) =>
            {
                UiHelper.Try(this, () => products = new SanPhamBLL().GetAll());
                products = products.OrderBy(x => x.TenSP).ToList();
                cboSP.DataSource = products;
                SanPhamDTO chon = products.FirstOrDefault(x => x.MaSP == maSPChon);
                if (chon != null) cboSP.SelectedItem = chon;
                UpdateTotal();
            };
        }

        private void AddLine()
        {
            var sp = cboSP.SelectedItem as SanPhamDTO;
            if (sp == null) { UiHelper.Warn(this, "Hãy chọn sản phẩm có trong danh sách."); return; }
            if (numSL.Value <= 0 || numGia.Value <= 0) { UiHelper.Warn(this, "Số lượng hoặc đơn giá nhập phải lớn hơn 0."); return; }
            ChiTietPhieuNhapDTO existing = lines.FirstOrDefault(x => x.MaSP == sp.MaSP);
            if (existing != null)
            {
                existing.SoLuongNhap += (int)numSL.Value;
                existing.GiaNhap = numGia.Value;
                lines.ResetBindings();
            }
            else
            {
                lines.Add(new ChiTietPhieuNhapDTO { MaSP = sp.MaSP, TenSP = sp.TenSP, SoLuongNhap = (int)numSL.Value, GiaNhap = numGia.Value });
            }
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            lblTong.Text = "Tổng tiền: " + lines.Sum(x => x.ThanhTien).ToString("N0") + " đ";
        }

        private void Save()
        {
            var pn = new PhieuNhapKhoDTO { GhiChu = txtGhiChu.Text };
            pn.SetChiTiet(lines.ToList());
            int maPN = 0;
            if (!UiHelper.Try(this, () => maPN = new KhoBLL().LapPhieuNhap(pn))) return;
            UiHelper.Info(this, "Đã lưu phiếu nhập PN" + maPN + ". Tồn kho đã được cộng dồn.");
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
