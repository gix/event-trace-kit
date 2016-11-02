namespace EventTraceKit.VsExtension.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using VsExtension.Serialization;
    using Xunit;

    public class TypeHelperTest
    {
        private class StringCollection : Collection<string>
        {
        }

        private class DerivedStringCollection : StringCollection
        {
        }

        private class MultiCollection : List<string>, IList<int>
        {
            IEnumerator<int> IEnumerable<int>.GetEnumerator()
            {
                throw new NotImplementedException();
            }

            void ICollection<int>.Add(int item)
            {
                throw new NotImplementedException();
            }

            bool ICollection<int>.Contains(int item)
            {
                throw new NotImplementedException();
            }

            void ICollection<int>.CopyTo(int[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            bool ICollection<int>.Remove(int item)
            {
                throw new NotImplementedException();
            }

            bool ICollection<int>.IsReadOnly
            {
                get { throw new NotImplementedException(); }
            }

            int IList<int>.IndexOf(int item)
            {
                throw new NotImplementedException();
            }

            void IList<int>.Insert(int index, int item)
            {
                throw new NotImplementedException();
            }

            int IList<int>.this[int index]
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
        }

        [Theory]
        [InlineData(typeof(IList<int>), typeof(int))]
        [InlineData(typeof(IList<string>), typeof(string))]
        [InlineData(typeof(List<string>), typeof(string))]
        [InlineData(typeof(string[]), typeof(string))]
        [InlineData(typeof(int[][]), typeof(int[]))]
        [InlineData(typeof(List<List<string>>), typeof(List<string>))]
        [InlineData(typeof(Collection<string>), typeof(string))]
        [InlineData(typeof(StringCollection), typeof(string))]
        [InlineData(typeof(DerivedStringCollection), typeof(string))]
        [InlineData(typeof(string), null)]
        [InlineData(typeof(MultiCollection), null)]
        public void TryGetCollectionItemType(Type listType, Type expected)
        {
            Type itemType;
            Assert.Equal(expected != null, TypeHelper.TryGetGenericListItemType(listType, out itemType));
            Assert.Equal(expected, itemType);
        }
    }
}
