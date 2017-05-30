using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;

namespace ParserTenders
{
    public class ParserTend44 : Parser
    {
        protected DataTable DtRegion;

        public ParserTend44(TypeArguments arg) : base(arg)
        {
        }

        public override void Parsing()
        {
            DtRegion = GetRegions();
            foreach (DataRow row in DtRegion.Rows)
            {
                List<String> arch = new List<string>();
                string PathParse = "";
                string RegionPath = (string) row["path"];
                switch (Program.Periodparsing)
                {
                    case TypeArguments.Last44:
                        PathParse = $"/fcs_regions/{RegionPath}/notifications/";
                        arch = GetListArchLast(PathParse, RegionPath);
                        break;
                    case TypeArguments.Curr44:
                        PathParse = $"/fcs_regions/{RegionPath}/notifications/currMonth/";
                        arch = GetListArchCurr(PathParse, RegionPath);
                        break;
                    case TypeArguments.Prev44:
                        PathParse = $"/fcs_regions/{RegionPath}/notifications/prevMonth/";
                        arch = GetListArchPrev(PathParse, RegionPath);
                        break;
                }

                if (arch.Capacity == 0)
                {
                    Log.Logger("Не получили список архивов по региону", row["path"]);
                    continue;
                }
            }
        }

        public override List<String> GetListArchLast(string PathParse, string RegionPath)
        {
            WorkWithFtp ftp = ClientFtp44();
            ftp.ChangeWorkingDirectory(PathParse);
            List<String> archtemp = ftp.ListDirectory();
            List<String> years_search = Program.Years.Select(y => $"notification_{RegionPath}{y}").ToList();
            return archtemp.Where(a => years_search.Any(t => a.IndexOf(t, StringComparison.Ordinal) != -1)).ToList();
        }
        
        public override List<String> GetListArchCurr(string PathParse, string RegionPath)
        {
            List<String> arch = new List<string>();
            WorkWithFtp ftp = ClientFtp44();
            ftp.ChangeWorkingDirectory(PathParse);
            List<String> archtemp = ftp.ListDirectory();
            List<String> years_search = Program.Years.Select(y => $"notification_{RegionPath}{y}").ToList();
            foreach (var a in archtemp.Where(a => years_search.Any(t => a.IndexOf(t, StringComparison.Ordinal) != -1)))
            {
                using (MySqlConnection connect = ConnectToDb.GetDBConnection())
                {
                    connect.Open();
                    string select_arch =
                        $"SELECT id FROM {Program.Prefix}arhiv_tenders WHERE arhiv = @archive AND region =  @region";
                    MySqlCommand cmd = new MySqlCommand(select_arch, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@archive", a);
                    cmd.Parameters.AddWithValue("@region", RegionPath);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    bool res_read = reader.HasRows;
                    reader.Close();
                    if (!res_read)
                    {
                        string add_arch =
                            $"INSERT INTO {Program.Prefix}arhiv_tenders SET arhiv = @archive, region =  @region";
                        MySqlCommand cmd1 = new MySqlCommand(add_arch, connect);
                        cmd1.Prepare();
                        cmd1.Parameters.AddWithValue("@archive", a);
                        cmd1.Parameters.AddWithValue("@region", RegionPath);
                        cmd1.ExecuteNonQuery();
                        arch.Add(a);
                    }
                }
            }

            return arch;
        }
        
        public override List<String> GetListArchPrev(string PathParse, string RegionPath)
        {
            List<String> arch = new List<string>();
            WorkWithFtp ftp = ClientFtp44();
            ftp.ChangeWorkingDirectory(PathParse);
            List<String> archtemp = ftp.ListDirectory();
            string serachd = $"{Program.LocalDate:yyyyMMdd}";
            foreach (var a in archtemp.Where(a => a.IndexOf(serachd, StringComparison.Ordinal) != -1))
            {
                string prev_a = $"prev_{a}";
                using (MySqlConnection connect = ConnectToDb.GetDBConnection())
                {
                    connect.Open();
                    string select_arch =
                        $"SELECT id FROM {Program.Prefix}arhiv_contract WHERE arhiv = @archive AND region =  @region";
                    MySqlCommand cmd = new MySqlCommand(select_arch, connect);
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@archive", prev_a);
                    cmd.Parameters.AddWithValue("@region", RegionPath);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    bool res_read = reader.HasRows;
                    reader.Close();
                    if (!res_read)
                    {
                        string add_arch =
                            $"INSERT INTO {Program.Prefix}arhiv_contract SET arhiv = @archive, region =  @region";
                        MySqlCommand cmd1 = new MySqlCommand(add_arch, connect);
                        cmd1.Prepare();
                        cmd1.Parameters.AddWithValue("@archive", prev_a);
                        cmd1.Parameters.AddWithValue("@region", RegionPath);
                        cmd1.ExecuteNonQuery();
                        arch.Add(a);
                    }
                }
            }

            return arch;
        }
    }
}