using System;
using System.Windows.Forms;
using SportZoneAdmin.BLL;
using SportZoneAdmin.DTO;

namespace SportZoneAdmin.GUI
{
    /// <summary>UC 1.2.2 - Quản lý danh mục: thêm, sửa, xóa, tìm kiếm</summary>
    public class frmDanhMuc : Form
    {
        private readonly DanhMucBLL bll = new DanhMucBLL();
        private readonly DataGridView grid;
        private readonly TextBox txtSearch;
        private readonly Label lblMa;
        private readonly TextBox txtTen;
        private readonly TextBox txtMoTa;
        private int currentId;

        public frmDanhMuc()
        {
            UiHelper.SetupChild(this, "Quản lý danh mục");

            FlowLayoutPanel toolbar = UiHelper.MakeToolbar();
            txtSearch = UiHelper.AddTool(toolbar, "Tìm danh mục:", new TextBox { Width = 260 });
            Button btnRefresh = UiHelper.AddTool(toolbar, null, UiHelper.MakeButton("Làm mới", UiHelper.Gray, 100));
            UiHelper.OnTextChangedDebounced(txtSearch, LoadData);
            btnRefresh.Click += (s, e) => { txtSearch.Clear(); LoadData(); };

            TableLayoutPanel fields;
            FlowLayoutPanel buttons;
            Panel editor = UiHelper.MakeEditor("Thông tin danh mục", out fields, out buttons);
            lblMa = UiHelper.AddField(fields, "Mã danh mục:", new Label { Text = "(mới)", AutoSize = true, Font = UiHelper.BoldFont });
            txtTen = UiHelper.AddField(fields, "Tên danh mục (*):", new TextBox { MaxLength = 100 });
            txtMoTa = UiHelper.AddField(fields, "Mô tả:", UiHelper.MakeMultiline(110));
            txtMoTa.MaxLength = 500;

            Button btnNew = UiHelper.MakeButton("Thêm mới", UiHelper.Blue, 110);
            Button btnSave = UiHelper.MakeButton("Lưu", UiHelper.Primary, 90);
            Button btnDelete = UiHelper.MakeButton("Xóa", UiHelper.Danger, 90);
            buttons.Controls.AddRange(new Control[] { btnNew, btnSave, btnDelete });
            btnNew.Click += (s, e) => ClearEditor();
            btnSave.Click += BtnSave_Click;
            btnDelete.Click += BtnDelete_Click;

            grid = UiHelper.MakeGrid();
            grid.SelectionChanged += (s, e) => FillEditor(UiHelper.Selected<DanhMucDTO>(grid));

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
                    "MaDM|Mã||40", "TenDM|Tên danh mục||120", "MoTa|Mô tả||220", "SoSanPham|Số sản phẩm|N0|60");
            });
            if (grid.Rows.Count == 0) ClearEditor();
        }

        private void FillEditor(DanhMucDTO dm)
        {
            if (dm == null) return;
            currentId = dm.MaDM;
            lblMa.Text = dm.MaDM.ToString();
            txtTen.Text = dm.TenDM;
            txtMoTa.Text = dm.MoTa;
        }

        private void ClearEditor()
        {
            grid.ClearSelection();
            currentId = 0;
            lblMa.Text = "(mới)";
            txtTen.Clear();
            txtMoTa.Clear();
            txtTen.Focus();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var dm = new DanhMucDTO { MaDM = currentId, TenDM = txtTen.Text, MoTa = txtMoTa.Text };
            bool isNew = currentId == 0;
            int id = 0;
            if (!UiHelper.Try(this, () => id = bll.Save(dm))) return;
            UiHelper.Info(this, isNew ? "Đã thêm danh mục mới." : "Đã cập nhật danh mục.");
            LoadData();
            SelectRow(id);
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (currentId == 0) { UiHelper.Warn(this, "Hãy chọn danh mục cần xóa."); return; }
            if (!UiHelper.Confirm(this, "Xóa danh mục \"" + txtTen.Text + "\"?")) return;
            if (!UiHelper.Try(this, () => bll.Delete(currentId))) return;
            UiHelper.Info(this, "Đã xóa danh mục.");
            LoadData();
        }

        private void SelectRow(int id)
        {
            foreach (DataGridViewRow r in grid.Rows)
            {
                var dm = r.DataBoundItem as DanhMucDTO;
                if (dm != null && dm.MaDM == id)
                {
                    r.Selected = true;
                    grid.CurrentCell = r.Cells[grid.Columns["TenDM"].Index];
                    return;
                }
            }
        }
    }
}
