// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.AspNetCore.Mvc.Analyzers.ExtractToApiConvention
{
    internal sealed class AddNewConventionMethodToExistingConventionCodeFixStrategy : ExtractToConventionCodeFixStrategy
    {
        public override async Task ExecuteAsync(ExtractToConventionCodeFixStrategyContext context)
        {
            var (result, conventionTypeSyntax) = await TryGetExistingConventionType(context);
            if (!result)
            {
                return;
            }

            var semanticModel = context.SemanticModel;
            var editor = await DocumentEditor.CreateAsync(context.Document, context.CancellationToken).ConfigureAwait(false);

            var conventionMethodAttributes = new SyntaxList<AttributeListSyntax>();

            foreach (var attributeList in context.MethodSyntax.AttributeLists)
            {
                foreach (var attributeSyntax in attributeList.Attributes)
                {
                    var attributeSymbol = semanticModel.GetSymbolInfo(attributeSyntax, context.CancellationToken);
                    if (attributeSymbol.Symbol == null ||
                        attributeSymbol.Symbol.Kind != SymbolKind.Method)
                    {
                        continue;
                    }

                    var methodSymbol = (IMethodSymbol)attributeSymbol.Symbol;
                    if (methodSymbol.MethodKind != MethodKind.Constructor ||
                        methodSymbol.ContainingType != context.SymbolCache.ProducesResponseTypeAttribute)
                    {
                        continue;
                    }

                    editor.RemoveNode(attributeSyntax);
                    conventionMethodAttributes = conventionMethodAttributes.Add(SyntaxFactory.AttributeList().AddAttributes(attributeSyntax));
                }
            }

            var producesResponseTypeName = SimplifiedTypeName(SymbolNames.ProducesResponseTypeAttribute);
            foreach (var metadata in context.UndocumentedMetadata)
            {
                var attributeSyntax = CreateProducesResponseTypeAttribute(metadata);
                conventionMethodAttributes = conventionMethodAttributes.Add(SyntaxFactory.AttributeList().AddAttributes(attributeSyntax));
            }

            var nameMatchBehaviorAttribute = CreateNameMatchAttribute(SymbolNames.ApiConventionNameMatchBehavior_Prefix);
            conventionMethodAttributes = conventionMethodAttributes.Add(SyntaxFactory.AttributeList().AddAttributes(nameMatchBehaviorAttribute));

            var voidType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
            var methodName = GetConventionMethodName(context.Method.Name);

            var conventionParameterList = SyntaxFactory.ParameterList();
            foreach (var parameter in context.Method.Parameters)
            {
                var parameterName = GetConventionParameterName(parameter.Name);
                var parameterType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword));

                var parameterNameMatchBehaviorAttribute = CreateNameMatchAttribute(SymbolNames.ApiConventionNameMatchBehavior_Suffix);
                var parameterTypeMatchBehaviorAttribute = CreateTypeMatchAttribute(SymbolNames.ApiConventionTypeMatchBehavior_Any);

                var conventionParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameterName))
                    .WithType(parameterType.WithAdditionalAnnotations(Simplifier.Annotation))
                    .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(parameterNameMatchBehaviorAttribute, parameterTypeMatchBehaviorAttribute));

                conventionParameterList = conventionParameterList.AddParameters(conventionParameter);
            }

            var method = SyntaxFactory.MethodDeclaration(voidType, methodName)
               .WithAttributeLists(conventionMethodAttributes)
               .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
               .WithBody(SyntaxFactory.Block())
               .WithParameterList(conventionParameterList);

            editor.AddMember(conventionTypeSyntax, method);

            context.DocumentEditor = editor;
        }

        private async Task<(bool, TypeDeclarationSyntax)> TryGetExistingConventionType(ExtractToConventionCodeFixStrategyContext context)
        {
            var conventionTypes = SymbolApiResponseMetadataProvider.GetConventionTypes(context.SymbolCache, context.Method);
            foreach (var conventionType in conventionTypes)
            {
                var syntaxReferences = conventionType.DeclaringSyntaxReferences;
                if (syntaxReferences.Length == 0)
                {
                    continue;
                }

                var typeDeclarationSyntax = (TypeDeclarationSyntax)await syntaxReferences[0].GetSyntaxAsync(context.CancellationToken);
                if (typeDeclarationSyntax.GetLocation().IsInSource)
                {
                    return (true, typeDeclarationSyntax);
                }
            }

            return (false, null);
        }
    }
}
