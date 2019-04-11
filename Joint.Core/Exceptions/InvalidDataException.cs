using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Joint.Core.Exceptions
{

    public class InvalidDataException : CustomException
    {
        public InvalidDataException()
        { }

        public InvalidDataException(string message)
            : base(message)
        { }

        public InvalidDataException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }

}
