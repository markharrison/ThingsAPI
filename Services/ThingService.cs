using Azure;
using Azure.Data.Tables;
using System.Diagnostics;
using ThingsAPI.Models;
using Azure.Identity;

namespace ThingsAPI.Services
{
    public class ThingService
    {
        private TableClient _tableclient { get; set; }

        private readonly IConfiguration _config;

        public ThingService(IConfiguration config)
        {
            _config = config;
            string tableName = "thingsdata";
   
            try
            {
                if (_config.GetValue<bool>("ThingsStorageUseManagedIdentity"))
                {
                    string storageAccountName = config.GetValue<string>("ThingsStorageAccountName") ?? "";
                    var credential = new ManagedIdentityCredential();

                    // Create TableServiceClient
                    var serviceClient = new TableServiceClient(
                        new Uri($"https://{storageAccountName}.table.core.windows.net"),
                        credential
                    );

                    // Get TableClient
                    _tableclient = serviceClient.GetTableClient(tableName);
                }
                else
                {
                    string storageConnectionString = config.GetValue<string>("ThingsStorageConnectionString") ?? "";
                    _tableclient = new TableClient(storageConnectionString, tableName);
                }

                _tableclient.CreateIfNotExists();

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                throw;
            }

            return;
        }

        public async Task<IEnumerable<ThingItem>> GetAll()
        {

            List<ThingItem> _Things = new List<ThingItem>();

            try
            {
                Pageable<ThingItemEntity> queryResultsFilter = _tableclient.Query<ThingItemEntity>(e => e.PartitionKey == "Thing");
                foreach (ThingItemEntity entity in queryResultsFilter)
                {
                    ThingItem _Thing = new ThingItem
                    {
                        Thingid = entity.Thingid,
                        Name = entity.Name,
                        Longitude = entity.Longitude,
                        Latitude = entity.Latitude,
                        Text = entity.Text,
                        Status = entity.Status,
                        Image = entity.Image,
                        Data = entity.Data
                    };

                    _Things.Add(_Thing);

                }

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                throw;
            }

            await Task.Run(() => { });
            return _Things;
        }

        public async Task<ThingItem?> FindByNameLocation(ThingItem Thing)
        {
            if (Thing.Latitude == null || Thing.Longitude == null || Thing.Name == null)
            {
                return null;
            }

            try
            {

                Pageable<ThingItemEntity> queryResultsFilter = _tableclient.Query<ThingItemEntity>(e => e.PartitionKey == "Thing" && e.Name == Thing.Name);
                foreach (ThingItemEntity entity in queryResultsFilter)
                {
                    decimal deltaLongitude = Math.Abs((decimal)(entity.Longitude ?? 0) - (decimal)(Thing.Longitude ?? 0));
                    decimal deltaLatitude = Math.Abs((decimal)(entity.Latitude ?? 0) - (decimal)(Thing.Latitude ?? 0));

                    if (deltaLongitude < 0.00015m && deltaLatitude < 0.00015m)
                    {
                        ThingItem _Thing = new ThingItem
                        {
                            Thingid = entity.Thingid,
                            Name = entity.Name,
                            Longitude = entity.Longitude,
                            Latitude = entity.Latitude,
                            Text = entity.Text,
                            Status = entity.Status,
                            Image = entity.Image,
                            Data = entity.Data
                        };
                        return _Thing;
                    }
                }

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                throw;
            }

            await Task.Run(() => { });
            return null;
        }



        public async Task<ThingItem?> FindById(string id)
        {
            try
            {
                Pageable<ThingItemEntity> queryResultsFilter = _tableclient.Query<ThingItemEntity>(e => e.PartitionKey == "Thing" && e.RowKey == id.PadLeft(4, '0'));
                foreach (ThingItemEntity entity in queryResultsFilter)
                {
                    Debug.WriteLine(entity.Name);

                    ThingItem _Thing = new ThingItem
                    {
                        Thingid = entity.Thingid,
                        Name = entity.Name,
                        Longitude = entity.Longitude,
                        Latitude = entity.Latitude,
                        Text = entity.Text,
                        Status = entity.Status,
                        Image = entity.Image,
                        Data = entity.Data
                    };

                    return _Thing;

                }

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                throw;
            }


            await Task.Run(() => { });
            return null;
        }

        public async Task UpdateById(string id, ThingItem Thing)
        {

            try
            {

                ThingItemEntity entity = new ThingItemEntity
                {
                    PartitionKey = "Thing",
                    RowKey = id.PadLeft(4, '0'),
                    Thingid = long.Parse(id),
                    Name = Thing.Name,
                    Longitude = (double)(Thing.Longitude ?? 0),
                    Latitude = (double)(Thing.Latitude ?? 0),
                    Text = Thing.Text,
                    Status = Thing.Status,
                    Image = Thing.Image,
                    Data = Thing.Data
                };

                _tableclient.UpsertEntity(entity);

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                throw;
            }

            await Task.Run(() => { });

            return;
        }

