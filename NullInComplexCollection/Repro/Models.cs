using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;

namespace Repro
{
    public static class Model
    {
        private const string Namespace = "search";
        private const string DocEntityTypeName = "document";
        private const string FullDocEntityTypeName = Namespace + "." + DocEntityTypeName;

        public static IEdmCollectionTypeReference GetDocEntityCollectionTypeRef(this IEdmModel model) =>
            EdmCoreModel.GetCollection(model.GetDocTypeRef());

        public static IEdmEntityTypeReference GetDocTypeRef(this IEdmModel model)
        {
            var docType = (IEdmEntityType)model.FindDeclaredType(FullDocEntityTypeName);
            return new EdmEntityTypeReference(docType, isNullable: false);
        }

        public static IEdmModel Build()
        {
            var addressType = new EdmComplexType(Namespace, "address");
            addressType.AddStructuralProperty("street", EdmPrimitiveTypeKind.String);
            addressType.AddStructuralProperty("city", EdmPrimitiveTypeKind.String);

            var addressTypeRef = new EdmComplexTypeReference(addressType, isNullable: false);
            var addressesTypeRef = EdmCoreModel.GetCollection(addressTypeRef);

            var nonNullableStringTypeRef = EdmCoreModel.Instance.GetString(isNullable: false);
            var tagsTypeRef = EdmCoreModel.GetCollection(nonNullableStringTypeRef);

            var docEntityType = new EdmEntityType(Namespace, DocEntityTypeName);
            EdmStructuralProperty key = docEntityType.AddStructuralProperty("id", EdmPrimitiveTypeKind.String);
            docEntityType.AddKeys(key);
            docEntityType.AddStructuralProperty("addresses", addressesTypeRef);
            docEntityType.AddStructuralProperty("tags", tagsTypeRef);

            var docEntitySetType = EdmCoreModel.GetCollection(new EdmEntityTypeReference(docEntityType, isNullable: true));

            var container = new EdmEntityContainer("Default", "Container");
            container.AddEntitySet("Docs", docEntityType);

            var model = new EdmModel();
            model.AddElements(new IEdmSchemaElement[] { container, addressType, docEntityType });
            return model;
        }
    }
}
