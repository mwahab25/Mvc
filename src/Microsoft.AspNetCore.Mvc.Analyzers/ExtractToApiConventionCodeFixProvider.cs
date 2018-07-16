// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Analyzers.ExtractToApiConvention;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    [Shared]
    public class ExtractToApiConventionCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            DiagnosticDescriptors.MVC1004_ActionReturnsUndocumentedStatusCode.Id);

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (context.Diagnostics.Length == 0)
            {
                return Task.CompletedTask;
            }

            var diagnostic = context.Diagnostics[0];
            if (diagnostic.Descriptor != DiagnosticDescriptors.MVC1004_ActionReturnsUndocumentedStatusCode)
            {
                return Task.CompletedTask;
            }

            context.RegisterCodeFix(new ExtractToConventionCodeFix(context.Document, diagnostic), diagnostic);

            return Task.CompletedTask;
        }
    }
}
