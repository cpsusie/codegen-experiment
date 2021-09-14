using System;
using System.Collections.Generic;
using System.Text;

namespace Cjm.CodeGen
{
    public sealed class UninitializedTargetData
    {
        internal static UninitializedTargetData Instance { get; } = new ();

        public int Current { get; }

        public ref int CurrentRef => ref _foo;

        public ref readonly int CurrentRoRef => ref _foo;

        public bool MoveNext() => true;

        /// <inheritdoc />
        public override string ToString() =>
            $"This class is used to be the named type symbol referred to when a struct with non-nullable public named type symbols has not been properly initialized.";

        private int _foo;
        private UninitializedTargetData(){}


    }
}
