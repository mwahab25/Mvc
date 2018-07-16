// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.AspNetCore.Mvc.Analyzers.ExtractToApiConvention
{
    internal class ExtractToConventionCodeFix : CodeAction
    {
        private readonly Document _document;
        private readonly Diagnostic _diagnostic;
        private bool _fixExecuted;
        private DocumentEditor _documentEditor;
        private SolutionEditor _solutionEditor;

        public ExtractToConventionCodeFix(Document document, Diagnostic diagnostic)
        {
            _document = document;
            _diagnostic = diagnostic;
        }

        public override string Title => "Extract to convention";

        protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            await CalculateFixAsync(cancellationToken);
            if (_documentEditor != null)
            {
                return _documentEditor?.GetChangedDocument();
            }

            return await base.GetChangedDocumentAsync(cancellationToken);
        }

        protected override async Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
        {
            await CalculateFixAsync(cancellationToken);
            if (_solutionEditor != null)
            {
                return _solutionEditor.GetChangedSolution();
            }

            return await base.GetChangedSolutionAsync(cancellationToken);
        }

        private async Task CalculateFixAsync(CancellationToken cancellationToken)
        {
            if (_fixExecuted)
            {
                return;
            }

            var root = await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await _document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var methodReturnStatement = (ReturnStatementSyntax)root.FindNode(_diagnostic.Location.SourceSpan);
            var methodSyntax = methodReturnStatement.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            var method = semanticModel.GetDeclaredSymbol(methodSyntax, cancellationToken);

            var symbolCache = new ApiControllerSymbolCache(semanticModel.Compilation);
            var declaredResponseMetadata = SymbolApiResponseMetadataProvider.GetDeclaredResponseMetadata(symbolCache, method);

            if (!SymbolApiResponseMetadataProvider.TryGetActualResponseMetadata(symbolCache, semanticModel, methodSyntax, cancellationToken, out var actualResponseMetadata))
            {
                // If we cannot parse metadata correctly, don't offer a code fix.
                return;
            }

            var undocumentedMetadata = new List<ActualApiResponseMetadata>();
            foreach (var metadata in actualResponseMetadata)
            {
                if (!DeclaredApiResponseMetadata.HasStatusCode(declaredResponseMetadata, metadata))
                {
                    undocumentedMetadata.Add(metadata);
                }
            }

            var context = new ExtractToConventionCodeFixStrategyContext(
                _document,
                semanticModel,
                symbolCache,
                method,
                methodSyntax,
                declaredResponseMetadata,
                undocumentedMetadata,
                cancellationToken);

            foreach (var strategy in ExtractToConventionCodeFixStrategy.Strategies)
            {
                await strategy.ExecuteAsync(context);
                if (context.Success)
                {
                    (_documentEditor, _solutionEditor) = (context.DocumentEditor, context.SolutionEditor);
                    break;
                }
            }

            _fixExecuted = true;
        }
    }
}
