using System;
using System.Security.Cryptography;
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
        private readonly TableStorageController _tsc;
        private readonly RSA _rsaCrypt;

        public V1Controller(IConfiguration config)
        {
            _tsc = new TableStorageController(config);
            _rsaCrypt = RSA.Create();
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
