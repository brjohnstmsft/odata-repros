using System.Web.Http;
using System.Web.OData;

namespace Repro
{
    public class IndexesController : ODataController
    {
        [HttpPost]
        public Index Post([FromBody] Index index) => index;
    }
}
