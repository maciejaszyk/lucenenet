/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.IO;
using Lucene.Net.Support;
using NUnit.Framework;

using Directory = Lucene.Net.Store.Directory;
using IndexInput = Lucene.Net.Store.IndexInput;
using IndexOutput = Lucene.Net.Store.IndexOutput;
using SimpleFSDirectory = Lucene.Net.Store.SimpleFSDirectory;
using _TestHelper = Lucene.Net.Store._TestHelper;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
using _TestUtil = Lucene.Net.Util._TestUtil;

namespace Lucene.Net.Index
{
	[TestFixture]
	public class TestCompoundFile:LuceneTestCase
	{
#if !NETCOREAPP
        /// <summary>Main for running test case by itself. </summary>
        [STAThread]
		public static void  Main(System.String[] args)
		{
			// TestRunner.run(new TestSuite(typeof(TestCompoundFile))); // {{Aroush-2.9}} how is this done in NUnit?
			//        TestRunner.run (new TestCompoundFile("testSingleFile"));
			//        TestRunner.run (new TestCompoundFile("testTwoFiles"));
			//        TestRunner.run (new TestCompoundFile("testRandomFiles"));
			//        TestRunner.run (new TestCompoundFile("testClonedStreamsClosing"));
			//        TestRunner.run (new TestCompoundFile("testReadAfterClose"));
			//        TestRunner.run (new TestCompoundFile("testRandomAccess"));
			//        TestRunner.run (new TestCompoundFile("testRandomAccessClones"));
			//        TestRunner.run (new TestCompoundFile("testFileNotFound"));
			//        TestRunner.run (new TestCompoundFile("testReadPastEOF"));
			
			//        TestRunner.run (new TestCompoundFile("testIWCreate"));
		}
#endif


        private Directory dir;
		
		
		[SetUp]
		public override void  SetUp()
		{
			base.SetUp();

			Assert.Ignore("Known issue");

			System.IO.DirectoryInfo file = new System.IO.DirectoryInfo(System.IO.Path.Combine(AppSettings.Get("tempDir", Path.GetTempPath()), "testIndex"));
			_TestUtil.RmDir(file);
			// use a simple FSDir here, to be sure to have SimpleFSInputs
			dir = new SimpleFSDirectory(file, null);
		}
		
		[TearDown]
		public override void  TearDown()
		{
			dir?.Close();
			base.TearDown();
		}
		
		/// <summary>Creates a file of the specified size with random data. </summary>
		private void  CreateRandomFile(Directory dir, System.String name, int size)
		{
			IndexOutput os = dir.CreateOutput(name, null);
			for (int i = 0; i < size; i++)
			{
				byte b = (byte) ((new System.Random().NextDouble()) * 256);
				os.WriteByte(b);
			}
			os.Close();
		}
		
		/// <summary>Creates a file of the specified size with sequential data. The first
		/// byte is written as the start byte provided. All subsequent bytes are
		/// computed as start + offset where offset is the number of the byte.
		/// </summary>
		private void  CreateSequenceFile(Directory dir, System.String name, byte start, int size)
		{
			IndexOutput os = dir.CreateOutput(name, null);
			for (int i = 0; i < size; i++)
			{
				os.WriteByte(start);
				start++;
			}
			os.Close();
		}
		
		
		private void  AssertSameStreams(System.String msg, IndexInput expected, IndexInput test)
		{
			Assert.IsNotNull(expected, msg + " null expected");
			Assert.IsNotNull(test, msg + " null test");
			Assert.AreEqual(expected.Length(null), test.Length(null), msg + " length");
			Assert.AreEqual(expected.FilePointer(null), test.FilePointer(null), msg + " position");
			
			byte[] expectedBuffer = new byte[512];
			byte[] testBuffer = new byte[expectedBuffer.Length];
			
			long remainder = expected.Length(null) - expected.FilePointer(null);
			while (remainder > 0)
			{
				int readLen = (int) System.Math.Min(remainder, expectedBuffer.Length);
				expected.ReadBytes(expectedBuffer, 0, readLen, null);
				test.ReadBytes(testBuffer, 0, readLen, null);
				AssertEqualArrays(msg + ", remainder " + remainder, expectedBuffer, testBuffer, 0, readLen);
				remainder -= readLen;
			}
		}
		
		
		private void  AssertSameStreams(System.String msg, IndexInput expected, IndexInput actual, long seekTo)
		{
			if (seekTo >= 0 && seekTo < expected.Length(null))
			{
				expected.Seek(seekTo, null);
				actual.Seek(seekTo, null);
				AssertSameStreams(msg + ", seek(mid)", expected, actual);
			}
		}
		
		
		
