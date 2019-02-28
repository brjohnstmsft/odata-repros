using System.Web.Http;
using Microsoft.AspNet.OData;

namespace Repro
{
    public class IndexersController : ODataController
    {
        [HttpPost]
        public IHttpActionResult Post([FromBody] Indexer indexer)
        {
            if (indexer.Configuration.Properties.TryGetValue("test", out object value) && !(value is int))
            {
                return BadRequest($"Config item 'test': Expected int; Actual type '{value.GetType()}'");
            }

            return Ok(indexer);
        }
    }
}
