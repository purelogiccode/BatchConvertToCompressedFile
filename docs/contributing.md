# Contributing

See the repository-level
[CONTRIBUTING.md](https://github.com/purelogiccode/BatchConvertToCompressedFile/blob/master/CONTRIBUTING.md)
and [AGENTS.md](https://github.com/purelogiccode/BatchConvertToCompressedFile/blob/master/AGENTS.md)
before sending a pull request.

## Quick checklist

- [ ] `dotnet build CSharp_BatchConvertToCompressedFile.sln -c Release` succeeds
- [ ] `dotnet test CSharp_BatchConvertToCompressedFile.sln -c Release` succeeds
- [ ] New behavior has tests
- [ ] Async methods end with `Async`
- [ ] Models/enums live in `Models`, interfaces in `Interfaces`
- [ ] No live network calls in tests
- [ ] Expected user errors are logged below Warning
- [ ] User-facing changes are documented in `docs/`

## Documentation changes

`docs/` is published to GitHub Pages by `.github/workflows/docs.yml`. Keep links relative and
the page list in `index.md` up to date.
