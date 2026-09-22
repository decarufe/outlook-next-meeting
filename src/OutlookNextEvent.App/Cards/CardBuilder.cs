namespace OutlookNextEvent.App.Cards;

public sealed class CardBuilder
{
    public string BuildPlaceholderCardJson()
        => """
           {
             "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
             "type": "AdaptiveCard",
             "version": "1.5",
             "body": [
               {
                 "type": "TextBlock",
                 "text": "Outlook Next Event",
                 "weight": "Bolder",
                 "size": "Medium"
               },
               {
                 "type": "TextBlock",
                 "text": "${status}",
                 "wrap": true
               },
               {
                 "type": "TextBlock",
                 "text": "${detail}",
                 "wrap": true,
                 "isSubtle": true
               }
             ],
             "actions": [
               {
                 "type": "Action.Execute",
                 "title": "Actualiser",
                 "verb": "refresh"
               },
               {
                 "type": "Action.Execute",
                 "title": "Connecter Outlook",
                 "verb": "connect"
               }
             ]
           }
           """;
}
