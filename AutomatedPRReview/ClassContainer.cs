using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomatedPRReview
{
    internal class ClassContainer
    {
        string _classname;
        string _namespace;
        string _code;

        internal ClassContainer()
        {
        }

        internal ClassContainer(string classname, string namespce, string code)
        {
            this.Classname = classname;
            this.Namespace = namespce;
            this.Code = code;
        }

        internal string Classname { get => _classname; set => _classname = value; }
        internal string Code { get => _code; set => _code = value; }
        internal string Namespace { get => _namespace; set => _namespace = value; }
    }
}
