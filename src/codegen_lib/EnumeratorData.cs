using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Cjm.CodeGen
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct EnumeratorData : IEquatable<EnumeratorData>, IComparable<EnumeratorData>
    {

        public static EnumeratorData IsIEnumeratorT { get; } = new(
            EnumeratorDataCode.EnumeratorIsInterfaceType | EnumeratorDataCode.EnumeratorIsReferenceType |
            EnumeratorDataCode.ImplementsGenericIEnumerator | EnumeratorDataCode.ImplementsIEnumerator |
            EnumeratorDataCode.HasPublicDispose | EnumeratorDataCode.HasPublicDisposeReturningVoid |
            EnumeratorDataCode.EnumeratorHasPublicResetMethod | EnumeratorDataCode.HasPublicMoveNextReturningBool |
            EnumeratorDataCode.HasPublicResetReturningVoid | EnumeratorDataCode.EnumeratorHasDisposeMethod |
            EnumeratorDataCode.IsIEnumeratorT | EnumeratorDataCode.EnumeratorImplementsIDisposable);

        public static EnumeratorData IsNonGenericIEnumerator { get; } = new(
            EnumeratorDataCode.EnumeratorIsInterfaceType | EnumeratorDataCode.EnumeratorIsReferenceType |
            EnumeratorDataCode.ImplementsIEnumerator |
            EnumeratorDataCode.HasPublicDispose | EnumeratorDataCode.HasPublicDisposeReturningVoid |
            EnumeratorDataCode.EnumeratorHasPublicResetMethod | EnumeratorDataCode.HasPublicMoveNextReturningBool |
            EnumeratorDataCode.HasPublicResetReturningVoid | EnumeratorDataCode.EnumeratorHasDisposeMethod |
            EnumeratorDataCode.IsIEnumerator | EnumeratorDataCode.EnumeratorImplementsIDisposable);
        public bool ImplementsIDisposable => CheckIndividualFlagIsSet(EnumeratorDataCode.EnumeratorImplementsIDisposable);
        public bool IsGenericIEnumerator => CheckIndividualFlagIsSet(EnumeratorDataCode.IsIEnumeratorT);
        public bool IsIEnumerator => CheckIndividualFlagIsSet(EnumeratorDataCode.IsIEnumerator);
        public bool IsDataUnavailable => (_code & ClearMaskStartingPoint) == EnumeratorDataCode.Unavailable;
        public bool IsEnumeratorAReferenceType => CheckIndividualFlagIsSet(EnumeratorDataCode.EnumeratorIsReferenceType);
        public bool IsEnumeratorAnInterfaceType => CheckIndividualFlagIsSet(EnumeratorDataCode.EnumeratorIsInterfaceType);
        public bool IsEnumeratorAClassType => IsEnumeratorAReferenceType && !IsEnumeratorAnInterfaceType;
        public bool IsEnumeratorAValueType => CheckIndividualFlagIsSet(EnumeratorDataCode.EnumeratorIsValueType);
        public bool IsEnumeratorAReadOnlyValueType =>
            IsEnumeratorAValueType && CheckIndividualFlagIsSet(EnumeratorDataCode.EnumeratorIsRefStruct);
        public bool IsEnumeratorAStackOnlyValueType =>
            IsEnumeratorAValueType && CheckIndividualFlagIsSet(EnumeratorDataCode.EnumeratorIsRefStruct);

        public bool EnumeratorHasPublicCurrent => IsPublicCurrentAReferenceType || IsPublicCurrentAValueType;
        public bool IsPublicCurrentAValueType => CheckIndividualFlagIsSet(EnumeratorDataCode.PublicCurrentIsValueType);

        public bool IsPublicCurrentMarkedReadonly => IsPublicCurrentAValueType &&
                                                     (CheckIndividualFlagIsSet(EnumeratorDataCode
                                                          .EnumeratorIsReadOnlyStruct) ||
                                                      CheckIndividualFlagIsSet(EnumeratorDataCode.PublicCurrentIsReadOnly));
        public bool IsPublicCurrentAReferenceType =>
            CheckIndividualFlagIsSet(EnumeratorDataCode.PublicCurrentIsReferenceType);
        public bool IsPublicCurrentReturnedByReference =>
            CheckIndividualFlagIsSet(EnumeratorDataCode.PublicCurrentIsReturnedByReference);
        public bool IsPublicCurrentReturnedByReadonlyReference =>
            CheckIndividualFlagIsSet(EnumeratorDataCode.PublicCurrentIsReturnedByReadOnlyReference);
        public bool IsPublicCurrentReturnedByValue =>
            !IsPublicCurrentReturnedByReadonlyReference && !IsPublicCurrentReturnedByReference;

        public bool DoesEnumeratorImplementGenericIEnumerable =>
            CheckIndividualFlagIsSet(EnumeratorDataCode.ImplementsGenericIEnumerator);

        public bool DoesEnumeratorImplementIEnumerable => DoesEnumeratorImplementGenericIEnumerable ||
                                                          CheckIndividualFlagIsSet(EnumeratorDataCode.ImplementsIEnumerator);

        public bool HasPublicMoveNext => CheckIndividualFlagIsSet(EnumeratorDataCode.HasPublicMoveNext);

        public bool HasProperMoveNext =>
            HasPublicMoveNext && CheckIndividualFlagIsSet(EnumeratorDataCode.HasPublicMoveNextReturningBool);

        public bool HasPublicDispose => CheckIndividualFlagIsSet(EnumeratorDataCode.HasPublicDispose);
        public bool HasProperPublicDispose =>
            HasPublicDispose && CheckIndividualFlagIsSet(EnumeratorDataCode.HasPublicDisposeReturningVoid);
        public bool HasPublicReset => CheckIndividualFlagIsSet(EnumeratorDataCode.HasPublicDispose);
        public bool HasProperPublicReset =>
            HasPublicDispose && CheckIndividualFlagIsSet(EnumeratorDataCode.HasPublicDisposeReturningVoid);


        [Pure]
        public EnumeratorData AddPublicCurrentPropertyInfo(bool returnsValueType, bool propertyIsMarkedReadonly, bool returnsByValue,
            bool returnsByReadOnlyReference)
        {
            if (propertyIsMarkedReadonly && (_code & EnumeratorDataCode.EnumeratorIsValueType) !=
                EnumeratorDataCode.EnumeratorIsValueType)
                throw new ArgumentException(
                    "Current may be marked readonly only in value type enumerators.  Did you mean it returns by readonly reference?",
                    nameof(propertyIsMarkedReadonly));
            if (returnsByValue && returnsByReadOnlyReference)
                throw new ArgumentException("Returning by value is compatible with returning by readonly reference.");
            
            propertyIsMarkedReadonly = propertyIsMarkedReadonly ||
                                       ((_code & EnumeratorDataCode.EnumeratorIsReadOnlyStruct) ==
                                        EnumeratorDataCode.EnumeratorIsReadOnlyStruct);

            EnumeratorDataCode valueTypeOrRefTypeMask = returnsValueType
                ? EnumeratorDataCode.PublicCurrentIsValueType
                : EnumeratorDataCode.PublicCurrentIsReferenceType;
            EnumeratorDataCode returnByMask = (returnsByValue, returnsByReadOnlyReference) switch
            {
                //true, true excepts at top of function so discard here
                (true, _) => EnumeratorDataCode.Unavailable,
                (false, true) => EnumeratorDataCode.PublicCurrentIsReturnedByReference |
                                 EnumeratorDataCode.PublicCurrentIsReturnedByReadOnlyReference,
                (false, false) => EnumeratorDataCode.PublicCurrentIsReturnedByReference
            };
            EnumeratorDataCode currentPropRoMask = propertyIsMarkedReadonly
                ? EnumeratorDataCode.PublicCurrentIsReadOnly
                : EnumeratorDataCode.Unavailable;
            var newCode = _code;

            EnumeratorDataCode setMask = valueTypeOrRefTypeMask | returnByMask | currentPropRoMask;
            newCode |= setMask;
            Debug.Assert(QueryAreSpecifiedFlagsSet(newCode, setMask));
            return new EnumeratorData(newCode);
        }

        [Pure]
        public EnumeratorData AddEnumeratorTypeInfoForValueType(bool isReadOnlyStruct, bool isRefStruct)
        {
            EnumeratorDataCode clearMask = ClearTypeInfoMask;
            EnumeratorDataCode setMask = EnumeratorDataCode.EnumeratorIsValueType;
            if (isReadOnlyStruct)
                setMask |= EnumeratorDataCode.EnumeratorIsReadOnlyStruct;
            if (isRefStruct)
            {
                setMask |= EnumeratorDataCode.EnumeratorIsRefStruct;
            }

            var newCode = _code;
            newCode &= clearMask;
            Debug.Assert(QueryAreSpecifiedFlagsClear(newCode, clearMask));
            newCode |= setMask;
            Debug.Assert(QueryAreSpecifiedFlagsSet(newCode, setMask));
            Debug.Assert(QueryAreSpecifiedFlagsClear(newCode, (EnumeratorDataCode.EnumeratorIsReferenceType | EnumeratorDataCode.EnumeratorIsInterfaceType)));
            return new EnumeratorData(newCode);
        }

        [Pure]
        public EnumeratorData AddEnumeratorTypeInfoForReferenceType(bool isClass, bool isInterface)
        {
            EnumeratorDataCode setMask = (isClass, isInterface) switch
            {
                (true, true) => throw new ArgumentException($"{nameof(isClass)} (value: {isClass}) and {nameof(isInterface)} (value: {isInterface}) cannot both be true."),
                (false, true) => EnumeratorDataCode.EnumeratorIsReferenceType | EnumeratorDataCode.EnumeratorIsInterfaceType,
                (true, false) => EnumeratorDataCode.EnumeratorIsReferenceType | EnumeratorDataCode.EnumeratorIsClassType,
                (false, false) => EnumeratorDataCode.EnumeratorIsReferenceType,
            };
            var code = _code;
            code |= setMask;
            Debug.Assert(QueryAreSpecifiedFlagsSet(code, setMask));
            return new EnumeratorData(code);
        }

        [Pure]
        public EnumeratorData AddPublicMoveNextInfo(bool hasPublicMoveNext, bool publicMoveNextReturnsBool)
        {
            EnumeratorDataCode setMask = (hasPublicMoveNext, publicMoveNextReturnsBool) switch
            {
                (false, true) => throw new ArgumentException("Public MoveNext returning bool implies public MoveNext."),
                (false, false) => EnumeratorDataCode.Unavailable,
                (true, false) => EnumeratorDataCode.HasPublicMoveNext,
                (true, true) => EnumeratorDataCode.HasPublicMoveNext | EnumeratorDataCode.HasPublicMoveNextReturningBool,
            };
            var newCode = _code;
            newCode |= setMask;
            Debug.Assert(QueryAreSpecifiedFlagsSet(newCode, setMask));
            return new EnumeratorData(newCode);
        }

        [Pure]
        public EnumeratorData AddPublicDisposeInfo(bool hasPublicDispose, bool publicDisposeReturnsVoid)
        {
            EnumeratorDataCode setMask = (hasPublicDispose, publicDisposeReturnsVoid) switch
            {
                (false, true) => throw new ArgumentException("Public Dispose returning bool implies public Dispose."),
                (false, false) => EnumeratorDataCode.Unavailable,
                (true, false) => EnumeratorDataCode.HasPublicDispose,
                (true, true) => EnumeratorDataCode.HasPublicDispose | EnumeratorDataCode.HasPublicDisposeReturningVoid,
            };
            var newCode = _code;
            
            newCode |= setMask;
            Debug.Assert(QueryAreSpecifiedFlagsSet(newCode, setMask));
            return new EnumeratorData(newCode);
        }

        [Pure]
        public EnumeratorData AddPublicResetInfo(bool hasPublicReset, bool publicResetReturnsVoid)
        {

            EnumeratorDataCode setMask = (hasPublicDispose: hasPublicReset, publicDisposeReturnsVoid: publicResetReturnsVoid) switch
            {
                (false, true) => throw new ArgumentException("Public move next returning bool implies public MoveNext."),
                (false, false) => EnumeratorDataCode.Unavailable,
                (true, false) => EnumeratorDataCode.HasPublicResetMethod,
                (true, true) => EnumeratorDataCode.HasPublicResetMethod | EnumeratorDataCode.HasPublicResetReturningVoid,
            };
            var newCode = _code;
            newCode |= setMask;
            Debug.Assert(QueryAreSpecifiedFlagsSet(newCode , setMask));
            return new EnumeratorData(newCode);
        }

        [Pure]
        public EnumeratorData AddIEnumerableInterfaceImplementationData(bool implementsIEnumerable,
            bool implementsGenericIEnumerable)
        {
            bool isRefStruct = IsEnumeratorAStackOnlyValueType;
            EnumeratorDataCode clearMask = ClearMaskStartingPoint &
                                           (~(EnumeratorDataCode.ImplementsIEnumerator |
                                              EnumeratorDataCode.ImplementsGenericIEnumerator));
            EnumeratorDataCode setMask = (isRefStruct, implementsIEnumerable, implementsGenericIEnumerable) switch
            {
                (_, false, true) => throw new ArgumentException($"{nameof(implementsGenericIEnumerable)} (value: true) should imply {nameof(implementsIEnumerable)} (value: false)."),
                (true, true, _) => throw new ArgumentException($"{nameof(implementsGenericIEnumerable)} (value: {implementsGenericIEnumerable}) and {nameof(implementsIEnumerable)} (value: true) must both be false since this is a ref struct."),
                //(true, _, true) => throw new ArgumentException($"{nameof(implementsGenericIEnumerable)} (value: {implementsGenericIEnumerable}) and {nameof(implementsIEnumerable)} (value: true) must both be false since this is a ref struct."),
                (_, false, false) => EnumeratorDataCode.Unavailable,
                (_, true, true) => EnumeratorDataCode.ImplementsIEnumerator | EnumeratorDataCode.ImplementsGenericIEnumerator | EnumeratorDataCode.EnumeratorImplementsIDisposable,
                (_, true, false) => EnumeratorDataCode.ImplementsIEnumerator | EnumeratorDataCode.EnumeratorImplementsIDisposable,
            };

            var newCode = _code;
            newCode |= setMask;
            Debug.Assert(QueryAreSpecifiedFlagsSet(newCode, setMask));
            Debug.Assert((newCode & EnumeratorDataCode.EnumeratorIsRefStruct ) != EnumeratorDataCode.EnumeratorIsRefStruct);
            return new EnumeratorData(newCode);
        }

        [Pure]
        public EnumeratorData AddIDisposableImplementationData(bool implementsIDisposable)
        {
            if (implementsIDisposable && CheckIndividualFlagIsSet(EnumeratorDataCode.EnumeratorIsRefStruct))
                throw new ArgumentException("Parameter must be false since this is a ref struct.",
                    nameof(implementsIDisposable));
            var newCode = _code;
            if (implementsIDisposable)
            {
                newCode |= EnumeratorDataCode.EnumeratorImplementsIDisposable;
                QueryAreSpecifiedFlagsSet(newCode, EnumeratorDataCode.EnumeratorImplementsIDisposable);
            }
            return new EnumeratorData(newCode);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (int)((uint)_code);
            }
        }

        public static bool operator ==(EnumeratorData lhs, EnumeratorData rhs) => lhs._code == rhs._code;
        public static bool operator !=(EnumeratorData lhs, EnumeratorData rhs) => !(lhs == rhs);
        public static bool operator >(EnumeratorData lhs, EnumeratorData rhs) => lhs._code > rhs._code;
        public static bool operator <(EnumeratorData lhs, EnumeratorData rhs) => lhs._code < rhs._code;
        public static bool operator >=(EnumeratorData lhs, EnumeratorData rhs) => !(lhs < rhs);
        public static bool operator <=(EnumeratorData lhs, EnumeratorData rhs) => !(lhs > rhs);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is EnumeratorData d && d == this;
        public bool Equals(EnumeratorData other) => other == this;

        public int CompareTo(EnumeratorData other) => (this._code, other._code) switch
        {
            var (l, r) when l == r => 0,
            var (l, r) when l > r => 1,
            _ => -1
        };

        /// <inheritdoc />
        public override string ToString() => _code.ToString();
        

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CheckIndividualFlagIsSet(EnumeratorDataCode code)
        {
            EnsurePopcount1(code, nameof(code));
            return ((_code & ClearMaskStartingPoint)&  code) == code;
        }

        [Conditional("DEBUG")]
        private static void EnsurePopcount1(EnumeratorDataCode code, string paramName)
        {
            if (Popcnt((uint)code) != 1) // horizontal sum of bytes
                throw new ArgumentException($"More than one flag is set in {code}.", paramName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int Popcnt(uint val)
        {
            val = val - ((val >> 1) & 0x55555555u);        // add pairs of bits
            val = (val & 0x33333333u) + ((val >> 2) & 0x33333333u);  // quads
            val = (val + (val >> 4)) & 0x0F0F0F0Fu;        // groups of 8
            return (int) ((val * 0x01010101u) >> 24);          // horizontal sum of bytes

        }

        private static bool QueryAreSpecifiedFlagsSet(EnumeratorDataCode code, EnumeratorDataCode check) => check == EnumeratorDataCode.Unavailable || ((check & code) == check);

        private static bool QueryAreSpecifiedFlagsClear(EnumeratorDataCode code, EnumeratorDataCode check) => code == EnumeratorDataCode.Unavailable ||
            (((ClearMaskStartingPoint & (~check)) & code) == code);

        private EnumeratorData(EnumeratorDataCode code) => _code = code;

        [FieldOffset(0)] private readonly EnumeratorDataCode _code;

        private static EnumeratorDataCode GetAllSet()
        {
            var codes = typeof(EnumeratorDataCode).GetEnumValues().Cast<EnumeratorDataCode>().Where(code => code != EnumeratorDataCode.Unavailable)
                .ToImmutableSortedSet();
            EnumeratorDataCode allSet = EnumeratorDataCode.Unavailable;
            foreach (var enumeratorDataCode in codes)
            {
                EnsurePopcount1(enumeratorDataCode, nameof(enumeratorDataCode));
                allSet |= enumeratorDataCode;
            }
            return allSet;
        }

        private static readonly EnumeratorDataCode ClearMaskStartingPoint = GetAllSet();

        private static readonly EnumeratorDataCode ClearTypeInfoMask =ClearMaskStartingPoint & (~(EnumeratorDataCode.EnumeratorIsValueType |
            EnumeratorDataCode.EnumeratorIsReferenceType |
            EnumeratorDataCode.EnumeratorIsInterfaceType |
            EnumeratorDataCode.EnumeratorIsRefStruct |
            EnumeratorDataCode.EnumeratorIsReadOnlyStruct | EnumeratorDataCode.EnumeratorIsClassType | EnumeratorDataCode.IsIEnumerator | EnumeratorDataCode.IsIEnumeratorT));
        private static readonly EnumeratorDataCode ClearInterfaceImplementationDataMask = ClearMaskStartingPoint & (
            ~(EnumeratorDataCode.ImplementsIEnumerator | EnumeratorDataCode.ImplementsGenericIEnumerator));

        [Flags]
        private enum EnumeratorDataCode : uint
        {
            Unavailable                                     = 0x0000_0000,
            EnumeratorIsValueType                           = 0x0000_0001,
            EnumeratorIsReferenceType                       = 0x0000_0002,
            EnumeratorIsInterfaceType                       = 0x0000_0004,
            EnumeratorIsRefStruct                           = 0x0000_0008,
            EnumeratorHasPublicResetMethod                  = 0x0000_0010,
            EnumeratorHasDisposeMethod                      = 0x0000_0020,
            PublicCurrentIsReferenceType                    = 0x0000_0040,
            PublicCurrentIsValueType                        = 0x0000_0080,
            PublicCurrentIsReadOnly                         = 0x0000_0100,
            PublicCurrentIsReturnedByReference              = 0x0000_0200,
            PublicCurrentIsReturnedByReadOnlyReference      = 0x0000_0400,
            ImplementsIEnumerator                           = 0x0000_0800,
            ImplementsGenericIEnumerator                    = 0x0000_1000,
            HasPublicDispose                                = 0x0000_2000,
            HasPublicDisposeReturningVoid                   = 0x0000_4000,
            HasPublicMoveNext                               = 0x0000_8000,
            HasPublicMoveNextReturningBool                  = 0x0001_0000,
            HasPublicResetMethod                            = 0x0002_0000,
            HasPublicResetReturningVoid                     = 0x0004_0000,
            EnumeratorIsReadOnlyStruct                      = 0x0008_0000,
            EnumeratorImplementsIDisposable                 = 0x0010_0000, 
            EnumeratorIsClassType                           = 0x0020_0000,
            IsIEnumeratorT                                  = 0x0040_0000,
            IsIEnumerator                                   = 0x0080_0000,
        }
    }
}