using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Vocabularies;

namespace Repro
{
    public static class DynamicModel
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

        public static void AddToModel(IEdmModel model)
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

            var uploadAction = new EdmAction(Namespace, "UploadAddresses", returnType: null, isBound: true, entitySetPathExpression: null);
            uploadAction.AddParameter("bindingParameter", docEntitySetType);
            uploadAction.AddParameter("value", addressesTypeRef);

            var actionTerm = new EdmTerm(Namespace, "action", EdmPrimitiveTypeKind.String);

            var mutableModel = (EdmModel)model;
            mutableModel.AddElements(new IEdmSchemaElement[] { addressType, docEntityType, uploadAction, actionTerm });

            var container = (EdmEntityContainer)mutableModel.EntityContainer;
            container.AddEntitySet("Docs", docEntityType);
        }
    }
}
