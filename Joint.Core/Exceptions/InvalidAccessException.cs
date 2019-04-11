using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Joint.Core.Exceptions
{

    public class InvalidAccessException : CustomException
    {
        public InvalidAccessException()
        { }

        public InvalidAccessException(string message)
            : base(message)
        { }

        public InvalidAccessException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }

}
