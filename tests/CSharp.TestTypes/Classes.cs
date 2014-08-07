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

    public class WithNullable
    {
        public int? NullableInt { get; set; }
    }

    public class WithIndexedProperty
    {
        public WithIndexedProperty(bool Complete)
        {
            SingleLayer single;
            if (Complete)
            {
                single = new SingleLayer();
                single.Date = DateTime.Now;
                single.Dates = new List<DateTime>();
                single.Id = Guid.NewGuid();
            }
            else
            {
                single = new SingleLayer();
            }
            Singles = new List<SingleLayer> { single };
        }

        public List<SingleLayer> Singles { get; set; }
    }
}
