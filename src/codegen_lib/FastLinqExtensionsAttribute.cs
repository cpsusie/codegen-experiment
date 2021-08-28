using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cjm.CodeGen
{
    [AttributeUsage(AttributeTargets.Class)] 
    public class FastLinqExtensionsAttribute : Attribute
    {
        public const string ShortName = "FastLinqExtensions";
    }  
}
