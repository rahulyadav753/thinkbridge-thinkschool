# Why the Rich Quote Model?

The rich `Quote` model moves important business rules from the API and callers into the domain itself. The anemic model allowed any caller to directly set `Author` or `Text`, so every caller had to remember the validation rules. That made it easy for one endpoint or future feature to accidentally create invalid quotes.

The rich model enforces the rules at the point of creation through `Quote.Create(author, text)`. Author length is limited to 1–200 characters, while quote text is limited to 1–1000 characters. The `Text` property also has a private setter, so it cannot be changed after creation. Soft deletion is represented explicitly through `IsDeleted` and `SoftDelete()`.

For example, a future endpoint could accidentally create a 5,000-character quote with the old anemic model because nothing prevented it. The rich model rejects that invalid quote during `Quote.Create`, before it reaches the database.

The rich model therefore makes invalid states harder to create and keeps business rules close to the data they protect.
