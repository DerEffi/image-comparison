using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageComparison.Models
{
    public enum ExitCode
    {
        Success = 0,
        GeneralError = 1,
        Warning = 11,
        PermissionDenied = 126,
        BadRequest = 127
    }
}