		private void  AssertSameSeekBehavior(System.String msg, IndexInput expected, IndexInput actual)
		{
			// seek to 0
			long point = 0;
			AssertSameStreams(msg + ", seek(0)", expected, actual, point);
			
			// seek to middle
			point = expected.Length(null) / 2L;
			AssertSameStreams(msg + ", seek(mid)", expected, actual, point);
			
			// seek to end - 2
			point = expected.Length(null) - 2;
			AssertSameStreams(msg + ", seek(end-2)", expected, actual, point);
			
			// seek to end - 1
			point = expected.Length(null) - 1;
			AssertSameStreams(msg + ", seek(end-1)", expected, actual, point);
			
			// seek to the end
			point = expected.Length(null);
			AssertSameStreams(msg + ", seek(end)", expected, actual, point);
			
			// seek past end
			point = expected.Length(null) + 1;
			AssertSameStreams(msg + ", seek(end+1)", expected, actual, point);
		}
		
		
		private void  AssertEqualArrays(System.String msg, byte[] expected, byte[] test, int start, int len)
		{
			Assert.IsNotNull(expected, msg + " null expected");
			Assert.IsNotNull(test, msg + " null test");
			
			for (int i = start; i < len; i++)
			{
				Assert.AreEqual(expected[i], test[i], msg + " " + i);
			}
		}
		
		
		// ===========================================================
		//  Tests of the basic CompoundFile functionality
		// ===========================================================
		
		
		/// <summary>This test creates compound file based on a single file.
		/// Files of different sizes are tested: 0, 1, 10, 100 bytes.
		/// </summary>
		[Test]
		public virtual void  TestSingleFile()
		{
			int[] data = new int[]{0, 1, 10, 100};
			for (int i = 0; i < data.Length; i++)
			{
				System.String name = "t" + data[i];
				CreateSequenceFile(dir, name, (byte) 0, data[i]);
				CompoundFileWriter csw = new CompoundFileWriter(dir, name + ".cfs");
				csw.AddFile(name);
				csw.Close();
				
				CompoundFileReader csr = new CompoundFileReader(dir, name + ".cfs", null);
				IndexInput expected = dir.OpenInput(name, null);
				IndexInput actual = csr.OpenInput(name, null);
				AssertSameStreams(name, expected, actual);
				AssertSameSeekBehavior(name, expected, actual);
				expected.Close();
				actual.Close();
				csr.Close();
			}
		}
		
		
		/// <summary>This test creates compound file based on two files.
		/// 
		/// </summary>
		[Test]
		public virtual void  TestTwoFiles()
		{
			CreateSequenceFile(dir, "d1", (byte) 0, 15);
			CreateSequenceFile(dir, "d2", (byte) 0, 114);
			
			CompoundFileWriter csw = new CompoundFileWriter(dir, "d.csf");
			csw.AddFile("d1");
			csw.AddFile("d2");
			csw.Close();
			
			CompoundFileReader csr = new CompoundFileReader(dir, "d.csf", null);
			IndexInput expected = dir.OpenInput("d1", null);
			IndexInput actual = csr.OpenInput("d1", null);
			AssertSameStreams("d1", expected, actual);
			AssertSameSeekBehavior("d1", expected, actual);
			expected.Close();
			actual.Close();
			
			expected = dir.OpenInput("d2", null);
			actual = csr.OpenInput("d2", null);
			AssertSameStreams("d2", expected, actual);
			AssertSameSeekBehavior("d2", expected, actual);
			expected.Close();
			actual.Close();
			csr.Close();
		}
		
