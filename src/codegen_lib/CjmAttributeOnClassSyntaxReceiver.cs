using System;
using LoggerLibrary;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cjm.CodeGen
{
    public abstract class CjmAttributeOnClassSyntaxReceiver<TTargetData> : ISyntaxReceiver where TTargetData : struct, ITargetData, IEquatable<TTargetData>
    {
        public bool HasTargetData => _alreadySet.IsSet && TargetData.HasValue;
        public TTargetData? TargetData => _targetData;
        protected bool AlreadySet => _alreadySet.IsSet;

        public void OnVisitSyntaxNode(SyntaxNode visited)
        {
            TTargetData? result = ExtractTargetDataFromNodeOrNot(visited);
            if (result.HasValue)
            {
                SetTargetData(ref result);
            }
        }

        protected abstract TTargetData? ExtractTargetDataFromNodeOrNot(SyntaxNode visited);

        private void SetTargetData(ref TTargetData? setMe)
        {
            _alreadySet.SetOrThrow();
            _targetData = setMe;
        }

        protected static bool IsPublicStaticClassDeclaration(ClassDeclarationSyntax cds)
        {
            bool foundPublic = false;
            bool foundStatic = false;
            foreach (var modifier in cds.Modifiers)
            {
                if (modifier.IsKind(SyntaxKind.PublicKeyword))
                {
                    foundPublic = true;
                }

                if (modifier.IsKind(SyntaxKind.StaticKeyword))
                {
                    foundStatic = true;
                }

                if (foundStatic && foundPublic)
                {
                    return true;
                }
            }

            return foundStatic && foundPublic;
        }

        protected static AttributeSyntax? FindExtensionsAttribute(ClassDeclarationSyntax cds, string attributeShortName)
        {
            foreach (var attribList in cds.AttributeLists)
            {
                foreach (var attrib in attribList.Attributes)
                {

                    if (attrib.Name.ToString() == attributeShortName)
                        return attrib;
                }
            }
            return null;
        }

        private LocklessSetOnlyFlag _alreadySet;
        private TTargetData? _targetData;
    }
}