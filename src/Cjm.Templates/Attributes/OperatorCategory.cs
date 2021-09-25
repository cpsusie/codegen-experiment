using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using HpTimeStamps;

namespace Cjm.Templates.Attributes
{
    public readonly partial struct OperatorSpecifier 
        : IEquatable<OperatorSpecifier>, IComparable<OperatorSpecifier>
    {
        #region Static Predefined Values
        public static readonly OperatorSpecifier UnaryPlus = new(OperatorForm.Unary, OperatorCategory.Arithmetic,
            OperatorName.UnaryPlus);
        public static readonly OperatorSpecifier UnaryMinus = new(OperatorForm.Unary, OperatorCategory.Arithmetic,
            OperatorName.UnaryMinus);
        public static readonly OperatorSpecifier Increment = new(OperatorForm.Unary, OperatorCategory.IncDec,
            OperatorName.Increment);
        public static readonly OperatorSpecifier Decrement = new(OperatorForm.Unary, OperatorCategory.IncDec,
            OperatorName.Decrement);
        public static readonly OperatorSpecifier ExplicitConversion =
            new(OperatorForm.Unary, OperatorCategory.Casting, OperatorName.ExplicitConversion);
        public static readonly OperatorSpecifier ImplicitConversion =
            new(OperatorForm.Unary, OperatorCategory.Casting, OperatorName.ImplicitConversion);
        public static readonly OperatorSpecifier CheckEquals = new(OperatorForm.Binary, OperatorCategory.Equality,
            OperatorName.CheckEquals);
        public static readonly OperatorSpecifier CheckNotEquals = new(OperatorForm.Binary, OperatorCategory.Equality,
            OperatorName.CheckNotEquals);
        public static readonly OperatorSpecifier GreaterThan = new(OperatorForm.Binary, OperatorCategory.Relational,
            OperatorName.GreaterThan);
        public static readonly OperatorSpecifier GreaterThanOrEqual = new(OperatorForm.Binary, OperatorCategory.Relational,
            OperatorName.GreaterThanOrEqual);
        public static readonly OperatorSpecifier LessThan = new(OperatorForm.Binary, OperatorCategory.Relational,
            OperatorName.LessThan);
        public static readonly OperatorSpecifier LessThanOrEqualTo = new(OperatorForm.Binary, OperatorCategory.Relational,
            OperatorName.LessThanOrEqual);
        public static readonly OperatorSpecifier BitwiseAnd = new(OperatorForm.Binary, OperatorCategory.BitwiseLogic,
            OperatorName.BitwiseAnd);
        public static readonly OperatorSpecifier BitwiseOr = new(OperatorForm.Binary, OperatorCategory.BitwiseLogic,
            OperatorName.BitwiseOr);
        public static readonly OperatorSpecifier BitwiseXor = new(OperatorForm.Binary, OperatorCategory.BitwiseLogic,
            OperatorName.BitwiseXor);
        public static readonly OperatorSpecifier BitwiseNot = new(OperatorForm.Unary, OperatorCategory.BitwiseLogic,
            OperatorName.BitwiseNot);
        public static readonly OperatorSpecifier LeftShift = new(OperatorForm.Binary, OperatorCategory.BitShift,
            OperatorName.LeftShift);
        public static readonly OperatorSpecifier RightShift = new(OperatorForm.Binary, OperatorCategory.BitShift,
            OperatorName.RightShift);

        public static readonly OperatorSpecifier Addition = new(OperatorForm.Binary, OperatorCategory.Arithmetic,
            OperatorName.Addition);
        public static readonly OperatorSpecifier Subtraction = new(OperatorForm.Binary, OperatorCategory.Arithmetic,
            OperatorName.Subtraction);
        public static readonly OperatorSpecifier Multiplication = new(OperatorForm.Binary, OperatorCategory.Arithmetic,
            OperatorName.Multiplication);
        public static readonly OperatorSpecifier Division = new(OperatorForm.Binary, OperatorCategory.Arithmetic,
            OperatorName.Division);
        public static readonly OperatorSpecifier Modulus = new(OperatorForm.Binary, OperatorCategory.Arithmetic,
            OperatorName.Modulus);
        #endregion

        #region Conversion Operators
        public static implicit operator OperatorSpecifier(OperatorName name) => SpecifierByNameLookup[name];
        public static implicit operator OperatorName(OperatorSpecifier specifier) => specifier.Name; 
        #endregion

        #region Public Properties
        public OperatorName Name => _name;
        public OperatorCategory Category => _category;
        public OperatorForm Form => _form; 
        #endregion

        #region Private and Static CTORS
        private OperatorSpecifier(OperatorForm f, OperatorCategory c, OperatorName n)
        {
            _form = f.ValueOrThrowIfNDef(nameof(f));
            _category = c.ValueOrThrowIfNDef(nameof(c));
            _name = n.ValueOrThrowIfNDef(nameof(n));
        }

        static OperatorSpecifier()
        {
            OperatorNameComparer theNameComparer = OperatorNameComparer.CreateInstance();
            var dictBldr = ImmutableDictionary.CreateBuilder<OperatorName, OperatorSpecifier>(theNameComparer);

            dictBldr.Add(UnaryPlus.Name, UnaryPlus);
            dictBldr.Add(UnaryMinus.Name, UnaryMinus);
            dictBldr.Add(Increment.Name, Increment);
            dictBldr.Add(Decrement.Name, Decrement);

            dictBldr.Add(ExplicitConversion.Name, ExplicitConversion);
            dictBldr.Add(ImplicitConversion.Name, ImplicitConversion);
            dictBldr.Add(CheckEquals.Name, CheckEquals);
            dictBldr.Add(CheckNotEquals.Name, CheckNotEquals);

            dictBldr.Add(GreaterThan.Name, GreaterThan);
            dictBldr.Add(GreaterThanOrEqual.Name, GreaterThanOrEqual);
            dictBldr.Add(LessThan.Name, LessThan);
            dictBldr.Add(LessThanOrEqualTo.Name, LessThanOrEqualTo);

            dictBldr.Add(BitwiseAnd.Name, BitwiseAnd);
            dictBldr.Add(BitwiseOr.Name, BitwiseOr);
            dictBldr.Add(BitwiseXor.Name, BitwiseXor);
            dictBldr.Add(BitwiseNot.Name, BitwiseNot);

            dictBldr.Add(LeftShift.Name, LeftShift);
            dictBldr.Add(RightShift.Name, RightShift);

            dictBldr.Add(Addition.Name, Addition);
            dictBldr.Add(Subtraction.Name, Subtraction);
            dictBldr.Add(Multiplication.Name, Multiplication);
            dictBldr.Add(Division.Name, Division);
            dictBldr.Add(Modulus.Name, Modulus);
            
            Debug.Assert(dictBldr.Count == OperatorEnumExtensions.DefinedNames.Length);
            SpecifierByNameLookup = dictBldr.ToImmutable();
        } 
        #endregion

        #region Public Methods and Operators
        public override int GetHashCode()
        {
            int hash = (byte)_name;
            unchecked
            {
                hash = (hash * 397) ^ ((byte)_category);
                hash = (hash * 397) ^ ((byte)_form);
            }
            return hash;
        }

        public static int Compare(OperatorSpecifier l, OperatorSpecifier r)
        {
            int ret = OperatorEnumExtensions.CompareTo(l._name, r._name);
            if (ret == 0)
            {
                ret = OperatorEnumExtensions.CompareTo(l._category, r._category);
                ret = ret == 0 ? OperatorEnumExtensions.CompareTo(l._form, r._form) : ret;
            }
            return ret;
        }

        public static bool operator ==(OperatorSpecifier l, OperatorSpecifier r) =>
            l._form == r._form && l._name == r._name && l._category == r._category;
        public static bool operator !=(OperatorSpecifier l, OperatorSpecifier r) =>
            !(l == r);
        public static bool operator >(OperatorSpecifier l, OperatorSpecifier r)
            => Compare(l, r) > 0;
        public static bool operator <(OperatorSpecifier l, OperatorSpecifier r)
            => Compare(l, r) < 0;
        public static bool operator >=(OperatorSpecifier l, OperatorSpecifier r)
            => !(l < r);
        public static bool operator <=(OperatorSpecifier l, OperatorSpecifier r)
            => !(l > r);
        public int CompareTo(OperatorSpecifier other) => Compare(this, other);
        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is OperatorSpecifier os && os == this;
        public bool Equals(OperatorSpecifier other)
            => other == this;
        /// <inheritdoc />
        public override string ToString() =>
            $"[{nameof(OperatorSpecifier)}] -- Name: \t[{Name}]; " +
            $"\tForm: \t[{Form}]; \tCategory: \t[{Category}]."; 
        #endregion

        #region Private Data
        private readonly OperatorForm _form;
        private readonly OperatorCategory _category;
        private readonly OperatorName _name;
        private static readonly ImmutableDictionary<OperatorName, OperatorSpecifier> SpecifierByNameLookup;
        #endregion
    }


