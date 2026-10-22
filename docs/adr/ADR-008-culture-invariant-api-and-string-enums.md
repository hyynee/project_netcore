# ADR-008: Culture invariant request handling and string enums

## Status

Accepted for MVP.

## Context

`RangeAttribute` with decimal bounds parses its bound strings with
`CultureInfo.CurrentCulture`. On a server whose culture uses `,` as the decimal
separator the attribute throws `ArgumentException` while validating an
otherwise valid request, which surfaces as a `500` instead of a `400`. The
public API also serialized enums as numbers, which is hard to read for clients
and hides the domain vocabulary.

## Decision

- Every request runs with the invariant culture. `RequestLocalizationOptions`
  pins the default request culture to `CultureInfo.InvariantCulture` and
  `app.UseRequestLocalization()` is the first middleware of the pipeline. API
  responses are therefore identical on every host.
- Money validation uses `PositiveAmountAttribute` instead of a decimal
  `RangeAttribute`. The attribute compares the decimal value directly and never
  parses culture dependent strings. The service layer repeats the check so the
  rule holds even when a request bypasses model validation.
- Enums are serialized and accepted as strings through
  `JsonStringEnumConverter`, which matches how the values are stored in SQL
  Server.

## Consequences

- Validation errors are deterministic and a `500` caused by a locale can no
  longer reach a client.
- The API never localizes messages or number formats. Localized payloads would
  need a separate representation layer.
- Enum values become part of the public contract, so renaming an enum member is
  a breaking change.
