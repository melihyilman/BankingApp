using MediatR;

namespace CustomerService.Commands;
public record RegisterCustomerCommand(string FirstName, string LastName, string Email, string IdNumber) : IRequest<Unit>;