    #region Enum Definitions
    public enum OperatorForm : byte
    {
        Unary = 0,
        Binary
    }

    public enum OperatorCategory : byte
    {
        Arithmetic = 0,
        Casting,
        Equality,
        Relational,
        BitwiseLogic,
        BitShift,
        IncDec,

    }

    public enum OperatorName : byte
    {
        UnaryPlus = 0,
        UnaryMinus,
        Increment,
        Decrement,
        ExplicitConversion,
        ImplicitConversion,
        CheckEquals,
        CheckNotEquals,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
        BitwiseAnd,
        BitwiseOr,
        BitwiseXor,
        BitwiseNot,
        LeftShift,
        RightShift,
        Addition,
        Subtraction,
        Multiplication,
        Division,
        Modulus
    } 
    #endregion

    #region Enum Extension Methods and Helpers
    public static class OperatorEnumExtensions
    {
        public static readonly ImmutableArray<OperatorCategory> DefinedCategories;
        public static readonly ImmutableArray<OperatorForm> DefinedForms;
        public static readonly ImmutableArray<OperatorName> DefinedNames;

        public static bool IsDefined(this OperatorCategory category) => DefinedCategories.Contains(category);
        public static bool IsDefined(this OperatorForm form) => DefinedForms.Contains(form);
        public static bool IsDefined(this OperatorName name) => DefinedNames.Contains(name);

