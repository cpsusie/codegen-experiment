using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HpTimeStamps;

namespace TemplateLibrary
{
    public delegate ref readonly T PureRefFunc<T>() where T : struct;

    public delegate ref readonly T PureRefFunc<T, TInput>(in TInput x) where T : struct where TInput : struct;

    public delegate ref readonly T PureRefFunc<T, TInput1, TInput2>(in TInput1 x, in TInput2 y)
        where T : struct where TInput1 : struct where TInput2 : struct;

    public delegate T RefFunc<T, TInput1>(in TInput1 x);

    public delegate T RefFunc<T, TInput1, TInput2>(in TInput1 x, in TInput2 y);

    public delegate void RefAction<T>(ref T val) where T : struct;

    public delegate void RefAction<T1, T2>(ref T1 val, in T2 x) where T1 : struct;

    public delegate bool RefEqualityTester<T>(in T l, in T r) where T : struct;

    public delegate int RefComparer<T>(in T l, in T r) where T : struct;

    public delegate bool EqualityTester<T>(T l, T r) where T : struct;

    //public struct EnumerableWrapper<TEnumerable, TItem> : IEquatable<EnumerableWrapper<TEnumerable, TItem>>
    //{
    //    public TEnumerator GetEnumerator() => _enumerable.GetEnumerator();
        
    //    public static bool operator
    //        ==(EnumerableWrapper<TEnumerable, TItem> l, EnumerableWrapper<TEnumerable, TItem> r) =>
    //        TheComparer.Equals(l._enumerable, r._enumerable);

    //    public static bool operator
    //        !=(EnumerableWrapper<TEnumerable, TItem> l, EnumerableWrapper<TEnumerable, TItem> r) =>
    //        !(l == r);

    //    public EnumerableWrapper(TEnumerable enumerable) =>
    //        _enumerable = enumerable ?? throw new ArgumentNullException(nameof(enumerable));

    //    public override int GetHashCode()
    //    {
    //        return _enumerable != null ? TheComparer.GetHashCode(_enumerable) : 0;
    //    }

    //    public bool Equals(EnumerableWrapper<TEnumerable, TItem> enumerableWrapper) => enumerableWrapper == this;

    //    /// <inheritdoc />
    //    public override bool Equals(object? obj) => obj is EnumerableWrapper<TEnumerable, TItem> val && val == this;


    //    private readonly TEnumerable _enumerable;
    //    private static readonly EqualityComparer<TEnumerable> TheComparer = EqualityComparer<TEnumerable>.Default; 
    //}

    //public sealed class ExtensionSetAttribute<TExtend> : Attribute
    //{

    //}

    //public static partial class Extender
    //{
    //    public static TEumerableWrapper GetEnumerableWrapper(TEnumerable wrapMe);

    //    public static TTransformEnumerable FastWhere(TEnumerable source, RefFunc<TInput, TItem> predicate)
    //    {
            
    //    }
    //}

