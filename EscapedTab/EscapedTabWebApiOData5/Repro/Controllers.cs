using System.Web.Http;
using System.Web.OData;

namespace Repro
{
    public class IndexersController : ODataController
    {
        [HttpPost]
        public Indexer Post([FromBody] Indexer indexer) => indexer;
    }
}
