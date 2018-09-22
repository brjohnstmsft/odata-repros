// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;

namespace System.Xml.Linq
{
    public static class XElementExtensions
    {
        /// <summary>
        /// Removes whitespace, comments, and PI's. Orders attributes by namespace then local name
        /// </summary>
        public static XElement Normalize(this XElement element)
        {
            return new XElement(element.Name, element.NormalizedAttributes(), element.NormalizedNodes());
        }

        public static IEnumerable<XAttribute> NormalizedAttributes(this XElement element)
        {
            return element.Attributes().Where(a => !a.IsNamespaceDeclaration)
                                       .OrderBy(a => a.Name.NamespaceName)
                                       .ThenBy(a => a.Name.LocalName);
        }

        public static IEnumerable<XNode> NormalizedNodes(this XElement element)
        {
            foreach (XNode node in element.Nodes())
            {
                if (node is XComment ||
                   node is XProcessingInstruction)
                {
                    continue;
                }

                if (node is XElement)
                {
                    yield return ((XElement)node).Normalize();
                }
                else
                {
                    // Node is either XCData or XText
                    yield return node;
                }
            }
        }
    }
}