        public static OperatorCategory ValueOrThrowIfNDef(this OperatorCategory category, string? paramName) =>
            DefinedCategories.Contains(category)
                ? category
                : throw new UndefinedEnumArgumentException<OperatorCategory>(category, paramName ?? nameof(category));
        public static OperatorForm ValueOrThrowIfNDef(this OperatorForm form, string? paramName) => DefinedForms.Contains(form)
            ? form
            : throw new UndefinedEnumArgumentException<OperatorForm>(form, paramName ?? nameof(form));
        public static OperatorName ValueOrThrowIfNDef(this OperatorName name, string? paramName) => DefinedNames.Contains(name)
            ? name
            : throw new UndefinedEnumArgumentException<OperatorName>(name, paramName ?? nameof(name));

        public static int CompareTo(this OperatorForm f, OperatorForm of) => ((byte)f).CompareTo((byte)of);
        public static int CompareTo(this OperatorName n, OperatorName on) => ((byte)n).CompareTo((byte)on);
        public static int CompareTo(this OperatorCategory c, OperatorCategory oc) => ((byte)c).CompareTo((byte)oc);

        static OperatorEnumExtensions()
        {
            DefinedCategories = Enum.GetValues(typeof(OperatorCategory)).Cast<OperatorCategory>().ToImmutableArray();
            DefinedForms = Enum.GetValues(typeof(OperatorForm)).Cast<OperatorForm>().ToImmutableArray();
            DefinedNames = Enum.GetValues(typeof(OperatorName)).Cast<OperatorName>().ToImmutableArray();
        }
    } 
    #endregion

    #region Nested Type Def
    partial struct OperatorSpecifier
    {
        internal sealed class OperatorNameComparer : EqualityComparer<OperatorName>, IComparer<OperatorName>
        {
            internal static OperatorNameComparer CreateInstance() => new();
            /// <inheritdoc />
            public override bool Equals(OperatorName x, OperatorName y) => x == y;

            /// <inheritdoc />
            public override int GetHashCode(OperatorName obj) => (byte)obj;

            /// <inheritdoc />
            public int Compare(OperatorName x, OperatorName y)
                => ((byte)x).CompareTo((byte)y);

            private OperatorNameComparer() { }
        }
    } 
    #endregion
}
