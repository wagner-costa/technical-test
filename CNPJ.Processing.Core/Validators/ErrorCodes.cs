using System.Net;

namespace CNPJ.Processing.Core.Validators
{
    public static class ErrorCodes
    {
        public static readonly int NOT_FOUND = HttpStatusCode.NotFound.GetHashCode();
        public static readonly int NO_CONTENT = HttpStatusCode.NoContent.GetHashCode();
        public static readonly int FAILED_DEPENDENCY = HttpStatusCode.FailedDependency.GetHashCode();
        public static readonly int AMBIGUOUS = HttpStatusCode.Ambiguous.GetHashCode();
        public static readonly int NOT_ALLOWED = HttpStatusCode.MethodNotAllowed.GetHashCode();
        public static readonly int FORBIDEN = HttpStatusCode.Forbidden.GetHashCode();
        public static readonly int INTERNAL_SERVER_ERROR = HttpStatusCode.InternalServerError.GetHashCode();
        public static readonly int BAD_REQUEST = HttpStatusCode.BadRequest.GetHashCode();
    }
}
