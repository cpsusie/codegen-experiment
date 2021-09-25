using System;
using Cjm.Templates.SetOnce;

namespace TemplateLibraryTests
{
    public abstract class TestFixtureBase
    {
        protected Type ConcreteType => _concrete.ConcreteType;
        protected string ConcreteTypeName => _concrete.ConcreteTypeName;


        protected TestFixtureBase() => _concrete = new(this);

        private readonly LocklessConcreteType _concrete;
    }
}