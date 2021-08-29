using System;

namespace TemplateLibrary
{
    public delegate TOutput TransformRoRefIn<TInput, TOutput>(in TInput input) where TInput : struct;

    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public sealed class GenerateEnumeratorAttribute : Attribute
    {

    }

    [GenerateEnumerator]
    public readonly struct RoRefInTransformEnumerable<TRoRefEnumerable, TRoRefEnumerator, TItemIn, TItemOut>
        where TRoRefEnumerator : struct, IByRoRefEnumerator<TItemIn>
        where TRoRefEnumerable : ISpecificallyRefReadOnlyEnumerable<TItemIn, TRoRefEnumerator>
        where TItemIn : struct
        where TItemOut : class
    {
        public bool IsDefault => !_initialized;

        public RoRefInTransformEnumerator<TRoRefEnumerable, TRoRefEnumerator, TItemIn, TItemOut> GetEnumerator()
        {
            return new RoRefInTransformEnumerator<TRoRefEnumerable, TRoRefEnumerator, TItemIn, TItemOut>(
                _enumerable.GetEnumerator(), _transformation);
        }

        public RoRefInTransformEnumerable(TRoRefEnumerable collection,
            TransformRoRefIn<TItemIn, TItemOut> transformation)
        {
            _transformation = transformation ?? throw new ArgumentNullException(nameof(transformation));
            _enumerable = collection ?? throw new ArgumentNullException(nameof(collection));
            _initialized = true;
        }

        private readonly TransformRoRefIn<TItemIn, TItemOut> _transformation;
        private readonly TRoRefEnumerable _enumerable;
        private readonly bool _initialized;
    }


    public partial struct RoRefInTransformEnumerator<TRoRefEnumerable, TRoRefInEnumerator, TItemIn, TItemOut>
        where TRoRefInEnumerator : struct, IByRoRefEnumerator<TItemIn>
        where TRoRefEnumerable : ISpecificallyRefReadOnlyEnumerable<TItemIn, TRoRefInEnumerator>
        where TItemIn : struct
        where TItemOut : class
    {
        public readonly TItemOut? Current => _out;

        public bool MoveNext()
        {
            if (_baseEnumerator.MoveNext())
            {
                ref readonly TItemIn transformMe = ref _baseEnumerator.Current;
                _out = _transformation(in transformMe);
                return true;
            }
            return false;
        }

        public void Reset() => _baseEnumerator.Reset();

        public RoRefInTransformEnumerator(TRoRefInEnumerator inEnumerator,
            TransformRoRefIn<TItemIn, TItemOut> transformation)
        {
            _baseEnumerator = inEnumerator;
            _transformation = transformation ?? throw new ArgumentNullException(nameof(transformation));
            _out = default;
        }

        private TRoRefInEnumerator _baseEnumerator;
        private readonly TransformRoRefIn<TItemIn, TItemOut> _transformation;
        private TItemOut? _out;
    }
    public partial struct RoRefInTransformEnumeratorValOut<TRoRefEnumerable, TRoRefInEnumerator, TItemIn, TItemOut>
        where TRoRefInEnumerator : struct, IByRoRefEnumerator<TItemIn>
        where TRoRefEnumerable : ISpecificallyRefReadOnlyEnumerable<TItemIn, TRoRefInEnumerator>
        where TItemIn : struct
        where TItemOut : struct
    {
        public readonly TItemOut Current => _out;

        public bool MoveNext()
        {
            if (_baseEnumerator.MoveNext())
            {
                ref readonly TItemIn transformMe = ref _baseEnumerator.Current;
                _out = _transformation(in transformMe);
                return true;
            }
            return false;
        }

        public void Reset() => _baseEnumerator.Reset();

        public RoRefInTransformEnumeratorValOut(TRoRefInEnumerator inEnumerator,
            TransformRoRefIn<TItemIn, TItemOut> transformation)
        {
            _baseEnumerator = inEnumerator;
            _transformation = transformation ?? throw new ArgumentNullException(nameof(transformation));
            _out = default;
        }

        private TRoRefInEnumerator _baseEnumerator;
        private readonly TransformRoRefIn<TItemIn, TItemOut> _transformation;
        private TItemOut _out;
    }

}
