// Copyright (C) 2018 Hylke Bons <hi@planetpeanut.uk>
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
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


using NUnit.Framework; 

using System;
using Sparkles;

namespace Sparkles.Tests {

    [TestFixture ()]
    public class TestExtensions {

        [Test ()]
        public void ReturnSHA256 ()
        {
            string result = "hello".SHA256 ();
            Assert.IsTrue (result == "2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
        }


        [Test ()]
        public void ReturnSHA256WithSalt ()
        {
            string salt = "salt";
            string result = "hello".SHA256 (salt);

            Assert.IsTrue (result == "87daba3fe263b34c335a0ee3b28ffec4d159aad6542502eaf551dc7b9128c267");
        }


        [Test ()]
        public void ReturnMD5 ()
        {
            string result = "hello".MD5 ();
            Assert.IsTrue (result == "5d41402abc4b2a76b9719d911017c592");
        }


        string cipher_text;
        string plain_text = "secret";
        string password = "password";

        [Test (), Order (1)]
        public void ReturnAESEncrypt ()
        {
            string result = plain_text.AESEncrypt (password);
            cipher_text = result;

            Assert.That (result, Is.Not.Null.And.Not.Empty);
        }    


        [Test (), Order (2)]
        public void ReturnAESDecrypt ()
        {
            string result = cipher_text.AESDecrypt (password);
            Assert.IsTrue (result == plain_text);
        }


        [Test ()]
        public void ReturnReplaceUnderScoreWithSpace ()
        {
            string result = "good_morning_to_you".ReplaceUnderscoreWithSpace ();
            Assert.IsTrue (result == "good morning to you");
        }


        [Test ()]
        public void ReturnToSize ()
        {
            Assert.IsTrue (1099511627776.0.ToSize () == "1 ᴛʙ");
            Assert.IsTrue (1073741824.0.ToSize () == "1 ɢʙ");
            Assert.IsTrue (1048576.0.ToSize () == "1 ᴍʙ");
            Assert.IsTrue (1024.0.ToSize () == "1 ᴋʙ");
            Assert.IsTrue (0.0.ToSize () == "0 ʙ");
        }


        [Test ()]
        public void ReturnToPrettyDate ()
        {
            // TODO
        }


        [Test ()]
        public void ReturnIsSymlink ()
        {
            // TODO
        }
    }
}
