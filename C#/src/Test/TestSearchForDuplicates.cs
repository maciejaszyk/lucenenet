/*
 * Copyright 2004 The Apache Software Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
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
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Searchable = Lucene.Net.Search.Searchable;
using Lucene.Net.Store;
using NUnit.Framework;

namespace Lucene.Net
{
	
	
	/// <summary>JUnit adaptation of an older test case DocTest.</summary>
	/// <author>  dmitrys@earthlink.net
	/// </author>
	/// <version>  $Id: TestSearchForDuplicates.java 150494 2004-09-06 22:29:22Z dnaber $
	/// </version>
	[TestFixture]
    public class TestSearchForDuplicates
	{
		
		/// <summary>Main for running test case by itself. </summary>
		[STAThread]
		public static void  Main(System.String[] args)
		{
			// NUnit.Core.TestRunner.Run(new NUnit.Core.TestSuite(typeof(TestSearchForDuplicates)));    // {{Aroush}} where is 'TestRunner' in NUnit
		}
		
		
		
		internal const System.String PRIORITY_FIELD = "priority";
		internal const System.String ID_FIELD = "id";
		internal const System.String HIGH_PRIORITY = "high";
		internal const System.String MED_PRIORITY = "medium";
		internal const System.String LOW_PRIORITY = "low";
		
		
		/// <summary>This test compares search results when using and not using compound
		/// files.
		/// 
		/// TODO: There is rudimentary search result validation as well, but it is
		/// simply based on asserting the output observed in the old test case,
		/// without really knowing if the output is correct. Someone needs to
		/// validate this output and make any changes to the checkHits method.
		/// </summary>
		[Test]
        public virtual void  TestRun()
		{
			System.IO.StringWriter sw = new System.IO.StringWriter();
			System.IO.StreamWriter pw = null; // new System.IO.StreamWriter(sw);    // {{Aroush}} how do we pass 'sw' to StreamWriter?
			DoTest(pw, false);
			pw.Close();
			sw.Close();
			System.String multiFileOutput = sw.GetStringBuilder().ToString();
			//System.out.println(multiFileOutput);
			
			sw = new System.IO.StringWriter();
			pw = null; // new System.IO.StreamWriter(sw);   // {{Aroush}} how do we pass 'sw' to StreamWriter?
			DoTest(pw, true);
			pw.Close();
			sw.Close();
			System.String singleFileOutput = sw.GetStringBuilder().ToString();
			
			Assert.AreEqual(multiFileOutput, singleFileOutput);
		}
		
		
		private void  DoTest(System.IO.StreamWriter out_Renamed, bool useCompoundFiles)
		{
			Directory directory = new RAMDirectory();
			Analyzer analyzer = new SimpleAnalyzer();
			IndexWriter writer = new IndexWriter(directory, analyzer, true);
			
			writer.SetUseCompoundFile(useCompoundFiles);
			
			//UPGRADE_NOTE: Final was removed from the declaration of 'MAX_DOCS '. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"
			int MAX_DOCS = 225;
			
			for (int j = 0; j < MAX_DOCS; j++)
			{
				Lucene.Net.Documents.Document d = new Lucene.Net.Documents.Document();
				d.Add(new Field(PRIORITY_FIELD, HIGH_PRIORITY, Field.Store.YES, Field.Index.TOKENIZED));
				d.Add(new Field(ID_FIELD, System.Convert.ToString(j), Field.Store.YES, Field.Index.TOKENIZED));
				writer.AddDocument(d);
			}
			writer.Close();
			
			// try a search without OR
			Searcher searcher = new IndexSearcher(directory);
			Hits hits = null;
			
			Lucene.Net.QueryParsers.QueryParser parser = new Lucene.Net.QueryParsers.QueryParser(PRIORITY_FIELD, analyzer);
			
			Query query = parser.Parse(HIGH_PRIORITY);
			//UPGRADE_TODO: Method 'java.io.PrintWriter.println' was converted to 'System.IO.TextWriter.WriteLine' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioPrintWriterprintln_javalangString'"
			out_Renamed.WriteLine("Query: " + query.ToString(PRIORITY_FIELD));
			
			hits = searcher.Search(query);
			PrintHits(out_Renamed, hits);
			CheckHits(hits, MAX_DOCS);
			
			searcher.Close();
			
			// try a new search with OR
			searcher = new IndexSearcher(directory);
			hits = null;
			
			parser = new Lucene.Net.QueryParsers.QueryParser(PRIORITY_FIELD, analyzer);
			
			query = parser.Parse(HIGH_PRIORITY + " OR " + MED_PRIORITY);
			//UPGRADE_TODO: Method 'java.io.PrintWriter.println' was converted to 'System.IO.TextWriter.WriteLine' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioPrintWriterprintln_javalangString'"
			out_Renamed.WriteLine("Query: " + query.ToString(PRIORITY_FIELD));
			
			hits = searcher.Search(query);
			PrintHits(out_Renamed, hits);
			CheckHits(hits, MAX_DOCS);
			
			searcher.Close();
		}
		
		
		private void  PrintHits(System.IO.StreamWriter out_Renamed, Hits hits)
		{
			//UPGRADE_TODO: Method 'java.io.PrintWriter.println' was converted to 'System.IO.TextWriter.WriteLine' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioPrintWriterprintln_javalangString'"
			out_Renamed.WriteLine(hits.Length() + " total results\n");
			for (int i = 0; i < hits.Length(); i++)
			{
				if (i < 10 || (i > 94 && i < 105))
				{
					Lucene.Net.Documents.Document d = hits.Doc(i);
					//UPGRADE_TODO: Method 'java.io.PrintWriter.println' was converted to 'System.IO.TextWriter.WriteLine' which has a different behavior. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1073_javaioPrintWriterprintln_javalangString'"
					out_Renamed.WriteLine(i + " " + d.Get(ID_FIELD));
				}
			}
		}
		
		private void  CheckHits(Hits hits, int expectedCount)
		{
			Assert.AreEqual(expectedCount, hits.Length(), "total results");
			for (int i = 0; i < hits.Length(); i++)
			{
				if (i < 10 || (i > 94 && i < 105))
				{
					Lucene.Net.Documents.Document d = hits.Doc(i);
					Assert.AreEqual("check " + i, System.Convert.ToString(i), d.Get(ID_FIELD));
				}
			}
		}
	}
}