using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

[assembly: InternalsVisibleTo("TemplateLibraryTests")]
namespace Cjm.Templates.Utilities.SetOnce
{
    internal sealed class LocklessConcreteType
    {
        public Type ConcreteType
        {
            get
            {
                Type? ret = _concreteType;
                if (ret == null)
                {
                    ret = Volatile.Read(ref _concreteType);
                    if (ret == null)
                    {
                        Type theType = InitType();
                        Debug.Assert(theType != null);
                        Interlocked.CompareExchange(ref _concreteType, theType, null);
                        ret = Volatile.Read(ref _concreteType);
                    }
                }
                Debug.Assert(ret != null);
                return ret!;
            }
        }

        public string ConcreteTypeName => ConcreteType.Name;

        internal LocklessConcreteType(object owner) => _owner = owner ?? throw new ArgumentNullException(nameof(owner));

        private Type InitType() => _owner.GetType();


        private readonly object _owner;
        private Type? _concreteType;
    }
}
