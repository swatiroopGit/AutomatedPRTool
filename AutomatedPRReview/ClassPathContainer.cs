using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomatedPRReview
{
    internal class ClassPathContainer
    {
        string _classname;
        string _path;

        internal ClassPathContainer()
        {
        }

        internal string Classname { get => _classname; set => _classname = value; }
        internal string Path { get => _path; set => _path = value; }
    }
}
