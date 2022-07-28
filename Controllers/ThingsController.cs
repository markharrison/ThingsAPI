using System;
using System.Threading.Tasks;
using ThingsAPI.Models;
using ThingsAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Diagnostics;

namespace ThingsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ThingsController : ControllerBase
    {
        private readonly ThingService _ThingService;
        private readonly IHubContext<NotifyHub> _HubContext;
        private readonly IMemoryCache _MemoryCache;

        public ThingsController(ThingService ThingService, IHubContext<NotifyHub> HubContext, IMemoryCache MemoryCache)
        {
            _ThingService = ThingService;
            _HubContext = HubContext;
            _MemoryCache = MemoryCache;
        }

        private async Task<IEnumerable<ThingItem>> DoGetAllThingsAsync()
        {
            IEnumerable<ThingItem> _ThingList;
            if (_MemoryCache.TryGetValue("GetAll", out _ThingList))
            {
                return _ThingList;
            }
            _ThingList = await _ThingService.GetAll();
            _MemoryCache.Set("GetAll", _ThingList, TimeSpan.FromMinutes(1));
            return _ThingList;
        }

        private void DoResetCache()
        {
            _MemoryCache.Remove("GetAll");
        }

        private ProblemDetails DoValidateThingItem(ThingItem Thing)
        {
            if (Thing.Latitude != null)
            {
                double numLat = Convert.ToDouble(Thing.Latitude);
                if (numLat > 90.0 || numLat < -90.0)
                {
                    return new ProblemDetails { Status = 422, Title = "Unprocessable Entity - Latitude must be between -90.0 and 90.0" };
                }
            }

            if (Thing.Longitude != null)
            {
                double numLong = Convert.ToDouble(Thing.Longitude);
                if (numLong > 180.0 || numLong < -180.0)
                {
                    return new ProblemDetails { Status = 422, Title = "Unprocessable Entity - Longitude must be between -180.0 and 180.0" };
                }
            }


            if ((Thing.Longitude != null && Thing.Latitude == null) || (Thing.Latitude != null && Thing.Longitude == null))
            {
                return new ProblemDetails { Status = 422, Title = "Unprocessable Entity - Must update both Latitude and Longitude simulatenously" };
            }

            if (Thing.Status != null && Thing.Status != "green" && Thing.Status != "red" && Thing.Status != "amber")
            {
                return new ProblemDetails { Status = 422, Title = "Unprocessable Entity - Status must be 'green' | 'red' | 'amber'" };
            }

            if (Thing.Image != null && Thing.Image != "")
            {
                if (!(Thing.Image.StartsWith("https://") || Thing.Image.StartsWith("http://")))
                {
                    return new ProblemDetails { Status = 422, Title = "Unprocessable Entity - Image must be a valid URL" };
                }
            }

            return null;
        }

        private async Task<IActionResult> DoInsertThingAsync(string id, ThingItem ThingUpdate)
        {
            ThingItem _Thing = new ThingItem
            {
                Thingid = Convert.ToInt64(id)
            };

            if (ThingUpdate.Latitude == null || ThingUpdate.Longitude == null)
            {
                return UnprocessableEntity(new ProblemDetails { Status = 422, Title = "Unprocessable Entity - Latitude/Longitude needed to create new Thing'" });
            }

            _Thing.Name = (ThingUpdate.Name ?? "Thing " + id).Trim();
            _Thing.Longitude = (double)ThingUpdate.Longitude;
            _Thing.Latitude = (double)ThingUpdate.Latitude;
            _Thing.Text = ThingUpdate.Text ?? "";
            _Thing.Status = ThingUpdate.Status ?? "green";
            _Thing.Image = ThingUpdate.Image ?? "";
            _Thing.Data = ThingUpdate.Data ?? "";

            await _ThingService.UpdateById(id, _Thing);

            DoResetCache();

            await _HubContext.Clients.All.SendAsync("BroadcastThingUpdate", _Thing);

            return Created("Things/" + id, _Thing);
        }

        private async Task<IActionResult> DoUpdateThingAsync(string id, ThingItem ThingUpdate, ThingItem ThingStored)
        {
            ThingItem _Thing = new ThingItem
            {
                Thingid = Convert.ToInt64(id)
            };

            _Thing.Name = (ThingUpdate.Name ?? ThingStored.Name).Trim();
            _Thing.Longitude = ThingUpdate.Longitude != null ? (double)ThingUpdate.Longitude : ThingStored.Longitude;
            _Thing.Latitude = ThingUpdate.Latitude != null ? (double)ThingUpdate.Latitude : ThingStored.Latitude;
            _Thing.Text = ThingUpdate.Text ?? ThingStored.Text;
            _Thing.Status = ThingUpdate.Status ?? ThingStored.Status;
            _Thing.Image = ThingUpdate.Image ?? ThingStored.Image;
            _Thing.Data = ThingUpdate.Data ?? ThingStored.Data;

            await _ThingService.UpdateById(id, _Thing);

            DoResetCache();

            await _HubContext.Clients.All.SendAsync("BroadcastThingUpdate", _Thing);

            return Ok(_Thing);
        }
        private async Task<string> DoGetNextFreeIdAsync()
        {
            long freeThingid = 1;
            var _ThingList = await _ThingService.GetAll();
            foreach (ThingItem _Thing in _ThingList)
            {
                if (_Thing.Thingid != freeThingid)
                {
                    return freeThingid.ToString();
                }
                freeThingid++;
            }
            if (freeThingid <= 1000)
            {
                return freeThingid.ToString();
            }
            return null;
        }

        [HttpGet(Name = "GetThings")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {

            var _ThingList = await DoGetAllThingsAsync();

            return Ok(_ThingList);
        }

        [HttpGet("{id}", Name = "GetThingsById")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(string id)
        {
            ThingItem _Thing = await _ThingService.FindById(id);
            if (_Thing == null)
            {
                return NotFound(new ProblemDetails { Status = 404, Title = "Not Found" });
            }
            return Ok(_Thing);
        }


        [HttpPost(Name = "UpdateThings")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Update([FromBody] ThingItem ThingUpdate)
        {
            if (ThingUpdate == null)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = 400,
                    Title = "Bad Request"
                });
            }

            ProblemDetails problemVal = DoValidateThingItem(ThingUpdate);
            if (problemVal != null)
                return UnprocessableEntity(problemVal);

            ThingItem ThingStored = await _ThingService.FindByNameLocation(ThingUpdate);
            if (ThingStored == null)
            {
                string id = await DoGetNextFreeIdAsync();
                if (id == null)
                {
                    return UnprocessableEntity(new ProblemDetails { Status = 422, Title = "Unprocessable Entity - No Free Id'" });
                }
                return await DoInsertThingAsync(id, ThingUpdate);
            }
            else
            {
                string id = ThingStored.Thingid.ToString();
                return await DoUpdateThingAsync(id, ThingUpdate, ThingStored);
            }

        }

        [HttpPut("{id}", Name = "UpdateThingsById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Update(string id, [FromBody] ThingItem ThingUpdate)
        {
            if (ThingUpdate == null)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = 400,
                    Title = "Bad Request"
                });
            }

            long numId = Convert.ToInt64(id);
            if (numId == 0)
            {
                return await Update(ThingUpdate); // Special - treat as Post 
            }
            if (numId < 0 || numId > 1000)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = 400,
                    Title = "Bad Request - {id} must be between 0 and 1000"
                });
            }

            ProblemDetails problemVal = DoValidateThingItem(ThingUpdate);
            if (problemVal != null)
                return UnprocessableEntity(problemVal);

            ThingItem ThingStored = await _ThingService.FindById(id);

            if (ThingStored == null)
                return await DoInsertThingAsync(id, ThingUpdate);
            else
                return await DoUpdateThingAsync(id, ThingUpdate, ThingStored);

        }


        [HttpDelete("{id}", Name = "DeleteThingsById")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteById(string id)
        {
            long numId = Convert.ToInt64(id);
            if (numId < 0 || numId > 1000)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = 400,
                    Title = "Bad Request - {id} must be between 0 and 1000"
                });
            }

            ThingItem Thing = await _ThingService.FindById(id);
            if (Thing == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = 404,
                    Title = "Not Found"
                });
            }

            await _ThingService.DeleteById(id);

            DoResetCache();

            await _HubContext.Clients.All.SendAsync("BroadcastThingDelete", Thing);

            return NoContent();

        }

        [HttpDelete(Name = "DeleteThings")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteAll()
        {
            await _ThingService.DeleteAll();

            DoResetCache();

            await _HubContext.Clients.All.SendAsync("BroadcastThingDeleteAll", "");

            return NoContent();
        }

    }
}

