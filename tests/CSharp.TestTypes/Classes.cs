using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharp.TestTypes
{
    public class SingleLayer
    {
        public Guid Id { get; set; }
        public List<DateTime> Dates { get; set; }
        public DateTime Date;
    }

    public class NestedClass
    {
        public SingleLayer Single { get; set; }
        public Guid Id { get; set; }
        public DateTime Date;
    }
}
