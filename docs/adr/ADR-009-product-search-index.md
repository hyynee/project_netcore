# ADR-009: Product search index

## Status

Accepted for MVP.

## Context

Phase 03 has to answer `GET /api/v1/products?search=...` for the storefront. The
first implementation filtered with `LIKE '%term%'` on `Products.Name`, which
forces SQL Server to scan every product name, cannot use any index, and cannot
express "the shopper typed the beginning of a word".

The obvious candidate is a SQL Server full-text index queried with `CONTAINS`.
It was rejected after verification on the target platform:

- The development and CI database is `mcr.microsoft.com/mssql/server:2022-latest`
  (Linux). That image does not ship the Full-Text Search feature:
  `SERVERPROPERTY('IsFullTextInstalled') = 0` and `CREATE FULLTEXT INDEX`
  fails with `Msg 102, Level 15: Incorrect syntax near '<index name>'`.
- A migration that creates the catalog and the index therefore breaks every
  developer machine and the CI pipeline, because the test fixtures apply the
  migrations with `Database.MigrateAsync()`.

## Decision

The search index is an inverted index kept in its own table:

- `ProductSearchTerms(ProductId, Term)` with a composite primary key
  `(ProductId, Term)` and an index on `(Term, ProductId)`.
- A term is a maximal run of Unicode letters or digits of the product name,
  lowercased, with single character terms dropped. The rules live in
  `ProductSearchTokenizer` and are shared by the write path and the read path,
  so both sides always agree.
- `ProductSearchIndexInterceptor` (an EF Core `SaveChangesInterceptor`) rewrites
  the terms of a product whenever the product is created or renamed.
- A search is translated into a set of term lookups. Every token but the last
  one has to match exactly; the last token matches as a prefix, so
  `headphon` still finds `headphones`. All tokens have to match, which makes
  the result set narrower than a plain substring search.
- A search that contains no indexable token (for example `%%%`) returns no
  product instead of every product.
- `POST /api/v1/admin/search/reindex` rebuilds the whole index, so products
  created before the index existed can be recovered without a data migration.

## Consequences

- Search is answered by an index seek on the term index instead of a scan of
  the product names. Measured on a 2 000 product table (SQL Server 2022, one
  matching row): the inverted index reads 2 pages of `ProductSearchTerms` and
  2 pages of `Products`, the `LIKE '%1999%'` variant reads 41 pages of
  `Products` and reports one scan.
- The index is only as good as the tokenizer. Diacritics are not folded, so
  `khoac` does not find `khoác`, exactly like the previous `LIKE` behaviour.
  Folding diacritics would need a second, normalized term column.
- Every product write pays a small extra cost: the terms of the product are
  rewritten inside the same transaction.
- The index has to be rebuilt for data that was written before the index
  existed. The admin reindex command exists for that case.

## Alternatives considered

- **Full-text index with `CONTAINS`**: rejected because the platform used in
  development and CI does not provide the feature, and a migration that
  depends on it fails there.
- **Keep `LIKE` on the product name**: rejected because it cannot use an index
  and does not support prefix search.
- **A trigram index**: only available through the same Full-Text Search
  feature, so it has the same problem as `CONTAINS`.
