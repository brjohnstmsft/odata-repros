using System.Web.Http;
using System.Web.OData;

namespace Repro
{
    public class DocsController : ODataController
    {
        [HttpPost]
        public IEdmEntityObject Post([FromBody] IEdmEntityObject doc) => doc;
    }
}