        public async Task DeleteById(string id)
        {
            //ThingItemEntity entity = new ThingItemEntity(id.PadLeft(4, '0'))
            {
                //ETag = "*"
            };

            try
            {
                _tableclient.DeleteEntity("Thing", id.PadLeft(4, '0'));

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                throw;
            }

            await Task.Run(() => { });
            return;
        }

        public async Task DeleteAll()
        {
            List<TableTransactionAction> batch = new List<TableTransactionAction>();
            List<ThingItem> _Things = new List<ThingItem>();
            try
            {
                Pageable<ThingItemEntity> queryResultsFilter = _tableclient.Query<ThingItemEntity>(e => e.PartitionKey == "Thing");
                foreach (ThingItemEntity entity in queryResultsFilter)
                {
                    batch.Add(new TableTransactionAction(TableTransactionActionType.Delete, entity));
                }

                if (batch.Count > 0)
                {
                    _tableclient.SubmitTransaction(batch);
                }

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                throw;
            }

            await Task.Run(() => { });

            return;
        }

        //public string EchoData(string key, string value)
        //{
        //    return null;
        //}

        public string GetAppConfigInfo(HttpContext context)
        {

            string EchoData(string key, string value)
            {
                return key + ": <span class='echodata'>" + value + "</span><br/>";
            }

            string EchoDataBull(string key, string value)
            {
                return EchoData("&nbsp;&bull;&nbsp;" + key, value);
            }


            string strHtml = "";
            strHtml += "<html><head>";
            strHtml += "<style>";
            strHtml += "body { font-family: \"Segoe UI\",Roboto,\"Helvetica Neue\",Arial;}";
            strHtml += ".echodata { color: blue }";
            strHtml += "</style>";
            strHtml += "</head><body>";
            strHtml += "<h3>ThingsAPI - AppConfigInfo </h3>";

            strHtml += EchoData("OS Description", System.Runtime.InteropServices.RuntimeInformation.OSDescription);
            strHtml += EchoData("Framework Description", System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
            strHtml += EchoData("BuildIdentifier", _config.GetValue<string>("BuildIdentifier") ?? "");

            if (_config.GetValue<string>("AdminPW") == context.Request.Query["pw"].ToString())
            {
                strHtml += EchoData("ASPNETCORE_ENVIRONMENT", _config.GetValue<string>("ASPNETCORE_ENVIRONMENT") ?? "");
                strHtml += EchoData("APPLICATIONINSIGHTS_CONNECTION_STRING", _config.GetValue<string>("APPLICATIONINSIGHTS_CONNECTION_STRING") ?? "");
                strHtml += EchoData("ThingsStorageUseManagedIdentity", (_config.GetValue<bool>("ThingsStorageUseManagedIdentity")) ? "true" : "false" );
                strHtml += EchoData("ThingsStorageConnectionString", _config.GetValue<string>("ThingsStorageConnectionString") ?? "");
                strHtml += EchoData("ThingsStorageAccountName", _config.GetValue<string>("ThingsStorageAccountName") ?? "");
            }

            strHtml += "RequestInfo: <br/>";
            strHtml += EchoDataBull("host", context.Request.Host.ToString());
            strHtml += EchoDataBull("ishttps", context.Request.IsHttps.ToString());
            strHtml += EchoDataBull("method", context.Request.Method.ToString());
            strHtml += EchoDataBull("path", context.Request.Path.ToString());
            strHtml += EchoDataBull("pathbase", context.Request.PathBase.ToString());
            strHtml += EchoDataBull("pathbase", context.Request.Protocol.ToString());
            strHtml += EchoDataBull("pathbase", context.Request.QueryString.ToString());
            strHtml += EchoDataBull("scheme", context.Request.Scheme.ToString());

            strHtml += "Headers: <br/>";
            foreach (var key in context.Request.Headers.Keys)
            {
                strHtml += EchoDataBull(key, $"{context.Request.Headers[key]}");
            }

            strHtml += "Connection:<br/>";
            strHtml += EchoDataBull("localipaddress", context.Connection.LocalIpAddress?.ToString() ?? "");
            strHtml += EchoDataBull("localport", context.Connection.LocalPort.ToString());
            strHtml += EchoDataBull("remoteipaddress", context.Connection.RemoteIpAddress?.ToString() ?? "");
            strHtml += EchoDataBull("remoteport", context.Connection.RemotePort.ToString());

            strHtml += "<hr/>";
            strHtml += "<a href='/'>Home</a>" + "<br/>";
            strHtml += "</body></html>";

            return strHtml;
        }
    }
}
