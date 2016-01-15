using System;
using System.Runtime.Serialization;

namespace ServiceRouter.ServiceDiscovery
{
    [Serializable]
    internal class UnRoutableAddressException : Exception
    {
        public UnRoutableAddressException()
        {
        }

        public UnRoutableAddressException(string message) : base(message)
        {
        }

        public UnRoutableAddressException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected UnRoutableAddressException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}