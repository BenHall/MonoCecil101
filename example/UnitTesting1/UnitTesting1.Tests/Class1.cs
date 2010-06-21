using System;

namespace UnitTesting1.Tests
{
    public class OneMethodClassTests
    {
        public void Test_Example1_With_One_Method_Call()
        {
            OneMethodClass oneMethodClass = new OneMethodClass();
            string v = oneMethodClass.Method1();
            Console.WriteLine(v);
        }
    }
}
