using System.Web.Http;
using Microsoft.AspNet.OData;

namespace Repro
{
    public class IndexesController : ODataController
    {
        [HttpPost]
        public Index Post([FromBody] Index index) => index;
    }
}
