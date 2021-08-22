using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics.Contracts;
using System.Linq;
using Cjm.CodeGen;

namespace TestApplication
{
    
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            ImmutableArray<string> str = new ImmutableArray<string>{ "Hi", "Bye" };
            foreach (var item in str)
            {
                Console.WriteLine(item + " World!");
            }

            foreach (var item in str.Select(itm => itm + " World!"))
            {
                Console.WriteLine(item);
            }

            BigImmutableStuff();
        }

        static void BigImmutableStuff()
        {
            ReadOnlyEnumerableArray<BigCoordinate> src = new BigCoordinate[]
                { new BigCoordinate(1, 2, 3), new(3, 4, 5), new(6, 7, 8) };
            foreach (ref readonly BigCoordinate bc in src)
            {
                Console.WriteLine($"Bc: {bc.ToString()}, Bc^2: {bc.Square().ToString()}");
            }

            IEnumerable<string> lines =
                ((BigCoordinate[])src).Select( static bc => $"Bc: {bc.ToString()}, Bc^2: {bc.Square().ToString()}");
            foreach (var line in lines)
            {
                Console.WriteLine(line);
            }
        }
    }

    public static class ImmutArrExtensions
    {
        public static void ApplyToAll(this ImmutableArray<string> src, Action<string> doMe)
        {
            foreach (var item in src)
            {
                doMe(item);
            }
        }
    }

    public readonly struct BigCoordinate : IEquatable<BigCoordinate>
    {
        public decimal X { get; }
        public decimal Y { get; }
        public decimal Z { get; }

        public BigCoordinate(decimal x, decimal y, decimal z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        [Pure] public BigCoordinate Square() => new BigCoordinate(X * X, Y * Y, Z * Z);

        public static bool operator ==(in BigCoordinate lhs, in BigCoordinate rhs) =>
            lhs.X == rhs.X && lhs.Y == rhs.Y && lhs.Z == rhs.Z;
        public static bool operator !=(in BigCoordinate lhs, in BigCoordinate rhs) =>
            !(lhs == rhs);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            int hash = X.GetHashCode();
            unchecked
            {
                hash = (hash * 397) ^ Y.GetHashCode();
                hash = (hash * 397) ^ Z.GetHashCode();
            }
            return hash;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is BigCoordinate bc && bc == this;

        public bool Equals(BigCoordinate other) => other == this;

        /// <inheritdoc />
        public override string ToString() => $"X: {X:F3}; Y: {Y:F3}; Z: {Z:F3}";

    }
}
