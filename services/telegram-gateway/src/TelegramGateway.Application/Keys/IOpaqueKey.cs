namespace TelegramGateway.Application.Keys;

internal interface IOpaqueKey
{
    string Text(string kind, string scope, long id);
    long Id(string kind, string scope, string text);
}
