using CommunityToolkit.Mvvm.Messaging.Messages;
using M3uEditor.Core.FindReplace;

namespace M3uEditor.App.Messages;

public sealed class NavigateToSpanMessage : ValueChangedMessage<M3uEditor.Core.TextSpan>
{
    public NavigateToSpanMessage(M3uEditor.Core.TextSpan value) : base(value)
    {
    }
}

public sealed class NavigateToFindMatchMessage : ValueChangedMessage<FindMatch>
{
    public NavigateToFindMatchMessage(FindMatch value) : base(value)
    {
    }
}
