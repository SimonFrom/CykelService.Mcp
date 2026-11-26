# CykelService.Mcp

En [MCP-server](https://modelcontextprotocol.io) til et cykelværksted. Den giver en AI-assistent som Claude læseadgang til værkstedets database, så man kan spørge på almindeligt dansk:

- "Hvilke cykler venter på værkstedet?"
- "Hvem har ikke betalt endnu?"
- "Hvad var omsætningen i oktober?"
- "Find Jens og hans cykler."

Serveren er skrevet i C# (.NET 10) og læser fra en PostgreSQL-database. Den kan kun læse og aldrig ændre data.

## Værktøjer

| Værktøj | Argumenter | Hvad det gør |
|---|---|---|
| `open_repairs` | ingen | Reparationer, der ikke er afsluttede, ældste først. Viser om der mangler dele, og om kunden er kontaktet. |
| `unpaid_repairs` | ingen | Afsluttede reparationer, der ikke er betalt, med indleverings- og udleveringsdato. |
| `revenue` | `from`, `to` (fx `2026-10-01`) | Omsætning for afsluttede reparationer i perioden, opdelt på måned. |
| `find_customer` | `query` | Kunder, hvis navn eller telefonnummer matcher, med deres cykler. Højst 10 resultater. |

## Prøv den lokalt

Den hurtigste måde at prøve serveren på er med en testdatabase i Docker. Testdatabasen indeholder to fiktive kunder, og du behøver ikke andet end det, der står herunder.

### Det skal du have

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://docs.docker.com/get-docker/)
- [Node.js](https://nodejs.org) (til MCP Inspector via `npx`)

### 1. Hent koden

```bash
git clone https://github.com/SimonFrom/CykelService.Mcp.git
cd CykelService.Mcp
```

Alle kommandoer herefter køres fra roden af repoet.

### 2. Start testdatabasen

```bash
docker run -d --rm --name cykel-pg -e POSTGRES_PASSWORD=test -p 55432:5432 postgres:17
sleep 5
docker exec -i cykel-pg psql -U postgres < test/seed.sql
```

`test/seed.sql` opretter tabellerne, lægger testdata ind og opretter rollen `mcp_readonly` med adgangskoden `laese-kode`. Rollen kan kun læse.

Databasen kører på port `55432`, så den ikke kolliderer med en Postgres, du måske allerede har kørende. Containeren er startet med `--rm`, så den bliver slettet, når den stopper.

### 3. Start serveren i MCP Inspector

[MCP Inspector](https://github.com/modelcontextprotocol/inspector) er en lille webside, hvor du kan kalde værktøjerne direkte uden en AI-assistent.

```bash
dotnet build
npx @modelcontextprotocol/inspector \
  -e CYKELSERVICE_DB="Host=localhost;Port=55432;Database=postgres;Username=mcp_readonly;Password=laese-kode" \
  dotnet run --project .
```

Inspector åbner i browseren. Tryk **Connect** → **Tools** → **List Tools**, og kald et værktøj.

Connection string skal gives med `-e`. Inspector sender ikke miljøvariablerne fra din terminal videre til serveren.

### 4. Forventede resultater

| Værktøj | Argumenter | Forventet svar |
|---|---|---|
| `open_repairs` | ingen | 1 reparation: Jens Hansen, Kildemoes City, 450 kr., mangler dele ("Venter på bremsekabel") |
| `unpaid_repairs` | ingen | 2 reparationer, ældste først: Jens Hansen (Kildemoes City, 300 kr.) og Mette Larsen (Trek Marlin 5, 1200 kr.) |
| `revenue` | `from` = `2026-10-01`, `to` = `2026-11-30` | Oktober 2026: 300 kr. · November 2026: 1200 kr. |
| `find_customer` | `query` = `Jens` | Jens Hansen, tlf. 20112233, med én cykel (Kildemoes City) |

### 5. Stop testdatabasen

```bash
docker stop cykel-pg
```

## Brug den med Claude

Inspector er kun til test. Hvis du vil stille spørgsmål til serveren på almindeligt dansk, skal den forbindes til en MCP-klient, fx Claude Code.

Byg først en færdig udgave, så serveren starter hurtigt:

```bash
dotnet publish -c Release -o ~/.local/share/cykelservice-mcp
```

Tilføj den derefter til Claude Code. Her peger den på testdatabasen:

```bash
claude mcp add cykelservice --scope user \
  -e CYKELSERVICE_DB="Host=localhost;Port=55432;Database=postgres;Username=mcp_readonly;Password=laese-kode" \
  -- ~/.local/share/cykelservice-mcp/CykelService.Mcp
```

Start Claude Code, skriv `/mcp` for at tjekke, at `cykelservice` er forbundet, og spørg "Hvilke cykler venter på værkstedet?".

Andre MCP-klienter, fx Claude Desktop, skal have de samme to ting: kommandoen til den publicerede fil og miljøvariablen `CYKELSERVICE_DB`.

Når du ændrer koden, skal du køre `dotnet publish` igen og genstarte klienten. Selve `claude mcp add` behøver du kun at køre én gang.

## Konfiguration

Serveren har kun én indstilling:

| Miljøvariabel | Indhold |
|---|---|
| `CYKELSERVICE_DB` | En [Npgsql connection string](https://www.npgsql.org/doc/connection-string-parameters.html) til databasen |

Hvis variablen mangler, stopper serveren med det samme og skriver `Missing connection string for CYKELSERVICE_DB`.

## Brug din egen database

Serveren forventer tabellerne `customers`, `bikes`, `repairs` og `repair_items` med de kolonner, der står i `test/seed.sql`. Bemærk især:

- `repairs."customerContacted"` er camelCase og skal stå i anførselstegn i SQL.
- `repairs.payment_recieved` er stavet sådan med vilje, fordi det er navnet i den oprindelige database.

Opret en rolle, der kun kan læse, i stedet for at bruge en administratorbruger:

```sql
create role mcp_readonly with login password '<lang tilfældig kode>';
grant usage on schema public to mcp_readonly;
grant select on customers, bikes, repairs, repair_items to mcp_readonly;
```

Hvis Row Level Security er slået til på tabellerne, som det ofte er i Supabase, skal rollen også have en `select`-policy på hver tabel. Ellers får den nul rækker uden nogen fejl:

```sql
create policy mcp_readonly_select on repairs for select to mcp_readonly using (true);
-- og tilsvarende for customers, bikes og repair_items
```

Læg aldrig en rigtig connection string i git. Giv den kun som miljøvariabel.

## Projektstruktur

```
Program.cs     Opsætning: læser CYKELSERVICE_DB, registrerer databasen og MCP-serveren
Models/        Records, som værktøjerne returnerer
Data/          SQL-forespørgsler med Dapper
Tools/         MCP-værktøjerne og deres beskrivelser
test/seed.sql  Testdatabase med tabeller, testdata og læserolle
```

Serveren taler MCP over stdio. Stdout er forbeholdt protokollen, så al logning går til stderr.

## Fejlfinding

| Fejl | Årsag og løsning |
|---|---|
| `The container name "/cykel-pg" is already in use` | Testdatabasen kører allerede. Brug den, eller start forfra med `docker rm -f cykel-pg`. |
| `Path 'test' does not exist` | Du står ikke i roden af repoet. |
| `psql: could not connect` lige efter `docker run` | Postgres var ikke klar endnu. Vent et par sekunder, og kør `docker exec` igen. |
| `Missing connection string for CYKELSERVICE_DB` | Miljøvariablen nåede ikke frem til serveren. Giv den med `-e` til Inspector eller `claude mcp add`. |
| `Format of the initialization string does not conform to specification` | Connection string er ugyldig, fx en pladsholder, der ikke er udskiftet. |
| `password authentication failed` | Forkert brugernavn eller adgangskode. På Supabase' pooler er brugernavnet `mcp_readonly.<project-ref>`. |
| `column ... does not exist` | Din database har andre kolonnenavne end `test/seed.sql`. Se [Brug din egen database](#brug-din-egen-database). |
| Et værktøj giver nul rækker uden fejl | RLS er slået til, og rollen mangler en `select`-policy. |
| Klienten viser bare "An error occurred invoking …" | Den rigtige fejl står i serverens stderr. I Claude Code finder du den i MCP-loggen, som `/mcp` viser stien til. |