		/// <summary>This test creates a compound file based on a large number of files of
		/// various length. The file content is generated randomly. The sizes range
		/// from 0 to 1Mb. Some of the sizes are selected to test the buffering
		/// logic in the file reading code. For this the chunk variable is set to
		/// the length of the buffer used internally by the compound file logic.
		/// </summary>
		[Test]
		public virtual void  TestRandomFiles()
		{
			// Setup the test segment
			System.String segment = "test";
			int chunk = 1024; // internal buffer size used by the stream
			CreateRandomFile(dir, segment + ".zero", 0);
			CreateRandomFile(dir, segment + ".one", 1);
			CreateRandomFile(dir, segment + ".ten", 10);
			CreateRandomFile(dir, segment + ".hundred", 100);
			CreateRandomFile(dir, segment + ".big1", chunk);
			CreateRandomFile(dir, segment + ".big2", chunk - 1);
			CreateRandomFile(dir, segment + ".big3", chunk + 1);
			CreateRandomFile(dir, segment + ".big4", 3 * chunk);
			CreateRandomFile(dir, segment + ".big5", 3 * chunk - 1);
			CreateRandomFile(dir, segment + ".big6", 3 * chunk + 1);
			CreateRandomFile(dir, segment + ".big7", 1000 * chunk);
			
			// Setup extraneous files
			CreateRandomFile(dir, "onetwothree", 100);
			CreateRandomFile(dir, segment + ".notIn", 50);
			CreateRandomFile(dir, segment + ".notIn2", 51);
			
			// Now test
			CompoundFileWriter csw = new CompoundFileWriter(dir, "test.cfs");
			System.String[] data = new System.String[]{".zero", ".one", ".ten", ".hundred", ".big1", ".big2", ".big3", ".big4", ".big5", ".big6", ".big7"};
			for (int i = 0; i < data.Length; i++)
			{
				csw.AddFile(segment + data[i]);
			}
			csw.Close();
			
			CompoundFileReader csr = new CompoundFileReader(dir, "test.cfs", null);
			for (int i = 0; i < data.Length; i++)
			{
				IndexInput check = dir.OpenInput(segment + data[i], null);
				IndexInput test = csr.OpenInput(segment + data[i], null);
				AssertSameStreams(data[i], check, test);
				AssertSameSeekBehavior(data[i], check, test);
				test.Close();
				check.Close();
			}
			csr.Close();
		}
		
		
		/// <summary>Setup a larger compound file with a number of components, each of
		/// which is a sequential file (so that we can easily tell that we are
		/// reading in the right byte). The methods sets up 20 files - f0 to f19,
		/// the size of each file is 1000 bytes.
		/// </summary>
		private void  SetUp_2()
		{
			CompoundFileWriter cw = new CompoundFileWriter(dir, "f.comp");
			for (int i = 0; i < 20; i++)
			{
				CreateSequenceFile(dir, "f" + i, (byte) 0, 2000);
				cw.AddFile("f" + i);
			}
			cw.Close();
		}
		
		
		[Test]
		public virtual void  TestReadAfterClose()
		{
			Demo_FSIndexInputBug(dir, "test");
		}
		
