using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Data.Base.Exceptions;

namespace Data.Service
{
    internal sealed class ApiService // : IGrpcClientBase
    {
        private readonly ILogger<ApiService> _logger;

        public ApiService(ILogger<ApiService>? logger = null)
        {
            //_client = client ?? throw new RpcException(new Status(StatusCode.Unknown, "Could not resolve IGrpcClient"));
            _logger = logger ?? NullLogger<ApiService>.Instance;
        }

        //[Authorize]
        //public async Task ConsumeJobs(IAsyncStreamReader<ConsumeRequest> requestStream, IServerStreamWriter<ConsumeReply> responseStream, ServerCallContext context)
        //{
        //    try
        //    {
        //        var currentTopic = String.Empty;
        //        await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken).ConfigureAwait(false))
        //        {
        //            var reply = new ConsumeReply();
        //            try
        //            {
        //                switch (request.RequestCase)
        //                {
        //                    case ConsumeRequest.RequestOneofCase.ReserveRequest:
        //                        reply.ReserveReply = await client.ReserveJobAsync(request.ReserveRequest).ConfigureAwait(false);
        //                        currentTopic = request.ReserveRequest.Topic;
        //                        break;
        //                    case ConsumeRequest.RequestOneofCase.UpdateRequest:
        //                        reply.UpdateReply = await client.UpdateJobAsync(request.UpdateRequest, currentTopic).ConfigureAwait(false);
        //                        break;
        //                    default:
        //                        reply.Status = StatusCode.Unimplemented.ToGoogleRpcStatus($"Not implemented Consume Command: '{request.RequestCase}'.");
        //                        _logger.LogWarning("Unexpected request in ConsumeJobs {RequestCase}.", request.RequestCase);
        //                        break;
        //                }
        //            }
        //            catch (RpcException exception)
        //            {
        //                reply.Status = exception.Status.ToGoogleRpcStatus();
        //                _logger.LogError(exception, "Exception in ConsumeJobs {RequestCase}.", request.RequestCase);
        //            }
        //            await responseStream.WriteAsync(reply).ConfigureAwait(false);
        //        }
        //    }
        //    catch (RpcException e)
        //    {
        //        _logger.LogWarning(e, "Exception in ConsumeJobs");
        //        throw;
        //    }
        //    catch (OperationCanceledException e)
        //    {
        //        _logger.LogWarning(e, "Cancellation exception in ConsumeJobs");
        //        throw new RpcException(new Status(StatusCode.Cancelled, e.Message, e));
        //    }
        //    catch (NotImplementedException e)
        //    {
        //        _logger.LogWarning(e, "Not implemented exception");
        //        throw new RpcException(new Status(StatusCode.Unimplemented, e.Message, e));
        //    }
        //    catch (InternalException e)
        //    {
        //        _logger.LogError(e, "Internal infrastructure issue");
        //        throw new RpcException(new Status(StatusCode.Internal, e.Message, e));
        //    }
        //    catch (Exception e)
        //    {
        //        _logger.LogWarning(e, "Generic exception in ConsumeJobs");
        //        throw new RpcException(new Status(StatusCode.Unknown, e.Message, e));
        //    }
        //}
    }
}