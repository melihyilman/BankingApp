using MediatR;

namespace AccountService.Commands;

public record CreateAccountCommand(string FirstName, string LastName, string Email) : IRequest<Unit>;