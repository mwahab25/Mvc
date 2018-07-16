// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    internal readonly struct DeclaredApiResponseMetadata
    {
        public DeclaredApiResponseMetadata(int statusCode, AttributeData attributeData, IMethodSymbol attributeSource)
        {
            StatusCode = statusCode;
            Attribute = attributeData;
            AttributeSource = attributeSource;
        }

        public int StatusCode { get; }

        public AttributeData Attribute { get; }

        public IMethodSymbol AttributeSource { get; }

        internal static bool HasStatusCode(IList<DeclaredApiResponseMetadata> declaredApiResponseMetadata, ActualApiResponseMetadata actualMetadata)
        {
            if (declaredApiResponseMetadata.Count == 0)
            {
                // When no status code is declared, a 200 OK is implied.
                return actualMetadata.IsDefaultResponse || actualMetadata.StatusCode == 200;
            }

            for (var i = 0; i < declaredApiResponseMetadata.Count; i++)
            {
                var declaredMetadata = declaredApiResponseMetadata[i];

                if (actualMetadata.IsDefaultResponse)
                {
                    if (declaredMetadata.StatusCode == 200 || declaredMetadata.StatusCode == 201)
                    {
                        return true;
                    }
                }
                else if (actualMetadata.StatusCode == declaredMetadata.StatusCode)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
