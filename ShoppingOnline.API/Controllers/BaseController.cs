using Microsoft.AspNetCore.Mvc;

namespace ShoppingOnline.API.Controllers
{
    /// <summary>
    /// Base controller with common validation and response methods
    /// </summary>
    public abstract class BaseController : ControllerBase
    {
        /// <summary>
        /// Returns a standardized success response
        /// </summary>
        protected ActionResult<T> SuccessResponse<T>(T data, string message = "Success")
        {
            return Ok(new
            {
                Success = true,
                Message = message,
                Data = data,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Returns a standardized error response
        /// </summary>
        protected ActionResult ErrorResponse(string message, object? errors = null, int statusCode = 400)
        {
            var response = new
            {
                Success = false,
                Message = message,
                Errors = errors,
                Timestamp = DateTime.UtcNow
            };

            return statusCode switch
            {
                400 => BadRequest(response),
                401 => Unauthorized(response),
                403 => StatusCode(403, response),
                404 => NotFound(response),
                409 => Conflict(response),
                500 => StatusCode(500, response),
                _ => BadRequest(response)
            };
        }

        /// <summary>
        /// Validates the ModelState and returns errors if invalid
        /// </summary>
        protected ActionResult? ValidateModelState()
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return ErrorResponse("Validation failed", errors);
            }
            return null;
        }

        /// <summary>
        /// Validates required parameters
        /// </summary>
        protected ActionResult? ValidateRequiredParameters(params (object? value, string name)[] parameters)
        {
            var missingParams = parameters
                .Where(p => p.value == null || (p.value is string str && string.IsNullOrWhiteSpace(str)))
                .Select(p => p.name)
                .ToList();

            if (missingParams.Any())
            {
                return ErrorResponse(
                    "Required parameters missing",
                    new { MissingParameters = missingParams }
                );
            }

            return null;
        }

        /// <summary>
        /// Validates ID parameter
        /// </summary>
        protected ActionResult? ValidateId(int id, string paramName = "id")
        {
            if (id <= 0)
            {
                return ErrorResponse($"Invalid {paramName}. Must be greater than 0.");
            }
            return null;
        }

        /// <summary>
        /// Validates email format
        /// </summary>
        protected bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates password strength
        /// </summary>
        protected (bool IsValid, string[] Errors) ValidatePassword(string password)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Password is required");
                return (false, errors.ToArray());
            }

            if (password.Length < 6)
                errors.Add("Password must be at least 6 characters long");

            if (password.Length > 100)
                errors.Add("Password must not exceed 100 characters");

            if (!password.Any(char.IsUpper))
                errors.Add("Password must contain at least one uppercase letter");

            if (!password.Any(char.IsLower))
                errors.Add("Password must contain at least one lowercase letter");

            if (!password.Any(char.IsDigit))
                errors.Add("Password must contain at least one number");

            return (errors.Count == 0, errors.ToArray());
        }

        /// <summary>
        /// Validates phone number format
        /// </summary>
        protected bool IsValidPhoneNumber(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return true; // Phone is optional

            // Simple Vietnamese phone number validation
            var phonePattern = @"^(\+84|84|0)(3|5|7|8|9)[0-9]{8}$";
            return System.Text.RegularExpressions.Regex.IsMatch(phone, phonePattern);
        }

        /// <summary>
        /// Sanitizes string input
        /// </summary>
        protected string SanitizeString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return input.Trim()
                       .Replace("<", "&lt;")
                       .Replace(">", "&gt;")
                       .Replace("\"", "&quot;")
                       .Replace("'", "&#x27;")
                       .Replace("/", "&#x2F;");
        }
    }
}
