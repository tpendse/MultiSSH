using MultiSSH.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiSSH
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var test_run = new SshConnection("suvlic603", "tejas.pendse", "password"))
            {
                test_run.Run("ls");
            }

            Console.Write("Done! Press any key to continue ... ");
            Console.ReadKey();
        }
    }
}
