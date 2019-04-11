using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Joint.Core.Exceptions
{

    public class OAuthException : CustomException
    {
        public OAuthException()
        { }

        public OAuthException(string message)
            : base(message)
        { }

        public OAuthException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }

}
