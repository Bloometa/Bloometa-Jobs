#r "Newtonsoft.Json"
#r "System.Configuration"
#r "System.Data"

using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Newtonsoft.Json;

class Account
{
    public Guid AccID { get; set; }
    public string Username { get; set; }
}

public static void Run(TimerInfo timerDaily, ICollector<string> igQueue, ICollector<string> twQueue, TraceWriter log)
{
    using (SqlConnection dbConn = new SqlConnection())
    {
        dbConn.ConnectionString = ConfigurationManager.ConnectionStrings["socialtracker_DB"].ConnectionString;
        dbConn.Open();

        SqlCommand RetrieveCompanies = new SqlCommand("SELECT [AccID], [Network], [Username] FROM UAccounts WHERE [Removed] = 0", dbConn);

        using (SqlDataReader results = RetrieveCompanies.ExecuteReader())
        {
            while (results.Read())
            {
                Account acc = new Account();
                acc.AccID = (Guid)results["AccID"];
                acc.Username = (string)results["Username"];

                switch ((string)results["Network"])
                {
                    case "instagram":
                        igQueue.Add(JsonConvert.SerializeObject(acc));
                        break;
                    case "twitter":
                        twQueue.Add(JsonConvert.SerializeObject(acc));
                        break;
                }
            }
        }
    }

    return;
}