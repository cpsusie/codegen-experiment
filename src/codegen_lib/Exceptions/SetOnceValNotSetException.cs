using System;
using System.Collections.Generic;
using System.Text;

namespace Cjm.CodeGen.Exceptions
{
    public sealed class SetOnceValNotSetException<TItem> : SetOnceValException<LocklessWriteOnce<TItem>, TItem> where TItem : class
    {
        public Type OwningType { get; }
        internal SetOnceValNotSetException(Type owningType) : base(
            CreateMessage(owningType ?? throw new ArgumentNullException(nameof(owningType))), null) => OwningType = owningType;

        static string CreateMessage(Type owningType) => $"The type {owningType.AssemblyQualifiedName}'s value has not yet been set.  Always set the value before accessing the Value property.";
    }

    public sealed class SetOnceValAlreadySetException<TItem> : SetOnceValException<LocklessWriteOnce<TItem>, TItem> where TItem : class
    {
        public TItem AttemptedToSetTo { get; }

        public TItem AlreadySetTo { get; }

        internal SetOnceValAlreadySetException(TItem valueAttemptedSet, TItem valueAlreadySet) : base(
            CreateMsg(valueAttemptedSet ?? throw new ArgumentNullException(nameof(valueAttemptedSet)),
                valueAlreadySet ?? throw new ArgumentNullException(nameof(valueAlreadySet))),
            null)
        {
            AttemptedToSetTo = valueAttemptedSet;
            AlreadySetTo = valueAlreadySet;
        }

        static string CreateMsg(TItem valueAttemptedSet, TItem valueAlreadySet) =>
            $"Illegal attempt to set value of {typeof(LocklessLazyWriteOnce<TItem>)} to {valueAttemptedSet}: the value is already set to {valueAlreadySet}.";
    }

    public abstract class SetOnceValException<TSetOnceType, TItemType> : SetOnceValException
    {
        public sealed override Type SetOnceTypeClassType { get; } = typeof(TSetOnceType);

        public sealed override Type SetOnceTypeParameter { get; } = typeof(TItemType);

        private protected SetOnceValException(string message, Exception? inner) : base(
            message ?? throw new ArgumentNullException(nameof(message)), inner) { }
    }

    public abstract class SetOnceValException : InvalidOperationException
    {
        public abstract Type SetOnceTypeClassType { get; } 

        public abstract Type SetOnceTypeParameter { get; }

        private protected SetOnceValException(string message, Exception? inner) : base(message ?? throw new ArgumentNullException(nameof(message)), inner) { }
    }

}
