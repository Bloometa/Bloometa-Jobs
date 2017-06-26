#r "System.Configuration"
#r "System.Data"

using System;
using System.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;

class Account
{
    public Guid AccID { get; set; }
    public string UserName { get; set; }
    public string UserID { get; set; }
    public string FullName { get; set; }
}

public static void Run(TimerInfo timer, TraceWriter log)
{
    IEnumerable<long> UserIDs;
    Dictionary<long, Account> Accounts = new Dictionary<long, Account>();
    using (SqlConnection dbConn = new SqlConnection())
    {
        dbConn.ConnectionString = ConfigurationManager.ConnectionStrings["db"].ConnectionString;
        dbConn.Open();

        SqlCommand RetrieveTWAccounts =
            new SqlCommand(@"
            SELECT TOP(100) Acc.[AccID], Acc.[UserName], Acc.[UserID], Acc.[FullName] FROM UAccounts Acc
            INNER JOIN (
                SELECT TOP(100) [AccID], MAX([Run]) AS [LatestRun] FROM UData
                GROUP BY [AccID]
                ORDER BY [LatestRun] ASC
            ) AS Prio ON Prio.[AccID] = Acc.[AccID]
            WHERE
                Acc.[Removed] = 0
                AND Acc.[Network] = 'twitter'
                AND CONVERT(DATE, Prio.[LatestRun]) < CONVERT(DATE, GETUTCDATE())
            ORDER BY Prio.[LatestRun] ASC", dbConn);

        using (SqlDataReader results = RetrieveTWAccounts.ExecuteReader())
        {
            if (results.HasRows)
            {
                while (results.Read())
                {
                    Accounts.Add(long.Parse((string)results["UserID"]),
                        new Account() {
                            AccID = (Guid)results["AccID"],
                            UserName = (string)results["UserName"],
                            FullName = (string)results["FullName"]
                        }
                    );
                }
            }
            else
            {
                log.Info("No twitter acounts need checking.");
                return;
            }
        }

        IEnumerable<IUser> Users;
        UserIDs = new List<long>(Accounts.Keys);
        var TWCredentials = new TwitterCredentials(
            ConfigurationManager.ConnectionStrings["TwitterKey"].ConnectionString,
            ConfigurationManager.ConnectionStrings["TwitterSecret"].ConnectionString,
            ConfigurationManager.ConnectionStrings["TwitterAccessKey"].ConnectionString,
            ConfigurationManager.ConnectionStrings["TwitterAccessSecret"].ConnectionString);
        
        try
        {
            Users = Auth.ExecuteOperationWithCredentials(TWCredentials, () =>
            {
                return Tweetinvi.User.GetUsersFromIds(UserIDs);
            });

            foreach (IUser User in Users)
            {
                Console.WriteLine(String.Format("{0} (@{1}) ID: {2}", User.Name, User.ScreenName, User.Id));
                Console.WriteLine(String.Format("Following: {0}\t Followers: {1}", User.FriendsCount, User.FollowersCount));

                using(SqlCommand InsertDataRow =
                    new SqlCommand(@"
                    INSERT INTO UData ([AccID], [FollowCount], [FollowerCount]) VALUES (@AccID, @FollowCount, @FollowerCount)", dbConn))
                {
                    InsertDataRow.Parameters.Add("@AccID", SqlDbType.UniqueIdentifier).Value = Accounts[User.Id].AccID;
                    InsertDataRow.Parameters.Add("@FollowCount", SqlDbType.Int).Value = User.FriendsCount;
                    InsertDataRow.Parameters.Add("@FollowerCount", SqlDbType.Int).Value = User.FollowersCount;
                    
                    InsertDataRow.ExecuteNonQuery();
                }

                if (Accounts[User.Id].UserName != User.ScreenName 
                    || Accounts[User.Id].FullName != User.Name)
                {
                    log.Info($"Account {Accounts[User.Id].AccID} is out of date, updating with latest data");
                    using(SqlCommand UpdateAccDetails =
                        new SqlCommand(@"
                        UPDATE UAccounts
                        SET [UserName] = @UserName, [FullName] = @FullName
                        WHERE [AccID] = @AccID", dbConn))
                    {
                        UpdateAccDetails.Parameters.Add("@AccID", SqlDbType.UniqueIdentifier).Value = Accounts[User.Id].AccID;
                        UpdateAccDetails.Parameters.Add("@UserName", SqlDbType.NVarChar).Value = User.ScreenName;
                        UpdateAccDetails.Parameters.Add("@FullName", SqlDbType.NVarChar).Value = User.Name;
                        
                        UpdateAccDetails.ExecuteNonQuery();
                    }
                }
            }
        }
        catch
        {
            log.Error("Failed to connect to Twitter API");
        }
    }

    return;
}