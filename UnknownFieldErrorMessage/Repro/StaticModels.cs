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

        public List<Analyzer> Analyzers { get; set; }
    }

    public abstract class Analyzer
    {
        public string Name { get; set; }
    }

    public sealed class CustomAnalyzer : Analyzer
    {
        public string TokenizerName { get; set; }

        public string[] TokenFilters { get; set; }
    }

    public static class StaticModel
    {
        public static IEdmModel BuildModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<Index>("Indexes");
            return builder.GetEdmModel();
        }
    }
}
