using Grpc.Core;
using Grpc.Core.Interceptors;

namespace BuildingBlocks.Exception;

public class GrpcExceptionInterceptor : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (System.Exception exception)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, exception.Message));
        }
    }
}
