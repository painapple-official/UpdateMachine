using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateMachine.Core;

namespace UpdateMachine.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var a = new Updater("http://s.lictex.pw/TestUpdater2/");
            a.OnStatusChanged += status => Console.WriteLine(status);
            a.OnProgressChanged += (current, total) => Console.WriteLine("{0}/{1}", current, total);
            a.OnException += error => Console.WriteLine("Exception: {0}", error.Message);
            a.Update();

            Console.WriteLine("你成功了?");
            Console.ReadLine();
        }
    }
}
