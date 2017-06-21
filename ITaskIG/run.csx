#r "Newtonsoft.Json"
#r "System.Configuration"
#r "System.Data"

using System;
using System.Net;
using System.Data;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static void Run(string accountData, TraceWriter log)
{
    JObject igData;
    JObject acc = JObject.Parse(accountData);
    
    using (WebClient wClient = new WebClient())
    {
        var wcResponse = wClient.DownloadString(String.Format("https://instagram.com/{0}/?__a=1", (string)acc.SelectToken("Username")));
        igData = JObject.Parse(wcResponse);
    }

    log.Info((string)igData.SelectToken("user")
        .SelectToken("followed_by")
            .SelectToken("count"));

    using (SqlConnection dbConn = new SqlConnection())
    {
        dbConn.ConnectionString = ConfigurationManager.ConnectionStrings["db"].ConnectionString;
        dbConn.Open();
        using(SqlCommand InsertDataRow =
            new SqlCommand("INSERT INTO UData ([AccID], [FollowCount], [FollowerCount]) VALUES (@AccID, @FollowCount, @FollowerCount)", dbConn))
        {
            InsertDataRow.Parameters.Add("@AccID", SqlDbType.UniqueIdentifier).Value = new Guid((string)acc.SelectToken("AccID"));
            InsertDataRow.Parameters.Add("@FollowCount", SqlDbType.Int).Value = Int32
                .Parse((string)igData.SelectToken("user")
                    .SelectToken("follows")
                        .SelectToken("count"));
            InsertDataRow.Parameters.Add("@FollowerCount", SqlDbType.Int).Value = Int32
                .Parse((string)igData.SelectToken("user")
                    .SelectToken("followed_by")
                        .SelectToken("count"));
            
            InsertDataRow.ExecuteNonQuery();
        }
    }

    return;
}