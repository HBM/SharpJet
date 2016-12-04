// <copyright file="FetchId.cs" company="Hottinger Baldwin Messtechnik GmbH">
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

namespace Hbm.Devices.Jet
{
    public class FetchId
    {
        private int id;

        internal FetchId(int id)
        {
            this.id = id;
        }

        internal int GetId()
        {
            return this.id;
        }

        public override string ToString()
        {
            return this.id.ToString();
        }

        public override bool Equals(object obj)
        {
            FetchId other = obj as FetchId;
            if (other == null)
            {
                return false;
            }

            return other.id == this.id;
        }

        public override int GetHashCode()
        {
            return id;
        }

        public static bool operator ==(FetchId a, FetchId b)
        {
            if (object.ReferenceEquals(a, b))
            {
                return true;
            }

            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a.id == b.id;
        }

        public static bool operator !=(FetchId a, FetchId b)
        {
            return !(a == b);
        }
    }
}
