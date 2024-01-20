using Betfair.ESASwagger.Model;
using System;

namespace Betfair.ESAClient.Protocol
{
    /// <summary>
    /// Exception used by api to raise a status fail.
    /// </summary>
    public class StatusException(StatusMessage message) : Exception(message.ErrorCode +": " +message.ErrorMessage)
    {
        public readonly StatusMessage.ErrorCodeEnum ErrorCode = (StatusMessage.ErrorCodeEnum)message.ErrorCode;
        public readonly string ErrorMessage = message.ErrorMessage;
    }
}