// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.AspNetCore.Mvc.Analyzers.ExtractToApiConvention
{
    internal abstract class ExtractToConventionCodeFixStrategy
    {
        public static readonly ExtractToConventionCodeFixStrategy[] Strategies = new ExtractToConventionCodeFixStrategy[]
        {
            new UpdateExistingConventionMethodCodeFixStrategy(),
            new AddNewConventionMethodToExistingConventionCodeFixStrategy(),
        };

        public abstract Task ExecuteAsync(ExtractToConventionCodeFixStrategyContext context);

        protected static NameSyntax SimplifiedTypeName(string typeName)
        {
            return SyntaxFactory.ParseName(typeName).WithAdditionalAnnotations(Simplifier.Annotation);
        }

        protected static AttributeSyntax CreateProducesResponseTypeAttribute(ActualApiResponseMetadata metadata)
        {
            var statusCode = metadata.IsDefaultResponse ? 200 : metadata.StatusCode;

            var attribute = SyntaxFactory.Attribute(
                SimplifiedTypeName(SymbolNames.ProducesResponseTypeAttribute),
                SyntaxFactory.AttributeArgumentList().AddArguments(
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(statusCode)))));
            return attribute;
        }

        protected static AttributeSyntax CreateNameMatchAttribute(string nameMatchBehavior)
        {
            var attribute = SyntaxFactory.Attribute(
                SimplifiedTypeName(SymbolNames.ApiConventionNameMatchAttribute),
                SyntaxFactory.AttributeArgumentList().AddArguments(
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SimplifiedTypeName(SymbolNames.ApiConventionNameMatchBehavior),
                            SyntaxFactory.IdentifierName(nameMatchBehavior)))));
            return attribute;
        }

        protected static AttributeSyntax CreateTypeMatchAttribute(string typeMatchBehavior)
        {
            var attribute = SyntaxFactory.Attribute(
                SimplifiedTypeName(SymbolNames.ApiConventionTypeMatchAttribute),
                SyntaxFactory.AttributeArgumentList().AddArguments(
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SimplifiedTypeName(SymbolNames.ApiConventionTypeMatchBehavior),
                            SyntaxFactory.IdentifierName(typeMatchBehavior)))));
            return attribute;
        }

        protected internal static string GetConventionMethodName(string methodName)
        {
            // PostItem -> Post

            if (methodName.Length < 2)
            {
                return methodName;
            }

            for (var i = 1; i < methodName.Length; i++)
            {
                if (char.IsUpper(methodName[i]) && char.IsLower(methodName[i - 1]))
                {
                    return methodName.Substring(0, i);
                }
            }

            return methodName;
        }

        protected internal static string GetConventionParameterName(string parameterName)
        {
            // userName -> name

            if (parameterName.Length < 2)
            {
                return parameterName;
            }

            for (var i = parameterName.Length - 1; i > 0; i--)
            {
                if (char.IsUpper(parameterName[i]) && char.IsLower(parameterName[i - 1]))
                {
                    return parameterName.Substring(i);
                }
            }

            return parameterName;
        }
    }
}
