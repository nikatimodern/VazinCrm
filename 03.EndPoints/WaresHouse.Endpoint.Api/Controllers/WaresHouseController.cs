using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wares.Core.Domain.WaresHouses.Entities;
using Wares.Infrastructures.Sql;

namespace Wares.Endpoint.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WaresHouseController : Controller
    {
        private readonly WaresHouseContextDb db;

        public WaresHouseController(WaresHouseContextDb db)
        {
            this.db = db;
        }
        [HttpGet]
        public async Task<IEnumerable<WaresHouse>> Get()
        {
            var data = await db.WaresHouses.ToListAsync();
            return data;
        }



    }
}
