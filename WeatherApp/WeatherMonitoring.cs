using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using Microsoft.Azure.KeyVault;
using System.Data.SqlClient;
using System.Data;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace WeatherMonitoring
{
    public static class WeatherMonitoring
    {

        public static string API = "http://api.openweathermap.org/data/2.5/weather?zip=";
        public const string CLIENTSECRET = "Qn382J.Yf9A6MN.-lorm54tKV70.4U1.mg";
        public const string CLIENTID = "42ed081d-f2a1-41a4-b423-15629ca64302";
        public const string BASESECRETURI = "https://weathermonitoringapp.vault.azure.net/"; // available from the Key Vault resource page
        public static KeyVaultClient kvc = null;


        [FunctionName("WeatherMonitoring")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            var apiSecretValue = GetVaultValue("ApiKey");
            var sqlSecretValue = GetVaultValue("SqlAuthenticationpassword");

            string connectionString = $"Server=tcp:myweatherserver.database.windows.net,1433;Initial Catalog=WeatherDatabase;Persist Security Info=False;User ID=huy0801;Password={sqlSecretValue};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
            SqlConnection _con = new SqlConnection(connectionString);
            if (_con.State == ConnectionState.Closed)
                _con.Open();


            log.LogInformation("C# HTTP trigger function processed a request.");
            string zipcode = req.Query["zipcode"];


            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
            httpResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            httpResponse.Headers.Add("Access-Control-Allow-Methods", "*");


            try
            {
                string url = API + zipcode + "&appid=" + apiSecretValue;
                HttpClient client = new HttpClient();

                HttpClient newClient = new HttpClient();
                HttpRequestMessage newRequest = new HttpRequestMessage(HttpMethod.Get, url);

                //Read Server Response
                HttpResponseMessage response = await newClient.SendAsync(newRequest);

                WeatherResponse wR = await response.Content.ReadAsAsync<WeatherResponse>();

                string sunrise = UnixTimeStampToDateTime(wR.sys.sunrise).ToString();
                string sunset = UnixTimeStampToDateTime(wR.sys.sunset).ToString();
                string dt = UnixTimeStampToDateTime(wR.dt).ToString();
                string queryStatement = $"INSERT INTO weather_entries (base, visibility, dt, name, cod, main_temp, main_feels_like, main_temp_min, main_temp_max, main_pressure, " +
                    $"main_humidity, wind_speed, wind_deg, clouds_all, sys_type, sys_id, sys_country, sys_sunrise, sys_sunset) VALUES (\'{wR.Base}\', {wR.visibility}, \'{dt}\', \'{wR.name}\'," +
                    $" {wR.cod}, {wR.main.temp}, {wR.main.feels_like}, {wR.main.temp_min}, {wR.main.temp_max}, {wR.main.pressure}, {wR.main.humidity}, {wR.wind.speed}," +
                    $"{wR.wind.deg}, {wR.clouds.all}, {wR.sys.type}, {wR.sys.id}, \'{wR.sys.country}\', \'{sunrise}\', \'{sunset}\')";
                SqlCommand _cmd = new SqlCommand(queryStatement, _con);
                _cmd.ExecuteNonQuery();

                foreach (var item in wR.weather)
                {
                    queryStatement = $"INSERT INTO weather_data(weather_entries_id, main, description, icon) VALUES((SELECT TOP 1 id FROM weather_entries ORDER BY id DESC), \'{item.main}\', \'{item.description}\', \'{item.icon}\')";
                    _cmd = new SqlCommand(queryStatement, _con);
                    _cmd.ExecuteNonQuery();
                }
                _con.Close();

                httpResponse.Content = new StringContent(JsonConvert.SerializeObject(wR).ToString(), System.Text.Encoding.UTF8, "application/json");
                return httpResponse;
            }
            catch (Exception e)
            {
                httpResponse.Content = new StringContent(e.Message);
                return httpResponse;
            }

        }

        //Convert unix timestamp to date time
        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }


        //Get access token through autenticate 
        public static async Task<string> GetToken(string authority, string resource, string scope)
        {

            ClientCredential credential = new ClientCredential(CLIENTID, CLIENTSECRET);
            var context = new AuthenticationContext(authority, TokenCache.DefaultShared);
            var result = await context.AcquireTokenAsync(resource, credential);
            return result.AccessToken;
        }

        /**
         * Retrieves the access key vault accountKey (needed to authenticate access into the role assignments table)
         * @secretName: secret name on key vault
         */
        public static string GetVaultValue(string secretName)
        {
            KeyVaultClient client = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetToken));
            var secret = client.GetSecretAsync(BASESECRETURI, secretName).GetAwaiter().GetResult();
            return secret.Value;
        }

    }
}
