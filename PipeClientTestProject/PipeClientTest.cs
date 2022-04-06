using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectWpf;

namespace PipeClientTestProject
{
    internal class PipeClientTest
    {
        public static int Main()
        {
            while (true)
            {

                Console.WriteLine( VirtualKinectPipeClient.get().timeStamp);

            }
            return 0;
        }
    }
}
