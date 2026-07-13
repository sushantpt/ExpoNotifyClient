using System.Collections.Generic;
using System.Linq;

namespace ExpoNotifyClient.Common
{
    /// <summary>
    /// Represents the result of a validation operation.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the validation passed.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the error messages if validation failed.
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Gets the combined error messages as a single string.
        /// </summary>
        public string ErrorMessage => string.Join("; ", Errors);

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        public static ValidationResult Success() => new ValidationResult { IsValid = true };

        /// <summary>
        /// Creates a failed validation result with error messages.
        /// </summary>
        public static ValidationResult Failure(params string[] errors) => new ValidationResult
        {
            IsValid = false,
            Errors = errors?.ToList() ?? new List<string>()
        };

        /// <summary>
        /// Creates a failed validation result with error messages.
        /// </summary>
        public static ValidationResult Failure(IEnumerable<string> errors) => new ValidationResult
        {
            IsValid = false,
            Errors = errors?.ToList() ?? new List<string>()
        };
    }
}