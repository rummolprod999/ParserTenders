using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.IO;

namespace ParserTenders
{
    public class TenderTypeSign223 : Tender
    {
        public event Action<int> AddTenderSign223;
        public event Action<int> UpdateTenderSign223;
        
        public TenderTypeSign223(FileInfo f, string region, int region_id, JObject json)
            : base(f, region, region_id, json)
        {
            
            AddTenderSign223 += delegate(int d)
            {
                if (d > 0)
                    Program.AddSign223++;
                else
                    Log.Logger("Не удалось добавить TenderSign223", file_path);
            };
            
            UpdateTenderSign223 += delegate(int d)
            {
                if (d > 0)
                    Program.UpdateSign223++;
                else
                    Log.Logger("Не удалось обновить TenderSign223", file_path);
            };
        }
    }
}