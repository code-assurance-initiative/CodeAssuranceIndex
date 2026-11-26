# ES3: PII in the event store

**Lens:** eventSourcing · **Scored:** yes

## Property
Whether the append-only event log avoids un-erasable personal data (or has a crypto-shredding strategy) — a GDPR right-to-erasure risk.

## Applies to
Any language. It applies to code that keeps state as an append-only log of events. The code may write to the event store itself or use a framework to do it (for example Axon in Java or the `eventsourcing` library in Python).

## Population
Production code as [production-code.md](../production-code.md) defines it. The population is the repository's **stored event types**. A stored event type is a type whose instances the code appends to the event store, or that an aggregate folds (replays) from its stream.

These are not stored event types:

- integration messages that no aggregate folds and that only pass through an outbox, which deletes them once they are sent;
- messages that are only published, to a message broker or to handlers in the same process, and never appended to the store.

## Unit and denominator
**Unit:** a stored event type that carries **un-shredded personal data**.

An event type carries personal data when a data member it stores holds personal data. This includes a member of a value type the event holds, one level down (for example `ContactInfo { Email }`). Personal data comes in two tiers:

- **Strong:** personal data in any domain. Examples are an e-mail address, a phone number, a date of birth, a government or national identity number (for example a social security or passport number), and a financial account (for example a card number or IBAN). A member whose type or declaration marks it as personal data is also strong.
- **Weak:** a person's name, a part of a postal address, a postcode, or an IP address. A weak member counts only when it is corroborated:
  - the same event carries strong personal data; or
  - the event type is itself a record of a person's account. For example, it stores a password hash, it extends a user-identity type, or one of its members is declared as personal data; or
  - another event folded by the same aggregate carries strong personal data, so both events are about one identified person. For example, `CustomerRenamed { FirstName }` is corroborated by `CustomerRegistered { Email }` on the same stream.

  A name on a stream that never carries strong personal data (for example a game character's name) does not count.

Personal data is **shredded** when crypto-shredding protects it. For that, all three of these must be true:

1. it is stored encrypted with a real cipher;
2. the key is per subject (one key per person);
3. the code has a path that destroys that key when the person asks for erasure.

An event type counts as shredded when crypto-shredding covers the event, or covers every personal-data member it carries. Encryption under one key for the whole application is not shredding. Neither is a per-subject key that is never destroyed.

A shredded event type is reported as a note and is not counted.

**Denominator:** the number of stored event types.

## Thresholds
- One un-shredded personal-data member is enough for its event type to count. Each event type counts once, however many such members it has.
- A weak member counts only when it is corroborated, as defined above.
- The score curve has two thresholds. The isolation threshold is a density of 10 % (counted types ÷ stored types). Headroom stops growing at a population of 200 stored event types.

## Score
Let v be the number of counted event types and n the number of stored event types.

- If v = 0, the score is **10**.
- Otherwise:
  - d = min(v / n, 1)
  - isolation = max(0, 1 − d / 0.10)
  - headroom = min(1, log10(n) / log10(200)), which is 0 at n = 1
  - ceiling = 6 + 2 × isolation × headroom
  - score = 1 + (1 − d) × (ceiling − 1), rounded to 2 decimals

If any event type is counted, the score is at most 8. The ceiling is 6 unless counted types are rare (below 10 %) among many stored event types. As the share of counted types grows, the score falls toward 1.

The score for one service in a repository uses the same formula. It counts the stored event types declared in that service's files and the counted types among them.

## Not measured when
- The production code has no stored event types.
- No stored event type carries personal data, shredded or not, and one of these is written in a human language that this page's definition of personal data does not cover: the stored event types' member names, or the comments in the files that declare those types. The covered languages are English, Danish, Norwegian Bokmål, Swedish, German, Dutch and Chinese. Pending re-definition says when a language counts as "written in". In this case the code does not show that personal data is absent, so the score is not 10. When an event type is counted, the count stands in any language.

## What the result is not
- It is not a check of the data actually in the store. It reads the event types the code declares, not the records in the store.
- It is not a GDPR compliance verdict. It does not judge lawful basis or retention. It does not look at personal data kept outside the event store (for example in read models, in logs, or in messages that are only sent).
- It does not check that the crypto-shredding is correct (key storage, key rotation, backups). It only checks that a per-subject key exists and that the code has a path that destroys it.
- A low score does not mean that data has leaked. It means that erasing a person's data would require rewriting an immutable log.

## Pending re-definition
These are parameters of the existing reading's model. Today's score depends on them. A later decision under decision 0013 will replace each one with a threshold on the property.

- **Depth into value types:** a member's own type is read one level down, and no deeper.
- **Score curve terms:** the base ceiling of 6, the extra 2 for isolated violations, the 10 % isolation density, the floor of 1, and the population of 200 at which headroom is full.
- **Human-language coverage:** a human language outside the covered list makes the dimension unmeasured when either of these is true:
  - it is the language of the comments in at least 5 % of the files that declare stored event types, or in at least 20 such files. Only files whose comments can be identified as one language count, and a file's comments must hold at least 200 letters to be identified;
  - a member name of a stored event type contains a letter of the Cyrillic, Arabic, Greek, Hebrew, Thai, Devanagari, Hangul or kana scripts.
