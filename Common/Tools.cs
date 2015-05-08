using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class Tools
    {
        public static string DateToString(DateTime time)
        {
            return time.ToString("yyyy-MM-dd HH:mm:ff");
        }
    }
}
