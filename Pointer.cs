using System;

namespace FftFlat
{
    internal ref struct Pointer<T>
    {
        private Span<T> span;

        public Pointer(Span<T> span)
        {
            this.span = span;
        }

        public T this[int i]
        {
            get => span[i];
            set => span[i] = value;
        }

        public Span<T> Span => span;

        public static Pointer<T> operator +(Pointer<T> p, int i)
        {
            return new Pointer<T>(p.span.Slice(i));
        }

        public static Pointer<T> operator +(int i, Pointer<T> p)
        {
            return new Pointer<T>(p.span.Slice(i));
        }
    }
}
