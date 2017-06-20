#r "Newtonsoft.Json"
#r "System.Configuration"
#r "System.Data"

using System;
using System.Net;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class Account
{
    public Guid AccID { get; set; }
    public string Username { get; set; }
}

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

    return;
}