# LuisData
C# console app to generate LUIS data from different source files (names, books, movies)

## Requesting LUIS

A request for looking the intent of a todo with the title `call Satya Nadela` is made like this:

https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/adf3f199-d0b7-4d38-b382-3bf815d0614e?subscription-key=911c0c272db34888abf16ff4509bbb73&timezoneOffset=0&verbose=true&q=call+Satya+Nadela

LUIS responds with the following result:

````
{
    "query": "call Satya Nadela",
    "topScoringIntent": {
        "intent": "call",
        "score": 0.9873301
    },
    "intents": [
        {
            "intent": "call",
            "score": 0.9873301
        },
        {
            "intent": "watch",
            "score": 0.00000432621846
        },
        {
            "intent": "read",
            "score": 0.0000022737122
        },
        {
            "intent": "None",
            "score": 6.535713e-7
        },
        {
            "intent": "message",
            "score": 4.30574318e-7
        }
    ],
    "entities": [
        {
            "entity": "satya",
            "type": "contact",
            "startIndex": 5,
            "endIndex": 9,
            "score": 0.9933844
        }
    ]
}
```