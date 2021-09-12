using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cjm.CodeGen
{
    sealed class CdsComparer : Comparer<ClassDeclarationSyntax>
    {

        internal static CdsComparer GetComparer() => TheCdsComparer;
        public override int Compare(ClassDeclarationSyntax? l, ClassDeclarationSyntax? r)
        {
            if (ReferenceEquals(l, r)) return 0;
            if (ReferenceEquals(l, null)) return -1;
            if (ReferenceEquals(r, null)) return 1;

            return Compare(l.GetLocation(), r.GetLocation());
        }

        private CdsComparer(){}

        private static int Compare(Location? l, Location? r)
        {
            if (ReferenceEquals(l, r)) return 0;
            if (ReferenceEquals(l, null)) return -1;
            if (ReferenceEquals(r, null)) return 1;

            int ret;
            int kindComp = Compare(l.Kind, r.Kind);
            if (kindComp == 0)
            {
                FileLinePositionSpan lSpan = l.GetMappedLineSpan();
                FileLinePositionSpan rSpan = r.GetMappedLineSpan();
                int spanComp = Compare(ref lSpan, ref rSpan);
                ret = spanComp == 0 ? l.SourceSpan.CompareTo(r.SourceSpan) : spanComp;
            }
            else
            {
                ret = kindComp;
            }
            return ret;
        }

        private static int Compare(ref FileLinePositionSpan l, ref FileLinePositionSpan r)
        {
            int ret;
            int pathComp = ComparePaths(l.Path, r.Path);
            if (pathComp == 0)
            {
                int startPosComp = l.StartLinePosition.CompareTo(r.StartLinePosition);
                ret = startPosComp == 0 ? r.EndLinePosition.CompareTo(r.EndLinePosition) : startPosComp;
            }
            else
            {
                ret = pathComp;
            }

            return ret;
            static int ComparePaths(string? l, string? r) => StringComparer.OrdinalIgnoreCase.Compare(l, r);
            
        }

        private static int Compare(LocationKind l, LocationKind r) => (l, r) switch
        {
            var (x, y) when x == y => 0,
            var (x, y) when x > y => 1,
            _ => -1,
        };

        private static readonly CdsComparer TheCdsComparer = new CdsComparer();
    }
}