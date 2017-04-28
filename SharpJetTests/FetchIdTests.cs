// <copyright file="ConnectTests.cs" company="Hottinger Baldwin Messtechnik GmbH">
//
// SharpJet, a library to communicate with Jet IPC.
//
// The MIT License (MIT)
//
// Copyright (C) Hottinger Baldwin Messtechnik GmbH
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
// ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
// </copyright>

namespace SharpJetTests
{
    using NUnit.Framework;
    using Hbm.Devices.Jet;

    [TestFixture]
    public class FetchIdTests
    {
        [Test]
        public void TestGetId()
        {
            FetchId fetchId = new FetchId(42);
            Assert.AreEqual(42, fetchId.GetId());
        }

        [Test]
        public void TestToString()
        {
            FetchId fetchId = new FetchId(42);
            Assert.AreEqual("42", fetchId.ToString());
        }

        [Test]
        public void TestGetHashcodeReturnsId()
        {
            FetchId fetchId = new FetchId(42);
            Assert.AreEqual(42, fetchId.GetHashCode());
        }

        [Test]
        public void TestEqualsObjIsNull()
        {
            FetchId fetchId = new FetchId(42);
            bool mustBeFalse = fetchId.Equals(null);
            Assert.IsFalse(mustBeFalse, "Expected 'Equals' call to be false.");
        }

        [Test]
        public void TestEqualsFetchIdMustReturnFalse()
        {
            FetchId fetchId = new FetchId(42);
            FetchId anotherFetchId = new FetchId(15);
            bool mustBeFalse = fetchId.Equals(anotherFetchId);
            Assert.IsFalse(mustBeFalse, "Expected 'Equals' call to be false.");
        }

        [Test]
        public void TestEqualsFetchIdMustReturnTrueSameReference()
        {
            FetchId fetchId = new FetchId(42);
            bool mustBeTrue = fetchId.Equals(fetchId);
            Assert.IsTrue(mustBeTrue, "Expected 'Equals' call to be true.");
        }

        [Test]
        public void TestEqualsFetchIdMustReturnTrueNewInstance()
        {
            FetchId fetchId = new FetchId(42);
            FetchId anotherFetchId = new FetchId(42);
            bool mustBeTrue = fetchId.Equals(anotherFetchId);
            Assert.IsTrue(mustBeTrue, "Expected 'Equals' call to be true.");
        }

        [Test]
        public void TestOperatorEqualsLeftOperandIsNull()
        {
            FetchId leftOperand = null;
            FetchId rightOperand = new FetchId(42);
            Assert.IsFalse(leftOperand == rightOperand, "Expected == call to be false.");
        }

        [Test]
        public void TestOperatorEqualsBothOperandsAreNull()
        {
            FetchId leftOperand = null;
            FetchId rightOperand = null;
            Assert.IsFalse(leftOperand == rightOperand, "Expected == call to be false.");
        }

        [Test]
        public void TestOperatorNotEqualsLeftOperandIsNull()
        {
            FetchId leftOperand = null;
            FetchId rightOperand = new FetchId(42);
            Assert.IsTrue(leftOperand != rightOperand, "Expected != call to be true.");
        }

        [Test]
        public void TestOperatorNotEqualsRightOperandIsNull()
        {
            FetchId leftOperand = new FetchId(42);
            FetchId rightOperand = null;
            Assert.IsTrue(leftOperand != rightOperand, "Expected != call to be true.");
        }

        [Test]
        public void TestOperatorNotEqualsBothOperandsAreNull()
        {
            FetchId leftOperand = null;
            FetchId rightOperand = null;
            Assert.IsTrue(leftOperand != rightOperand, "Expected != call to be true.");
        }

        [Test]
        public void TestOperatorNotEqualsDifferentFetchIdsMustReturnTrue()
        {
            FetchId leftOperand = new FetchId(15);
            FetchId rightOperand = new FetchId(42);
            Assert.IsTrue(leftOperand != rightOperand, "Expected != call to be true.");
        }

        [Test]
        public void TestOperatorNotEqualsSameIdsMustReturnFalse()
        {
            FetchId leftOperand = new FetchId(42);
            FetchId rightOperand = new FetchId(42);
            Assert.IsFalse(leftOperand != rightOperand, "Expected != call to be false.");
        }

        [Test]
        public void TestOperatorNotEqualsSameInstancesMustReturnFalse()
        {
            FetchId operand = new FetchId(42);
            Assert.IsFalse(operand != operand, "Expected != call to be false.");
        }
    }
}
