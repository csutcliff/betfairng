using Betfair.ESASwagger.Model;
using System;
using System.Threading.Tasks;

namespace Betfair.ESAClient.Protocol
{
    /// <summary>
    /// Wraps a standard completion source to create a pairing of request message to status message
    /// </summary>
    public class RequestResponse(int id, RequestMessage request, Action<RequestResponse> onSuccess)
    {
        public readonly RequestMessage Request = request;
        private readonly TaskCompletionSource<StatusMessage> _completionSource = new();

        public int Id { get; private set; } = id;

        public Action<RequestResponse> OnSuccess { get; set; } = onSuccess;

        public StatusMessage Result
        {
            get
            {
                return _completionSource.Task.Result;
            }
        }

        public Task<StatusMessage> Task
        {
            get
            {
                return _completionSource.Task;
            }
        }

        public void ProcesStatusMessage(StatusMessage statusMessage)
        {
            if (statusMessage.StatusCode == StatusMessage.StatusCodeEnum.Success)
            {
                OnSuccess?.Invoke(this);
            }
            _completionSource.TrySetResult(statusMessage);
        }

        internal void Cancelled()
        {
            _completionSource.TrySetCanceled();
        }
    }
}