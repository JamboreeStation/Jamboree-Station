// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite;

// Jamboree's older schema lacks the icon expected by upstream's patron
// target models. Add it before their SQLite table rebuilds copy this column.
[DbContext(typeof(SqliteServerDbContext))]
[Migration("20260714000000_JamboreePatronTierIcon")]
public sealed class JamboreePatronTierIcon : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // PATREON TABLES ARE REMOVED BY RemoveRMCPatreon.
        // Do not add a column that already exists.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        //no schema changes to reverse.
    }
}
