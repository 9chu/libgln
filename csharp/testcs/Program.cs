using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace testcs
{
    class Program
    {
        class TestListener : Bakachu.GLN.Parser.IParseListener
        {
            private ArrayList _RootList = new ArrayList();

            public ArrayList Root
            {
                get
                {
                    return _RootList;
                }
            }

            public void OnParseValue(Bakachu.GLN.Parser.DataType ValueType, Bakachu.GLN.Parser.DataFormatType FormatType, object Value, object Parent)
            {
                Console.WriteLine(String.Format("<VALUE> {0} {1} {2}", ValueType, FormatType, Value));
                if (Parent == null)
                    Root.Add(Value);
                else
                    ((ArrayList)Parent).Add(Value);
            }

            public void OnParseComment(string Comment)
            {
                Console.WriteLine("<COMMENT> " + Comment);
            }

            public object OnParseList()
            {
                Console.WriteLine("<LIST CREATED>");
                return new ArrayList();
            }

            public void OnReachEOF()
            {
                Console.WriteLine("<EOF>");
            }
        }

        static void PrintList(object o, int Indent = 0)
        {
            Console.WriteLine(new String(' ', Indent * 3) + o.ToString());

            if (o.GetType() == typeof(ArrayList))
            {
                foreach (object x in (ArrayList)o)
                {
                    PrintList(x, Indent + 1);
                }
            }
        }

        static void PrintList(Bakachu.GLN.Value o, int Indent = 0)
        {
            Console.WriteLine(new String(' ', Indent * 3) + o.ToString());

            if (o.ValueType == Bakachu.GLN.Parser.DataType.List)
            {
                foreach (Bakachu.GLN.Value x in o.ToList())
                {
                    PrintList(x, Indent + 1);
                }
            }
        }

        static void Main(string[] args)
        {
            string tTestSource = @"
                shop {
                    bookshelf : [lastAccess:""10 days ago""] {
                        book(name:""The CXX Programming Language"" price:450.0)
                    }
                }
            ";

            /*
             * TEST 1
            TestListener tListener = new TestListener();

            try
            {
                Bakachu.GLN.Parser.FromString(tTestSource, tListener);
            }
            catch (Exception e)
            {
                Console.WriteLine("<ERROR> " + e.Message);
            }
            
            PrintList(tListener.Root);
            */

            /*
             * TEST2
             */
            Bakachu.GLN.Value tResult = Bakachu.GLN.Parser.ParseToListFromString(tTestSource);

            PrintList(tResult);

            Console.Write(tResult.Write(new Bakachu.GLN.Value.ValueOutputFormat()));

            Console.ReadKey();
        }
    }
}
