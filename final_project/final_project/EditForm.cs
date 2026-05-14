using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace final_project
{
    public partial class EditForm : Form
    {
        private string _tableName, _pkName;
        private object _id;
        private bool _isEdit;
        private FlowLayoutPanel _flowPanel;
        private Dictionary<string, Control> _controls = new Dictionary<string, Control>();

        public EditForm(string tableName, string pkName, object id = null)
        {
            this._tableName = tableName;
            this._pkName = pkName;
            this._id = id;
            this._isEdit = (id != null);

            this.Text = (_isEdit ? "修改" : "新增") + _tableName;
            this.Size = new Size(450, 800);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Microsoft JhengHei UI", 10);
            InitializeDynamicUI();
        }

        private void InitializeDynamicUI()
        {
            _flowPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), AutoScroll = true };

            DataTable schema = DBHelper.GetDataTable($"SELECT TOP 0 * FROM {_tableName}");
            DataRow oldRow = null;
            if (_isEdit)
            {
                DataTable dt = DBHelper.GetDataTable($"SELECT * FROM {_tableName} WHERE {_pkName} = {_id}");
                if (dt.Rows.Count > 0) oldRow = dt.Rows[0];
            }

            foreach (DataColumn col in schema.Columns)
            {
                // 1. 隱藏主鍵、自動遞增，以及「系統計算欄位(SubTotal)」避免 UPDATE 報錯
                if (col.AutoIncrement || col.ColumnName == _pkName || col.ColumnName == "SubTotal") continue;

                _flowPanel.Controls.Add(new Label { Text = col.ColumnName, Width = 380, Margin = new Padding(0, 10, 0, 0) });

                // 判斷是否為「總額」或「日期」等不該讓使用者手動亂改的系統欄位
                bool isSystemReadOnly = (_tableName.Contains("Master") && (col.ColumnName == "TotalAmount" || col.ColumnName == "SalesDate" || col.ColumnName == "PurchaseDate"));

                // 2. 外鍵強制轉下拉選單
                if (col.ColumnName.EndsWith("ID"))
                {
                    ComboBox cb = new ComboBox { Width = 380, DropDownStyle = ComboBoxStyle.DropDownList };
                    string refTable = "", displayCol = "";

                    if (col.ColumnName == "CustomerID") { refTable = "Customers"; displayCol = "CustomerName"; }
                    else if (col.ColumnName == "ProductID") { refTable = "Products"; displayCol = "ProductName"; }
                    else if (col.ColumnName == "SupplierID") { refTable = "Suppliers"; displayCol = "CompanyName"; }
                    else if (col.ColumnName == "CategoryID") { refTable = "Categories"; displayCol = "CategoryName"; }
                    else if (col.ColumnName == "SalesID") { refTable = "SalesMaster"; displayCol = "SalesID"; }
                    else if (col.ColumnName == "PurchaseID") { refTable = "PurchaseMaster"; displayCol = "PurchaseID"; }

                    if (refTable != "")
                    {
                        cb.DataSource = DBHelper.GetDataTable($"SELECT * FROM {refTable}");
                        cb.DisplayMember = displayCol;
                        cb.ValueMember = col.ColumnName;
                        if (oldRow != null && oldRow[col.ColumnName] != DBNull.Value) cb.SelectedValue = oldRow[col.ColumnName];
                    }
                    _flowPanel.Controls.Add(cb);
                    _controls.Add(col.ColumnName, cb);
                }
                // 3. 狀態欄位強制下拉選單
                else if (col.ColumnName == "Status")
                {
                    ComboBox cbStatus = new ComboBox { Width = 380, DropDownStyle = ComboBoxStyle.DropDownList };
                    cbStatus.Items.AddRange(new object[] { "已結帳", "處理中", "已退貨", "已取消" });
                    if (oldRow != null && oldRow[col.ColumnName] != DBNull.Value) cbStatus.SelectedItem = oldRow[col.ColumnName].ToString();
                    else cbStatus.SelectedIndex = 0;
                    _flowPanel.Controls.Add(cbStatus);
                    _controls.Add(col.ColumnName, cbStatus);
                }
                // 4. 照片處理
                else if (col.DataType == typeof(byte[]) || col.ColumnName == "ProductImage")
                {
                    PictureBox pb = new PictureBox { Width = 150, Height = 150, BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom };
                    Button btnLoad = new Button { Text = "上傳相片", Width = 100 };
                    if (oldRow != null && oldRow[col.ColumnName] != DBNull.Value) pb.Image = ByteArrayToImage((byte[])oldRow[col.ColumnName]);
                    btnLoad.Click += (s, e) => {
                        OpenFileDialog ofd = new OpenFileDialog { Filter = "圖片檔案|*.jpg;*.png;*.jpeg" };
                        if (ofd.ShowDialog() == DialogResult.OK) pb.Image = Image.FromFile(ofd.FileName);
                    };
                    _flowPanel.Controls.Add(pb);
                    _flowPanel.Controls.Add(btnLoad);
                    _controls.Add(col.ColumnName, pb);
                }
                // 5. 日期處理
                else if (col.DataType == typeof(DateTime))
                {
                    DateTimePicker dtp = new DateTimePicker { Width = 380, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd HH:mm" };
                    if (oldRow != null && oldRow[col.ColumnName] != DBNull.Value) dtp.Value = (DateTime)oldRow[col.ColumnName];
                    dtp.Enabled = !isSystemReadOnly; // 鎖定不可改
                    _flowPanel.Controls.Add(dtp);
                    _controls.Add(col.ColumnName, dtp);
                }
                // 6. 一般文字/數字輸入框 (價格、數量、名稱等)
                else
                {
                    TextBox txt = new TextBox { Width = 380 };
                    if (oldRow != null && oldRow[col.ColumnName] != DBNull.Value) txt.Text = oldRow[col.ColumnName].ToString();

                    if (isSystemReadOnly)
                    {
                        txt.ReadOnly = true;
                        txt.BackColor = Color.FromArgb(243, 244, 246); // 灰色代表鎖定
                    }
                    _flowPanel.Controls.Add(txt);
                    _controls.Add(col.ColumnName, txt);
                }
            }

            Button btnSave = new Button { Text = "儲存資料", Width = 380, Height = 45, BackColor = Color.FromArgb(79, 70, 229), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 20, 0, 0) };
            btnSave.Click += SaveData;
            _flowPanel.Controls.Add(btnSave);
            this.Controls.Add(_flowPanel);
        }

        // --- 取值給 SQL 的邏輯 ---
        private SqlParameter CreateParameter(string name, Control c)
        {
            if (c is PictureBox pb)
            {
                SqlParameter sqlP = new SqlParameter("@" + name, SqlDbType.VarBinary);
                sqlP.Value = pb.Image != null ? ImageToByteArray(pb.Image) : (object)DBNull.Value;
                return sqlP;
            }
            if (c is DateTimePicker dtp) return new SqlParameter("@" + name, dtp.Value);

            // 抓取 ComboBox 的 Value
            if (c is ComboBox cb)
            {
                if (cb.SelectedValue != null) return new SqlParameter("@" + name, cb.SelectedValue);
                if (cb.SelectedItem != null) return new SqlParameter("@" + name, cb.SelectedItem.ToString());
                return new SqlParameter("@" + name, DBNull.Value);
            }

            if (c is TextBox txt) return new SqlParameter("@" + name, string.IsNullOrEmpty(txt.Text) ? DBNull.Value : (object)txt.Text);

            return new SqlParameter("@" + name, DBNull.Value);
        }
        private bool ValidateInputs()
        {
            foreach (var kvp in _controls)
            {
                string columnName = kvp.Key;
                Control ctrl = kvp.Value;

                // 跳過圖片檢查 (因為圖片可不上傳)
                if (ctrl is PictureBox) continue;

                // 檢查 TextBox 是否為空
                if (ctrl is TextBox txt)
                {
                    if (string.IsNullOrWhiteSpace(txt.Text))
                    {
                        MessageBox.Show(string.Format("欄位「{0}」尚未填寫，請完整輸入資料。", columnName),
                                        "資料未完成", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txt.Focus(); // 自動將游標移到該欄位
                        return false;
                    }
                }

                // DateTimePicker 通常預設就有值，但如果是「LastUpdated」通常會設定成當前時間，不需檢查
                // 如果有其他必填邏輯可在此擴充
            }
            return true; // 所有欄位皆已填寫
        }

        // 2. 修改：存檔邏輯 (加入驗證判斷)
        private void SaveData(object sender, EventArgs e)
        {
            // --- 高手級防呆：先執行驗證 ---
            if (!ValidateInputs()) return;

            List<SqlParameter> p = new List<SqlParameter>();
            string sql = "";

            if (_isEdit)
            {
                List<string> sets = new List<string>();
                foreach (var c in _controls)
                {
                    sets.Add(string.Format("[{0}] = @{0}", c.Key));
                    p.Add(CreateParameter(c.Key, c.Value));
                }
                sql = string.Format("UPDATE [{0}] SET {1} WHERE [{2}] = @id", _tableName, string.Join(",", sets), _pkName);
                p.Add(new SqlParameter("@id", _id));
            }
            else
            {
                List<string> cols = new List<string>();
                List<string> vals = new List<string>();
                foreach (var c in _controls)
                {
                    cols.Add(string.Format("[{0}]", c.Key));
                    vals.Add("@" + c.Key);
                    p.Add(CreateParameter(c.Key, c.Value));
                }
                sql = string.Format("INSERT INTO [{0}] ({1}) VALUES ({2})", _tableName, string.Join(",", cols), string.Join(",", vals));
            }

            if (DBHelper.ExecuteNonQuery(sql, p.ToArray()) > 0)
            {
                MessageBox.Show("資料儲存成功！");
                this.DialogResult = DialogResult.OK;
            }
        }

        private byte[] ImageToByteArray(Image img)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                return ms.ToArray();
            }
        }
        private Image ByteArrayToImage(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes)) return Image.FromStream(ms);
        }
    }
}