using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace final_project
{
    public static class DBHelper
    {
        private static string connString = @"Data Source=.\sqlexpress;Initial Catalog=InventorySystem;Integrated Security=True;Encrypt=False";

        public static DataTable GetDataTable(string sql)
        {
            DataTable dt = new DataTable();
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
                    adapter.Fill(dt);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("資料庫讀取失敗：\n" + ex.Message);
            }
            return dt;
        }

        public static int ExecuteNonQuery(string sql, SqlParameter[] parameters = null)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(sql, conn);
                    if (parameters != null) cmd.Parameters.AddRange(parameters);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("指令執行失敗：\n" + ex.Message);
                return -1;
            }
        }
    }
}