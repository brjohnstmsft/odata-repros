using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.OData.Builder;
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

    public static class Model
    {
        public static IEdmModel BuildModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<Index>("Indexes");
            return builder.GetEdmModel();
        }
    }
}