    [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
    public static class BetterListExtenderNeedInAttrib<TTCollection> 
    {
        public static bool IsCollectionStackOnlyValueType { get; }
        public static bool IsCollectionInterface { get; }
        public static bool IsCollectionReferenceType { get; }
        public static string ReasonForCollectionTypeIllegality { get; }
        public static bool IsCollectionClass { get; }

        public static bool IsCollectionValueType { get; }

        public static bool IllegalCollectionType { get; }

        public static bool IsEnumeratorValueType { get; }

        public static bool IsEnumeratorClass { get; }

        public static bool IsEnumeratorReferenceType { get; }

        public static bool IsEnumeratorInterface { get; }

        public static bool IsEnumeratorStackOnlyValueType { get; }


        public static bool DoesEnumeratorReturnByReference { get; }

        public static bool IsItemInterface { get; }

        public static bool IsItemClassOrDelegate { get; }

        public static bool IsItemValueType { get; }

        public static bool IsItemArray { get; }

        public static bool IsItemStackOnlyValueType { get; }


        public static Type Enumerator { get; }

        public static Type Collection => typeof(TTCollection);

        public static Type Item { get; }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        static BetterListExtenderNeedInAttrib()
        {
            Enumerator = typeof(void);
            Item = typeof(void);
            if (Collection.IsValueType)
            {
                IsCollectionValueType = true;
                IsCollectionStackOnlyValueType = Collection.IsByRefLike;
                IsCollectionInterface = false;
                IsCollectionClass = false;
                IsCollectionReferenceType = false;
            }
            else if (Collection.IsInterface)
            {
                IsCollectionInterface = true;
                IsCollectionStackOnlyValueType = false;
                IsCollectionClass = false;
                IsCollectionValueType = false;
                IsCollectionReferenceType = true;
            }
            else if (Collection.IsClass)
            {
                IsCollectionClass = true;
                IsCollectionValueType = false;
                IsCollectionReferenceType = true;
                IsCollectionStackOnlyValueType = false;
                IsCollectionInterface = false;
            }
            else
            {
                IllegalCollectionType = true;
                IsCollectionClass = false;
                IsCollectionValueType = false;
                IsCollectionReferenceType = false;
                IsCollectionStackOnlyValueType = false;
                IsCollectionInterface = false;
            }

            if (IllegalCollectionType)
            {
                ReasonForCollectionTypeIllegality = "Collection type supplied is neither a class nor a struct nor an interface.  It is thus illegal.";
                return;
            }

            Type t = typeof(void);
            IEnumerable<(MethodInfo Meth, Type Ret)> getEnumeratorMethodsWithReturnTypes =
                from methodInfo in Collection.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                where 
                    methodInfo.IsPublic && !methodInfo.IsStatic &&
                    methodInfo.Name == GetEnumeratorMethodName && methodInfo.MemberType == MemberTypes.Method
                let parameters = methodInfo.GetParameters()
                let returnType = methodInfo.ReturnType
                where parameters.Length == 0 && returnType != null && returnType.Name != typeof(void).Name && (returnType.IsPublic || returnType.IsNestedPublic) && returnType.IsVisible && (returnType.IsByRefLike ||
                    returnType.IsClass || returnType.IsValueType || returnType.IsInterface)
                select (Meth: methodInfo, Ret: returnType);
            
            IEnumerable<(MethodInfo GetEnumeratorMethod, Type EnumeratorType, Type ItemType, PropertyInfo CurrentPropertInfo)?> potentialMatches = (from tuple in getEnumeratorMethodsWithReturnTypes
                where tuple.Meth != null && tuple.Ret != null
                let getEnumeratorMethod = tuple.Meth
                let returnType = tuple.Ret
                let x = GetEnumeratorInfo(returnType)
                let currentPropertyInfo = x.CurrentPi
                let itemType = x.CurrentReturnType
                let moveNextInfo = x.MoveNextMethodInfo
                let resetMethodInfo = x.ResetMethodInfo
                let enumeratorIsClass = x.EnumeratorIsClass
                let enumeratorIsStruct = x.EnumeratorIsStruct
                let enumeratorIsInterface = x.EnumeratorIsInterface
                let enumeratorIsStackOnlyStruct = x.EnumeratorIsStackOnlyStruct
                where currentPropertyInfo != null && itemType != null && (itemType.IsInterface ||
                                                                          itemType.IsValueType || itemType.IsClass ||
                                                                          itemType.IsByRefLike || itemType.IsArray)
                select ((MethodInfo GetEnumeratorMethod, Type EnumeratorType, Type ItemType, PropertyInfo CurrentPropertInfo)?) (GetEnumeratorMethod: getEnumeratorMethod, EnumeratorType: returnType, ItemType: itemType,
                    CurrentPropertInfo: currentPropertyInfo));

                var firstMatch = potentialMatches.FirstOrDefault();
                if (firstMatch == null)
                {
                    IllegalCollectionType = true;
                    ReasonForCollectionTypeIllegality =
                        "Collection type does not have a valid GetEnumerator method that returns a valid Enumerator.";
                    Enumerator = typeof(void);
                    Item = typeof(void);
                    return;
                }
            //,, , firstItemType.FullName
            (MethodInfo firstGetEnumeratorMethod, Type firstEnumeratorType, Type firstItemType, PropertyInfo firstPropertyInfo)  = firstMatch.Value;
                Func<string, string, string, string, StringBuilder> sbCtor = static (string getEnumMn, string enumTypeN, string piN, string itmTypeName ) 
                    => new StringBuilder($"There are too many possible GetEnumerator matches for this collection to be legal.  The first identified one had a matching method called {getEnumMn} which returned an enumerator of type {enumTypeN}.  This enumerator had a current property called {piN} which returned an item type called {itmTypeName}." +
                    $"  There were, however additional matches:{Environment.NewLine}");
                StringBuilder? tooManyMatches = null;
                    

                bool extra = false;
                int extraMatchNumber = 1;
                foreach ( (MethodInfo getEnumMi, Type enumType, Type itemType, PropertyInfo propertyInfo) in potentialMatches.Skip(1).Where(itm => itm != null).Select(itm => itm!.Value))
                {
                    tooManyMatches ??= sbCtor(firstGetEnumeratorMethod.Name, firstEnumeratorType.FullName!, firstPropertyInfo.Name, firstItemType.FullName!);
                    extra = true;
                    tooManyMatches.AppendLine(
                        $"Extra match# {extraMatchNumber++}: \tmatching method called {getEnumMi.Name} which returned an enumerator of type {enumType.FullName}.  This enumerator had a current property called {propertyInfo.Name} which returned an item type called {itemType.FullName}.");
                }

                if (extra)
                {
                    IllegalCollectionType = true;
                    ReasonForCollectionTypeIllegality = tooManyMatches!.ToString();
                    Enumerator = typeof(void);
                    Item = typeof(void);
                    return;
                }

                Enumerator = firstEnumeratorType;
                IsEnumeratorValueType = firstEnumeratorType.IsValueType || firstEnumeratorType.IsByRefLike;
                if (IsEnumeratorValueType)
                {
                    IsEnumeratorClass = false;
                    IsEnumeratorStackOnlyValueType = firstEnumeratorType.IsByRefLike;
                    IsEnumeratorInterface = false;
                    IsEnumeratorReferenceType = false;
                }
                else if (firstEnumeratorType.IsClass)
                {
                    IsEnumeratorValueType = false;
                    IsEnumeratorStackOnlyValueType = false;
                    IsEnumeratorInterface = false;
                    IsEnumeratorClass = true;
                    IsEnumeratorReferenceType = true;
                }
                else if (firstEnumeratorType.IsInterface)
                {
                    IsEnumeratorValueType = false;
                    IsEnumeratorReferenceType = true;
                    IsEnumeratorStackOnlyValueType = false;
                    IsEnumeratorInterface = true;
                    IsEnumeratorClass = false;
                }
                else
                {
                    IllegalCollectionType = true;
                    ReasonForCollectionTypeIllegality =
                        "It's enumerator is not a class, value type, interface or stack-only value type.";
                    Item = typeof(void);
                    return;
                }

                DoesEnumeratorReturnByReference = firstPropertyInfo.GetMethod?.ReturnType.IsByRef == true;

                Item = firstItemType;
                IsItemValueType = firstItemType.IsByRefLike || firstItemType.IsValueType;
                if (IsItemValueType)
                {
                    IsItemStackOnlyValueType = firstItemType.IsByRefLike;
                    IsItemArray = false;
                    IsItemClassOrDelegate = false;
                    IsItemInterface = false;
                }
                else if (firstItemType.IsArray)
                {
                    IsItemStackOnlyValueType = false;
                    IsItemArray = false;
                    IsItemValueType = false;
                    IsItemClassOrDelegate = false;
                    IsItemInterface = false;
                }
                else if (firstItemType.IsClass)
                {
                    IsItemClassOrDelegate = true;
                    IsItemArray = false;
                    IsItemValueType = false;
                    IsItemStackOnlyValueType = false;
                    IsItemInterface = false;
                }
                else
                {
                    IllegalCollectionType = true;
                    ReasonForCollectionTypeIllegality =
                        "Illegal because collection return type is not class, delegate, array, value type or stack-only value type.";
                    return;
                }

                IllegalCollectionType = false;
                ReasonForCollectionTypeIllegality = string.Empty;

        }

        static (PropertyInfo? CurrentPi, Type? CurrentReturnType, 
            MethodInfo? MoveNextMethodInfo, MethodInfo? ResetMethodInfo, 
            bool EnumeratorIsClass, bool EnumeratorIsInterface, 
            bool EnumeratorIsStruct, bool EnumeratorIsStackOnlyStruct, bool ItemReturnedByRef) GetEnumeratorInfo(Type t)
        {
            IEnumerable<(PropertyInfo pi, Type resolvedReturnType, bool isByRefReturn)?> currentPropertiesWithReturnType =
                    from pi in t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    where pi.CanRead && pi.SetMethod?.IsPublic != true
                    let getter = pi.GetMethod
                    where getter?.IsPublic == true && !getter.IsStatic
                    let returnType = getter.ReturnType
                    where returnType?.IsVisible == true
                    let resolvedData = GetCurrentReturnTypeData(returnType)
                    let resolvedReturnType = resolvedData.DeRefedType
                    let isByRefReturn = resolvedData.IsRefReturn
                where resolvedReturnType != null && resolvedReturnType.IsVisible && resolvedReturnType.Name != typeof(void).Name && (resolvedReturnType.IsClass ||
                    resolvedReturnType.IsValueType || resolvedReturnType.IsByRefLike || resolvedReturnType.IsInterface || resolvedReturnType.IsArray)
                select ((PropertyInfo pi, Type resolvedReturnType, bool isByRefReturn)?) (pi, resolvedReturnType, isByRefReturn);

            (PropertyInfo currentPropertyInfo, Type returnTypeInfo, bool itemReturnedByRef)? x =
                currentPropertiesWithReturnType.SingleOrDefault();
            PropertyInfo? currentPropertyInfo = x?.currentPropertyInfo;
            Type? returnTypeInfo = x?.returnTypeInfo;
            bool? itemReturnedByRef = x?.itemReturnedByRef;

            
            IEnumerable<MethodInfo?> moveNextMethodsReturningBool = from mi in t.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                where mi.IsPublic && !mi.IsStatic && mi.MemberType == MemberTypes.Method &&
                      mi.Name == MoveNextMethodName
                let parameters = mi.GetParameters()
                let returnType = mi.ReturnType
                where !parameters.Any() && returnType != null && returnType.IsPublic && returnType.IsPrimitive &&
                      returnType == typeof(bool)
                select mi;
             MethodInfo? moveNextInfo = moveNextMethodsReturningBool.SingleOrDefault();

            var resetMethods = from mi in t.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                where mi.IsPublic && !mi.IsStatic && mi.MemberType == MemberTypes.Method &&
                      mi.Name == ResetMethodName
                let parameters = mi.GetParameters()
                let returnType = mi.ReturnType
                where !parameters.Any() && (returnType == null || returnType == typeof(void))
                select mi;
            MethodInfo? resetMethodInfo = resetMethods.SingleOrDefault();

            return (currentPropertyInfo, returnTypeInfo, moveNextInfo, resetMethodInfo, t.IsClass, t.IsInterface,
                t.IsValueType, t.IsByRefLike, itemReturnedByRef == true);


        }

        static (bool IsRefReturn, Type DeRefedType) GetCurrentReturnTypeData(Type returnType)
        {
            Type deRefedType = returnType;
            bool isRefReturn = returnType.IsByRef && returnType.AssemblyQualifiedName?.Any(c => c == '&') == true;
            
            if (isRefReturn)
            {
                string assemblyName = returnType.AssemblyQualifiedName ?? string.Empty;
                Span<char> chars = stackalloc char[assemblyName.Length-1];
                int idx = 0;
                foreach (char c in assemblyName)
                {
                    if (c != '&')
                    {
                        chars[idx++] = c;
                    }
                }

                assemblyName = chars.TrimEnd().ToString();
                var baseType = Type.GetType(assemblyName, true);
                deRefedType = baseType ?? throw new InvalidOperationException("Can't get base type.");
            }

            return (isRefReturn, deRefedType);
        }
        private const string GetEnumeratorMethodName = "GetEnumerator";
        private const string MoveNextMethodName = "MoveNext";
        private const string ResetMethodName = "Reset";
    }

    public static partial class BetterListExtenderForPortableMonotonicStamp
    {
        public static TransformEnumerableWrapperForPortableMonotonicStamp<TOut> Select<TOut>(
            this BetterList<PortableMonotonicStamp> bl, RefFunc<TOut, PortableMonotonicStamp> transformation)
        {
            if (bl == null) throw new ArgumentNullException(nameof(bl));
            if (transformation == null) throw new ArgumentNullException(nameof(transformation));
            return new TransformEnumerableWrapperForPortableMonotonicStamp<TOut>(transformation,
                new EnumerableWrapperForPortableMonotonicStamp(bl));
        }

        internal static TransformEnumerableWrapperForPortableMonotonicStamp<TOut> Select<TOut>(
            EnumerableWrapperForPortableMonotonicStamp ew, RefFunc<TOut, PortableMonotonicStamp> transformation)
        {
            return new TransformEnumerableWrapperForPortableMonotonicStamp<TOut>(transformation, ew);
        }

        public static FilterEnumerableWrapperForPortableMonotonicStamp Where(this BetterList<PortableMonotonicStamp> bl,
            RefFunc<bool, PortableMonotonicStamp> predicate)
        {
            if (bl == null) throw new ArgumentNullException(nameof(bl));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return Where(new EnumerableWrapperForPortableMonotonicStamp(bl), predicate);
        }

        internal static FilterEnumerableWrapperForPortableMonotonicStamp Where(
            EnumerableWrapperForPortableMonotonicStamp ewpms, RefFunc<bool, PortableMonotonicStamp> predicate)
        {
            return new FilterEnumerableWrapperForPortableMonotonicStamp(predicate, ewpms);
        }

        public static EnumerableWrapperForPortableMonotonicStamp
            GetEnumerableWrapper(BetterList<PortableMonotonicStamp> source) =>
            new EnumerableWrapperForPortableMonotonicStamp(source ?? throw new ArgumentNullException(nameof(source)));

        public readonly ref struct FilterEnumerableWrapperForPortableMonotonicStamp
        {
            public Enumerator GetEnumerator() =>Enumerator.CreateEnumerator(in this);

            internal FilterEnumerableWrapperForPortableMonotonicStamp(RefFunc<bool, PortableMonotonicStamp> predicate,
                EnumerableWrapperForPortableMonotonicStamp wrapper)
            {
                _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
                _wrapper = wrapper == default ? throw new ArgumentException("The value has not been initialized.", nameof(wrapper)) : wrapper;
                
            }

            public ref struct Enumerator
            {
                internal static Enumerator CreateEnumerator(in FilterEnumerableWrapperForPortableMonotonicStamp few) =>
                    new Enumerator(few._predicate ?? throw new ArgumentNullException(nameof(few)),
                        few._wrapper.GetEnumerator());


                public readonly ref readonly PortableMonotonicStamp Current
                {
                    get
                    {
                        if (_ok)
                            return ref _baseEnumerator.Current;
                        throw new InvalidOperationException("The enumerator is not in a valid state.");
                    }
                }

                public bool MoveNext()
                {
                    bool stillMoreToGoInBase = _baseEnumerator.MoveNext();
                    if (!stillMoreToGoInBase)
                    {
                        return (_ok = false);
                    }

                    bool predOk = _predicate(in _baseEnumerator.Current);
                    while (!predOk && stillMoreToGoInBase)
                    {
                        stillMoreToGoInBase = _baseEnumerator.MoveNext();
                        predOk = stillMoreToGoInBase && _predicate(in _baseEnumerator.Current);
                    }

                    return (_ok = predOk && stillMoreToGoInBase);
                }

                public void Reset()
                {
                    _baseEnumerator.Reset();
                    _ok = false;
                }

                private Enumerator(RefFunc<bool, PortableMonotonicStamp> predicate, in BetterList<PortableMonotonicStamp>.Enumerator baseEnum)
                {
                    _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
                    _baseEnumerator = baseEnum;
                    _ok = false;
                }

                private readonly RefFunc<bool, PortableMonotonicStamp> _predicate;
                private BetterList<PortableMonotonicStamp>.Enumerator _baseEnumerator;
                private bool _ok;
            }

            private readonly RefFunc<bool, PortableMonotonicStamp> _predicate;
            private readonly EnumerableWrapperForPortableMonotonicStamp _wrapper;
            
        }


        public  readonly ref struct
            TransformEnumerableWrapperForPortableMonotonicStamp<TOutput> 
        {
            public Enumerator GetEnumerator() => Enumerator.CreateEnumerator(in this);

            public ref struct Enumerator
            {
                public readonly TOutput Current => _transformation(in _baseEnumerator.Current);

                internal static Enumerator CreateEnumerator(in TransformEnumerableWrapperForPortableMonotonicStamp<TOutput> owner)
                {
                    return new Enumerator(owner._transformation, owner._wrapper.GetEnumerator());
                }

                public bool MoveNext()
                {
                    return _baseEnumerator.MoveNext();
                }

                public void Reset() => _baseEnumerator.Reset();

                private Enumerator(RefFunc<TOutput, PortableMonotonicStamp> transformation,
                    BetterList<PortableMonotonicStamp>.Enumerator baseEnumerator)
                {
                    _transformation = transformation ?? throw new ArgumentNullException(nameof(transformation));
                    _baseEnumerator = baseEnumerator;
                }

                private readonly RefFunc<TOutput, PortableMonotonicStamp> _transformation;
                private BetterList<PortableMonotonicStamp>.Enumerator _baseEnumerator;
            }

            public static bool operator ==(in TransformEnumerableWrapperForPortableMonotonicStamp<TOutput> l,
                in TransformEnumerableWrapperForPortableMonotonicStamp<TOutput> r) =>
                ReferenceEquals(l._transformation, r._transformation) && l._wrapper == r._wrapper;
            public static bool operator !=(in TransformEnumerableWrapperForPortableMonotonicStamp<TOutput> l,
                in TransformEnumerableWrapperForPortableMonotonicStamp<TOutput> r) => !(l == r);

            internal TransformEnumerableWrapperForPortableMonotonicStamp(
                RefFunc<TOutput, PortableMonotonicStamp> transformation,
                in EnumerableWrapperForPortableMonotonicStamp wrapper)
            {
                _transformation = transformation ?? throw new ArgumentNullException(nameof(transformation));
                _wrapper = wrapper == default
                    ? throw new ArgumentException("The value has not been initialized.", nameof(wrapper))
                    : wrapper;
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                int hash = _transformation.GetHashCode();
                unchecked
                {
                    hash = (hash * 397) ^ _wrapper.GetHashCode();
                }
                return hash;
            }

            /// <inheritdoc />
            public override bool Equals(object? obj) => false;

            public bool Equals(in TransformEnumerableWrapperForPortableMonotonicStamp<TOutput> other) => other == this;


            private readonly RefFunc<TOutput, PortableMonotonicStamp> _transformation;
            private readonly EnumerableWrapperForPortableMonotonicStamp _wrapper;
        }

        public readonly struct
            EnumerableWrapperForPortableMonotonicStamp : IEquatable<EnumerableWrapperForPortableMonotonicStamp>
        {
            public BetterList<PortableMonotonicStamp>.Enumerator GetEnumerator()
            {
                if (_enumerable == null)
                    throw new InvalidOperationException();
                return _enumerable.GetEnumerator();
            }

            public static bool operator
                    ==(EnumerableWrapperForPortableMonotonicStamp l, EnumerableWrapperForPortableMonotonicStamp r) =>
                    TheComparer.Equals(l._enumerable, r._enumerable);
            public static bool operator
                !=(EnumerableWrapperForPortableMonotonicStamp l, EnumerableWrapperForPortableMonotonicStamp r) =>
                !(l == r);

            public bool Equals(EnumerableWrapperForPortableMonotonicStamp other) => other == this;

            internal EnumerableWrapperForPortableMonotonicStamp(BetterList<PortableMonotonicStamp> source) =>
                _enumerable = source ?? throw new ArgumentNullException(nameof(source));

            public override int GetHashCode()
            
                => _enumerable != null ? TheComparer.GetHashCode(_enumerable) : 0;

            /// <inheritdoc />
            public override bool Equals(object? obj) =>
                obj is EnumerableWrapperForPortableMonotonicStamp ew && ew == this;

            /// <inheritdoc />
            public override string ToString() =>
                $"Wrapper for {typeof(BetterList<PortableMonotonicStamp>)}.  Contents: \"{_enumerable?.ToString() ?? "NULL"}\"";
            

            private static readonly EqualityComparer<BetterList<PortableMonotonicStamp>> TheComparer = EqualityComparer<BetterList<PortableMonotonicStamp>>.Default;

            private readonly BetterList<PortableMonotonicStamp>? _enumerable;
        }
        //{
        //    public TEnumerator GetEnumerator() => _enumerable.GetEnumerator();

        //    public static bool operator
        //        ==(EnumerableWrapper<TEnumerable, TItem> l, EnumerableWrapper<TEnumerable, TItem> r) =>
        //        TheComparer.Equals(l._enumerable, r._enumerable);

        //    public static bool operator
        //        !=(EnumerableWrapper<TEnumerable, TItem> l, EnumerableWrapper<TEnumerable, TItem> r) =>
        //        !(l == r);

        //    public EnumerableWrapper(TEnumerable enumerable) =>
        //        _enumerable = enumerable ?? throw new ArgumentNullException(nameof(enumerable));

        //    public override int GetHashCode()
        //    {
        //        return _enumerable != null ? TheComparer.GetHashCode(_enumerable) : 0;
        //    }

        //    public bool Equals(EnumerableWrapper<TEnumerable, TItem> enumerableWrapper) => enumerableWrapper == this;

        //    /// <inheritdoc />
        //    public override bool Equals(object? obj) => obj is EnumerableWrapper<TEnumerable, TItem> val && val == this;


        //    private readonly TEnumerable _enumerable;
        //    private static readonly EqualityComparer<TEnumerable> TheComparer = EqualityComparer<TEnumerable>.Default; 
        //}
    }

    public sealed class BetterList<T> : ISpecificallyRefEnumerable<T, BetterList<T>.Enumerator> where T : struct
    {
        public long Count => _count;
        public long Capacity => _array.LongLength;
        public ref T this[long index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get =>
                ref _array[
                    index > -1 && index < Count
                        ? index
                        : throw new ArgumentOutOfRangeException(nameof(index), index,
                            $"Index must be non-negative and less than {Count}.")];
        }

        public Span<T> AsSpan() => _array.AsSpan();

        public Span<T> Slice(int idx, int length) => _array.AsSpan(idx, length);

        public Span<T> Slice(int idx) => _array.AsSpan(idx);

        public Enumerator GetEnumerator() => new Enumerator(this);

        public BetterList()
        {
            _array = new T[DefaultSize];
            _count = 0;
        }

        public BetterList(long capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Value must non-negative.");
            _array = new T[capacity];
            _count = 0;
        }

        public BetterList(IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            (T[] array, long capcity, long count) = (source) switch
            {
                BetterList<T> bl =>  (bl._array.ToArray(), bl._array.LongLength, bl._count),
                T[] arr => (arr, arr.LongLength, arr.LongLength),
                IReadOnlyCollection<T> roc => (roc.ToArray(), roc.Count, roc.Count),
                _ => ToArrayWithLengthHardWay(source)    
            };

            _array = array;
            _count = count;
            Debug.Assert(_count <= _array.LongLength);

            (T[] Array, long Capacity, long Count) ToArrayWithLengthHardWay(IEnumerable<T> src)
            {
                T[] arr = src.ToArray();
                return (arr, arr.LongLength, arr.LongLength);
            }
            
        }

        public BetterList(ReadOnlySpan<T> source)
        {
            switch (source.Length)
            {
                case 0:
                    _array = new T[DefaultSize];
                    _count = 0;
                    break;
                case { } i when i > 0 && i <= DefaultSize:
                    _array = new T[DefaultSize];
                    _count = i;
                    break;
                default:
                    _array = new T[source.Length];
                    _count = source.Length;
                    break;
            }

            for (int i = 0; i < source.Length; ++i)
            {
                _array[i] =  source[i];
            }
        }

        public void Add(T item)
        {
            if (Count + 1 > Capacity)
            {
                T[] arr = GetBiggerArray();
                for (long i = 0; i < Count; ++i)
                {
                    arr[i] = _array[i];
                }

                _array = arr;
            }
            _array[_count++] = item;
        }

        public void Add(in T item)
        {
            if (Count + 1 > Capacity)
            {
                T[] arr = GetBiggerArray();
                for (long i = 0; i < Count; ++i)
                {
                    arr[i] = _array[i];
                }

                _array = arr;
            }
            _array[_count++] = item;
        }

        public (bool Popped, T? PoppedValue) RemoveLast()
        {
            if (Count == 0)
            {
                return (false, default);
            }

            ref readonly T val = ref _array[_count-- - 1];
            return (true, val);
        }

        public bool Contains(in T value, RefEqualityTester<T> eqTester) => FirstIndexOf(in value, eqTester) > -1;

        public int FirstIndexOf(in T value, RefEqualityTester<T> eqTester)
        {
            if (eqTester == null) throw new ArgumentNullException(nameof(eqTester));
            int idx = -1;
            foreach (ref readonly T item in this)
            {
                ++idx;
                if (eqTester(in value, in item))
                {
                    return idx;
                }
            }
            return -1;
        }

        public void InsertAt(T value, long idx)
        {
            if (idx < 0 || idx >= Count)
                throw new ArgumentOutOfRangeException(nameof(idx), idx,
                    $"Value must be non-negative and less than the size of the collection ({Count}).");
            T[] srcArray = _array;
            T[] targetArray = srcArray;
            if (Count + 1 > Capacity)
            {
                targetArray = GetBiggerArray();
                for (long i = 0; i < idx; ++i)
                {
                    targetArray[i] = srcArray[i];
                }
            }

            for (long i = idx; i <= Count; ++i)
            {
                targetArray[i + i] = srcArray[i];
            }

            targetArray[idx] = srcArray[idx];
            _array = targetArray;
            ++_count;
        }

        public T RemoveAt(long idx)
        {
            if (idx < 0 || idx >= Count)
                throw new ArgumentOutOfRangeException(nameof(idx), idx,
                    $"Value must be non-negative and less than the size of the collection ({Count}).");
            
            T ret = _array[idx];
            if (idx < _count - 1)
            {
                long afterIndex = idx + 1;
                for (long i = idx + 1; i < _count; ++i)
                {
                    _array[i - 1] = _array[i];
                }
            }

            --_count;
            return ret;
        }

        public void Clear()
        {
            _count = 0;
        }

        public bool Trim()
        {
            if ((_array.LongLength - _count) >= _array.LongLength * 0.10)
            {
                T[] newArr = new T[_count];
                for (int i = 0; i < _count; ++i)
                {
                    newArr[i] = _array[i];
                }

                _array = newArr;
                return true;
            }

            return false;
        }

        private T[] GetBiggerArray()
        {
            long newCapacity;
            checked
            {
                newCapacity =  Math.Max((long)Math.Ceiling(Count * 1.75), DefaultSize * 2);
                Debug.Assert(newCapacity > Capacity && newCapacity >= Count + 1);
            }

            return new T[newCapacity];
        }

        public struct Enumerator : IByRefEnumerator<T>
        {
            public readonly ref T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    if (_index < 0 || _index >= _owner.Count)
                        throw new InvalidOperationException(
                            "The enumerator is not in a valid state for retrieval of Current.");
                    return ref _owner[_index];
                }
            }

            /// <inheritdoc />
            T INoDisposeEnumerator<T>.Current => Current;

            /// <inheritdoc />
            ref readonly T IByRoRefEnumerator<T>.Current => ref Current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                ++_index;
                return _index < _owner.Count && _index > -1;
            }

