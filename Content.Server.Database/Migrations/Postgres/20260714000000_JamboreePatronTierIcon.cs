// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres;

// Jamboree's older schema lacks the icon expected by upstream's patron
// target models. Add it before their SQLite table rebuilds copy this column.
[DbContext(typeof(PostgresServerDbContext))]
[Migration("20260714000000_JamboreePatronTierIcon")]
public sealed class JamboreePatronTierIcon : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "icon",
            table: "rmc_patron_tiers",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "icon", table: "rmc_patron_tiers");
    }
}
