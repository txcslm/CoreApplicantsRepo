using ChatSystem.Application.Commands;
using ChatSystem.Application.Interfaces;
using ChatSystem.Application.Notifications;
using ChatSystem.Domain.Entities;

namespace ChatSystem.Application.Handlers;

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, bool>
{
  private readonly IMediator _mediator;

  public SendMessageCommandHandler(IMediator mediator) =>
    _mediator = mediator;

  public async Task<bool> Handle(SendMessageCommand command)
  {
    try
    {
      var message = new ChatMessage
      {
        Content = command.Content,
        Sender = command.Sender,
        Type = command.Type,
        TeamId = command.TeamId
      };

      await _mediator.Publish(new ChatNotification { Message = message });

      return true;
    }
    catch
    {
      return false;
    }
  }
}