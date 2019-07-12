using System.Web.Http;
using Microsoft.AspNet.OData;

namespace Repro
{
    public class DocsController : ODataController
    {
        [HttpPost]
        public IEdmEntityObject Post([FromBody] IEdmEntityObject doc) => doc;

        [HttpPost]
        public IHttpActionResult UploadAddresses(ODataUntypedActionParameters parameters) => Ok();
    }
}
