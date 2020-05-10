using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Greyhound.Logic
{
    public interface IModelFormatWriter
    {
        void Export(PhilLibX.ModelTemp model, string path);
    }
}
