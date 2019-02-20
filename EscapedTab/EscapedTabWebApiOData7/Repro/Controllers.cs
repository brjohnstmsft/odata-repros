using System.Web.Http;
using Microsoft.AspNet.OData;

namespace Repro
{
    public class IndexersController : ODataController
    {
        [HttpPost]
        public Indexer Post([FromBody] Indexer indexer) => indexer;
    }
}
