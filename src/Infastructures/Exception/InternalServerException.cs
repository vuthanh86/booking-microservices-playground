using System.Globalization;
using System.Net;

namespace BuildingBlocks.Exception;

public class InternalServerException : CustomException
{
    public InternalServerException()
    { }

    public InternalServerException(string message, int? code) : base(message, HttpStatusCode.InternalServerError, code)
    { }

    public InternalServerException(string message, int? code = null, params object[] args)
        : base(string.Format(CultureInfo.CurrentCulture, message, args, HttpStatusCode.InternalServerError, code))
    { }
}