		private void  Demo_FSIndexInputBug(Directory fsdir, System.String file)
		{
			// Setup the test file - we need more than 1024 bytes
			IndexOutput os = fsdir.CreateOutput(file, null);
			for (int i = 0; i < 2000; i++)
			{
				os.WriteByte((byte) i);
			}
			os.Close();
			
			IndexInput in_Renamed = fsdir.OpenInput(file, null);
			
			// This read primes the buffer in IndexInput
			byte b = in_Renamed.ReadByte(null);
			
			// Close the file
			in_Renamed.Close();
			
			// ERROR: this call should fail, but succeeds because the buffer
			// is still filled
			b = in_Renamed.ReadByte(null);
			
			// ERROR: this call should fail, but succeeds for some reason as well
			in_Renamed.Seek(1099, null);
			
			// OK: this call correctly fails. We are now past the 1024 internal
			// buffer, so an actual IO is attempted, which fails
            Assert.Throws<NullReferenceException>(() => in_Renamed.ReadByte(null), "expected readByte() to throw exception");
		}
		
		
		internal static bool IsCSIndexInput(IndexInput is_Renamed)
		{
			return is_Renamed is CompoundFileReader.CSIndexInput;
		}
		
		internal static bool IsCSIndexInputOpen(IndexInput is_Renamed)
		{
			if (IsCSIndexInput(is_Renamed))
			{
				CompoundFileReader.CSIndexInput cis = (CompoundFileReader.CSIndexInput) is_Renamed;
				
				return _TestHelper.IsSimpleFSIndexInputOpen(cis.base_Renamed_ForNUnit);
			}
			else
			{
				return false;
			}
		}
		
		
		[Test]
		public virtual void  TestClonedStreamsClosing()
		{
			SetUp_2();
			CompoundFileReader cr = new CompoundFileReader(dir, "f.comp", null);
			
			// basic clone
			IndexInput expected = dir.OpenInput("f11", null);
			
			// this test only works for FSIndexInput
			Assert.IsTrue(_TestHelper.IsSimpleFSIndexInput(expected));
			Assert.IsTrue(_TestHelper.IsSimpleFSIndexInputOpen(expected));
			
			IndexInput one = cr.OpenInput("f11", null);
			Assert.IsTrue(IsCSIndexInputOpen(one));
			
			IndexInput two = (IndexInput) one.Clone(null);
			Assert.IsTrue(IsCSIndexInputOpen(two));
			
			AssertSameStreams("basic clone one", expected, one);
			expected.Seek(0, null);
			AssertSameStreams("basic clone two", expected, two);
			
			// Now close the first stream
			one.Close();
			Assert.IsTrue(IsCSIndexInputOpen(one), "Only close when cr is closed");
			
			// The following should really fail since we couldn't expect to
			// access a file once close has been called on it (regardless of
			// buffering and/or clone magic)
			expected.Seek(0, null);
			two.Seek(0, null);
			AssertSameStreams("basic clone two/2", expected, two);
			
			
			// Now close the compound reader
			cr.Close();
			Assert.IsFalse(IsCSIndexInputOpen(one), "Now closed one");
			Assert.IsFalse(IsCSIndexInputOpen(two), "Now closed two");
			
			// The following may also fail since the compound stream is closed
			expected.Seek(0, null);
			two.Seek(0, null);
			//assertSameStreams("basic clone two/3", expected, two);
			
			
			// Now close the second clone
			two.Close();
			expected.Seek(0, null);
			two.Seek(0, null);
			//assertSameStreams("basic clone two/4", expected, two);
			
			expected.Close();
		}
		
		
		/// <summary>This test opens two files from a compound stream and verifies that
		/// their file positions are independent of each other.
		/// </summary>
		[Test]
		public virtual void  TestRandomAccess()
		{
			SetUp_2();
			CompoundFileReader cr = new CompoundFileReader(dir, "f.comp", null);
			
			// Open two files
			IndexInput e1 = dir.OpenInput("f11", null);
			IndexInput e2 = dir.OpenInput("f3", null);
			
			IndexInput a1 = cr.OpenInput("f11", null);
			IndexInput a2 = dir.OpenInput("f3", null);
			
			// Seek the first pair
			e1.Seek(100, null);
			a1.Seek(100, null);
			Assert.AreEqual(100, e1.FilePointer(null));
			Assert.AreEqual(100, a1.FilePointer(null));
			byte be1 = e1.ReadByte(null);
			byte ba1 = a1.ReadByte(null);
			Assert.AreEqual(be1, ba1);
			
			// Now seek the second pair
			e2.Seek(1027, null);
			a2.Seek(1027, null);
			Assert.AreEqual(1027, e2.FilePointer(null));
			Assert.AreEqual(1027, a2.FilePointer(null));
			byte be2 = e2.ReadByte(null);
			byte ba2 = a2.ReadByte(null);
			Assert.AreEqual(be2, ba2);
			
			// Now make sure the first one didn't move
			Assert.AreEqual(101, e1.FilePointer(null));
			Assert.AreEqual(101, a1.FilePointer(null));
			be1 = e1.ReadByte(null);
			ba1 = a1.ReadByte(null);
			Assert.AreEqual(be1, ba1);
			
			// Now more the first one again, past the buffer length
			e1.Seek(1910, null);
			a1.Seek(1910, null);
			Assert.AreEqual(1910, e1.FilePointer(null));
			Assert.AreEqual(1910, a1.FilePointer(null));
			be1 = e1.ReadByte(null);
			ba1 = a1.ReadByte(null);
			Assert.AreEqual(be1, ba1);
			
			// Now make sure the second set didn't move
			Assert.AreEqual(1028, e2.FilePointer(null));
			Assert.AreEqual(1028, a2.FilePointer(null));
			be2 = e2.ReadByte(null);
			ba2 = a2.ReadByte(null);
			Assert.AreEqual(be2, ba2);
			
			// Move the second set back, again cross the buffer size
			e2.Seek(17, null);
			a2.Seek(17, null);
			Assert.AreEqual(17, e2.FilePointer(null));
			Assert.AreEqual(17, a2.FilePointer(null));
			be2 = e2.ReadByte(null);
			ba2 = a2.ReadByte(null);
			Assert.AreEqual(be2, ba2);
			
			// Finally, make sure the first set didn't move
			// Now make sure the first one didn't move
			Assert.AreEqual(1911, e1.FilePointer(null));
			Assert.AreEqual(1911, a1.FilePointer(null));
			be1 = e1.ReadByte(null);
			ba1 = a1.ReadByte(null);
			Assert.AreEqual(be1, ba1);
			
			e1.Close();
			e2.Close();
			a1.Close();
			a2.Close();
			cr.Close();
		}
		
