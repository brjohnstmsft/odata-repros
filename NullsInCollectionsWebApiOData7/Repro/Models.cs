using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.OData.Builder;
using Microsoft.OData.Edm;

namespace Repro
{
    public class Field
    {
        public string Name { get; set; }

        public string Type { get; set; }
    }

    public class Index
    {
        [Key]
        public string Name { get; set; }

        public IList<Field> Fields { get; set; }
    }

    public static class DynamicModel
    {
        private const string Namespace = "search";
        private const string DocEntityTypeName = "document";
        private const string DocComplexTypeName = "documentFields";
        private const string FullDocEntityTypeName = Namespace + "." + DocEntityTypeName;
        private const string FullDocComplexTypeName = Namespace + "." + DocComplexTypeName;

        public static IEdmCollectionTypeReference GetDocEntityCollectionTypeRef(this IEdmModel model) =>
            EdmCoreModel.GetCollection(model.GetDocTypeRef());

        public static IEdmEntityTypeReference GetDocTypeRef(this IEdmModel model)
        {
            var docType = (IEdmEntityType)model.FindDeclaredType(FullDocEntityTypeName);
            return new EdmEntityTypeReference(docType, isNullable: false);
        }

        public static IEdmModel AddDynamicModel(this EdmModel model)
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

            var docComplexType = new EdmComplexType(Namespace, DocComplexTypeName);

            foreach (IEdmStructuralProperty property in docEntityType.StructuralProperties())
            {
                docComplexType.AddStructuralProperty(property.Name, property.Type);
            }

            var valueParameterTypeRef =
                EdmCoreModel.GetCollection(new EdmComplexTypeReference(docComplexType, isNullable: false));

            var container = (EdmEntityContainer)model.EntityContainer;
            container.AddEntitySet("Docs", docEntityType);

            var indexAction =
                new EdmAction(
                    Namespace,
                    "Index",
                    returnType: null,
                    isBound: true,
                    entitySetPathExpression: null);

            indexAction.AddParameter("bindingParameter", docEntitySetType);
            indexAction.AddParameter("value", valueParameterTypeRef);

            var indexActionImport = new EdmActionImport(container, indexAction.Name, indexAction);

            model.AddElements(new IEdmSchemaElement[] { addressType, docEntityType, docComplexType, indexActionImport.Operation });
            return model;
        }
    }

    public static class StaticModel
    {
        public static EdmModel BuildModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<Index>("Indexes");
            return (EdmModel)builder.GetEdmModel();
        }
    }
}
