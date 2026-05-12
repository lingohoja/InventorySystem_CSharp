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

            // 取得欄位型態架構
            DataTable schema = DBHelper.GetDataTable($"SELECT TOP 0 * FROM {_tableName}");
            DataRow oldRow = null;
            if (_isEdit)
            {
                DataTable dt = DBHelper.GetDataTable($"SELECT * FROM {_tableName} WHERE {_pkName} = {_id}");
                if (dt.Rows.Count > 0) oldRow = dt.Rows[0];
            }

            foreach (DataColumn col in schema.Columns)
            {
                if (col.AutoIncrement || col.ColumnName == _pkName) continue;

                _flowPanel.Controls.Add(new Label { Text = col.ColumnName, Width = 380, Margin = new Padding(0, 10, 0, 0) });

                // 修正：針對二進位圖片欄位
                if (col.DataType == typeof(byte[]) || col.ColumnName == "ProductImage")
                {
                    PictureBox pb = new PictureBox { Width = 150, Height = 150, BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom };
                    Button btnLoad = new Button { Text = "上傳相片", Width = 100 };

                    if (oldRow != null && oldRow[col.ColumnName] != DBNull.Value)
                        pb.Image = ByteArrayToImage((byte[])oldRow[col.ColumnName]);

                    btnLoad.Click += (s, e) => {
                        OpenFileDialog ofd = new OpenFileDialog { Filter = "圖片檔案|*.jpg;*.png;*.jpeg" };
                        if (ofd.ShowDialog() == DialogResult.OK) pb.Image = Image.FromFile(ofd.FileName);
                    };
                    _flowPanel.Controls.Add(pb);
                    _flowPanel.Controls.Add(btnLoad);
                    _controls.Add(col.ColumnName, pb);
                }
                // 修正：針對日期欄位使用 DateTimePicker
                else if (col.DataType == typeof(DateTime))
                {
                    DateTimePicker dtp = new DateTimePicker { Width = 380, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy/MM/dd HH:mm" };
                    if (oldRow != null && oldRow[col.ColumnName] != DBNull.Value) dtp.Value = (DateTime)oldRow[col.ColumnName];
                    _flowPanel.Controls.Add(dtp);
                    _controls.Add(col.ColumnName, dtp);
                }
                else
                {
                    TextBox txt = new TextBox { Width = 380, Text = oldRow != null ? oldRow[col.ColumnName].ToString() : "" };
                    _flowPanel.Controls.Add(txt);
                    _controls.Add(col.ColumnName, txt);
                }
            }

            Button btnSave = new Button { Text = "儲存資料", Width = 380, Height = 45, BackColor = Color.FromArgb(79, 70, 229), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 20, 0, 0) };
            btnSave.Click += SaveData;
            _flowPanel.Controls.Add(btnSave);
            this.Controls.Add(_flowPanel);
        }

        // 核心修正：根據控制項類型動態獲取值，徹底解決轉型失敗
        // 1. 修正：建立 SQL 參數的邏輯 (解決轉型與 SQL 型態不符)
        private SqlParameter CreateParameter(string name, Control c)
        {
            // 處理圖片 (varbinary)
            if (c is PictureBox pb)
            {
                SqlParameter sqlP = new SqlParameter("@" + name, SqlDbType.VarBinary);
                if (pb.Image != null)
                {
                    sqlP.Value = ImageToByteArray(pb.Image);
                }
                else
                {
                    sqlP.Value = DBNull.Value; // 解決 CS8957 錯誤
                }
                return sqlP;
            }

            // 處理日期 (解決 DateTimePicker 轉型 TextBox 失敗)
            if (c is DateTimePicker dtp)
            {
                return new SqlParameter("@" + name, dtp.Value);
            }

            // 處理文字 (TextBox)
            if (c is TextBox txt)
            {
                return new SqlParameter("@" + name, (object)txt.Text ?? DBNull.Value);
            }

            return new SqlParameter("@" + name, DBNull.Value);
        }

        // 2. 修正：存檔主邏輯 (使用 string.Format 讓舊版 C# 更穩定)
        // 1. 新增：檢查欄位是否填寫完整的方法
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