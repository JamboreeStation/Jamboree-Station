// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres;

// Older Jamboree databases may lack the icon column expected by upstream's model.
[DbContext(typeof(PostgresServerDbContext))]
[Migration("20260714000000_JamboreePatronTierIcon")]
public sealed class JamboreePatronTierIcon : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE rmc_patron_tiers ADD COLUMN IF NOT EXISTS icon text;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // The column may predate this migration, so rollback must preserve it and its data.
    }
}
