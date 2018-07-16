// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.AspNetCore.Mvc.Analyzers.ExtractToApiConvention
{
    internal class ExtractToConventionCodeFixStrategyContext
    {
        private DocumentEditor _documentEditor;
        private SolutionEditor _solutionEditor;

        public ExtractToConventionCodeFixStrategyContext(
            Document document,
            SemanticModel semanticModel,
            ApiControllerSymbolCache symbolCache,
            IMethodSymbol method,
            MethodDeclarationSyntax methodSyntax,
            IList<DeclaredApiResponseMetadata> declaredMetadata,
            IList<ActualApiResponseMetadata> undocumentedMetadata,
            CancellationToken cancellationToken)
        {
            Document = document;
            SemanticModel = semanticModel;
            SymbolCache = symbolCache;
            Method = method;
            MethodSyntax = methodSyntax;
            DeclaredApiResponseMetadata = declaredMetadata;
            UndocumentedMetadata = undocumentedMetadata;
            CancellationToken = cancellationToken;
        }

        public Document Document { get; }
        public SemanticModel SemanticModel { get; }
        public ApiControllerSymbolCache SymbolCache { get; }
        public IMethodSymbol Method { get; }
        public MethodDeclarationSyntax MethodSyntax { get; }
        public IList<DeclaredApiResponseMetadata> DeclaredApiResponseMetadata { get; }
        public IList<ActualApiResponseMetadata> UndocumentedMetadata { get; }
        public CancellationToken CancellationToken { get; }

        public DocumentEditor DocumentEditor
        {
            get => _documentEditor;
            set
            {
                _documentEditor = value;
                Success = true;
            }
        }

        public SolutionEditor SolutionEditor
        {
            get => _solutionEditor;
            set
            {
                _solutionEditor = value;
                Success = true;
            }
        }

        public bool Success { get; set; }
    }

}
