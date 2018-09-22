using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.OData.Edm;

namespace Repro
{
    public class IndexesController : ODataController
    {
        [HttpPost]
        public Index Post([FromBody] Index index) => index;
    }

    public class DocsController : ODataController
    {
        // Not thread-safe to keep things simple.
        private static readonly List<IEdmEntityObject> Documents = new List<IEdmEntityObject>();

        [HttpGet]
        public EdmEntityObjectCollection Get()
        {
            IEdmModel model = Request.GetModel();
            IEdmCollectionTypeReference collectionType = model.GetDocEntityCollectionTypeRef();
            return new EdmEntityObjectCollection(collectionType, Documents);
        }

        [HttpPost]
        public void Index([FromBody] ODataUntypedActionParameters parameters)
        {
            IEdmModel model = Request.GetModel();
            IEdmEntityTypeReference docTypeRef = model.GetDocTypeRef();
            IEdmEntityType docType = docTypeRef.EntityDefinition();

            IEdmEntityObject CopyDocument(IEdmComplexObject source)
            {
                var target = new EdmEntityObject(docTypeRef);

                foreach (string propertyName in docType.StructuralProperties().Select(p => p.Name))
                {
                    if (source.TryGetPropertyValue(propertyName, out object propertyValue))
                    {
                        target.TrySetPropertyValue(propertyName, propertyValue);
                    }
                }

                return target;
            }

            var documentsObjects = (EdmComplexObjectCollection)parameters["value"];
            IEnumerable<IEdmEntityObject> documents = documentsObjects.Select(CopyDocument);
            Documents.AddRange(documents);
        }

        [HttpDelete]
        public void Delete([FromUri] string key)
        {
            // Ignore the key; just delete everything.
            Documents.Clear();
        }
    }
}
