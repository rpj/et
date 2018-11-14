using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ET.Controllers
{
    [Serializable]
    public struct APIv1Post
    {
        public Guid Id;
        public DateTime Timestamp;
        public string Data;
    }

    [Route("api/[controller]")]
    [ApiController]
    public class V1Controller : ControllerBase
    {
        private TableStorageController _tsc;
        public V1Controller(IConfiguration config)
        {
            _tsc = new TableStorageController(config);
        }

        [HttpPost]
        public void Post([FromBody] APIv1Post value)
        {
            _tsc.Add(new TableStorageEntity(value.Id, value.Timestamp)
            {
                Data = value.Data
            });
        }
    }
}
