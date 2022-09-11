using Grpc.Core.Logging;
using Microsoft.Azure.Management.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACIFunction
{
   
    public class ContainerGroup
    {
        public string rgName { get; set; }
        public string name { get; set; }
        public string acrServer { get; set; }
        public string acrUserName { get; set; }
        public string acrPassword { get; set; }
        public string acrImageName { get; set; }
        public object startCommandLine { get; set; }
    }

    public class Data
    {
        public ContainerGroup ContainerGroup { get; set; }
    }


}



