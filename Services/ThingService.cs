using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ThingsAPI.Models;
using Azure.Data.Tables;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis.CSharp;
using Azure;
using System.Collections.Concurrent;

namespace ThingsAPI.Services
{
    public class ThingService
    {
        private TableClient _tableclient { get; set; }

        private readonly IConfiguration _config;

        public ThingService(IConfiguration config)
        {
            _config = config;

            var vCS = config.GetConnectionString("ThingsDbConnectionString");
            string tableName = "thingsdata";

            try
            {
                _tableclient = new TableClient(vCS, tableName);
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

        public async Task<ThingItem> FindByNameLocation(ThingItem Thing)
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
                    decimal deltaLongitude = Math.Abs((decimal)entity.Longitude - (decimal)Thing.Longitude);
                    decimal deltaLatitude = Math.Abs((decimal)entity.Latitude - (decimal)Thing.Latitude);
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



        public async Task<ThingItem> FindById(string id)
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
                    Longitude = (double)Thing.Longitude,
                    Latitude = (double)Thing.Latitude,
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

                if (batch.Count > 0) {
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


        public string GetAppConfigInfo(HttpContext context)
        {

            string pw = context.Request.Query["pw"].ToString();


            string strAppConfigInfoHtml = "";
            strAppConfigInfoHtml += "<html><head>";
            strAppConfigInfoHtml += "<style>";
            strAppConfigInfoHtml += "body { font-family: \"Segoe UI\",Roboto,\"Helvetica Neue\",Arial;}";
            strAppConfigInfoHtml += "</style>";
            strAppConfigInfoHtml += "</head><body>";
            strAppConfigInfoHtml += "<h3>ThingsAPI - AppConfigInfo </h3>";
            strAppConfigInfoHtml += "OS Description:  " + System.Runtime.InteropServices.RuntimeInformation.OSDescription + "<br/>";
            strAppConfigInfoHtml += "Framework Description:  " + System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription + "<br/>";
            strAppConfigInfoHtml += "BuildIdentifier:  " + _config.GetValue<string>("BuildIdentifier") + "<br/>";
            if (_config.GetValue<string>("AdminPW") == context.Request.Query["pw"].ToString())
            {
                strAppConfigInfoHtml += "ASPNETCORE_ENVIRONMENT:  " + _config.GetValue<string>("ASPNETCORE_ENVIRONMENT") + "<br/>";
                strAppConfigInfoHtml += "ApplicationInsights ConnectionString:  " + _config.GetValue<string>("ApplicationInsights:ConnectionString") + "<br/>";
                strAppConfigInfoHtml += "Datastore ConnectionString:  " + _config.GetConnectionString("ThingsDbConnectionString") + "<br/>";
            }

            strAppConfigInfoHtml += "<hr/>";
            strAppConfigInfoHtml += "<a href='/'>Home</a>" + "<br/>";
            strAppConfigInfoHtml += "</body></html>";

            return strAppConfigInfoHtml;
        }
    }
}
