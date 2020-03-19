using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FixedThreadPool
{
   class Program
   {
      static void Main(string[] args)
      {
         FixedThreadPool fixedThreadPool = new FixedThreadPool(10);

         for (int i = 0; i < 6; i++)
         {
            thread hh = new thread("h");
            fixedThreadPool.Execute(hh, Priority.High);
         }
         for (int i = 0; i < 6; i++)
         {
            thread nn = new thread("n");
            fixedThreadPool.Execute(nn, Priority.Normal);
         }
         for (int i = 0; i < 6; i++)
         {
            thread hh = new thread("h");
            fixedThreadPool.Execute(hh, Priority.High);
         }
         for (int i = 0; i < 6; i++)
         {
            thread ll = new thread("l");
            fixedThreadPool.Execute(ll, Priority.Low);
         }
         fixedThreadPool.Stop();
         Console.ReadKey();

      }

      class thread : ITask
      {
         string str;
         public thread(string str)
         {
            this.str = str;
         }
         public void Execute()
         {
            Console.WriteLine(str);
            Thread.Sleep(10000);
         }
         
      }
   }
}
