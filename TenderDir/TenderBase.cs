using System.Data;
using MySql.Data.MySqlClient;

namespace ParserTenders.TenderDir
{
    public class TenderBase
    {
        protected void AddVerNumber(MySqlConnection connect, string purchaseNumber, int typeFz)
        {
            int verNum = 1;
            string selectTenders =
                $"SELECT id_tender FROM {Program.Prefix}tender WHERE purchase_number = @purchaseNumber AND type_fz = @typeFz ORDER BY id_tender ASC";
            MySqlCommand cmd1 = new MySqlCommand(selectTenders, connect);
            cmd1.Prepare();
            cmd1.Parameters.AddWithValue("@purchaseNumber", purchaseNumber);
            cmd1.Parameters.AddWithValue("@typeFz", typeFz);
            DataTable dt1 = new DataTable();
            MySqlDataAdapter adapter1 = new MySqlDataAdapter {SelectCommand = cmd1};
            adapter1.Fill(dt1);
            if (dt1.Rows.Count > 0)
            {
                string updateTender =
                    $"UPDATE {Program.Prefix}tender SET num_version = @num_version WHERE id_tender = @id_tender";
                foreach (DataRow ten in dt1.Rows)
                {
                    int idTender = (int) ten["id_tender"];
                    MySqlCommand cmd2 = new MySqlCommand(updateTender, connect);
                    cmd2.Prepare();
                    cmd2.Parameters.AddWithValue("@id_tender", idTender);
                    cmd2.Parameters.AddWithValue("@num_version", verNum);
                    cmd2.ExecuteNonQuery();
                    verNum++;
                }
            }
        }
    }
}