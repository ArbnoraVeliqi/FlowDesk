using FlowDesk.Api.Models;

namespace FlowDesk.Api.DTOs;

public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, object User);

public record CustomerRequest(string Name, string Email, string? Phone, string? Company);

public record TicketRequest(
    string Title,
    string Description,
    TicketPriority Priority,
    int CustomerId,
    int? CategoryId,
    int? AssignedUserId);

public record CommentRequest(string Body, bool IsInternal = false);
public record StatusRequest(TicketStatus Status);
public record AssignRequest(int? UserId);
