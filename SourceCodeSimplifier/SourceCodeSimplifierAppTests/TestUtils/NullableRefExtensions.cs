﻿using NUnit.Framework;

namespace SourceCodeSimplifierAppTests.TestUtils
{
    internal static class NullableRefExtensions
    {
        public static T Must<T>(this T? source) where T : class
        {
            Assert.That(source, Is.Not.Null);
            return source!;
        }
    }
}