            public void Reset()
            {
                _index = -1;
            }

            internal Enumerator(BetterList<T> owner)
            {
                _owner = owner;
                _index = -1;
            }

            private readonly BetterList<T> _owner;
            private int _index;
        }

        private T[] _array;
        private long _count;
        private const long DefaultSize = 8;

    }

    public static class ExamplePmsFast
    {
        internal static Random Rng => TheRng.Value!;
        
        public static void Example()
        {
            Duration timeSpentPopulating;
            PortableDuration oneThousandAndOneYears = GetOneThousandAndOneYears();
            Console.WriteLine("Seconds per 1001 years: {0:N}.", oneThousandAndOneYears.TotalSeconds);
            
            Debug.Assert(oneThousandAndOneYears.TotalDays / 365 - 1001 <= 0.1);

            const long size = 5_000_000;
            //const long size = 100_000;
            BetterList<PortableMonotonicStamp> bl;
            {

                var startedAt = HpTimeStamps.MonotonicTimeStampUtil<MonotonicStampContext>.StampNow;
                bl = CreateRandomBetterList((int)size);
                //bl = new BetterList<PortableMonotonicStamp>(size);
                //while (bl.Count < size)
                //{
                //    pms = RandomStamp();
                //    bl.Add(in pms);
                //}
                timeSpentPopulating =
                    HpTimeStamps.MonotonicTimeStampUtil<MonotonicStampContext>.StampNow - startedAt;
            }
            
            

            var justJune = bl.Where((in PortableMonotonicStamp pm) => pm.Month == 6 && pm.Day == 10);
            BetterList<PortableMonotonicStamp> justJune10List =
                new BetterList<PortableMonotonicStamp>((bl.Count / 365));
            foreach (ref readonly PortableMonotonicStamp stamp in justJune)
            {
                justJune10List.Add(in stamp);
            }

            Console.WriteLine("{0:N0} of the {1:N0} stamps were on June 10.", justJune10List.Count, bl.Count);
            
            int count = 0;
            string txtCnt = justJune10List.Count.ToString("N0");
            foreach (var dateTime in justJune10List.Select((in PortableMonotonicStamp stamp) => stamp.ToUtcDateTime()))
            {
                Console.WriteLine("June 10th# {0:N0} of {1}: \t[{2:O}].", ++count, txtCnt, dateTime);
            }
            Console.WriteLine("{0}{0}", Environment.NewLine);
            Console.WriteLine($"Spent {timeSpentPopulating.TotalSeconds:N7} seconds populating {bl.Count} random portable monotonic stamps.");
            if (bl.Count > 0)
            {
                Duration average = timeSpentPopulating / bl.Count;
                Console.WriteLine($"The average time per pop was: {average.TotalMicroseconds:N1} microseconds.");
            }
        }

        private static PortableDuration GetOneThousandAndOneYears()
        {
            PortableDuration ret;
            long secondsIn1001Years = 1;
            checked
            {
                secondsIn1001Years *= 60; //per minute
                secondsIn1001Years *= 60; //per hour
                secondsIn1001Years *= 24; //per day;
                secondsIn1001Years *= 365; //per year
                secondsIn1001Years *= 1001; //per 1001 years.
                ret = (PortableDuration) Duration.FromSeconds(secondsIn1001Years);
            }
            return ret;
        }

        

        private static BetterList<PortableMonotonicStamp> CreateRandomBetterList(int size)
        {
            int numCores = Environment.ProcessorCount;
            if (numCores > 1)
            {
                var itemsPerCore = size / numCores;
                if (itemsPerCore >= 25_000 || size > 1_000_000)
                {
                    Task<BetterList<PortableMonotonicStamp>> createBetterTask =
                        CreateBetterMultithreaded(size, numCores, itemsPerCore);
                    createBetterTask.Wait();
                    return createBetterTask.Result;
                }
            }

            return CreateRandomBetterListOneThread(size);

            static BetterList<PortableMonotonicStamp> CreateRandomBetterListOneThread(int size)
            {
                PortableMonotonicStamp[] temp = new PortableMonotonicStamp[size];
                Span<PortableMonotonicStamp> span = temp;
                PopulateSpanWithRandomStamps(span, TheBaseline, CancellationToken.None);
                return new BetterList<PortableMonotonicStamp>(span);
            }
        }

        private static Task<BetterList<PortableMonotonicStamp>> CreateBetterMultithreaded(int size, int numThreads,
            int minItemsPerCore) => Task.Run(() =>
        {
            
            PortableMonotonicStamp[] temp = new PortableMonotonicStamp[size];
            Memory<PortableMonotonicStamp> mem = temp;


            int threadsCreated = 0;
            var tasks = new  Task[numThreads];
            using var cts = new CancellationTokenSource();
            while (threadsCreated < tasks.Length)
            {
                int length = Math.Min(minItemsPerCore, mem.Length);
                var slice = mem.Slice(0, length);
                mem = mem.Slice(length);
                tasks[threadsCreated++] = Fill(slice, TheBaseline, cts.Token);
            }

            foreach (Task work in tasks)
            {
                work.Start();
            }

            int numDoneOk = tasks.Count(task =>
            {
                try
                {
                    task.Wait();
                    return task.IsCompletedSuccessfully;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLineAsync(ex.Message);
                    return false;
                }
            });

            if (numDoneOk != tasks.Length)
            {
                cts.Cancel();
                throw new OperationCanceledException();
            }

            return new BetterList<PortableMonotonicStamp>(temp.AsSpan());

        });

        private static Task Fill(Memory<PortableMonotonicStamp> stampsMem, PortableMonotonicStamp baseline,
            CancellationToken token) => new(() =>
        {
            PopulateSpanWithRandomStamps(stampsMem.Span, baseline, token);
        }, token);

        private static void PopulateSpanWithRandomStamps(Span<PortableMonotonicStamp> stamps, PortableMonotonicStamp baseline, CancellationToken token)
        {
            var random = Rng;
            long secondsMin, secondsMax;
            checked
            {
                secondsMin = -((long)Math.Truncate(OneThousandAndOneYears.TotalSeconds));
                secondsMax = (-secondsMin) + 1;
            }
            token.ThrowIfCancellationRequested();
            PortableDuration yearsToSecondsPrecision;
            for (int i = 0; i < stamps.Length; ++i)
            {
                long secondsPart = random.NextLong(secondsMin, secondsMax);
                long fracPart = random.NextLong(-999_999_999, 1_000_000);
                yearsToSecondsPrecision = PortableDuration.FromSeconds(secondsPart);
                yearsToSecondsPrecision += PortableDuration.FromNanoseconds(fracPart);
                stamps[i] = baseline + yearsToSecondsPrecision;
            }
        }
        
        private static readonly PortableDuration OneThousandAndOneYears = GetOneThousandAndOneYears();
        private static readonly PortableMonotonicStamp TheBaseline = (PortableMonotonicStamp) MonotonicTimeStampUtil<MonotonicStampContext>.StampNow;
        private static readonly ThreadLocal<Random> TheRng = new ThreadLocal<Random>(() => new Random(), false);
    }
}

//https://stackoverflow.com/a/13095144/5481748