		/// <summary>This test opens two files from a compound stream and verifies that
		/// their file positions are independent of each other.
		/// </summary>
		[Test]
		public virtual void  TestRandomAccessClones()
		{
			SetUp_2();
			CompoundFileReader cr = new CompoundFileReader(dir, "f.comp", null);
			
			// Open two files
			IndexInput e1 = cr.OpenInput("f11", null);
			IndexInput e2 = cr.OpenInput("f3", null);
			
			IndexInput a1 = (IndexInput) e1.Clone(null);
			IndexInput a2 = (IndexInput) e2.Clone(null);
			
			// Seek the first pair
			e1.Seek(100, null);
			a1.Seek(100, null);
			Assert.AreEqual(100, e1.FilePointer(null));
			Assert.AreEqual(100, a1.FilePointer(null));
			byte be1 = e1.ReadByte(null);
			byte ba1 = a1.ReadByte(null);
			Assert.AreEqual(be1, ba1);
			
			// Now seek the second pair
			e2.Seek(1027, null);
			a2.Seek(1027, null);
			Assert.AreEqual(1027, e2.FilePointer(null));
			Assert.AreEqual(1027, a2.FilePointer(null));
			byte be2 = e2.ReadByte(null);
			byte ba2 = a2.ReadByte(null);
			Assert.AreEqual(be2, ba2);
			
			// Now make sure the first one didn't move
			Assert.AreEqual(101, e1.FilePointer(null));
			Assert.AreEqual(101, a1.FilePointer(null));
			be1 = e1.ReadByte(null);
			ba1 = a1.ReadByte(null);
			Assert.AreEqual(be1, ba1);
			
			// Now more the first one again, past the buffer length
			e1.Seek(1910, null);
			a1.Seek(1910, null);
			Assert.AreEqual(1910, e1.FilePointer(null));
			Assert.AreEqual(1910, a1.FilePointer(null));
			be1 = e1.ReadByte(null);
			ba1 = a1.ReadByte(null);
			Assert.AreEqual(be1, ba1);
			
			// Now make sure the second set didn't move
			Assert.AreEqual(1028, e2.FilePointer(null));
			Assert.AreEqual(1028, a2.FilePointer(null));
			be2 = e2.ReadByte(null);
			ba2 = a2.ReadByte(null);
			Assert.AreEqual(be2, ba2);
			
			// Move the second set back, again cross the buffer size
			e2.Seek(17, null);
			a2.Seek(17, null);
			Assert.AreEqual(17, e2.FilePointer(null));
			Assert.AreEqual(17, a2.FilePointer(null));
			be2 = e2.ReadByte(null);
			ba2 = a2.ReadByte(null);
			Assert.AreEqual(be2, ba2);
			
			// Finally, make sure the first set didn't move
			// Now make sure the first one didn't move
			Assert.AreEqual(1911, e1.FilePointer(null));
			Assert.AreEqual(1911, a1.FilePointer(null));
			be1 = e1.ReadByte(null);
			ba1 = a1.ReadByte(null);
			Assert.AreEqual(be1, ba1);
			
			e1.Close();
			e2.Close();
			a1.Close();
			a2.Close();
			cr.Close();
		}
		
		
		[Test]
		public virtual void  TestFileNotFound()
		{
			SetUp_2();
			CompoundFileReader cr = new CompoundFileReader(dir, "f.comp", null);
			// Open two files
		    Assert.Throws<System.IO.IOException>(() => cr.OpenInput("bogus", null), "File not found");
			cr.Close();
		}
		
		
		[Test]
		public virtual void  TestReadPastEOF()
		{
			SetUp_2();
			CompoundFileReader cr = new CompoundFileReader(dir, "f.comp", null);
			IndexInput is_Renamed = cr.OpenInput("f2", null);
			is_Renamed.Seek(is_Renamed.Length(null) - 10, null);
			byte[] b = new byte[100];
			is_Renamed.ReadBytes(b, 0, 10, null);
			
            Assert.Throws<System.IO.IOException>(() => is_Renamed.ReadByte(null), "Single byte read past end of file");
			
			is_Renamed.Seek(is_Renamed.Length(null) - 10, null);
		    Assert.Throws<System.IO.IOException>(() => is_Renamed.ReadBytes(b, 0, 50, null), "Block read past end of file");
			
			is_Renamed.Close();
			cr.Close();
		}
		
		/// <summary>This test that writes larger than the size of the buffer output
		/// will correctly increment the file pointer.
		/// </summary>
		[Test]
		public virtual void  TestLargeWrites()
		{
			IndexOutput os = dir.CreateOutput("testBufferStart.txt", null);
			
			byte[] largeBuf = new byte[2048];
			for (int i = 0; i < largeBuf.Length; i++)
			{
				largeBuf[i] = (byte) ((new System.Random().NextDouble()) * 256);
			}
			
			long currentPos = os.FilePointer;
			os.WriteBytes(largeBuf, largeBuf.Length);
			
			try
			{
				Assert.AreEqual(currentPos + largeBuf.Length, os.FilePointer);
			}
			finally
			{
				os.Close();
			}
		}
	}
}