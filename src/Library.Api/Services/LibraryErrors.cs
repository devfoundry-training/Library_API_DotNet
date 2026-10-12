namespace Library.Api.Services;

/// <summary>
/// A broken rule, with the code and message from RULES.md. The service layer knows nothing about
/// HTTP: the API layer turns each kind into a status (400, 404 or 409) in one place.
/// </summary>
public abstract class LibraryError(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

/// <summary>A request value that breaks a validation rule (400).</summary>
public sealed class ValidationError(string message) : LibraryError("validation_error", message);

/// <summary>Something named by id does not exist (404).</summary>
public abstract class NotFoundError(string code, string message) : LibraryError(code, message);

public sealed class BookNotFound(int id) : NotFoundError("book_not_found", $"no book with id {id}");

public sealed class MemberNotFound(int id) : NotFoundError("member_not_found", $"no member with id {id}");

public sealed class LoanNotFound(int id) : NotFoundError("loan_not_found", $"no loan with id {id}");

/// <summary>The request is valid but a business rule refuses it (409).</summary>
public abstract class RuleBroken(string code, string message) : LibraryError(code, message);

public sealed class LoanLimitReached(string name, int open, int limit)
    : RuleBroken("loan_limit_reached", $"{name} already has {open} loans (limit {limit})");

public sealed class NoCopiesAvailable(string title)
    : RuleBroken("no_copies_available", $"{title} has no copies available");

public sealed class AlreadyReturned(int loanId)
    : RuleBroken("already_returned", $"loan {loanId} has already been returned");

public sealed class ReturnBeforeBorrowed(int loanId, DateOnly borrowed, DateOnly today)
    : RuleBroken("return_before_borrowed",
        $"loan {loanId} was borrowed on {borrowed:yyyy-MM-dd} and cannot be returned on {today:yyyy-MM-dd}");

public sealed class CopiesBelowOnLoan(int onLoan)
    : RuleBroken("copies_below_on_loan", $"copies cannot be less than the {onLoan} on loan");

public sealed class BookHasLoans(string title)
    : RuleBroken("book_has_loans", $"{title} has loans and cannot be deleted");

public sealed class EmailTaken(string email, int memberId)
    : RuleBroken("email_taken", $"email {email} is already used by member {memberId}");
