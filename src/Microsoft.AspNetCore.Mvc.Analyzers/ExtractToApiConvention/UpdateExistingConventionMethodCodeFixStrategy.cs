// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.AspNetCore.Mvc.Analyzers.ExtractToApiConvention
{
    internal sealed class UpdateExistingConventionMethodCodeFixStrategy : ExtractToConventionCodeFixStrategy
    {
        public override async Task ExecuteAsync(ExtractToConventionCodeFixStrategyContext context)
        {
            var (result, methodSyntax) = await TryGetExistingConventionMethod(context);

            if (!result)
            {
                return;
            }

            var editor = await DocumentEditor.CreateAsync(context.Document, context.CancellationToken).ConfigureAwait(false);

            var attributeName = SimplifiedTypeName(SymbolNames.ProducesResponseTypeAttribute);
            foreach (var metadata in context.UndocumentedMetadata)
            {
                var attribute = CreateProducesResponseTypeAttribute(metadata);

                editor.AddAttribute(methodSyntax, attribute);
            }

            context.DocumentEditor = editor;
        }

        internal async Task<(bool, MethodDeclarationSyntax)> TryGetExistingConventionMethod(ExtractToConventionCodeFixStrategyContext context)
        {
            if (context.DeclaredApiResponseMetadata.Count == 0)
            {
                return (false, null);
            }

            var sourceMethod = context.DeclaredApiResponseMetadata[0].AttributeSource;
            if (context.Method == sourceMethod)
            {
                // The attribute is defined on the method. In this case, we need to create a new convention.
                return (false, null);
            }

            // Ensure that the convention exists in code.
            if (sourceMethod.DeclaringSyntaxReferences.Length != 1)
            {
                return (false, null);
            }

            var syntaxReference = sourceMethod.DeclaringSyntaxReferences[0];
            var syntaxToUpdate = (MethodDeclarationSyntax)await syntaxReference.GetSyntaxAsync(context.CancellationToken);
            return (syntaxToUpdate.GetLocation().IsInSource, syntaxToUpdate);
        }
    }
}
